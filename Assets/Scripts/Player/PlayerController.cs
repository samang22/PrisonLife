using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("참조")]
    public PlayerStats stats;
    public VirtualJoystick joystick;
    public Animator animator;

    [Header("설정")]
    public float gravity = -20f;

    private CharacterController _cc;
    private float _verticalVelocity;

    // 현재 상호작용 중인 대상 (바위, 죄수, 업그레이드 구역)
    private IInteractable _currentInteractable;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
    }

    private void Update()
    {
        HandleMovement();
        HandleInteraction();
    }

    private void HandleMovement()
    {
        Vector2 input = joystick != null ? joystick.Direction : Vector2.zero;

        Vector3 move = new Vector3(input.x, 0f, input.y);

        // 아이소메트릭 카메라 기준 방향 변환 (45도 회전)
        move = Quaternion.Euler(0, 45f, 0) * move;

        if (_cc.isGrounded)
            _verticalVelocity = -1f;
        else
            _verticalVelocity += gravity * Time.deltaTime;

        move.y = _verticalVelocity;
        _cc.Move(move * stats.moveSpeed * Time.deltaTime);

        if (animator != null)
            animator.SetFloat(SpeedHash, new Vector3(input.x, 0, input.y).magnitude);

        // 이동 방향으로 캐릭터 회전
        Vector3 flatMove = new Vector3(move.x, 0, move.z);
        if (flatMove.magnitude > 0.1f)
        {
            Quaternion targetRot = Quaternion.LookRotation(flatMove);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, 15f * Time.deltaTime);
        }
    }

    private void HandleInteraction()
    {
        if (_currentInteractable != null)
            _currentInteractable.OnInteract(this);
    }

    private void OnTriggerEnter(Collider other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null)
            _currentInteractable = interactable;
    }

    private void OnTriggerExit(Collider other)
    {
        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null && _currentInteractable == interactable)
            _currentInteractable = null;
    }
}
