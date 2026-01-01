using System;
using UnityEngine;

namespace ItTakesTwo
{
    public class ClassTypeName: PropertyAttribute
    {
        public Type type;

        public ClassTypeName(Type type)
        {
            this.type = type;
        }
    }
}