using UnityEngine;

/// <summary>
/// 체포 순간 전용 이펙트 프리팹을 월드에 스폰합니다.
/// 프리팹 루트에 <see cref="MineParticleFadeOut"/> 등 스케일 연출 스크립트를 붙이면 됩니다.
/// Prisoner 프리팹에 같이 두고, effectPrefab만 Inspector에서 지정합니다.
/// </summary>
public class PrisonerArrestEffect : MonoBehaviour
{
    [Header("스폰")]
    [Tooltip("체포 시 생성할 이펙트 프리팹 (루트에 MineParticleFadeOut 권장)")]
    [SerializeField] GameObject effectPrefab;

    [Tooltip("이 죄수 Transform 기준 로컬 오프셋 (예: 발밑이면 약 (0,0,0) 또는 살짝 위)")]
    [SerializeField] Vector3 localSpawnOffset = Vector3.zero;

    [Tooltip("체크 시 이펙트 회전을 죄수와 동일하게 맞춤. 해제 시 Quaternion.identity")]
    [SerializeField] bool matchPrisonerRotation = true;

    /// <summary>
    /// <see cref="PrisonerController.OnHandcuffDelivered"/>에서 호출됩니다.
    /// </summary>
    public void Spawn()
    {
        if (effectPrefab == null) return;

        Vector3    worldPos = transform.TransformPoint(localSpawnOffset);
        Quaternion rot      = matchPrisonerRotation ? transform.rotation : Quaternion.identity;
        Instantiate(effectPrefab, worldPos, rot);
    }
}
