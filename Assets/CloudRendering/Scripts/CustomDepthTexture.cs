using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
[ExecuteInEditMode]
public class CustomDepthTexture : MonoBehaviour
{
    public string nameOfDepthTextureInShader = "_CustomDepthTexture";
    private RenderTexture depthRT;
    private RenderTexture colorRT;
    private RenderTexture depthTex;

    private CommandBuffer _cbDepth = null;

    private Camera _Camera = null;

    private void Awake()
    {
        _Camera = Camera.main;

        int Width = _Camera.pixelWidth;
        int Height = _Camera.pixelHeight;

        depthRT = new RenderTexture(Width, Height, 24, RenderTextureFormat.Depth);
        depthRT.name = "MainDepthBuffer";
        colorRT = new RenderTexture(Width, Height, 0, RenderTextureFormat.RGB111110Float);
        colorRT.name = "MainColorBuffer";

        depthTex = new RenderTexture(Width, Height, 0, RenderTextureFormat.RHalf);
        depthTex.name = "SceneDepthTex";

        _cbDepth = new CommandBuffer();
        _cbDepth.name = "CopyOpaqueDepthTexture";
        _cbDepth.Blit(depthRT.depthBuffer, depthTex.colorBuffer);
        _Camera.AddCommandBuffer(CameraEvent.AfterForwardOpaque, _cbDepth);
    }

    private void Update()
    {
        Shader.SetGlobalTexture(nameOfDepthTextureInShader, depthTex);
    }

    void OnPreRender()
    {
        _Camera.SetTargetBuffers(colorRT.colorBuffer, depthRT.depthBuffer);
    }

    private void OnPostRender()
    {
        //目前的机制不需要这次拷贝
        Graphics.Blit(colorRT, (RenderTexture)null);
    }
}