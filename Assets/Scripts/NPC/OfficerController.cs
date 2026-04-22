using UnityEngine;

public class OfficerController : MonoBehaviour
{
    [Header("연결")]
    public HandcuffPickupZone  pickupZone;
    public HandcuffSubmitRelay handcuffSubmitZone;

    [Header("이동 설정")]
    public float moveSpeed   = 3.5f;
    public float arriveRange = 1.2f;

    [Header("수갑 수거 설정")]
    public int   maxCarry       = 10;
    public float pickupInterval = 0.2f;
    public float submitInterval = 0.2f;

    [Header("수갑 스택 시각화")]
    public Transform  handcuffStackRoot;
    public GameObject handcuffVisualPrefab;
    public float      stackSpacing = 0.08f;

    [Header("애니메이션")]
    public Animator animator;
    public string speedParam = "Vert";

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

    private void UpdateIdle()
    {
        SetAnimSpeed(0f);

        _idleTimer += Time.deltaTime;
        if (_idleTimer < IdleCheckInterval) return;
        _idleTimer = 0f;

        if (pickupZone != null && pickupZone.StoredCount > 0)
            _state = State.MovingToPickup;
    }

    private void UpdateMovingToPickup()
    {
        if (pickupZone == null) { _state = State.Idle; return; }

        if (MoveToward(pickupZone.transform.position))
        {
            _actionTimer = 0f;
            _state = State.Picking;
        }
    }

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

        if (_carried >= maxCarry || pickupZone.StoredCount <= 0)
        {
            if (_carried > 0)
                _state = State.MovingToSubmit;
            else
                _state = State.Idle;
        }
    }

    private void UpdateMovingToSubmit()
    {
        if (handcuffSubmitZone == null) { _state = State.Idle; return; }

        if (MoveToward(handcuffSubmitZone.transform.position))
        {
            _actionTimer = 0f;
            _state = State.Submitting;
        }
    }

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

    // Y축을 고정하고 수평으로만 이동, 도달 시 true 반환
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

        int current = handcuffStackRoot.childCount;

        for (int i = _carried; i < current; i++)
            handcuffStackRoot.GetChild(i).gameObject.SetActive(false);

        for (int i = 0; i < _carried; i++)
        {
            GameObject obj;
            if (i < current)
            {
                obj = handcuffStackRoot.GetChild(i).gameObject;
                obj.SetActive(true);
            }
            else
            {
                obj = Instantiate(handcuffVisualPrefab, handcuffStackRoot);
            }
            obj.transform.localPosition = Vector3.up * i * stackSpacing;
            obj.transform.localRotation = Quaternion.identity;
        }
    }

    public int CarriedHandcuffs => _carried;
}
