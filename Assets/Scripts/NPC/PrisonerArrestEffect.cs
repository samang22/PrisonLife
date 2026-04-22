using UnityEngine;

// PrisonerController.OnHandcuffDelivered()에서 호출
// Prisoner 프리팹에 추가하고 effectPrefab을 Inspector에서 지정
public class PrisonerArrestEffect : MonoBehaviour
{
    [Header("스폰")]
    [Tooltip("체포 시 생성할 이펙트 프리팹 (루트에 MineParticleFadeOut 권장)")]
    [SerializeField] GameObject effectPrefab;

    [Tooltip("이 죄수 Transform 기준 로컬 오프셋")]
    [SerializeField] Vector3 localSpawnOffset = Vector3.zero;

    [Tooltip("체크 시 이펙트 회전을 죄수와 동일하게 맞춤")]
    [SerializeField] bool matchPrisonerRotation = true;

    public void Spawn()
    {
        if (effectPrefab == null) return;

        Vector3    worldPos = transform.TransformPoint(localSpawnOffset);
        Quaternion rot      = matchPrisonerRotation ? transform.rotation : Quaternion.identity;
        Instantiate(effectPrefab, worldPos, rot);
    }
}
