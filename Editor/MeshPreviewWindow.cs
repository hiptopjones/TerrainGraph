using UnityEditor;
using UnityEngine;

public class MeshPreviewWindow : EditorWindow
{
    [SerializeField] private Mesh _mesh;

    private PreviewRenderUtility _preview;

    float _orbitSpeed = 0.3f;

    float _cameraYaw = 45f;
    float _cameraPitch = 30f;
    float _cameraDistance = 1f;

    float _lightYaw = 45f;
    float _lightPitch = 45f;

    bool _draggingCamera;
    bool _draggingLight;
    Vector2 _lastScreenMousePosition;

    private Material _previewMaterial;

    [MenuItem("Tools/Mesh/Preview Window")]
    public static void Open()
    {
        GetWindow<MeshPreviewWindow>("Mesh Preview");
    }

    void OnEnable()
    {
        _preview = new PreviewRenderUtility();
        _preview.cameraFieldOfView = 30f;
        _preview.camera.nearClipPlane = 0.01f;
        _preview.camera.farClipPlane = 1000f;

        _mesh = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        FrameMesh();

        var shader = Shader.Find("Universal Render Pipeline/Lit");
        _previewMaterial = new Material(shader);

        // Lighting
        _preview.lights[0].intensity = 1.3f;
        _preview.lights[1].intensity = 0.2f;
    }

    void OnDisable()
    {
        _preview?.Cleanup();
    }

    void OnGUI()
    {
        HandleInput(Event.current);
        
        EditorGUI.BeginChangeCheck();
        _mesh = (Mesh)EditorGUILayout.ObjectField("Mesh", _mesh, typeof(Mesh), false);
        if (EditorGUI.EndChangeCheck())
        {
            FrameMesh(); // auto-frame when mesh changes
        }

        Rect rect = GUILayoutUtility.GetRect(position.width, position.height);
        DrawPreview(rect);

        Repaint(); // continuous refresh
    }

    void HandleInput(Event e)
    {
        int id = GUIUtility.GetControlID(FocusType.Passive);

        Vector2 screenMousePosition = EditorGUIUtility.GUIToScreenPoint(e.mousePosition);

        switch (e.type)
        {
            case EventType.MouseDown:
                {
                    if (e.button == 0 || e.button == 1)
                    {
                        GUIUtility.hotControl = id;

                        _draggingCamera = e.button == 0;
                        _draggingLight = e.button == 1;

                        _lastScreenMousePosition = screenMousePosition;
                        e.Use();
                    }
                    break;
                }

            case EventType.MouseDrag:
                {
                    if (GUIUtility.hotControl == id && (_draggingCamera || _draggingLight))
                    {
                        Vector2 delta = screenMousePosition - _lastScreenMousePosition;
                        _lastScreenMousePosition = screenMousePosition;

                        if (_draggingCamera)
                        {
                            _cameraYaw += delta.x * _orbitSpeed;
                            _cameraPitch += delta.y * _orbitSpeed;
                        }
                        else if (_draggingLight)
                        {
                            _lightYaw -= delta.x * _orbitSpeed;
                            _lightPitch -= delta.y * _orbitSpeed;
                        }

                        e.Use();
                    }
                    break;
                }

            case EventType.MouseUp:
                {
                    if (GUIUtility.hotControl == id)
                    {
                        GUIUtility.hotControl = 0;
                        _draggingCamera = _draggingLight = false;
                        e.Use();
                    }
                    break;
                }

            case EventType.ScrollWheel:
                {
                    _cameraDistance *= 1f + e.delta.y * 0.05f;
                    _cameraDistance = Mathf.Clamp(_cameraDistance, 2f, 1000f);
                    e.Use();
                    break;
                }
        }
    }

    void DrawPreview(Rect rect)
    {
        if (_mesh == null)
        {
            return;
        }

        _preview.BeginPreview(rect, GUIStyle.none);

        var meshCenter = GetMeshBounds().center;

        Quaternion cameraRotation = Quaternion.Euler(_cameraPitch, _cameraYaw, 0);
        Vector3 cameraOffset = cameraRotation * new Vector3(0, 0, -_cameraDistance);

        _preview.camera.transform.position = meshCenter + cameraOffset;
        _preview.camera.transform.LookAt(meshCenter);

        Quaternion lightRotation = Quaternion.Euler(_lightPitch, _lightYaw, 0);
        _preview.lights[0].transform.rotation = lightRotation;

        _preview.DrawMesh(_mesh, Matrix4x4.identity, _previewMaterial, 0);
        _preview.camera.Render();

        Texture texture = _preview.EndPreview();
        GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, false);
    }

    void FrameMesh()
    {
        Bounds bounds = GetMeshBounds();

        float radius = bounds.extents.magnitude;
        float fov = _preview.cameraFieldOfView * Mathf.Deg2Rad;

        // Distance needed to fit sphere in vertical FOV
        _cameraDistance = radius / Mathf.Sin(fov * 0.5f);

        // Add padding so it doesn't touch edges
        _cameraDistance *= 1.2f;
    }

    Bounds GetMeshBounds()
    {
        if (_mesh == null)
        {
            return new Bounds(Vector3.zero, Vector3.one);
        }

        return _mesh.bounds;
    }
}
