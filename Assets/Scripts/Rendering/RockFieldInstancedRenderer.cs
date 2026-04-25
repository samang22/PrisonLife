using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

/// 씬의 <see cref="RockController"/> (채광 암석) 목록 <see cref="RockController.All"/>을 기준으로
/// 각 Rock의 <c>localToWorldMatrix</c> 를 인스턴스 행렬로 쓰고 DrawMeshInstancedIndirect 로 그립니다.
/// 기본 Rock 메쉬/머티리얼 루트 렌더러는 선택적으로 끄고(중복 방지) 콜라이더는 그대로 둡니다.
[DefaultExecutionOrder(50)]
public class RockFieldInstancedRenderer : MonoBehaviour
{
    [Header("RockController 씬")]
    [Tooltip("켜면 Play 중 각 Rock의 MeshRenderer를 끕니다. GPU 경로가 실패하면 암석이 보이지 않을 수 있어 기본은 끔(원본 메쉬로도 표시).")]
    public bool disableOriginalRenderers = false;

    [Tooltip("비우면 All[0]에서 메쉬를 찾습니다.")]
    public Mesh sourceMesh;

    [Tooltip("옵션. 행렬은 transform.localToWorldMatrix 로 CPU에서 채움 (스케일·회전 반영)")]
    public ComputeShader rockCompute;
    [Tooltip("비우면 PrisonLife/Rendering/RockFieldInstanced")]
    public Shader instancedShader;

    [Header("GPU 변형")]
    [Min(0f)] public float wobbleHeight = 0.08f;

    [Header("렌더 대상")]
    [Tooltip("비우면 SRP 인스턴스 경로는 \"지금 그리는\" URP Base 카메라마다 제출. FindObjectOfType으로 잡으면 실제 게임 카메라와 달라 전부 스킵될 수 있어 비우는 경우가 안전함.")]
    public Camera targetCamera;

    [Tooltip("켜면 SRP(URP)와 별도로 Graphics.DrawMeshInstancedIndirect 도 호출합니다. 디버그·비상용(이중 그리기·비용). 기본 끔 = SRP Pass만(에디터에서 URP에 RockFieldInstancedRenderFeature 자동 등록).")]
    public bool alsoDrawWithGraphics;

    Matrix4x4[] _matrixUpload;
    ComputeBuffer _matrices;
    GraphicsBuffer _args;
    uint _argsInstanceCount;
    Material _mat;
    MaterialPropertyBlock _mpb;
    static readonly int sMatrices = Shader.PropertyToID("_InstanceMatrices");
    static readonly int sActive = Shader.PropertyToID("_InstanceActive");
    static readonly int sBaseMap = Shader.PropertyToID("_BaseMap");
    static readonly int sBaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int sBaseMapSt = Shader.PropertyToID("_BaseMap_ST");
    bool _copiedAlbedoFromRockMaterial;
    ComputeBuffer _activeBuf;
    int _lastCount = -1;
    /// <summary>인스턴스 그리기용: 메쉬가 붙은 Transform(자식) — 루트 transform만 쓰면 메쉬 오프셋이 빠져 뜬 것처럼 보일 수 있음</summary>
    Transform[] _instanceMeshRoots;

    /// 씬에 RockField가 있고 disableOriginalRenderers 켜짐 — RockController.Revive 등에서 머티리얼을 다시 켤지 말지 판단
    public static bool IsOriginalRockRenderersSuppressed()
    {
        var rf = UnityEngine.Object.FindObjectOfType<RockFieldInstancedRenderer>();
        return rf != null && rf.disableOriginalRenderers;
    }

    void OnDisable()
    {
        RockFieldInstancedSrpState.ClearFrame();
        _instanceMeshRoots = null;
        ReleaseBuffers();
    }

