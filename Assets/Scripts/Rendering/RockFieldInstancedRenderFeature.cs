using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

/// URP: <see cref="CommandBuffer.DrawMeshInstancedIndirect"/> — Forward는 BeforeRenderingOpaques,
/// 그림자 투영은 메인 라이트 섀도맵에 AfterRenderingShadows 에서 ShadowCaster 패스로 추가.
/// (CommandBuffer로 Forward만 그리면 URP 섀도 패스에 포함되지 않음)
[DisallowMultipleRendererFeature("RockFieldInstancedRenderFeature")]
public class RockFieldInstancedRenderFeature : ScriptableRendererFeature
{
    class RockFieldForwardPass : ScriptableRenderPass
    {
        const string kProfile = "PrisonLife RockField Instanced Indirect (Forward)";

        public void Setup(RenderPassEvent evt)
        {
            renderPassEvent = evt;
            profilingSampler = new ProfilingSampler(kProfile);
        }

        public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
        {
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
            ConfigureTarget(r.cameraColorTargetHandle, r.cameraDepthTargetHandle);
            var cameraData = renderingData.cameraData;
            RenderingUtils.SetViewAndProjectionMatrices(
                cmd,
                cameraData.GetViewMatrix(),
                cameraData.GetGPUProjectionMatrix(0),
                setInverseMatrices: true);

            int forwardPass = s.material.FindPass("ForwardLit");
            if (forwardPass < 0)
                forwardPass = s.material.FindPass("UniversalForward");
            if (forwardPass < 0)
                forwardPass = 0;

            cmd.DrawMeshInstancedIndirect(
                s.mesh,
                s.submeshIndex,
                s.material,
                forwardPass,
                s.argsBuffer,
                s.argsOffset,
                s.propertyBlock);

            context.ExecuteCommandBuffer(cmd);
            cmd.Clear();
            CommandBufferPool.Release(cmd);
        }
    }

    class RockFieldShadowPass : ScriptableRenderPass
    {
        const string kProfile = "PrisonLife RockField Instanced Indirect (ShadowCaster)";
        static readonly int sMainLightShadowmap = Shader.PropertyToID("_MainLightShadowmapTexture");
        static readonly int sIdWorldSpaceCameraPos = Shader.PropertyToID("_WorldSpaceCameraPos");
        static readonly int sIdUnityWorldToCamera = Shader.PropertyToID("unity_WorldToCamera");
        static readonly int sIdUnityCameraToWorld = Shader.PropertyToID("unity_CameraToWorld");

        /// <summary>URP ShadowUtils.SetCameraPosition / SetWorldToCamera* 는 internal — MainLightShadowCasterPass 와 동일 셰도 상수 설정</summary>
        static void SetCameraPositionPublic(CommandBuffer cmd, Vector3 worldSpaceCameraPos)
        {
            cmd.SetGlobalVector(sIdWorldSpaceCameraPos, worldSpaceCameraPos);
        }

        static void SetWorldToCameraAndCameraToWorldPublic(CommandBuffer cmd, Matrix4x4 viewMatrix)
        {
            Matrix4x4 worldToCamera = Matrix4x4.Scale(new Vector3(1f, 1f, -1f)) * viewMatrix;
            cmd.SetGlobalMatrix(sIdUnityWorldToCamera, worldToCamera);
            cmd.SetGlobalMatrix(sIdUnityCameraToWorld, worldToCamera.inverse);
        }

