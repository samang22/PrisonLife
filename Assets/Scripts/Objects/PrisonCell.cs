using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// 감옥 수용 구역 - 수감된 죄수가 동선을 따라 이동 후 정지
/// 최대 수용 시 흔들림 연출
/// </summary>
public class PrisonCell : MonoBehaviour
{
    public static PrisonCell Instance { get; private set; }

    [Header("수용 설정")]
    public int baseCapacity = 5;
    public int capacityPerExpansion = 5;

    [Header("수감 위치 (순서대로)")]
    public Transform[] cellSlots;

    [Header("UI")]
    public TextMeshPro capacityText;

    [Header("흔들림 연출")]
    public float shakeAmplitude = 0.08f;
    public float shakeDuration = 0.6f;

    private int _maxCapacity;
    private int _currentCount;
    private int _expansionLevel;

    private readonly List<PrisonerController> _prisoners = new List<PrisonerController>();
    private bool _isShaking;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        _maxCapacity = baseCapacity;
    }

    private void Start()
    {
        RefreshUI();
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

        Transform slot = cellSlots != null && _currentCount < cellSlots.Length
            ? cellSlots[_currentCount]
            : null;

        if (slot != null)
        {
            prisoner.MoveToCell(slot.position, () =>
            {
                prisoner.transform.SetParent(transform);
            });
        }

        _prisoners.Add(prisoner);
        _currentCount++;

        GameManager.Instance?.NotifyPrisonerAdded(_currentCount, _maxCapacity);
        RefreshUI();
    }

    public void Expand()
    {
        _expansionLevel++;
        _maxCapacity = baseCapacity + _expansionLevel * capacityPerExpansion;
        RefreshUI();
    }

    private void RefreshUI()
    {
        if (capacityText != null)
            capacityText.text = $"{_currentCount}/{_maxCapacity}";
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
