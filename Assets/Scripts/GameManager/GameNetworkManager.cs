using Mirror;

namespace GameManager
{
    public class GameNetworkManager: NetworkRoomManager
    {
        public bool IsRoleTaken(int roleId, NetworkConnectionToClient except = null)
        {
            foreach (var slot in roomSlots)
            {
                if (slot == null) continue;
                var rp = slot.GetComponent<MyRoomPlayer>();
                if (rp != null && rp.roleId == roleId && slot.connectionToClient != except)
                    return true;
            }
            return false;
        }
    }
    public class MyRoomPlayer : NetworkRoomPlayer
    {
        [SyncVar(hook = nameof(OnRoleChanged))] public int roleId = -1;

        [Command]
        public void CmdSelectRole(int newRoleId)
        {
            var mgr = NetworkManager.singleton as GameNetworkManager;
            if (mgr == null) return;
            if (mgr.IsRoleTaken(newRoleId, connectionToClient)) return;

            roleId = newRoleId;
        }

        void OnRoleChanged(int oldRole, int newRole)
        {
            // TODO: 更新房间 UI
        }
    }
}