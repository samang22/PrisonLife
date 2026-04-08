using UnityEngine;

/// <summary>
/// Main Camera에 부착 - 플레이어를 고정 오프셋으로 부드럽게 추적
/// </summary>
public class CameraFollow : MonoBehaviour
{
    [Header("추적 대상")]
    public Transform target;

    [Header("오프셋 (플레이어 기준 카메라 상대 위치)")]
    public Vector3 offset = new Vector3(0f, 15f, -8f);

    [Header("추적 부드러움 (높을수록 빠르게 따라감)")]
    public float smoothSpeed = 5f;

    [Header("카메라 각도 고정 여부")]
    public bool fixRotation = true;
    public Vector3 fixedRotation = new Vector3(60f, 0f, 0f);

    private void Start()
    {
        if (fixRotation)
            transform.rotation = Quaternion.Euler(fixedRotation);

        if (target != null)
            transform.position = target.position + offset;
    }

    private void LateUpdate()
    {
        if (target == null) return;

        Vector3 desiredPosition = target.position + offset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);
    }
}
