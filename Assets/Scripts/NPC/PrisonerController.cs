using System.Collections;
using UnityEngine;

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
        // WorkerController는 PrisonCell.HireWorkers()에서 명시적으로 활성화
        WorkerController worker = GetComponent<WorkerController>();
        if (worker != null)
            worker.enabled = false;
    }

    public void OnHandcuffDelivered()
    {
        if (IsArrested) return;
        IsArrested = true;

        GetComponent<PrisonerArrestEffect>()?.Spawn();

        if (equipment != null && prisonCostumePrefab != null)
            equipment.Equip("OutWear", prisonCostumePrefab);
    }

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

        // onArrived 호출 이후 비활성화
        this.enabled = false;
    }
}
