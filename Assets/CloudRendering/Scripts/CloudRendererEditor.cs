#if (UNITY_EDITOR) 
using UnityEditor;

[CustomEditor(typeof(CloudRenderer))]
[CanEditMultipleObjects]
public class CloudRendererEditor : Editor
{
    private CloudRenderer _renderer;
    private MaterialEditor _materialEditor; 

    void OnEnable()
    {
        _renderer = (CloudRenderer)target;
        if (_renderer.material != null) {
            _materialEditor = (MaterialEditor)CreateEditor(_renderer.material);
        }
    }

    public override void OnInspectorGUI ()
    {
        EditorGUI.BeginChangeCheck();
        // Draw the material field of Cloud Renderer
        DrawDefaultInspector();

        if (EditorGUI.EndChangeCheck()) {
            serializedObject.ApplyModifiedProperties(); 

            if (_materialEditor != null) {
                // Free the memory used by the previous MaterialEditor
                DestroyImmediate (_materialEditor);
            }

            if (_renderer.material != null) {
                // Create a new instance of the default MaterialEditor
                _materialEditor = (MaterialEditor)CreateEditor(_renderer.material);
            }
        }

        if (_materialEditor != null) {
            // Draw the material's foldout and the material shader field
            // Required to call _materialEditor.OnInspectorGUI ();
            _materialEditor.DrawHeader();
        
            //  We need to prevent the user to edit Unity default materials
            bool isDefaultMaterial = !AssetDatabase.GetAssetPath (_renderer.material).StartsWith ("Assets");

            using (new EditorGUI.DisabledGroupScope(isDefaultMaterial)) {
                // Draw the material properties
                // Works only if the foldout of _materialEditor.DrawHeader () is open
                _materialEditor.OnInspectorGUI (); 
            }
        }
    }

    void OnDisable ()
    {
        if (_materialEditor != null) {
            // Free the memory used by default MaterialEditor
            DestroyImmediate (_materialEditor);
        }
    }
}
#endif