    void LateUpdate()
    {
        if (!Application.isPlaying)
            return;
        IReadOnlyList<RockController> all = RockController.All;
        int n = all.Count;
        if (n == 0)
        {
            RockFieldInstancedSrpState.ClearFrame();
            return;
        }

        if (!EnsureInit(n, all))
        {
            RockFieldInstancedSrpState.ClearFrame();
            return;
        }

        EnsureInstanceMeshRootCache(n, all);

        // n이 변할 때만: 첫 구동·암석 수 변화 시에 머티리얼 끄기. 리스폰(개수 동일)은 RockController 쪽이
        // IsOriginalRockRenderersSuppressed일 때 r.enabled = true 를 하지 않게 처리
        if (disableOriginalRenderers && _lastCount != n)
        {
            ApplyRenderersOff(all, true);
            _lastCount = n;
        }

        if (_activeBuf == null || _activeBuf.count != n)
        {
            if (_activeBuf != null) _activeBuf.Dispose();
            _activeBuf = new ComputeBuffer(n, sizeof(float), ComputeBufferType.Structured);
        }
        var act = new float[n];
        for (int j = 0; j < n; j++) act[j] = all[j] != null && all[j].IsAvailable ? 1f : 0f;
        _activeBuf.SetData(act);

        if (_matrixUpload == null || _matrixUpload.Length != n)
            _matrixUpload = new Matrix4x4[n];
        for (int i = 0; i < n; i++)
        {
            var r = all[i];
            if (r == null)
            {
                // float4x4.zero 는 셰이더侧 inverse(3x3) 에서 NaN — 폐기·인스턴스이지만 vert는 돈다
                _matrixUpload[i] = Matrix4x4.Translate(new Vector3(0f, -1e5f, 0f));
                continue;
            }
            var meshRoot = (i < _instanceMeshRoots.Length && _instanceMeshRoots[i] != null)
                ? _instanceMeshRoots[i]
                : r.transform;
            Matrix4x4 m = meshRoot.localToWorldMatrix;
            if (r.IsAvailable && wobbleHeight > 0f)
            {
                Vector4 col = m.GetColumn(3);
                col.y += Mathf.Sin(Time.time * 2.2f + i * 0.13f) * wobbleHeight;
                m.SetColumn(3, col);
            }
            _matrixUpload[i] = m;
        }
        _matrices.SetData(_matrixUpload);

        BindInstanceBuffersToMpb();
        var drawBounds = CullingBoundsForRocks(n, all);
        int drawLayer = CullingLayerForRocks(n, all);

        // SRP: drawCamera에 GetAnyCamera()를 넣으면 씬의 "첫" Camera가 URP Base와 다를 때
        // RockFieldInstancedRenderFeature가 매 프레임 스킵되어 시작부터 암석이 안 그려짐.
        // targetCamera 미지정 시 null -> Pass는 현재 URP Base 렌더와 카메라 일치 필터를 쓰지 않음.
        Camera drawCamForSrp = targetCamera;
        Camera drawCamGraphics = targetCamera != null ? targetCamera : GetAnyCamera();
        if (drawCamGraphics == null && alsoDrawWithGraphics)
            Debug.LogWarning("RockFieldInstancedRenderer: 카메라 없음 — Graphics.DrawMeshInstancedIndirect 가 스킵될 수 있음.");

        RockFieldInstancedSrpState.PrepareDraw(
            sourceMesh, 0, _mat, _mpb, _args, 0, drawBounds, drawCamForSrp);

        if (alsoDrawWithGraphics)
            DrawMeshInstancedGraphics(drawBounds, drawLayer, drawCamGraphics);
    }

    void OnDestroy()
    {
        if (disableOriginalRenderers && _lastCount > 0)
        {
            IReadOnlyList<RockController> all = RockController.All;
            ApplyRenderersOff(all, false);
        }
    }

