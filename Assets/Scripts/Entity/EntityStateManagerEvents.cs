using System;
using UnityEngine.Events;

namespace ItTakesTwo
{
    [Serializable]
    public class EntityStateManagerEvents
    {
        public UnityEvent onChange;

        /// <summary>
        /// Called when entering a state.
        /// </summary>
        public UnityEvent<Type> onEnter;

        /// <summary>
        /// Called when exiting a state.
        /// </summary>
        public UnityEvent<Type> onExit;
    }
}