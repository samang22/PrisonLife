using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 죄수 대기 구역 - 플레이어가 수갑을 납품하면 저장,
/// 저장된 수갑이 있으면 맨 앞 죄수부터 자동 체포
/// </summary>
public class PrisonerQueue : MonoBehaviour, IInteractable
{
    [Header("죄수 목록 (씬에 배치된 순서대로 등록)")]
    public List<PrisonerController> waitingPrisoners = new List<PrisonerController>();

    [Header("연결")]
    public PrisonCell prisonCell;
    public DollarZone dollarZone;

    [Header("설정")]
    public int rewardPerPrisoner = 20;
    public float deliveryInterval = 0.25f;  // 수갑 납품 주기
    public float arrestInterval = 0.5f;     // 수갑 → 체포 처리 주기

    private int _storedHandcuffs;
    private float _deliveryTimer;
    private float _arrestTimer;

    private void Update()
    {
        // 저장된 수갑으로 순서대로 자동 체포
        _arrestTimer += Time.deltaTime;
        if (_arrestTimer >= arrestInterval)
        {
            _arrestTimer = 0f;
            TryArrestNext();
        }
    }

    // 플레이어가 구역에 있으면 수갑을 1개씩 납품
    public void OnInteract(PlayerController player)
    {
        if (player.HandcuffCount <= 0) return;

        _deliveryTimer += Time.deltaTime;
        if (_deliveryTimer < deliveryInterval) return;
        _deliveryTimer = 0f;

        player.TakeHandcuff(1);
        _storedHandcuffs++;
    }

    private void TryArrestNext()
    {
        if (_storedHandcuffs <= 0) return;
        if (waitingPrisoners.Count == 0) return;

        PrisonerController prisoner = waitingPrisoners[0];
        if (prisoner == null || prisoner.IsArrested)
        {
            waitingPrisoners.RemoveAt(0);
            return;
        }

        _storedHandcuffs--;
        prisoner.OnHandcuffDelivered();
        waitingPrisoners.RemoveAt(0);

        // 달러 구역에 보상 추가
        dollarZone?.AddDollars(rewardPerPrisoner);

        // 감옥으로 이동
        prisonCell?.ReceivePrisoner(prisoner);
    }

    public void AddPrisoner(PrisonerController prisoner)
    {
        waitingPrisoners.Add(prisoner);
    }

    public int StoredHandcuffs => _storedHandcuffs;
    public int WaitingCount => waitingPrisoners.Count;
}
