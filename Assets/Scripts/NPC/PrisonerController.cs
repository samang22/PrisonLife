using System.Collections;
using UnityEngine;

/// <summary>
/// 개별 죄수 - 줄을 서서 대기 중인 상태
/// 수갑이 납품되면 복장 변경 후 감옥으로 이동
/// </summary>
public class PrisonerController : MonoBehaviour
{
    [Header("복장")]
    public Renderer characterRenderer;
    public Material civilianMaterial;   // 수갑 전 평상복
    public Material prisonMaterial;     // 수갑 후 죄수복

    [Header("이동")]
    public float moveSpeed = 3f;

    [Header("참조")]
    public Animator animator;

    public bool IsArrested { get; private set; }

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private void Start()
    {
        // 초기 복장: 평상복
        if (characterRenderer != null && civilianMaterial != null)
            characterRenderer.material = civilianMaterial;
    }

    /// <summary>
    /// PrisonerQueue에서 수갑 납품 완료 시 호출
    /// </summary>
    public void OnHandcuffDelivered()
    {
        if (IsArrested) return;
        IsArrested = true;

        // 죄수복으로 변경
        if (characterRenderer != null && prisonMaterial != null)
            characterRenderer.material = prisonMaterial;
    }

    /// <summary>
    /// 감옥 내 지정 위치로 이동
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

        onArrived?.Invoke();
    }
}
