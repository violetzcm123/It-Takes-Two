using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace ItTakesTwo
{
    [RequireComponent(typeof(Player))]
    public class PlayerAnimator : NetworkBehaviour
    {
        public Animator animator;
        [Header("Parameters Names")]
        public string stateName = "State";

        [Header("Settings")]
        public float minLateralAnimationSpeed = 0.5f;

        protected int m_stateHash;
        protected Player m_player;

        [SyncVar(hook = nameof(OnStateIndexChanged))]
        private int m_stateIndex = -1;

        protected virtual void Start()
        {
            m_player = GetComponent<Player>();
            m_stateHash = Animator.StringToHash(stateName);
        }

        protected virtual void LateUpdate()
        {
            if (!isServer)
                return;

            if (m_player == null || m_player.states == null)
                return;

            var currentIndex = m_player.states.index;
            if (currentIndex != m_stateIndex)
            {
                m_stateIndex = currentIndex;
            }
        }

        private void OnStateIndexChanged(int oldIndex, int newIndex)
        {
            if (animator == null)
                return;

            animator.SetInteger(m_stateHash, newIndex);
        }
    }
}