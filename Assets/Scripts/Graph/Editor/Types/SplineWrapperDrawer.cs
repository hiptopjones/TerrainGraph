using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

[CustomPropertyDrawer(typeof(SplineWrapper))]
public class SplineWrapperDrawer : PropertyDrawer
{
    public override VisualElement CreatePropertyGUI(SerializedProperty property)
    {
        List<SplineContainer> splineContainers = Object.FindObjectsByType<SplineContainer>(FindObjectsSortMode.None).ToList();
        List<string> splineNames = splineContainers.Select(s => s.gameObject.name).ToList();

        var popup = new PopupField<string>("Select Spline", splineNames, 0);
        popup.RegisterValueChangedCallback(e => {
            int index = splineNames.IndexOf(e.newValue);
            var selectedSplineContainer = splineContainers[index];

            var selectedSpline = SplineHelpers.GetWorldSpaceSpline(selectedSplineContainer);

            // Get the actual object instance we're drawing
            object target = property.serializedObject.targetObject;

            // fieldInfo is provided by PropertyDrawer; it points at the SplineWrapper field
            var wrapper = fieldInfo.GetValue(target) as SplineWrapper;
            if (wrapper != null)
            {
                wrapper.Spline = selectedSpline;   // assign the new spline
                EditorUtility.SetDirty((UnityEngine.Object)target);
            }

            property.serializedObject.ApplyModifiedProperties();
            property.serializedObject.Update();
        });

        return popup;
    }

}
