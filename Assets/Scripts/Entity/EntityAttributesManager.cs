using UnityEngine;

namespace ItTakesTwo
{
    public class EntityAttributesManager<T>: MonoBehaviour where T : EntityAttributes<T>
    {
        public T[] Attributes;

        /// <summary>
        /// The instance of the current activated Stats.
        /// </summary>
        public T current { get; protected set; }

        /// <summary>
        /// Changes from the current stats to the desired one.
        /// </summary>
        /// <param name="to">The desired index of the Stats you want.</param>
        public virtual void Change(int to)
        {
            if (to >= 0 && to < Attributes.Length)
            {
                if (current != Attributes[to])
                {
                    current = Attributes[to];
                }
            }
        }

        protected virtual void Start()
        {
            if (Attributes.Length > 0)
            {
                current = Attributes[0];
            }
        }
    }
}