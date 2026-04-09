using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 감옥 수용 구역 - 콜라이더 영역 안에 죄수가 위치하면 됨
/// 최대 수용 시 흔들림 연출
/// </summary>
public class PrisonCell : MonoBehaviour
{
    public static PrisonCell Instance { get; private set; }

    [Header("수용 설정")]
    public int baseCapacity = 5;
    public int capacityPerExpansion = 5;

    [Header("확장 연출")]
    public Vector3 expansionPerLevel = new Vector3(2f, 0f, 2f); // 단계당 콜라이더 size 증가량
    public Transform floorVisual;                                // 바닥 비주얼 오브젝트

    [Header("일꾼 고용 설정")]
    public IronOreSubmitRelay ironOreSubmitZone;
    public GameObject workerIronOrePrefab;

    [Header("죄수 배치 설정")]
    [Tooltip("죄수가 서는 Y 좌표 (바닥 높이에 맞게 조정)")]
    public float groundY = 0f;

    [Header("흔들림 연출")]
    public float shakeAmplitude = 0.3f;
    public float shakeDuration  = 0.6f;
    public float shakeFrequency = 30f;
    public Transform shakeTarget;

    private int _maxCapacity;
    private int _expansionLevel;
    private int _currentCount;
    private Vector3 _baseColliderSize;
    private Vector3 _baseFloorScale;

    private BoxCollider _area;
    private bool _isShaking;

    private readonly List<PrisonerController> _prisoners = new List<PrisonerController>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        _maxCapacity = baseCapacity;
        _area = GetComponent<BoxCollider>();

        if (_area != null)
            _baseColliderSize = _area.size;

        if (floorVisual != null)
            _baseFloorScale = floorVisual.localScale;
    }

    public void ReceivePrisoner(PrisonerController prisoner)
    {
        if (_currentCount >= _maxCapacity)
        {
            if (!_isShaking)
                StartCoroutine(ShakeRoutine());
            UIManager.Instance?.ShowMaxIndicator();
            return;
        }

        Vector3 targetPos = GetRandomPositionInArea();
        prisoner.MoveToCell(targetPos);

        _prisoners.Add(prisoner);
        _currentCount++;

        GameManager.Instance?.NotifyPrisonerAdded(_currentCount, _maxCapacity);

        // 수용량 한계 도달 시 흔들림 연출
        if (_currentCount >= _maxCapacity && !_isShaking)
        {
            Debug.Log($"[PrisonCell] 흔들림 시작 - shakeTarget: {(shakeTarget != null ? shakeTarget.name : "null(self)")}");
            StartCoroutine(ShakeRoutine());
        }
    }

    public void Expand()
    {
        _expansionLevel++;
        _maxCapacity = baseCapacity + _expansionLevel * capacityPerExpansion;

        // BoxCollider 확장
        if (_area != null)
            _area.size = _baseColliderSize + expansionPerLevel * _expansionLevel;

        // 바닥 비주얼 확장 (X, Z 비율을 콜라이더에 맞게 조정)
        if (floorVisual != null && _baseColliderSize.x > 0 && _baseColliderSize.z > 0)
        {
            Vector3 newSize = _baseColliderSize + expansionPerLevel * _expansionLevel;
            floorVisual.localScale = new Vector3(
                _baseFloorScale.x * (newSize.x / _baseColliderSize.x),
                _baseFloorScale.y,
                _baseFloorScale.z * (newSize.z / _baseColliderSize.z)
            );
        }

        // 수용량 변경을 UI에 즉시 반영
        GameManager.Instance?.NotifyPrisonerAdded(_currentCount, _maxCapacity);
    }

    // 콜라이더 영역 내 랜덤 위치 반환 (Y는 groundY로 고정)
    private Vector3 GetRandomPositionInArea()
    {
        if (_area == null)
            return new Vector3(transform.position.x, groundY, transform.position.z);

        Bounds bounds = _area.bounds;
        return new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            groundY,
            Random.Range(bounds.min.z, bounds.max.z)
        );
    }

    private IEnumerator ShakeRoutine()
    {
        _isShaking = true;
        Transform target = shakeTarget != null ? shakeTarget : transform;
        Vector3 originalPos = target.localPosition;
        float elapsed = 0f;

        // 가득 찬 상태가 유지되는 동안 계속 반복
        while (IsFull)
        {
            float offsetX = Mathf.Sin(elapsed * shakeFrequency) * shakeAmplitude;
            float offsetZ = Mathf.Cos(elapsed * shakeFrequency * 0.7f) * shakeAmplitude * 0.5f;
            target.localPosition = originalPos + new Vector3(offsetX, 0f, offsetZ);
            elapsed += Time.deltaTime;
            yield return null;
        }

        target.localPosition = originalPos;
        _isShaking = false;
    }

    /// <summary>
    /// PrisonCell에 수감된 죄수 중 WorkerController가 비활성 상태인 죄수를 count명 고용
    /// 고용 후 IronOreSubmitRelay를 타겟으로 설정하고 WorkerController 활성화
    /// </summary>
    public void HireWorkers(int count)
    {
        int hired = 0;
        foreach (PrisonerController p in _prisoners)
        {
            if (hired >= count) break;
            if (p == null) continue;

            WorkerController worker = p.GetComponent<WorkerController>();
            if (worker == null || worker.enabled) continue;

            worker.targetSubmitZone = ironOreSubmitZone;
            worker.ironOrePrefab    = workerIronOrePrefab;
            worker.enabled = true;
            hired++;
        }
    }

    public int MaxCapacity => _maxCapacity;
    public int CurrentCount => _currentCount;
    public bool IsFull => _currentCount >= _maxCapacity;

    /// <summary>
    /// 감옥 내 WorkerController가 비활성인 죄수(미고용)가 1명 이상 있는지 여부
    /// </summary>
    public bool HasUnhiredPrisoners
    {
        get
        {
            foreach (PrisonerController p in _prisoners)
            {
                if (p == null) continue;
                WorkerController w = p.GetComponent<WorkerController>();
                if (w != null && !w.enabled) return true;
            }
            return false;
        }
    }
}
