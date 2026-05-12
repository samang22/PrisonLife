using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// RockController.All 행렬 → DrawMeshInstanced (1023 단위 배치). URP 기본 그림자. 원본 Renderer 끄는 건 선택.
[DefaultExecutionOrder(50)]
public class RockFieldInstancedRenderer : MonoBehaviour
{
    const int MaxInstancesPerBatch = 1023;

    [Header("RockController 씬")]
    [Tooltip("켜면 Play 중 각 Rock MeshRenderer Off. 실패하면 안 보일 수 있어 기본 끔.")]
    public bool disableOriginalRenderers = false;

    [Tooltip("비우면 All[0]에서 메쉬를 찾습니다.")]
    public Mesh sourceMesh;

    [Tooltip("미사용")]
    public ComputeShader rockCompute;
    [Tooltip("비우면 PrisonLife/Rendering/RockFieldInstanced")]
    public Shader instancedShader;

    [Header("GPU 변형")]
    [Min(0f)] public float wobbleHeight = 0.08f;

    [Header("렌더 대상")]
    [Tooltip("비우면 URP Base 카메라마다.")]
    public Camera targetCamera;

    [Header("디버그(읽기 전용)")]
    [Tooltip("Play 중 마지막 프레임 All.Count")]
    [SerializeField] int _debugLastRegisteredRockCount;

    Matrix4x4[] _matrixUpload;
    Matrix4x4[] _batchMatrices;
    Material _mat;
    static readonly int sActive = Shader.PropertyToID("_InstanceActive");
    static readonly int sBatchInstanceOffset = Shader.PropertyToID("_BatchInstanceOffset");
    static readonly int sBaseMap = Shader.PropertyToID("_BaseMap");
    static readonly int sBaseColor = Shader.PropertyToID("_BaseColor");
    static readonly int sBaseMapSt = Shader.PropertyToID("_BaseMap_ST");
    bool _copiedAlbedoFromRockMaterial;
    ComputeBuffer _activeBuf;
    int _lastCount = -1;
    int _readyCount;
    // 메쉬 필터 트랜스폼 캐시 (루트만 쓰면 어색할 때 대비)
    Transform[] _instanceMeshRoots;

    // disableOriginalRenderers일 때 원본 렌더 켤지 결정 등에 참고
    public static bool IsOriginalRockRenderersSuppressed()
    {
        var rf = UnityEngine.Object.FindObjectOfType<RockFieldInstancedRenderer>();
        return rf != null && rf.disableOriginalRenderers;
    }

    void OnDisable()
    {
        _instanceMeshRoots = null;
        ReleaseBuffers();
    }

    // 버퍼/오프셋은 Material (MPB 버퍼가 안 묶이던 테스트 있음)
    void SubmitDrawMeshInstanced(IReadOnlyList<RockController> all)
    {
        if (_readyCount <= 0 || _mat == null || sourceMesh == null || _activeBuf == null)
            return;
        if (_matrixUpload == null || _matrixUpload.Length < _readyCount)
            return;
        if (all == null)
            return;

        if (_batchMatrices == null || _batchMatrices.Length != MaxInstancesPerBatch)
            _batchMatrices = new Matrix4x4[MaxInstancesPerBatch];

        int n = _readyCount;
        int drawLayer = CullingLayerForRocks(n, all);
        _mat.SetBuffer(sActive, _activeBuf);

        foreach (var camera in EnumerateDrawCameras())
        {
            if (camera == null || !camera.isActiveAndEnabled)
                continue;
            for (int start = 0; start < n; start += MaxInstancesPerBatch)
            {
                int batch = Mathf.Min(MaxInstancesPerBatch, n - start);
                Array.Copy(_matrixUpload, start, _batchMatrices, 0, batch);
                _mat.SetInt(sBatchInstanceOffset, start);
                // 12인자 오버로드만 존재
                Graphics.DrawMeshInstanced(
                    sourceMesh,
                    0,
                    _mat,
                    _batchMatrices,
                    batch,
                    null,
                    ShadowCastingMode.On,
                    true,
                    drawLayer,
                    camera,
                    LightProbeUsage.Off,
                    null);
            }
        }
    }

    IEnumerable<Camera> EnumerateDrawCameras()
    {
        if (targetCamera != null)
        {
            yield return targetCamera;
            yield break;
        }
        bool any = false;
#if UNITY_2023_1_OR_NEWER
        var list = UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None);
#else
        var list = UnityEngine.Object.FindObjectsOfType<Camera>();
#endif
        if (list != null)
        {
            for (int i = 0; i < list.Length; i++)
            {
                var c = list[i];
                if (c == null || !c.isActiveAndEnabled)
                    continue;
                if (c.cameraType == CameraType.Preview)
                    continue;
                var urp = c.GetUniversalAdditionalCameraData();
                if (urp == null || urp.renderType != CameraRenderType.Base)
                    continue;
                any = true;
                yield return c;
            }
        }
        if (any)
            yield break;
        if (Camera.main != null && Camera.main.isActiveAndEnabled)
            yield return Camera.main;
    }

    void LateUpdate()
    {
        if (!Application.isPlaying)
            return;
        IReadOnlyList<RockController> all = RockController.All;
        int n = all.Count;
        if (n == 0)
        {
            _readyCount = 0;
            _debugLastRegisteredRockCount = 0;
            return;
        }

        if (!EnsureInit(n, all))
        {
            _readyCount = 0;
            _debugLastRegisteredRockCount = 0;
            return;
        }

        EnsureInstanceMeshRootCache(n, all);

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

        _readyCount = n;
        _debugLastRegisteredRockCount = n;
        SubmitDrawMeshInstanced(all);
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

    static int CullingLayerForRocks(int n, IReadOnlyList<RockController> all)
    {
        for (int i = 0; i < n; i++)
        {
            if (all[i] != null)
                return all[i].gameObject.layer;
        }
        return 0;
    }

    void ReleaseBuffers()
    {
        if (_activeBuf != null) { _activeBuf.Dispose(); _activeBuf = null; }
        if (_mat != null)
        {
            if (Application.isPlaying) Destroy(_mat);
            else DestroyImmediate(_mat);
            _mat = null;
            _copiedAlbedoFromRockMaterial = false;
        }
    }
}
