using Codice.Client.Common.GameUI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Metadata;
using UnityEditor;
using UnityEngine;

namespace GG.Unity.Serialization.Editor
{
    [CustomPropertyDrawer(typeof(InterfaceRef<>), true)]
    public class InterfaceRefDrawer : PropertyDrawer
    {
        private const string SerializedFieldName = "_target";

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var target = property.FindPropertyRelative(SerializedFieldName);
            var iface = GetInterfaceType(fieldInfo);

            EditorGUI.BeginProperty(position, label, property);
            EditorGUI.BeginChangeCheck();

            var picked = EditorGUI.ObjectField(
                position,
                new GUIContent($"{label.text} ({iface.Name})", label.tooltip),
                target.objectReferenceValue,
                typeof(UnityEngine.Object),
                true);

            if (EditorGUI.EndChangeCheck())
                target.objectReferenceValue = Resolve(picked, iface);

            EditorGUI.EndProperty();
        }

        static UnityEngine.Object Resolve(UnityEngine.Object picked, Type iface)
        {
            if (picked == null) return null;
            if (iface.IsInstanceOfType(picked)) return picked;

            // Dragged a GameObject or another component: find one that implements the interface
            if (picked is GameObject go) return go.GetComponent(iface) as UnityEngine.Object;
            if (picked is Component c) return c.GetComponent(iface) as UnityEngine.Object;

            Debug.LogWarning($"{picked.name} does not implement {iface.Name}");
            return null;
        }

        static Type GetInterfaceType(FieldInfo fi)
        {
            var t = fi.FieldType;
            if (t.IsArray) t = t.GetElementType();
            else if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(List<>))
                t = t.GetGenericArguments()[0];
            return t.GetGenericArguments()[0]; // InterfaceRef<T> -> T
        }
    }
}
