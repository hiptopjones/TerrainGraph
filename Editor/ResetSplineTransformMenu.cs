using UnityEditor;
using UnityEngine;
using UnityEngine.Splines;

public static class ResetSplineTransformMenu
{
    private const string MENU_PATH = "Tools / Splines / Reset Transform (Keep Knots)";

    [MenuItem(MENU_PATH, priority = 100)]
    private static void ResetTransformKeepKnots()
    {
        GameObject go = Selection.activeGameObject;
        if (go == null)
        {
            return;
        }

        SplineContainer container = go.GetComponent<SplineContainer>();
        if (container == null)
        {
            return;
        }

        Transform t = container.transform;

        Undo.RegisterCompleteObjectUndo(container, "Reset Spline Transform");
        Undo.RecordObject(t, "Reset Transform");

        Matrix4x4 localToWorld = t.localToWorldMatrix;

        // Cache knot data in world space
        var splineWorldPositions = new Vector3[container.Splines.Count][];
        var tangentInWorld = new Vector3[container.Splines.Count][];
        var tangentOutWorld = new Vector3[container.Splines.Count][];

        for (int s = 0; s < container.Splines.Count; s++)
        {
            var spline = container.Splines[s];
            int count = spline.Count;

            splineWorldPositions[s] = new Vector3[count];
            tangentInWorld[s] = new Vector3[count];
            tangentOutWorld[s] = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                BezierKnot knot = spline[i];

                splineWorldPositions[s][i] = localToWorld.MultiplyPoint3x4(knot.Position);
                tangentInWorld[s][i] = localToWorld.MultiplyVector(knot.TangentIn);
                tangentOutWorld[s][i] = localToWorld.MultiplyVector(knot.TangentOut);
            }
        }

        // Reset transform
        t.position = Vector3.zero;
        t.rotation = Quaternion.identity;
        t.localScale = Vector3.one;

        Matrix4x4 worldToLocal = t.worldToLocalMatrix;

        // Restore knots in new local space
        for (int s = 0; s < container.Splines.Count; s++)
        {
            var spline = container.Splines[s];

            for (int i = 0; i < spline.Count; i++)
            {
                BezierKnot knot = spline[i];

                knot.Position = worldToLocal.MultiplyPoint3x4(splineWorldPositions[s][i]);
                knot.TangentIn = worldToLocal.MultiplyVector(tangentInWorld[s][i]);
                knot.TangentOut = worldToLocal.MultiplyVector(tangentOutWorld[s][i]);

                spline[i] = knot;
            }
        }

        EditorUtility.SetDirty(container);
    }

    // Validation: only enable when a SplineContainer is selected
    [MenuItem(MENU_PATH, validate = true)]
    private static bool ValidateResetTransformKeepKnots()
    {
        if (Selection.activeGameObject == null)
        {
            return false;
        }

        return Selection.activeGameObject.GetComponent<SplineContainer>() != null;
    }
}
