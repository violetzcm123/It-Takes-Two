using Mirror;
using UnityEngine;

namespace ItTakesTwo
{
    public class StateSyncAudit : NetworkBehaviour
    {
        [Header("Audit")]
        public float auditInterval = 0.5f;
        public float summaryInterval = 10f;

        [Header("Thresholds")]
        public float warnPosError = 0.2f;
        public float failPosError = 0.5f;
        public float warnRotError = 5f;
        public float failRotError = 15f;
        public float warnVelError = 0.5f;
        public float failVelError = 1.5f;

        public int failCountThreshold = 3;
        public bool logOnHost = false;

        private Player m_player;
        private float m_nextSendTime;
        private double m_nextSummaryTime;

        private int m_samples;
        private int m_warns;
        private int m_fails;
        private int m_consecutiveFails;

        private void Awake()
        {
            m_player = GetComponent<Player>();
        }

        [ServerCallback]
        private void Update()
        {
            if (Time.time < m_nextSendTime) return;
            m_nextSendTime = Time.time + auditInterval;

            var pos = transform.position;
            var rot = transform.rotation;
            var vel = m_player != null ? m_player.velocity : Vector3.zero;
            var stateIndex = m_player != null && m_player.states != null ? m_player.states.index : -1;

            RpcAuditSnapshot(pos, rot, vel, stateIndex, NetworkTime.time);
        }

        [ClientRpc(channel = Channels.Unreliable)]
        private void RpcAuditSnapshot(Vector3 pos, Quaternion rot, Vector3 vel, int stateIndex, double serverTime)
        {
            if (isServer && !logOnHost) return;

            var posErr = Vector3.Distance(transform.position, pos);
            var rotErr = Quaternion.Angle(transform.rotation, rot);
            var velErr = m_player != null ? Vector3.Distance(m_player.velocity, vel) : 0f;

            var status = "PASS";
            if (posErr >= failPosError || rotErr >= failRotError || velErr >= failVelError)
            {
                status = "FAIL";
                m_consecutiveFails++;
            }
            else if (posErr >= warnPosError || rotErr >= warnRotError || velErr >= warnVelError)
            {
                status = "WARN";
                m_consecutiveFails = 0;
            }
            else
            {
                m_consecutiveFails = 0;
            }

            m_samples++;
            if (status == "WARN") m_warns++;
            if (status == "FAIL") m_fails++;

            Debug.Log(
                $"AUDIT|t={NetworkTime.time:0.000}|netId={netId}|state={stateIndex}|posErr={posErr:0.000}|rotErr={rotErr:0.0}|velErr={velErr:0.000}|serverT={serverTime:0.000}|status={status}");

            if (NetworkTime.time >= m_nextSummaryTime)
            {
                m_nextSummaryTime = NetworkTime.time + summaryInterval;

                var overall = m_fails >= failCountThreshold ? "FAIL" : (m_warns > 0 ? "WARN" : "PASS");
                Debug.Log(
                    $"AUDIT_SUMMARY|t={NetworkTime.time:0.000}|netId={netId}|samples={m_samples}|warns={m_warns}|fails={m_fails}|overall={overall}");

                m_samples = 0;
                m_warns = 0;
                m_fails = 0;
            }
        }
    }
}
