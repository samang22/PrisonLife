using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 제작대 - 플레이어가 철광석을 납품하면 3초 후 수갑으로 변환하여 픽업 구역에 배출
/// 선납품 가능: 큐에 철광석을 쌓아두고 순서대로 변환
/// </summary>
public class HandcuffMachine : MonoBehaviour, IInteractable
{
    [Header("변환 설정")]
    public float conversionTime = 3f;
    public int maxQueue = 10;

    [Header("연결")]
    public HandcuffPickupZone pickupZone;

    [Header("납품 주기")]
    public float depositInterval = 0.3f;

    // 각 아이템의 변환 완료 예정 시간 목록
    private readonly List<float> _queue = new List<float>();
    private float _depositTimer;

    private void Update()
    {
        // 완료된 변환 처리 (가장 오래된 것부터)
        for (int i = _queue.Count - 1; i >= 0; i--)
        {
            if (Time.time >= _queue[i])
            {
                _queue.RemoveAt(i);
                pickupZone?.AddHandcuff(1);
            }
        }
    }

    // 플레이어가 제작대 구역에 있는 동안 자동 납품
    public void OnInteract(PlayerController player)
    {
        if (_queue.Count >= maxQueue) return;

        _depositTimer += Time.deltaTime;
        if (_depositTimer < depositInterval) return;
        _depositTimer = 0f;

        if (!player.TakeIronOre(1)) return;

        // 납품된 순서대로 conversionTime 후 완료
        float completionTime = Time.time + conversionTime;
        _queue.Add(completionTime);
    }

    public int QueueCount => _queue.Count;
}
