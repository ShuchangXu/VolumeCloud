using UnityEngine;
using System.Collections.Generic;

[ExecuteAlways]
public class CloudRenderer : MonoBehaviour
{
    [Header("Mesh & Material")]
    public Mesh baseMesh;
    public Material material;

    [Header("Shadows")]
    public bool castShadows = false;
    public bool receiveShadows = true;

    [Header("Shapes")]
    [Range(1, 50)]
    public int layers = 20;
    [Range(0, 0.2f)]
    public float thickness = 0.1f;
    [Range(0.1f, 10)]
    public float clipThresPower = 2f;
    [Header("Animation")]
    [Range(0.1f, 10)]
    public float noiseTiling = 4.0f;
    [Space(10.0f)]

    private MaterialPropertyBlock _MPB;

    /* ++++++++++++ DrawMeshInstanced ++++++++++++ */
    private Matrix4x4[] matrices;
    private List<float> _PosOffset_Array = new List<float>();
    private List<float> _ClipThres_Array = new List<float>();
    private List<float> _NoiseTiling_Array = new List<float>();
    /* ------------ DrawMeshInstanced ------------ */

    void Init(){
        _MPB = new MaterialPropertyBlock();

        /* ++++++++++++ DrawMeshInstanced ++++++++++++ */
        matrices = new Matrix4x4[layers];

        _PosOffset_Array.Clear();
        _ClipThres_Array.Clear();
        _NoiseTiling_Array.Clear();
        
        for(int i = 0; i < layers; i ++){
            float offset = i * 1.0f / layers;
            _PosOffset_Array.Add(offset * thickness);
            _ClipThres_Array.Add(Mathf.Pow(offset, clipThresPower));
            _NoiseTiling_Array.Add(noiseTiling);
        }
        _MPB.SetFloatArray("_PosOffset", _PosOffset_Array);
        _MPB.SetFloatArray("_ClipThres", _ClipThres_Array);
        _MPB.SetFloatArray("_NoiseTiling", _NoiseTiling_Array);
        /* ------------ DrawMeshInstanced ------------ */
    }

    private void Awake(){
        Init();
    }

    private void OnValidate() {
        Init();
    }

    void Update()
    {
        if(_MPB == null){
            _MPB = new MaterialPropertyBlock();
        }

        /* ++++++++++++ DrawMeshInstanced ++++++++++++ */
        for(int i = 0; i < layers; i ++)
            matrices[i] = transform.localToWorldMatrix;
        
        if(baseMesh)
            Graphics.DrawMeshInstanced(baseMesh, 0, material, matrices, layers, _MPB, 
            UnityEngine.Rendering.ShadowCastingMode.Off, receiveShadows, LayerMask.NameToLayer("Cloud"));
        /* ------------ DrawMeshInstanced ------------ */

        /* ++++++++++++ DrawMesh ++++++++++++ */
        // for(int i = 0; i < layers; i ++){
        //     float offset = i * 1.0f / layers;
        //     _MPB.SetFloat("_PosOffset", offset * thickness);
        //     _MPB.SetFloat("_ClipThres", Mathf.Pow(offset, clipThresPower));
        //     _MPB.SetFloat("_NoiseTiling", noiseTiling);
        //     material.
        //     material.SetPass(0);
        //     Graphics.DrawMesh(baseMesh, transform.localToWorldMatrix, material, 0, null, 0, _MPB, castShadows, receiveShadows);
        // }
        /* ------------ DrawMesh ------------ */
    }

    /* DrawMeshNow cannot be instanced */
    /* ++++++++++++ DrawMeshNow ++++++++++++ */
    // void OnRenderObject(){
    //     
    //     for(int i = 0; i < layers; i ++){
    //         float offset = i * 1.0f / layers;
    //         material.SetFloat("_PosOffset", offset * thickness);
    //         material.SetFloat("_ClipThres", Mathf.Pow(offset, clipThresPower));
    //         material.SetFloat("_NoiseTiling", noiseTiling);
    //         if(material.SetPass(0))
    //             Graphics.DrawMeshNow(baseMesh, transform.localToWorldMatrix);
    //     }
    // }
    /* ------------ DrawMeshNow ------------ */
}
