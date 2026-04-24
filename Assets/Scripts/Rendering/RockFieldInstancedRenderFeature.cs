using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// URP: <see cref="CommandBuffer.DrawMeshInstancedIndirect"/> in BeforeRenderingOpaques.
/// Per-frame data from <see cref="RockFieldInstancedRenderer"/>.
[DisallowMultipleRendererFeature("RockFieldInstancedRenderFeature")]
public class RockFieldInstancedRenderFeature : ScriptableRendererFeature
{
    class RockFieldPass : ScriptableRenderPass
    {
        const string kProfile = "PrisonLife RockField Instanced Indirect";

        public void Setup(RenderPassEvent evt)
        {
            renderPassEvent = evt;
            profilingSampler = new ProfilingSampler(kProfile);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
            // AddRenderPasses ??? ScriptableRenderer? RTHandle? ?? ?? ?? ? ??.
            // per-camera OnCameraSetup ?? cameraData.renderer ? ?? ?? URP 14 ??? ???.
            var r = renderingData.cameraData.renderer;
            if (r == null) return;
            if (r.cameraColorTargetHandle == null || r.cameraDepthTargetHandle == null)
                return;
            ConfigureTarget(r.cameraColorTargetHandle, r.cameraDepthTargetHandle);
            ConfigureClear(ClearFlag.None, Color.clear);
        }

        public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
        {
            if (!RockFieldInstancedSrpState.Ready)
                return;

            var s = RockFieldInstancedSrpState.Current;
            if (s.mesh == null || s.material == null || s.argsBuffer == null)
                return;

            if (s.drawCamera != null && renderingData.cameraData.camera != s.drawCamera)
                return;

            if (renderingData.cameraData.cameraType == CameraType.Preview)
                return;

            var r = renderingData.cameraData.renderer;
            if (r == null || r.cameraColorTargetHandle == null || r.cameraDepthTargetHandle == null)
                return;

            CommandBuffer cmd = CommandBufferPool.Get(kProfile);
            // OnCameraSetup ? RT? ? ?? ????? Execute?? ?? RT? ??
            ConfigureTarget(r.cameraColorTargetHandle, r.cameraDepthTargetHandle);
            var cameraData = renderingData.cameraData;
            RenderingUtils.SetViewAndProjectionMatrices(
                cmd,
                cameraData.GetViewMatrix(),
                cameraData.GetGPUProjectionMatrix(0),
                setInverseMatrices: true);

            cmd.DrawMeshInstancedIndirect(
                s.mesh,
                s.submeshIndex,
                s.material,
                0,
                s.argsBuffer,
                s.argsOffset,
                s.propertyBlock);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
    }

    RockFieldPass _pass;

    public override void Create()
    {
        _pass = new RockFieldPass();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        // ??? Overlay ????? Pass? ???? ??/??? ?? ? ?? ? ??? Base?
        if (renderingData.cameraData.renderType != CameraRenderType.Base)
            return;
        _pass.Setup(RenderPassEvent.BeforeRenderingOpaques);
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
    }
}
