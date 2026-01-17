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

        private float m_nextInputSendTime;
        private float m_nextStateSendTime;

        private Vector3 m_targetPos;
        private Quaternion m_targetRot;
        private Vector3 m_targetVel;

        private void Awake()
        {
            if (!player) player = GetComponent<Player>();
            if (!inputs) inputs = GetComponent<PlayerInputManager>();
        }
        
        public override void OnStartServer()
        {
            if (player != null) player.simulationEnabled = true;
            if (inputs != null) inputs.ClearNetworkMovement();
        }

        public override void OnStartClient()
        {
            if (!isServer && player != null) player.simulationEnabled = false;

            if (!isLocalPlayer && inputs != null)
                inputs.SetInputsEnabled(false);

            if (isLocalPlayer && inputs != null)
                inputs.SetInputsEnabled(true);
        }

        public override void OnStartLocalPlayer()
        {
            if (inputs != null)
                inputs.SetInputsEnabled(true);

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

            CmdSendInput(moveDir);
        }

        [Command(channel = Channels.Unreliable)]
        private void CmdSendInput(Vector3 moveDirWorld)
        {
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

            RpcReceiveState(pos, rot, vel, grounded);
        }

        [ClientRpc(channel = Channels.Unreliable)]
        private void RpcReceiveState(Vector3 pos, Quaternion rot, Vector3 vel, bool grounded)
        {
            if (isServer) return;

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
        
    }
}