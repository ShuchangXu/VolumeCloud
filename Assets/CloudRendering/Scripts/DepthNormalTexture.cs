using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Camera))]
public class DepthNormalTexture : MonoBehaviour
{
    public DepthTextureMode mode; 
    private Camera _camera;

    void Init(){
        if(!_camera) _camera = GetComponent<Camera>();
        _camera.depthTextureMode = mode;
    }
    void Awake()
    {
        Init();
    }

    void OnEnable() {
        Init();
    }

    // Update is called once per frame
    private void OnValidate() {
        Init();
    }
}
