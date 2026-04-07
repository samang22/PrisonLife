using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("참조")]
    public PlayerStats stats;
    public VirtualJoystick joystick;
    public Animator animator;

    [Header("아이템 스택 루트")]
    public Transform ironOreStackRoot;     // 등 위치
    public Transform handcuffStackRoot;    // 가슴 위치

    [Header("아이템 프리팹")]
    public GameObject ironOrePrefab;
    public GameObject handcuffPrefab;

    [Header("스택 간격")]
    public float stackSpacing = 0.12f;

    [Header("설정")]
    public float gravity = -20f;

    // 런타임 인벤토리
    public int IronOreCount { get; private set; }
    public int HandcuffCount { get; private set; }

    private CharacterController _cc;
    private float _verticalVelocity;
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

    // ── 철광석 ──
    public bool CanPickupIronOre() => IronOreCount < stats.maxIronOreCarry;

    public void AddIronOre(int amount = 1)
    {
        IronOreCount = Mathf.Min(IronOreCount + amount, stats.maxIronOreCarry);
        RefreshStack(ironOreStackRoot, ironOrePrefab, IronOreCount);
    }

    public bool TakeIronOre(int amount = 1)
    {
        if (IronOreCount < amount) return false;
        IronOreCount -= amount;
        RefreshStack(ironOreStackRoot, ironOrePrefab, IronOreCount);
        return true;
    }

    // ── 수갑 ──
    public bool CanPickupHandcuff() => HandcuffCount < stats.maxHandcuffCarry;

    public void AddHandcuff(int amount = 1)
    {
        HandcuffCount = Mathf.Min(HandcuffCount + amount, stats.maxHandcuffCarry);
        RefreshStack(handcuffStackRoot, handcuffPrefab, HandcuffCount);
    }

    public bool TakeHandcuff(int amount = 1)
    {
        if (HandcuffCount < amount) return false;
        HandcuffCount -= amount;
        RefreshStack(handcuffStackRoot, handcuffPrefab, HandcuffCount);
        return true;
    }

    // 스택 시각화: 기존 자식 오브젝트 제거 후 count만큼 재생성
    private void RefreshStack(Transform root, GameObject prefab, int count)
    {
        if (root == null || prefab == null) return;

        foreach (Transform child in root)
            Destroy(child.gameObject);

        for (int i = 0; i < count; i++)
        {
            Vector3 offset = Vector3.up * i * stackSpacing;
            Instantiate(prefab, root.position + root.TransformDirection(offset),
                        root.rotation, root);
        }
    }

    private void HandleMovement()
    {
        Vector2 input = joystick != null ? joystick.Direction : Vector2.zero;
        Vector3 move = new Vector3(input.x, 0f, input.y);
        move = Quaternion.Euler(0, 45f, 0) * move;

        if (_cc.isGrounded)
            _verticalVelocity = -1f;
        else
            _verticalVelocity += gravity * Time.deltaTime;

        move.y = _verticalVelocity;
        _cc.Move(move * stats.moveSpeed * Time.deltaTime);

        if (animator != null)
            animator.SetFloat(SpeedHash, new Vector3(input.x, 0, input.y).magnitude);

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
