using System.Collections;
using UnityEngine;

/// <summary>
/// 개별 죄수 - 줄을 서서 대기 중인 상태
/// 수갑 납품 → 복장 변경 → 감옥 이동 → WorkerController로 전환
/// </summary>
public class PrisonerController : MonoBehaviour
{
    [Header("복장 교체")]
    public CharacterEquipment equipment;
    public GameObject civilianCostumePrefab;
    public GameObject prisonCostumePrefab;

    [Header("이동")]
    public float moveSpeed = 3f;

    [Header("참조")]
    public Animator animator;

    public bool IsArrested { get; private set; }

    private static readonly int VertHash = Animator.StringToHash("Vert");

    private void Start()
    {
        // 초기 복장은 CharacterEquipment.defaultEquipment의 "OutWear" 슬롯이 처리
        // WorkerController는 수감 전까지 비활성
        WorkerController worker = GetComponent<WorkerController>();
        if (worker != null)
            worker.enabled = false;
    }

    /// <summary>
    /// PrisonerQueue에서 수갑 납품 완료 시 호출
    /// </summary>
    public void OnHandcuffDelivered()
    {
        if (IsArrested) return;
        IsArrested = true;

        // 죄수복으로 교체 (defaultEquipment의 "OutWear" 슬롯을 덮어씀)
        if (equipment != null && prisonCostumePrefab != null)
            equipment.Equip("OutWear", prisonCostumePrefab);
    }

    /// <summary>
    /// 감옥 내 지정 위치로 이동. 도착 시 WorkerController로 전환.
    /// </summary>
    public void MoveToCell(Vector3 targetPosition, System.Action onArrived = null)
    {
        StartCoroutine(MoveToCellRoutine(targetPosition, onArrived));
    }

    private IEnumerator MoveToCellRoutine(Vector3 target, System.Action onArrived)
    {
        if (animator != null)
            animator.SetFloat(VertHash, 1f);

        while (Vector3.Distance(transform.position, target) > 0.1f)
        {
            Vector3 dir = (target - transform.position).normalized;
            transform.position = Vector3.MoveTowards(transform.position, target, moveSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation,
                                                  Quaternion.LookRotation(dir),
                                                  10f * Time.deltaTime);
            yield return null;
        }

        transform.position = target;

        if (animator != null)
            animator.SetFloat(VertHash, 0f);

        onArrived?.Invoke();

        // 도착 시 상태 전환: PrisonerController OFF (onArrived 이후 비활성화)
        TransitionToWorker();
    }

    private void TransitionToWorker()
    {
        // WorkerController 활성화는 PrisonCell.HireWorkers()에서 명시적으로 처리
        // 여기서는 PrisonerController만 비활성화하여 대기 상태로 전환
        this.enabled = false;
    }
}
