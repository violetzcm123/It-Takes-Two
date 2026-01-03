using Mirror;
using UnityEngine;

namespace ItTakesTwo
{
    public class PlayerNetworkDriver : NetworkBehaviour
    {
        [SerializeField] private Player player;
        [SerializeField] private PlayerInputManager inputs;
        
        [Header("Server Validation")]
        public float reportInterval = 0.1f;
        public float maxSpeedMultiplier = 1.2f;
        public float teleportThreshold = 2.5f;

        private float m_nextReportTime;

        private Vector3 m_lastServerPos;
        private float m_lastServerTime;

        private void Awake()
        {
            if (!player) player = GetComponent<Player>();
            if (!inputs) inputs = GetComponent<PlayerInputManager>();
        }
        
        [ClientCallback]
        private void Update()
        {
            if (!isLocalPlayer) return;
            if (Time.time < m_nextReportTime) return;

            m_nextReportTime = Time.time + reportInterval;

            var pos = transform.position;
            var vel = player != null ? player.velocity : Vector3.zero;

            CmdReportState(pos, vel, Time.time);
        }

        [Command]
        private void CmdReportState(Vector3 pos, Vector3 vel, float time)
        {
            if (m_lastServerTime > 0f)
            {
                var dt = time - m_lastServerTime;
                if (dt > 0f)
                {
                    var speed = (pos - m_lastServerPos).magnitude / dt;
                    var maxSpeed = (player != null ? player.stats.current.topSpeed : 1f) * maxSpeedMultiplier;

                    var tooFast = speed > maxSpeed;
                    var teleported = (pos - m_lastServerPos).magnitude > teleportThreshold;

                    if (tooFast || teleported)
                    {
                        var correctPos = m_lastServerPos;
                        var correctVel = Vector3.zero;
                        TargetCorrectState(connectionToClient, correctPos, correctVel);
                        return;
                    }
                }
            }

            m_lastServerPos = pos;
            m_lastServerTime = time;
        }

        [TargetRpc]
        private void TargetCorrectState(NetworkConnectionToClient conn, Vector3 pos, Vector3 vel)
        {
            if (player != null)
            {
                player.ApplyCorrection(pos, vel);
            }
        }
        public override void OnStartClient()
        {
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
        
    }
}