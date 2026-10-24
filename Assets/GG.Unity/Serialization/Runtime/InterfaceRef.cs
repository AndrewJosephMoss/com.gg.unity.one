using System;
using UnityEngine;

namespace GG.Unity.Serialization
{
    [Serializable]
    public class InterfaceRef<T> where T : class
    {
        [SerializeField] private UnityEngine.Object _target;

        /// <summary>The referenced object as T, or null if unset, destroyed, or not a T.</summary>
        public T Ref => _target != null ? _target as T : null;

        public bool HasValue => Ref != null;

        public bool Set(UnityEngine.Object value)
        {
            if (value == null) { _target = null; return true; }

            if (value is T) { _target = value; return true; }

            Debug.LogWarning($"{value.name} must implement {typeof(T).Name}");
            _target = null;
            return false;
        }
    }
}