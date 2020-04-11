using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(Camera))]
[ExecuteAlways]
public class MixCloudWithScenes : MonoBehaviour
{
    public Shader _mixShader = null;
    [Range(0.01f, 1)]
    public float occludeeVisibility = 0.35f;
    [Range(0, 1)]
    public float occludeeVisibleRange = 0.6f;

    private Shader mixShader{
        get{
            if(!_mixShader) _mixShader = Shader.Find("AlphaBlendWithDepthDiff");
            return _mixShader;
        }
    }
    private Material _material;
    private Material material{
        get{
            _material = CheckShaderAndCreateMaterial(mixShader, _material);
            return _material;
        }
    }
	Material CheckShaderAndCreateMaterial(Shader shader, Material material) {
		if (shader == null || !shader.isSupported) 
			return null;
		else if (material && material.shader == shader)
			return material;
		else {
			material = new Material(shader);
			material.hideFlags = HideFlags.DontSave;
			if (material)
				return material;
			else 
				return null;
		}
	}

    private RenderTexture ZBuffer;
    private RenderTexture ColorBuffer;
    private RenderTexture CT_opaque;
    private RenderTexture CT_cloud;
    private RenderTexture DT_opaque;//DepthTexture of Scenes with Opaque Objs
    private RenderTexture DT_cloud;//DepthTexture of Scenes with Opaque Objs and Clouds
    private CommandBuffer CB_BeforeAlpha = null;
    private CommandBuffer CB_AfterAlpha = null;

    private Camera _Camera;

    void OnEnable() {
        InitAll();
    }

    void OnDisable()
    {
        ReleaseAll();
    }

    private void OnPreRender() {
        UpdateAll();
        _Camera.SetTargetBuffers(ColorBuffer.colorBuffer, ZBuffer.depthBuffer);
    }

    private void OnPostRender() {
        if(material){
            material.SetTexture("_OpaqueColorTex", CT_opaque);
            material.SetTexture("_OpaqueDepthTex", DT_opaque);
            material.SetTexture("_AllDepthTex", DT_cloud);
            material.SetFloat("_OccludeeVisibility", occludeeVisibility);
            material.SetFloat("_OccludeeVisibleRange", occludeeVisibleRange);
            Graphics.Blit(CT_cloud, (RenderTexture)null, material);
        }
        else{
            Graphics.Blit(CT_cloud, (RenderTexture)null);
        }
        
    }

    void InitAll()
    {
        if(!_Camera) _Camera = gameObject.GetComponent<Camera>();
        InitAllRTs(Screen.width, Screen.height);
        InitCBs();
        InitNames();
    }

    void UpdateAll(){
        if(ColorBuffer.width != Screen.width || ColorBuffer.height != Screen.height){
            ReleaseAll();
            InitAllRTs(Screen.width, Screen.height);
            InitCBs();
        }
    }

    void InitNames(){
        ZBuffer.name = "myZbuffer";
        ColorBuffer.name = "myColorBuffer";
        CT_opaque.name = "Color_Opaque";
        CT_cloud.name = "Color_Cloud";
        DT_opaque.name = "Depth_Opaque";
        DT_cloud.name = "Depth_Cloud";
    }

    void InitCBs(){
        CB_BeforeAlpha = new CommandBuffer();
        CB_BeforeAlpha.name = "BeforeAlpha";
        CB_BeforeAlpha.Blit(ColorBuffer.colorBuffer, CT_opaque.colorBuffer);
        CB_BeforeAlpha.Blit(ZBuffer.depthBuffer, DT_opaque.colorBuffer);
        _Camera.AddCommandBuffer(CameraEvent.BeforeForwardAlpha, CB_BeforeAlpha);

        CB_AfterAlpha = new CommandBuffer();
        CB_AfterAlpha.name = "AfterAlpha";
        CB_AfterAlpha.Blit(ColorBuffer.colorBuffer, CT_cloud.colorBuffer);
        CB_AfterAlpha.Blit(ZBuffer.depthBuffer, DT_cloud.colorBuffer);
        _Camera.AddCommandBuffer(CameraEvent.AfterForwardAlpha, CB_AfterAlpha);
    }

    void ReleaseAll(){
        _Camera.targetTexture = null;
        _Camera.RemoveCommandBuffer(CameraEvent.BeforeForwardAlpha, CB_BeforeAlpha);
        _Camera.RemoveCommandBuffer(CameraEvent.AfterForwardAlpha, CB_AfterAlpha);
        CB_BeforeAlpha.Clear();
        CB_AfterAlpha.Clear();
        ZBuffer.Release();
        ColorBuffer.Release();
        CT_opaque.Release();
        DT_opaque.Release();
        CT_cloud.Release();
        DT_cloud.Release();
    }

    void InitAllRTs(int width, int height){
        InitRenderTexture(ref ZBuffer, width, height, 24, RenderTextureFormat.Depth);
        InitRenderTexture(ref ColorBuffer, width, height, 0, RenderTextureFormat.RGB111110Float);
        InitRenderTexture(ref CT_opaque, width, height, 0, RenderTextureFormat.RGB111110Float);
        InitRenderTexture(ref CT_cloud, width, height, 0, RenderTextureFormat.RGB111110Float);
        InitRenderTexture(ref DT_opaque, width, height, 0, RenderTextureFormat.RHalf);
        InitRenderTexture(ref DT_cloud, width, height, 0, RenderTextureFormat.RHalf);
    }

    void InitRenderTexture(ref RenderTexture rt, int width, int height, int depth, RenderTextureFormat format){
        rt = new RenderTexture(width, height, depth, format);
        rt.hideFlags = HideFlags.HideAndDontSave;
        //rt.antiAliasing = QualitySettings.antiAliasing > 0 ? QualitySettings.antiAliasing : 1;
        rt.Create();
    }
}