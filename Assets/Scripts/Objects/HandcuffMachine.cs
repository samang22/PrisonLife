using System.Collections.Generic;
using UnityEngine;

public class HandcuffMachine : MonoBehaviour, IInteractable, IResettable
{
    [Header("변환 설정")]
    public float conversionTime = 3f;
    public int maxQueue = 10;

    [Header("연결 - 납품 구역")]
    public IronOreSubmitRelay ironOreSubmitZone;

    [Header("연결 - 출력 구역")]
    public HandcuffPickupZone pickupZone;

    [Header("Worker 납품 스택 루트")]
    public Transform workerStackRoot;

    [Header("납품 주기 (플레이어)")]
    public float depositInterval = 0.3f;

    [Header("제작 중 연출")]
    [Tooltip("비우면 이 오브젝트 자신의 Transform을 사용합니다")]
    public Transform scaleTarget;
    public float pulseSpeed = 6f;
    public float pulseAmount = 0.08f;

    // 변환 완료 시각 큐 (플레이어/워커 공통)
    private readonly List<float> _queue = new List<float>();
    // 플레이어 납품은 null, 워커 납품은 GameObject로 구분
    private readonly List<GameObject> _stackedOres = new List<GameObject>();

    private int _playerOreCount;
    private float _depositTimer;

    private Transform _scaleRoot;
    private Vector3 _baseLocalScale;

    private void Awake()
    {
        _scaleRoot = scaleTarget != null ? scaleTarget : transform;
        _baseLocalScale = _scaleRoot.localScale;
        ResetRegistry.Register(this);
    }

    private void OnDestroy() => ResetRegistry.Unregister(this);

    public void ResetState() => ResetMachineState();

    private void Start()
    {
        if (ironOreSubmitZone != null)
            ironOreSubmitZone.machine = this;
    }

    private void Update()
    {
        // FIFO 순서로 변환 완료 처리
        while (_queue.Count > 0 && Time.time >= _queue[0])
        {
            _queue.RemoveAt(0);
            bool wasPlayerOre = RemoveOldestStackedOre();

            if (wasPlayerOre)
            {
                _playerOreCount--;
                ironOreSubmitZone?.RefreshVisual(_playerOreCount);
            }

            pickupZone?.AddHandcuff(1);
        }

        UpdateCraftingScalePulse();
    }

    private void UpdateCraftingScalePulse()
    {
        if (_scaleRoot == null) return;

        if (_queue.Count > 0)
        {
            float mult = 1f + Mathf.Sin(Time.time * pulseSpeed) * pulseAmount;
            _scaleRoot.localScale = _baseLocalScale * mult;
        }
        else
            _scaleRoot.localScale = _baseLocalScale;
    }

    public void OnInteract(PlayerController player)
    {
        if (_queue.Count >= maxQueue) return;

        _depositTimer += Time.deltaTime;
        if (_depositTimer < depositInterval) return;
        _depositTimer = 0f;

        if (!player.TakeIronOre(1)) return;

        _playerOreCount++;
        ironOreSubmitZone?.RefreshVisual(_playerOreCount);
        EnqueueConversion(null);
    }

    // WorkerController에서 날아온 광석 수신
    public void ReceiveIronOre(GameObject ore)
    {
        if (_queue.Count >= maxQueue)
        {
            Destroy(ore);
            return;
        }

        if (ore != null) Destroy(ore);

        _playerOreCount++;
        ironOreSubmitZone?.RefreshVisual(_playerOreCount);
        EnqueueConversion(null);
    }

    private void EnqueueConversion(GameObject ore)
    {
        _queue.Add(Time.time + conversionTime);
        _stackedOres.Add(ore);
    }

    private bool RemoveOldestStackedOre()
    {
        if (_stackedOres.Count == 0) return false;

        GameObject oldest = _stackedOres[0];
        _stackedOres.RemoveAt(0);

        bool isPlayerOre = oldest == null;
        if (!isPlayerOre)
            Destroy(oldest);

        return isPlayerOre;
    }

    public int QueueCount => _queue.Count;
    public Transform StackRoot => workerStackRoot;

    public void ResetMachineState()
    {
        _queue.Clear();
        foreach (GameObject ore in _stackedOres)
        {
            if (ore != null) Destroy(ore);
        }
        _stackedOres.Clear();
        _playerOreCount = 0;
        _depositTimer = 0f;
        ironOreSubmitZone?.RefreshVisual(0);

        if (_scaleRoot != null)
            _scaleRoot.localScale = _baseLocalScale;
    }
}
