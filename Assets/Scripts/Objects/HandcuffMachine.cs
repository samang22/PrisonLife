using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 제작대 - 철광석을 받아 변환 시간 후 수갑으로 변환하여 픽업 구역에 배출
/// 플레이어 직접 납품 / WorkerController 자동 납품 모두 지원
/// </summary>
public class HandcuffMachine : MonoBehaviour, IInteractable
{
    [Header("변환 설정")]
    public float conversionTime = 3f;
    public int maxQueue = 10;

    [Header("연결 - 납품 구역")]
    public IronOreSubmitRelay ironOreSubmitZone;

    [Header("연결 - 출력 구역")]
    public HandcuffPickupZone pickupZone;

    [Header("Worker 납품 스택 루트 (날아온 철광석이 쌓이는 위치)")]
    public Transform workerStackRoot;

    [Header("납품 주기 (플레이어)")]
    public float depositInterval = 0.3f;

    // 변환 완료 시각 큐 (플레이어/워커 공통)
    private readonly List<float> _queue = new List<float>();
    // 큐 항목별 오브젝트: 플레이어 납품=null, 워커 납품=GameObject
    private readonly List<GameObject> _stackedOres = new List<GameObject>();

    // 플레이어가 납품한 큐 내 철광석 수 (SubmitZone 시각화용)
    private int _playerOreCount;

    private float _depositTimer;

    private void Start()
    {
        if (ironOreSubmitZone != null)
            ironOreSubmitZone.machine = this;
    }

    private void Update()
    {
        // FIFO 순서로 변환 완료 처리 (큐 앞쪽부터)
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
    }

    // 플레이어가 납품 구역에 있는 동안 자동 납품
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

    // WorkerController에서 날아온 철광석 수신 - 플레이어 납품과 동일하게 카운트 기반 처리
    public void ReceiveIronOre(GameObject ore)
    {
        if (_queue.Count >= maxQueue)
        {
            Destroy(ore);
            return;
        }

        // 날아온 오브젝트는 제거하고 SubmitZone 시각화 카운트만 증가
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

    // 가장 오래된 항목 제거. 플레이어 납품(null)이면 true 반환
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
}
