using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 제작대 - 철광석을 받아 3초 후 수갑으로 변환하여 픽업 구역에 배출
/// 플레이어 직접 납품 / WorkerController 자동 납품 모두 지원
/// </summary>
public class HandcuffMachine : MonoBehaviour, IInteractable
{
    [Header("변환 설정")]
    public float conversionTime = 3f;
    public int maxQueue = 10;

    [Header("연결")]
    public HandcuffPickupZone pickupZone;

    [Header("스택 루트 (날아온 철광석이 쌓이는 위치)")]
    public Transform stackRoot;

    [Header("납품 주기 (플레이어)")]
    public float depositInterval = 0.3f;

    private readonly List<float> _queue = new List<float>();
    private readonly List<GameObject> _stackedOres = new List<GameObject>();
    private float _depositTimer;

    private void Update()
    {
        for (int i = _queue.Count - 1; i >= 0; i--)
        {
            if (Time.time >= _queue[i])
            {
                _queue.RemoveAt(i);
                RemoveOldestStackedOre();
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

        EnqueueConversion(null);
    }

    // WorkerController에서 날아온 철광석 오브젝트를 수신
    public void ReceiveIronOre(GameObject ore)
    {
        if (_queue.Count >= maxQueue)
        {
            Destroy(ore);
            return;
        }

        if (stackRoot != null && ore != null)
        {
            ore.transform.SetParent(stackRoot);
            ore.transform.localPosition = Vector3.up * (_stackedOres.Count * 0.12f);
            ore.transform.localRotation = Quaternion.identity;
            _stackedOres.Add(ore);
        }

        EnqueueConversion(ore);
    }

    private void EnqueueConversion(GameObject ore)
    {
        float completionTime = Time.time + conversionTime;
        _queue.Add(completionTime);

        // 플레이어 납품은 시각 오브젝트가 없으므로 null 추가로 인덱스 맞춤
        if (ore == null)
            _stackedOres.Add(null);
    }

    private void RemoveOldestStackedOre()
    {
        if (_stackedOres.Count == 0) return;
        GameObject oldest = _stackedOres[0];
        _stackedOres.RemoveAt(0);
        if (oldest != null)
            Destroy(oldest);
    }

    public int QueueCount => _queue.Count;
    public Transform StackRoot => stackRoot;
}
