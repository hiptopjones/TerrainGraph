using UnityEditor;
using UnityEngine;

namespace Indiecat.TerrainGraph.Editor
{
    [CustomPropertyDrawer(typeof(WrappedParameter<float>))]
    public class FloatParameterDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Find the inner float field
            SerializedProperty valueProp = property.FindPropertyRelative("Value");

            // Draw it exactly like a normal float field
            EditorGUI.BeginProperty(position, label, property);
            valueProp.floatValue = EditorGUI.FloatField(position, label, valueProp.floatValue);
            EditorGUI.EndProperty();
        }
    }
}
