using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 죄수 대기 구역
/// - 자동으로 Prisoner를 스폰하여 줄 세움
/// - 플레이어가 수갑을 납품하면 맨 앞 죄수부터 순서대로 체포
/// - 체포 후 남은 죄수들이 한 칸씩 앞으로 이동
/// </summary>
public class PrisonerQueue : MonoBehaviour, IInteractable
{
    [Header("스폰 설정")]
    public GameObject prisonerPrefab;
    public int maxQueueSize = 5;
    public float spawnInterval = 3f;

    [Header("위치 바인딩")]
    public Transform spawnPoint;         // 죄수가 생성되는 위치
    public Transform queueStartPoint;    // 줄의 맨 앞 (체포 위치)
    public Transform prisonCellEntrance; // 체포 후 죄수가 이동할 감옥 입구

    [Header("줄 서기 설정")]
    public float queueSpacing = 1.2f;   // 줄 간격
    public float shiftSpeed = 5f;       // 앞으로 이동 속도

    [Header("연결 - 납품 구역")]
    public HandcuffSubmitRelay handcuffSubmitZone;

    [Header("연결 - 출력 구역")]
    public PrisonCell prisonCell;
    public DollarZone dollarZone;

    [Header("설정")]
    public int rewardPerPrisoner = 20;
    public float deliveryInterval = 0.25f;
    public float arrestInterval = 0.5f;

    private readonly List<PrisonerController> _waitingPrisoners = new List<PrisonerController>();
    private int _storedHandcuffs;
    private float _deliveryTimer;
    private float _arrestTimer;
    private float _spawnTimer;

    private void Start()
    {
        // HandcuffSubmitZone이 이 PrisonerQueue를 참조하도록 자동 설정
        // (Awake 대신 Start 사용 - HandcuffSubmitRelay.Awake 이후 실행 보장)
        if (handcuffSubmitZone != null)
            handcuffSubmitZone.queue = this;
    }

    private void Update()
    {
        TrySpawn();

        _arrestTimer += Time.deltaTime;
        if (_arrestTimer >= arrestInterval)
        {
            _arrestTimer = 0f;
            TryArrestNext();
        }

        UpdateQueuePositions();
    }

    // ── 자동 스폰 ──
    private void TrySpawn()
    {
        if (prisonerPrefab == null) return;
        if (_waitingPrisoners.Count >= maxQueueSize) return;

        _spawnTimer += Time.deltaTime;
        if (_spawnTimer < spawnInterval) return;
        _spawnTimer = 0f;

        // spawnPoint가 지정된 경우 해당 위치에 스폰, 아니면 줄 마지막 위치에 스폰
        Vector3 pos = spawnPoint != null
            ? spawnPoint.position
            : GetQueuePosition(_waitingPrisoners.Count);

        GameObject obj = Instantiate(prisonerPrefab, pos, Quaternion.identity);
        PrisonerController prisoner = obj.GetComponent<PrisonerController>();
        if (prisoner != null)
            _waitingPrisoners.Add(prisoner);
    }

    // ── 줄 위치 계산 (앞=0번, 뒤로 갈수록 index 증가) ──
    // SpawnPoint와 QueueStartPoint가 모두 지정된 경우: 두 지점을 잇는 직선 위에 배치
    private Vector3 GetQueuePosition(int index)
    {
        if (queueStartPoint == null)
            return transform.position + transform.forward * -(index * queueSpacing);

        if (spawnPoint != null)
        {
            Vector3 dir = (spawnPoint.position - queueStartPoint.position).normalized;
            return queueStartPoint.position + dir * (index * queueSpacing);
        }

        return queueStartPoint.position + queueStartPoint.forward * -(index * queueSpacing);
    }

    // ── 매 프레임 줄 서기 위치로 부드럽게 이동 + 진행 방향을 바라보도록 회전 ──
    private void UpdateQueuePositions()
    {
        // 줄의 진행 방향: SpawnPoint → QueueStartPoint
        Vector3 faceDir = GetQueueFaceDirection();

        for (int i = 0; i < _waitingPrisoners.Count; i++)
        {
            if (_waitingPrisoners[i] == null) continue;

            Vector3 target = GetQueuePosition(i);
            _waitingPrisoners[i].transform.position = Vector3.MoveTowards(
                _waitingPrisoners[i].transform.position,
                target,
                shiftSpeed * Time.deltaTime
            );

            if (faceDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(faceDir);
                _waitingPrisoners[i].transform.rotation = Quaternion.Slerp(
                    _waitingPrisoners[i].transform.rotation,
                    targetRot,
                    10f * Time.deltaTime
                );
            }
        }
    }

    // SpawnPoint → QueueStartPoint 방향 (줄이 바라보는 방향)
    private Vector3 GetQueueFaceDirection()
    {
        if (queueStartPoint == null) return transform.forward;
        if (spawnPoint == null) return queueStartPoint.forward;

        Vector3 dir = (queueStartPoint.position - spawnPoint.position);
        dir.y = 0f;
        return dir.normalized;
    }

    // ── 플레이어 수갑 납품 ──
    public void OnInteract(PlayerController player)
    {
        if (player.HandcuffCount <= 0) return;

        _deliveryTimer += Time.deltaTime;
        if (_deliveryTimer < deliveryInterval) return;
        _deliveryTimer = 0f;

        player.TakeHandcuff(1);
        _storedHandcuffs++;
        handcuffSubmitZone?.RefreshVisual(_storedHandcuffs);
    }

    // ── 맨 앞 죄수 체포 ──
    private void TryArrestNext()
    {
        if (_storedHandcuffs <= 0) return;
        if (_waitingPrisoners.Count == 0) return;

        // PrisonCell이 포화 상태면 체포 중단 (수갑 소모 없음)
        if (prisonCell != null && prisonCell.IsFull) return;

        PrisonerController prisoner = _waitingPrisoners[0];
        if (prisoner == null || prisoner.IsArrested)
        {
            _waitingPrisoners.RemoveAt(0);
            return;
        }

        _storedHandcuffs--;
        handcuffSubmitZone?.RefreshVisual(_storedHandcuffs);
        prisoner.OnHandcuffDelivered();
        _waitingPrisoners.RemoveAt(0);

        dollarZone?.AddDollars(rewardPerPrisoner);

        // prisonCellEntrance가 지정된 경우: 입구까지 이동 후 수감 처리
        // 지정되지 않은 경우: 즉시 수감 처리
        if (prisonCellEntrance != null)
        {
            prisoner.MoveToCell(prisonCellEntrance.position, () =>
            {
                prisonCell?.ReceivePrisoner(prisoner);
            });
        }
        else
        {
            prisonCell?.ReceivePrisoner(prisoner);
        }
    }

    public int StoredHandcuffs => _storedHandcuffs;
    public int WaitingCount => _waitingPrisoners.Count;
}
