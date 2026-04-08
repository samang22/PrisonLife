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

    [Header("흔들림 연출")]
    public float shakeAmplitude = 0.08f;
    public float shakeDuration = 0.6f;

    private int _maxCapacity;
    private int _expansionLevel;
    private int _currentCount;

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
        prisoner.MoveToCell(targetPos, () =>
        {
            prisoner.transform.SetParent(transform);
        });

        _prisoners.Add(prisoner);
        _currentCount++;

        GameManager.Instance?.NotifyPrisonerAdded(_currentCount, _maxCapacity);
    }

    public void Expand()
    {
        _expansionLevel++;
        _maxCapacity = baseCapacity + _expansionLevel * capacityPerExpansion;
    }

    // 콜라이더 영역 내 랜덤 위치 반환
    private Vector3 GetRandomPositionInArea()
    {
        if (_area == null)
            return transform.position;

        Bounds bounds = _area.bounds;
        return new Vector3(
            Random.Range(bounds.min.x, bounds.max.x),
            bounds.min.y,
            Random.Range(bounds.min.z, bounds.max.z)
        );
    }

    private IEnumerator ShakeRoutine()
    {
        _isShaking = true;
        Vector3 originalPos = transform.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            transform.localPosition = originalPos + (Vector3)Random.insideUnitCircle * shakeAmplitude;
            elapsed += Time.deltaTime;
            yield return null;
        }

        transform.localPosition = originalPos;
        _isShaking = false;
    }

    public int MaxCapacity => _maxCapacity;
    public int CurrentCount => _currentCount;
    public bool IsFull => _currentCount >= _maxCapacity;
}
