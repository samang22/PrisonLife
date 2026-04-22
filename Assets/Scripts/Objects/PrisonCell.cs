using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PrisonCell : MonoBehaviour
{
    public static PrisonCell Instance { get; private set; }

    [Header("수용 설정")]
    public int baseCapacity = 5;
    public int capacityPerExpansion = 5;

    [Header("확장 연출")]
    public Vector3 expansionPerLevel = new Vector3(2f, 0f, 2f);
    public Transform floorVisual;

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

    private Vector3 _shakeBaseLocalPosition;

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

        Transform shakeT = shakeTarget != null ? shakeTarget : transform;
        _shakeBaseLocalPosition = shakeT.localPosition;
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

        if (_area != null)
            _area.size = _baseColliderSize + expansionPerLevel * _expansionLevel;

        // 바닥 비주얼을 콜라이더 비율에 맞게 동기화
        if (floorVisual != null && _baseColliderSize.x > 0 && _baseColliderSize.z > 0)
        {
            Vector3 newSize = _baseColliderSize + expansionPerLevel * _expansionLevel;
            floorVisual.localScale = new Vector3(
                _baseFloorScale.x * (newSize.x / _baseColliderSize.x),
                _baseFloorScale.y,
                _baseFloorScale.z * (newSize.z / _baseColliderSize.z)
            );
        }

        GameManager.Instance?.NotifyPrisonerAdded(_currentCount, _maxCapacity);
    }

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

        // IsFull 상태가 유지되는 동안 계속 반복
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

    public void ResetToInitialState()
    {
        StopAllCoroutines();
        _isShaking = false;

        Transform shakeT = shakeTarget != null ? shakeTarget : transform;
        shakeT.localPosition = _shakeBaseLocalPosition;

        foreach (PrisonerController p in _prisoners)
        {
            if (p != null) Destroy(p.gameObject);
        }
        _prisoners.Clear();
        _currentCount = 0;
        _expansionLevel = 0;
        _maxCapacity = baseCapacity;

        if (_area != null)
            _area.size = _baseColliderSize;
        if (floorVisual != null)
            floorVisual.localScale = _baseFloorScale;

        GameManager.Instance?.NotifyPrisonerAdded(0, _maxCapacity);
    }
}
