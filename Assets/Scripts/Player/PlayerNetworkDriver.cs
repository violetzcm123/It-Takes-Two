using Mirror;
using UnityEngine;

namespace ItTakesTwo
{
    public class PlayerNetworkDriver : NetworkBehaviour
    {
        [SerializeField] private Player player;
        [SerializeField] private PlayerInputManager inputs;

        private void Awake()
        {
            if (!player) player = GetComponent<Player>();
            if (!inputs) inputs = GetComponent<PlayerInputManager>();
        }

        public override void OnStartClient()
        {
            // 只禁用“非本地玩家”的 Player 脚本
            if (!isLocalPlayer && player != null)
                player.enabled = false;

            // 本地玩家确保启用（防止被别处禁掉）
            if (isLocalPlayer && player != null)
                player.enabled = true;

        }

        [ClientCallback]
        private void Update()
        {
            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"[NetDriver] isLocalPlayer={isLocalPlayer} inputs={(inputs!=null)} actionsEnabled={inputs?.actions?.enabled}");
            }
            if (!isLocalPlayer || inputs == null) return;

            var move = inputs.GetMovementDirection();
            var moveCam = inputs.GetMovementCameraDirection();
            //var look = inputs.GetLookDirection();
            

            //CmdSetInput(move, moveCam, look);
        }

        [Command]
        private void CmdSetInput(Vector3 move, Vector3 moveCam, Vector3 look)
        {
            if (inputs != null)
            {
                //inputs.SetNetworkInput(move, moveCam, look);
            }
        }
        public override void OnStartLocalPlayer()
        {
            var cam = FindObjectOfType<PlayerCamera>();
            if (cam != null)
            {
                //cam.BindPlayer(GetComponent<Player>());
            }
        }

    }
}