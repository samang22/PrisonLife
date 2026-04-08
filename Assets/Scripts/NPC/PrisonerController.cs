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

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private void Start()
    {
        // 초기 복장: 평상복
        if (equipment != null && civilianCostumePrefab != null)
            equipment.Equip("Costume", civilianCostumePrefab);

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

        // 죄수복으로 교체
        if (equipment != null && prisonCostumePrefab != null)
            equipment.Equip("Costume", prisonCostumePrefab);
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
            animator.SetFloat(SpeedHash, 1f);

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
            animator.SetFloat(SpeedHash, 0f);

        // 도착 시 상태 전환: PrisonerController OFF, WorkerController ON
        TransitionToWorker();

        onArrived?.Invoke();
    }

    private void TransitionToWorker()
    {
        WorkerController worker = GetComponent<WorkerController>();
        if (worker != null)
            worker.enabled = true;

        this.enabled = false;
    }
}
