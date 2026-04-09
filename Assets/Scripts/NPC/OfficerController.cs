using UnityEngine;

/// <summary>
/// 경찰관 NPC - HandcuffPickupZone에서 수갑을 가져와 HandcuffSubmitZone에 납품
/// Idle → MovingToPickup → Picking → MovingToSubmit → Submitting → Idle
/// </summary>
public class OfficerController : MonoBehaviour
{
    [Header("연결")]
    public HandcuffPickupZone  pickupZone;
    public HandcuffSubmitRelay handcuffSubmitZone;

    [Header("이동 설정")]
    public float moveSpeed    = 3.5f;
    public float arriveRange  = 1.2f;

    [Header("수갑 수거 설정")]
    public int   maxCarry          = 10;
    public float pickupInterval    = 0.2f;
    public float submitInterval    = 0.2f;

    [Header("수갑 스택 시각화")]
    public Transform  handcuffStackRoot;
    public GameObject handcuffVisualPrefab;
    public float      stackSpacing = 0.08f;

    [Header("애니메이션 (선택)")]
    public Animator animator;
    public string speedParam = "Vert";

    // ── 상태 ──
    private enum State { Idle, MovingToPickup, Picking, MovingToSubmit, Submitting }
    private State _state = State.Idle;

    private int   _carried;
    private float _actionTimer;

    private static readonly float IdleCheckInterval = 0.5f;
    private float _idleTimer;

    private void Start()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        switch (_state)
        {
            case State.Idle:           UpdateIdle();           break;
            case State.MovingToPickup: UpdateMovingToPickup(); break;
            case State.Picking:        UpdatePicking();        break;
            case State.MovingToSubmit: UpdateMovingToSubmit(); break;
            case State.Submitting:     UpdateSubmitting();     break;
        }
    }

    // ── Idle: 픽업존에 수갑이 있으면 출발 ──
    private void UpdateIdle()
    {
        SetAnimSpeed(0f);

        _idleTimer += Time.deltaTime;
        if (_idleTimer < IdleCheckInterval) return;
        _idleTimer = 0f;

        if (pickupZone != null && pickupZone.StoredCount > 0)
        {
            _state = State.MovingToPickup;
        }
    }

    // ── MovingToPickup: 픽업존으로 이동 ──
    private void UpdateMovingToPickup()
    {
        if (pickupZone == null) { _state = State.Idle; return; }

        if (MoveToward(pickupZone.transform.position))
        {
            _actionTimer = 0f;
            _state = State.Picking;
        }
    }

    // ── Picking: 수갑 수거 ──
    private void UpdatePicking()
    {
        SetAnimSpeed(0f);

        if (pickupZone == null) { _state = State.Idle; return; }

        _actionTimer += Time.deltaTime;
        if (_actionTimer < pickupInterval) return;
        _actionTimer = 0f;

        int taken = pickupZone.TakeHandcuff(1);
        _carried += taken;
        RefreshCarryVisual();

        // maxCarry만큼 채웠거나 픽업존이 비었으면 납품 이동
        if (_carried >= maxCarry || pickupZone.StoredCount <= 0)
        {
            if (_carried > 0)
                _state = State.MovingToSubmit;
            else
                _state = State.Idle;
        }
    }

    // ── MovingToSubmit: HandcuffSubmitZone으로 이동 ──
    private void UpdateMovingToSubmit()
    {
        if (handcuffSubmitZone == null) { _state = State.Idle; return; }

        if (MoveToward(handcuffSubmitZone.transform.position))
        {
            _actionTimer = 0f;
            _state = State.Submitting;
        }
    }

    // ── Submitting: 수갑 1개씩 납품 (HandcuffSubmitZone → PrisonerQueue) ──
    private void UpdateSubmitting()
    {
        SetAnimSpeed(0f);

        if (handcuffSubmitZone == null) { _carried = 0; RefreshCarryVisual(); _state = State.Idle; return; }

        _actionTimer += Time.deltaTime;
        if (_actionTimer < submitInterval) return;
        _actionTimer = 0f;

        handcuffSubmitZone.queue?.SubmitHandcuffByOfficer(1);
        _carried--;
        RefreshCarryVisual();

        if (_carried <= 0)
            _state = State.Idle;
    }

    // ── 공통 이동 (Y 고정) → 도달 시 true 반환 ──
    private bool MoveToward(Vector3 targetWorldPos)
    {
        Vector3 myPos    = transform.position;
        Vector3 flatDiff = new Vector3(targetWorldPos.x - myPos.x, 0f, targetWorldPos.z - myPos.z);

        if (flatDiff.magnitude <= arriveRange)
        {
            SetAnimSpeed(0f);
            return true;
        }

        Vector3 dir    = flatDiff.normalized;
        Vector3 newPos = myPos + dir * moveSpeed * Time.deltaTime;
        newPos.y = myPos.y;
        transform.position = newPos;

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            Quaternion.LookRotation(dir),
            10f * Time.deltaTime);

        SetAnimSpeed(1f);
        return false;
    }

    private void SetAnimSpeed(float speed)
    {
        if (animator != null && !string.IsNullOrEmpty(speedParam))
            animator.SetFloat(speedParam, speed);
    }

    private void RefreshCarryVisual()
    {
        if (handcuffStackRoot == null || handcuffVisualPrefab == null) return;

        foreach (Transform child in handcuffStackRoot)
            Destroy(child.gameObject);

        for (int i = 0; i < _carried; i++)
        {
            Vector3 offset = Vector3.up * i * stackSpacing;
            Instantiate(handcuffVisualPrefab,
                        handcuffStackRoot.position + offset,
                        handcuffStackRoot.rotation,
                        handcuffStackRoot);
        }
    }

    public int CarriedHandcuffs => _carried;
}
