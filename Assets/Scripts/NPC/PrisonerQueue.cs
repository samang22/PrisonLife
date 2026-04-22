using System.Collections.Generic;
using UnityEngine;

public class PrisonerQueue : MonoBehaviour, IInteractable, IResettable
{
    [Header("스폰 설정")]
    public GameObject prisonerPrefab;
    public int maxQueueSize = 5;
    public float spawnInterval = 3f;

    [Header("위치 바인딩")]
    public Transform spawnPoint;
    public Transform queueStartPoint;
    public Transform prisonCellEntrance;

    [Header("줄 서기 설정")]
    public float queueSpacing = 1.2f;
    public float shiftSpeed = 5f;

    [Header("연결 - 납품 구역")]
    public HandcuffSubmitRelay handcuffSubmitZone;

    [Header("연결 - 출력 구역")]
    public PrisonCell prisonCell;
    public DollarZone dollarZone;

    [Header("설정")]
    public int rewardPerPrisoner = 20;
    public int handcuffsPerPrisoner = 4;
    public float deliveryInterval = 0.25f;
    public float arrestInterval = 0.5f;

    private readonly List<PrisonerController> _waitingPrisoners = new List<PrisonerController>();
    private int _storedHandcuffs;
    private float _deliveryTimer;
    private float _arrestTimer;
    private float _spawnTimer;

    private void Awake() => ResetRegistry.Register(this);
    private void OnDestroy() => ResetRegistry.Unregister(this);

    public void ResetState() => ResetQueueState();

    private void Start()
    {
        // HandcuffSubmitRelay.Awake 이후 실행을 보장하기 위해 Start 사용
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

    private void TrySpawn()
    {
        if (prisonerPrefab == null) return;
        if (_waitingPrisoners.Count >= maxQueueSize) return;

        _spawnTimer += Time.deltaTime;
        if (_spawnTimer < spawnInterval) return;
        _spawnTimer = 0f;

        Vector3 pos = spawnPoint != null
            ? spawnPoint.position
            : GetQueuePosition(_waitingPrisoners.Count);

        GameObject obj = Instantiate(prisonerPrefab, pos, Quaternion.identity);
        PrisonerController prisoner = obj.GetComponent<PrisonerController>();
        if (prisoner != null)
            _waitingPrisoners.Add(prisoner);
    }

    // SpawnPoint와 QueueStartPoint 사이 직선 위에 index 간격으로 배치
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

    private void UpdateQueuePositions()
    {
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

    private Vector3 GetQueueFaceDirection()
    {
        if (queueStartPoint == null) return transform.forward;
        if (spawnPoint == null) return queueStartPoint.forward;

        Vector3 dir = (queueStartPoint.position - spawnPoint.position);
        dir.y = 0f;
        return dir.normalized;
    }

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

    private void TryArrestNext()
    {
        if (_storedHandcuffs < handcuffsPerPrisoner) return;
        if (_waitingPrisoners.Count == 0) return;

        // PrisonCell 포화 시 수갑 소모 없이 체포 중단
        if (prisonCell != null && prisonCell.IsFull) return;

        PrisonerController prisoner = _waitingPrisoners[0];
        if (prisoner == null || prisoner.IsArrested)
        {
            _waitingPrisoners.RemoveAt(0);
            return;
        }

        _storedHandcuffs -= handcuffsPerPrisoner;
        handcuffSubmitZone?.RefreshVisual(_storedHandcuffs);
        prisoner.OnHandcuffDelivered();
        _waitingPrisoners.RemoveAt(0);

        dollarZone?.AddDollars(rewardPerPrisoner);

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

    // OfficerController에서 직접 호출 (플레이어 없이 수갑 납품)
    public void SubmitHandcuffByOfficer(int amount = 1)
    {
        _storedHandcuffs += amount;
        handcuffSubmitZone?.RefreshVisual(_storedHandcuffs);
    }

    public int StoredHandcuffs => _storedHandcuffs;
    public int WaitingCount => _waitingPrisoners.Count;

    public void ResetQueueState()
    {
        foreach (PrisonerController p in _waitingPrisoners)
        {
            if (p != null) Destroy(p.gameObject);
        }
        _waitingPrisoners.Clear();
        _storedHandcuffs = 0;
        _deliveryTimer = 0f;
        _arrestTimer = 0f;
        _spawnTimer = 0f;
        handcuffSubmitZone?.RefreshVisual(0);
    }
}
