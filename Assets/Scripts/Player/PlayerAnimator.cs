using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace ItTakesTwo
{
    [RequireComponent(typeof(Player))]
    public class PlayerAnimator: MonoBehaviour
    {
        public Animator animator;
        [Header("Parameters Names")]
        public string stateName = "State";
        
        [Header("Settings")]
        public float minLateralAnimationSpeed = 0.5f;
        
        protected int m_stateHash;
        //protected int m_onStateChangedHash;
        
        protected Player m_player;
        private int _lastStateHash;
        
        protected virtual void InitializePlayer()
        {
            m_player = GetComponent<Player>();
            
        }
        protected virtual void InitializeParametersHash()
        {
            m_stateHash = Animator.StringToHash(stateName);
        }
        protected virtual void InitializeAnimatorTriggers()
        {
            //m_player.states.events.onChange.AddListener(() => animator.SetTrigger(m_onStateChangedHash));
        }
        protected virtual void Start()
        {
            InitializePlayer();
            InitializeParametersHash();
            InitializeAnimatorTriggers();
        }
        protected virtual void LateUpdate()
        {
            HandleAnimatorParameters();
        }
        
        protected virtual void HandleAnimatorParameters()
        {
            var lateralSpeed = m_player.lateralVelocity.magnitude;
            var verticalSpeed = m_player.verticalVelocity.y;
            var lateralAnimationSpeed = Mathf.Max(minLateralAnimationSpeed, lateralSpeed / m_player.stats.current.topSpeed);

            animator.SetInteger(m_stateHash, m_player.states.index);
            
        }
    }
}