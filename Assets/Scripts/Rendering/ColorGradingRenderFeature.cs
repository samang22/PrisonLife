using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[DisallowMultipleRendererFeature("ColorGradingRenderFeature")]
public class ColorGradingRenderFeature : ScriptableRendererFeature
{
    [Tooltip("ColorGradingBlit 셰이더로 만든 머티리얼")]
    public Material gradingMaterial;

    [Tooltip("후처리 직전에 끼워 넣으면 URP Post Processing과 순서를 맞추기 쉬움")]
    public RenderPassEvent injectionPoint = RenderPassEvent.BeforeRenderingPostProcessing;

    private ColorGradingPass _pass;

    public override void Create()
    {
        _pass = new ColorGradingPass();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (gradingMaterial == null)
            return;

        if (renderingData.cameraData.cameraType == UnityEngine.CameraType.Preview)
            return;

        // cameraColorTargetHandle 은 ScriptableRenderPass 콜백(Execute/OnCameraSetup) 안에서만 유효.
        // 여기서는 핸들을 읽지 않고, Pass에 renderer만 넘긴다.
        _pass.Setup(gradingMaterial, renderer, injectionPoint);
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        _pass?.Dispose();
    }

    class ColorGradingPass : ScriptableRenderPass
    {
        private Material           _mat;
        private ScriptableRenderer _renderer;
        private RTHandle           _tmp;

        public void Setup(Material mat, ScriptableRenderer renderer, RenderPassEvent evt)
        {
            _mat      = mat;
            _renderer = renderer;
            renderPassEvent = evt;
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            RenderTextureDescriptor d = renderingData.cameraData.cameraTargetDescriptor;
            d.msaaSamples     = 1;
            d.depthBufferBits = 0;
            RenderingUtils.ReAllocateIfNeeded(ref _tmp, d, FilterMode.Bilinear, TextureWrapMode.Clamp, name: "_ColorGradingTemp");
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (_mat == null || _renderer == null)
                return;

            // Pass 실행 시점에만 유효 (AddRenderPasses에서 읽으면 예외)
            RTHandle source = _renderer.cameraColorTargetHandle;

            CommandBuffer cmd = CommandBufferPool.Get("PrisonLife Color Grading");
            Blitter.BlitCameraTexture(cmd, source, _tmp, _mat, 0);
            Blitter.BlitCameraTexture(cmd, _tmp, source, bilinear: true);
            context.ExecuteCommandBuffer(cmd);
            CommandBufferPool.Release(cmd);
        }

        public void Dispose()
        {
            _tmp?.Release();
            _tmp = null;
        }
    }
}
