using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace ItTakesTwo
{
    public class PlayerNetworkDriver : NetworkBehaviour
    {
        [SerializeField] private Player player;
        [SerializeField] private PlayerInputManager inputs;
        
        [Header("Input / State Sync")]
        public float inputSendRate = 1f / 30f;
        public float stateSendRate = 1f / 20f;

        [Header("Client Smoothing")]
        public float interpolationSpeed = 12f;
        public float snapDistance = 3f;

        [Header("Client Prediction")]
        public int predictionBufferSize = 64;

        private float m_nextInputSendTime;
        private float m_nextStateSendTime;

        private Vector3 m_targetPos;
        private Quaternion m_targetRot;
        private Vector3 m_targetVel;

        private int m_localInputTick;
        private int m_lastProcessedInputTick;

        private readonly List<PredictedState> m_predictedStates = new List<PredictedState>(64);

        private struct PredictedState
        {
            public int tick;
            public Vector3 pos;
            public Vector3 vel;
        }

        private void Awake()
        {
            if (!player) player = GetComponent<Player>();
            if (!inputs) inputs = GetComponent<PlayerInputManager>();
        }
        
        public override void OnStartServer()
        {
            if (player != null) player.simulationEnabled = true;
            if (inputs != null) inputs.ClearNetworkMovement();
            m_lastProcessedInputTick = 0;
        }

        public override void OnStartClient()
        {
            if (player != null)
            {
                // Local player predicts, server remains authoritative; remote clients are interpolated.
                player.simulationEnabled = isServer || isLocalPlayer;
            }

            if (!isLocalPlayer && inputs != null)
                inputs.SetInputsEnabled(false);

            if (isLocalPlayer && inputs != null)
                inputs.SetInputsEnabled(true);
        }

        public override void OnStartLocalPlayer()
        {
            if (inputs != null)
                inputs.SetInputsEnabled(true);

            m_localInputTick = 0;
            m_predictedStates.Clear();

            var cam = FindObjectOfType<PlayerCamera>();
            if (cam != null)
            {
                cam.BindPlayer(player);
            }
        }

        private void FixedUpdate()
        {
            if (isLocalPlayer)
            {
                ClientSendInput();
            }

            if (isServer)
            {
                ServerSendState();
            }
        }

        [ClientCallback]
        private void ClientSendInput()
        {
            if (Time.time < m_nextInputSendTime) return;
            m_nextInputSendTime = Time.time + inputSendRate;

            var moveDir = inputs != null ? inputs.GetMovementCameraDirection() : Vector3.zero;
            if (moveDir.sqrMagnitude > 1f) moveDir = moveDir.normalized;

            m_localInputTick++;
            RecordPredictedState(m_localInputTick);
            CmdSendInput(m_localInputTick, moveDir);
        }

        [Command(channel = Channels.Unreliable)]
        private void CmdSendInput(int tick, Vector3 moveDirWorld)
        {
            if (tick <= m_lastProcessedInputTick) return;
            m_lastProcessedInputTick = tick;
            if (inputs == null) return;

            // Host 模式保持本地输入给服务器模拟，避免被网络覆盖
            if (!isLocalPlayer)
            {
                inputs.SetNetworkMovement(moveDirWorld);
            }
        }

        [ServerCallback]
        private void ServerSendState()
        {
            if (Time.time < m_nextStateSendTime) return;
            m_nextStateSendTime = Time.time + stateSendRate;

            var pos = transform.position;
            var rot = transform.rotation;
            var vel = player != null ? player.velocity : Vector3.zero;
            var grounded = player != null && player.isGrounded;

            RpcReceiveState(pos, rot, vel, grounded, m_lastProcessedInputTick);
        }

        [ClientRpc(channel = Channels.Unreliable)]
        private void RpcReceiveState(Vector3 pos, Quaternion rot, Vector3 vel, bool grounded, int lastProcessedTick)
        {
            if (isServer) return;

            if (isLocalPlayer)
            {
                if (player != null)
                {
                    if (TryGetPredictedState(lastProcessedTick, out var predicted))
                    {
                        var errorPos = pos - predicted.pos;
                        var errorVel = vel - predicted.vel;
                        var targetPos = transform.position + errorPos;
                        var targetVel = player.velocity + errorVel;

                        player.ApplyCorrection(targetPos, targetVel, snapDistance, interpolationSpeed);
                        TrimPredictedStates(lastProcessedTick);
                    }
                    else
                    {
                        player.ApplyCorrection(pos, vel, snapDistance, interpolationSpeed);
                    }
                    player.SetGrounded(grounded);
                }

                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    rot,
                    Time.deltaTime * interpolationSpeed);
                return;
            }

            m_targetPos = pos;
            m_targetRot = rot;
            m_targetVel = vel;

            if (player != null)
            {
                player.SetGrounded(grounded);
            }
        }

        private void Update()
        {
            if (isServer) return;
            if (player == null) return;
            if (isLocalPlayer) return;

            if (Vector3.Distance(transform.position, m_targetPos) > snapDistance)
            {
                transform.position = m_targetPos;
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, m_targetPos, Time.deltaTime * interpolationSpeed);
            }

            transform.rotation = Quaternion.Slerp(transform.rotation, m_targetRot, Time.deltaTime * interpolationSpeed);
            player.velocity = Vector3.Lerp(player.velocity, m_targetVel, Time.deltaTime * interpolationSpeed);
        }

        private void RecordPredictedState(int tick)
        {
            if (player == null) return;
            if (predictionBufferSize <= 0) return;

            if (m_predictedStates.Count >= predictionBufferSize)
            {
                m_predictedStates.RemoveAt(0);
            }

            m_predictedStates.Add(new PredictedState
            {
                tick = tick,
                pos = transform.position,
                vel = player.velocity
            });
        }

        private bool TryGetPredictedState(int tick, out PredictedState state)
        {
            for (var i = m_predictedStates.Count - 1; i >= 0; i--)
            {
                if (m_predictedStates[i].tick == tick)
                {
                    state = m_predictedStates[i];
                    return true;
                }
            }

            state = default;
            return false;
        }

        private void TrimPredictedStates(int lastProcessedTick)
        {
            for (var i = m_predictedStates.Count - 1; i >= 0; i--)
            {
                if (m_predictedStates[i].tick <= lastProcessedTick)
                {
                    m_predictedStates.RemoveAt(i);
                }
            }
        }
        
    }
}