    static void ApplyRenderersOff(IReadOnlyList<RockController> all, bool off)
    {
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i] == null) continue;
            var rends = all[i].GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends)
            {
                if (r == null) continue;
                r.enabled = !off;
            }
        }
    }

    bool EnsureInit(int n, IReadOnlyList<RockController> all)
    {
        if (instancedShader == null)
            instancedShader = Shader.Find("PrisonLife/Rendering/RockFieldInstanced");
        if (instancedShader == null)
        {
            Debug.LogError("RockFieldInstancedRenderer: 셰이더 PrisonLife/Rendering/RockFieldInstanced 를 찾을 수 없습니다.");
            return false;
        }
        if (sourceMesh == null)
        {
            for (int i = 0; i < all.Count; i++)
            {
                if (all[i] == null) continue;
                var mf = all[i].GetComponentInChildren<MeshFilter>(true);
                if (mf != null && mf.sharedMesh != null)
                {
                    sourceMesh = mf.sharedMesh;
                    break;
                }
            }
            if (sourceMesh == null)
            {
                var fallback = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
                if (fallback != null) sourceMesh = fallback;
            }
        }
        if (sourceMesh == null)
        {
            Debug.LogError("RockFieldInstancedRenderer: sourceMesh 를 설정하거나 씬에 MeshFilter Rock 이 필요합니다.");
            return false;
        }

        if (_mat == null)
        {
            _mat = new Material(instancedShader);
            _mat.enableInstancing = true;
            _copiedAlbedoFromRockMaterial = false;
        }
        if (_mat != null && !_copiedAlbedoFromRockMaterial)
        {
            TryCopyAlbedoFromFirstRockMaterial(all);
            _copiedAlbedoFromRockMaterial = true;
        }

        if (_matrices == null || _matrices.count != n)
        {
            if (_matrices != null) _matrices.Dispose();
            _matrices = new ComputeBuffer(n, sizeof(float) * 16, ComputeBufferType.Structured);
        }
        if (_args == null)
            _args = new GraphicsBuffer(GraphicsBuffer.Target.IndirectArguments, 1, sizeof(uint) * 5);

        _mpb ??= new MaterialPropertyBlock();

        WriteArgs(n);
        return true;
    }

    void EnsureInstanceMeshRootCache(int n, IReadOnlyList<RockController> all)
    {
        if (n <= 0) return;
        if (_instanceMeshRoots != null && _instanceMeshRoots.Length == n)
            return;
        _instanceMeshRoots = new Transform[n];
        for (int i = 0; i < n; i++)
        {
            var r = all[i];
            if (r == null) continue;
            _instanceMeshRoots[i] = ResolveInstanceMeshRoot(r);
        }
    }

    /// <summary>sourceMesh(인스턴스에 쓰는 메쉬)가 붙은 Transform — 씬에 자식이 여럿이어도 동일 메쉬에 맞춤</summary>
    Transform ResolveInstanceMeshRoot(RockController r)
    {
        if (sourceMesh != null)
        {
            var filters = r.GetComponentsInChildren<MeshFilter>(true);
            for (int j = 0; j < filters.Length; j++)
            {
                var mf = filters[j];
                if (mf != null && mf.sharedMesh == sourceMesh)
                    return mf.transform;
            }
        }
        var any = r.GetComponentInChildren<MeshFilter>(true);
        return any != null ? any.transform : r.transform;
    }

    /// <summary>씬 Rock과 동일한 병(텍스처) 색에 맞추기: 첫 MeshRenderer의 URP/레거시 알베도 복사</summary>
    void TryCopyAlbedoFromFirstRockMaterial(IReadOnlyList<RockController> all)
    {
        for (int i = 0; i < all.Count; i++)
        {
            if (all[i] == null) continue;
            var mr = all[i].GetComponentInChildren<MeshRenderer>(true);
            if (mr == null) continue;
            var src = mr.sharedMaterial;
            if (src == null) continue;
            if (src.HasProperty("_BaseMap"))
            {
                var t = src.GetTexture("_BaseMap");
                if (t != null) _mat.SetTexture(sBaseMap, t);
            }
            else if (src.HasProperty("_MainTex"))
            {
                var t = src.GetTexture("_MainTex");
                if (t != null) _mat.SetTexture(sBaseMap, t);
            }
            if (src.HasProperty("_BaseColor")) _mat.SetColor(sBaseColor, src.GetColor("_BaseColor"));
            else if (src.HasProperty("_Color")) _mat.SetColor(sBaseColor, src.GetColor("_Color"));
            if (src.HasProperty("_BaseMap_ST")) _mat.SetVector(sBaseMapSt, src.GetVector("_BaseMap_ST"));
            else if (src.HasProperty("_MainTex_ST")) _mat.SetVector(sBaseMapSt, src.GetVector("_MainTex_ST"));
            return;
        }
    }

    void WriteArgs(int n)
    {
        uint c = (uint)n;
        if (_argsInstanceCount == c)
            return;
        // D3D/플랫폼별 struct 패딩 이슈를 피하기 위해 Unity 문서에 나온 5*uint (indexed indirect) 사용
        var a = new uint[5];
        a[0] = sourceMesh.GetIndexCount(0);
        a[1] = c;
        a[2] = (uint)sourceMesh.GetIndexStart(0);
        a[3] = (uint)sourceMesh.GetBaseVertex(0);
        a[4] = 0;
        _args.SetData(a);
        _argsInstanceCount = c;
    }

    static Bounds CullingBoundsForRocks(int n, IReadOnlyList<RockController> all)
    {
        if (n <= 0)
            return new Bounds(Vector3.zero, Vector3.one * 4f);
        int first = 0;
        while (first < n && all[first] == null) first++;
        if (first >= n) return new Bounds(Vector3.zero, Vector3.one * 4f);
        var b = new Bounds(all[first].transform.position, Vector3.zero);
        for (int i = first + 1; i < n; i++)
        {
            if (all[i] == null) continue;
            b.Encapsulate(all[i].transform.position);
            var s = all[i].transform.lossyScale;
            float ext = Mathf.Max(s.x, Mathf.Max(s.y, s.z)) * 0.5f;
            b.Encapsulate(new Bounds(all[i].transform.position, Vector3.one * ext * 2f));
        }
        b.Expand(2f);
        return b;
    }

    void BindInstanceBuffersToMpb()
    {
        if (_matrices == null || _activeBuf == null) return;
        _mpb.SetBuffer(sMatrices, _matrices);
        _mpb.SetBuffer(sActive, _activeBuf);
    }

    static int CullingLayerForRocks(int n, IReadOnlyList<RockController> all)
    {
        for (int i = 0; i < n; i++)
        {
            if (all[i] != null)
                return all[i].gameObject.layer;
        }
        return 0;
    }

    void DrawMeshInstancedGraphics(Bounds bounds, int layer, Camera cam)
    {
        if (_matrices == null || _args == null || _activeBuf == null)
            return;
        Graphics.DrawMeshInstancedIndirect(
            sourceMesh,
            0,
            _mat,
            bounds,
            _args,
            0,
            _mpb,
            ShadowCastingMode.Off, // SRP: RockFieldInstancedRenderFeature의 ShadowCaster 패스가 투영(중복 방지)
            true,
            layer,
            cam,
            LightProbeUsage.Off,
            null);
    }

    static Camera GetAnyCamera()
    {
        if (Camera.main != null)
            return Camera.main;
#if UNITY_2023_1_OR_NEWER
        var list = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
        return list is { Length: > 0 } ? list[0] : null;
#else
        return UnityEngine.Object.FindObjectOfType<Camera>();
#endif
    }

    void ReleaseBuffers()
    {
        if (_activeBuf != null) { _activeBuf.Dispose(); _activeBuf = null; }
        if (_matrices != null) { _matrices.Dispose(); _matrices = null; }
        if (_args != null) { _args.Dispose(); _args = null; }
        _argsInstanceCount = 0;
        if (_mat != null)
        {
            if (Application.isPlaying) Destroy(_mat);
            else DestroyImmediate(_mat);
            _mat = null;
            _copiedAlbedoFromRockMaterial = false;
        }
    }
}