        public void Setup(RenderPassEvent evt)
        {
            renderPassEvent = evt;
            profilingSampler = new ProfilingSampler(kProfile);
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

            var shadowData = renderingData.shadowData;
            if (!shadowData.supportsMainLightShadows)
                return;

            int mainLightIndex = renderingData.lightData.mainLightIndex;
            if (mainLightIndex < 0)
                return;

            var visibleLights = renderingData.lightData.visibleLights;
            if (mainLightIndex >= visibleLights.Length)
                return;

            VisibleLight shadowLight = visibleLights[mainLightIndex];
            if (shadowLight.light == null || shadowLight.light.shadows == LightShadows.None)
                return;

            if (!renderingData.cullResults.GetShadowCasterBounds(mainLightIndex, out _))
                return;

            int shadowPass = s.material.FindPass("ShadowCaster");
            if (shadowPass < 0)
                return;

            int cascadeCount = shadowData.mainLightShadowCascadesCount;
            int shadowResolution = ShadowUtils.GetMaxTileResolutionInAtlas(
                shadowData.mainLightShadowmapWidth,
                shadowData.mainLightShadowmapHeight,
                cascadeCount);
            int renderTargetWidth = shadowData.mainLightShadowmapWidth;
            int renderTargetHeight = (cascadeCount == 2)
                ? shadowData.mainLightShadowmapHeight >> 1
                : shadowData.mainLightShadowmapHeight;

            var light = shadowLight.light;

            CommandBuffer cmd = CommandBufferPool.Get(kProfile);

            SetCameraPositionPublic(cmd, renderingData.cameraData.worldSpaceCameraPos);
            SetWorldToCameraAndCameraToWorldPublic(cmd, renderingData.cameraData.GetViewMatrix());

            // Property ID만 넘기면 "temporary render texture _MainLightShadowmapTexture not found" (URP는 RTHandle + SetGlobalTexture)
            Texture shadowTex = Shader.GetGlobalTexture(sMainLightShadowmap);
            if (shadowTex == null)
                return;
            if (shadowTex is not RenderTexture shadowRt)
                return;
            if (!shadowRt)
                return;

            cmd.SetRenderTarget(shadowRt, 0, CubemapFace.Unknown, -1);

            for (int cascadeIndex = 0; cascadeIndex < cascadeCount; cascadeIndex++)
            {
                bool ok = ShadowUtils.ExtractDirectionalLightMatrix(
                    ref renderingData.cullResults,
                    ref renderingData.shadowData,
                    mainLightIndex,
                    cascadeIndex,
                    renderTargetWidth,
                    renderTargetHeight,
                    shadowResolution,
                    light.shadowNearPlane,
                    out _,
                    out ShadowSliceData slice);

                if (!ok)
                    continue;

                Vector4 shadowBias = ShadowUtils.GetShadowBias(
                    ref shadowLight,
                    mainLightIndex,
                    ref renderingData.shadowData,
                    slice.projectionMatrix,
                    slice.resolution);
                ShadowUtils.SetupShadowCasterConstantBuffer(cmd, ref shadowLight, shadowBias);
                CoreUtils.SetKeyword(cmd, ShaderKeywordStrings.CastingPunctualLightShadow, false);

                cmd.SetGlobalDepthBias(1.0f, 2.5f);
                cmd.SetViewport(new Rect(slice.offsetX, slice.offsetY, slice.resolution, slice.resolution));
                cmd.SetViewProjectionMatrices(slice.viewMatrix, slice.projectionMatrix);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                cmd.DrawMeshInstancedIndirect(
                    s.mesh,
                    s.submeshIndex,
                    s.material,
                    shadowPass,
                    s.argsBuffer,
                    s.argsOffset,
                    s.propertyBlock);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();

                cmd.SetGlobalDepthBias(0f, 0f);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            }

            var r = renderingData.cameraData.renderer;
            if (r != null && r.cameraColorTargetHandle != null && r.cameraDepthTargetHandle != null)
            {
                cmd.SetRenderTarget(r.cameraColorTargetHandle.nameID, r.cameraDepthTargetHandle.nameID);
                context.ExecuteCommandBuffer(cmd);
                cmd.Clear();
            }

            CommandBufferPool.Release(cmd);
        }
    }

    RockFieldForwardPass _forwardPass;
    RockFieldShadowPass _shadowPass;

    public override void Create()
    {
        _forwardPass = new RockFieldForwardPass();
        _shadowPass = new RockFieldShadowPass();
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (renderingData.cameraData.renderType != CameraRenderType.Base)
            return;
        _forwardPass.Setup(RenderPassEvent.BeforeRenderingOpaques);
        // AfterRenderingShadows 직후엔(특히 Render Graph) _MainLightShadowmapTexture 글로벌이 아직 없을 수 있음
        _shadowPass.Setup(RenderPassEvent.BeforeRenderingPrePasses);
        renderer.EnqueuePass(_forwardPass);
        renderer.EnqueuePass(_shadowPass);
    }

    protected override void Dispose(bool disposing)
    {
    }
}
