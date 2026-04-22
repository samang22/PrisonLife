using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("참조")]
    public PlayerStats stats;
    public VirtualJoystick joystick;
    public Animator animator;

    [Header("아이템 스택 루트")]
    public Transform ironOreStackRoot;
    public Transform handcuffStackRoot;
    public Transform dollarStackRoot;

    [Header("아이템 프리팹")]
    public GameObject ironOrePrefab;
    public GameObject handcuffPrefab;
    public GameObject dollarPrefab;

    [Header("스택 간격")]
    public float stackSpacing = 0.12f;

    [Header("설정")]
    public float gravity = -20f;

    [Header("채굴 레이어")]
    public LayerMask miningLayerMask = ~0;

    [Header("장비 (드릴 단계별 프리팹)")]
    public Transform toolSocket;
    public GameObject[] drillPrefabs = new GameObject[3];

    public int IronOreCount { get; private set; }
    public int HandcuffCount { get; private set; }
    public int DollarCount { get; private set; }

    private CharacterController _cc;
    private float _verticalVelocity;
    private IInteractable _currentInteractable;
    private GameObject _currentToolInstance;

    private int _depositsInRange;
    private float _miningTimer;

    private static readonly int VertHash = Animator.StringToHash("Vert");

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();
        stats?.RuntimeReset();
    }

    private void Start()
    {
        RefreshTool();
    }

    private void Update()
    {
        HandleMovement();
        HandleMining();
        HandleInteraction();
        SetToolVisible(HasDepositInRange());
    }

    public void RefreshTool()
    {
        if (_currentToolInstance != null)
        {
            Destroy(_currentToolInstance);
            _currentToolInstance = null;
        }

        if (stats == null) return;

        int level = Mathf.Clamp(stats.drillLevel, 0, drillPrefabs.Length - 1);
        if (drillPrefabs[level] == null) return;

        // 채굴 범위 중심(정면 Z+1)에 도구 배치
        _currentToolInstance = Instantiate(drillPrefabs[level], transform);
        _currentToolInstance.transform.localPosition = Vector3.forward * 1f;
        _currentToolInstance.transform.localRotation = Quaternion.identity;
        _currentToolInstance.SetActive(_depositsInRange > 0);
    }

    private void SetToolVisible(bool visible)
    {
        if (_currentToolInstance != null)
            _currentToolInstance.SetActive(visible);
    }

    private bool HasDepositInRange()
    {
        if (stats == null) return false;
        Vector3 center = transform.position + transform.forward * 1f;
        Collider[] hits = Physics.OverlapSphere(center, stats.EffectiveMiningRange, miningLayerMask);
        foreach (Collider hit in hits)
            if (hit.GetComponent<RockController>() != null) return true;
        return false;
    }

    public bool CanPickupIronOre() => IronOreCount < stats.maxIronOreCarry;

    public void AddIronOre(int amount = 1)
    {
        int next = Mathf.Min(IronOreCount + amount, stats.maxIronOreCarry);
        if (next >= stats.maxIronOreCarry)
            UIManager.Instance?.ShowMaxIndicator();
        IronOreCount = next;
        RefreshStack(ironOreStackRoot, ironOrePrefab, IronOreCount);
    }

    public bool TakeIronOre(int amount = 1)
    {
        if (IronOreCount < amount) return false;
        IronOreCount -= amount;
        RefreshStack(ironOreStackRoot, ironOrePrefab, IronOreCount);
        return true;
    }

    public bool CanPickupHandcuff() => HandcuffCount < stats.maxHandcuffCarry;

    public void AddHandcuff(int amount = 1)
    {
        int next = Mathf.Min(HandcuffCount + amount, stats.maxHandcuffCarry);
        if (next >= stats.maxHandcuffCarry)
            UIManager.Instance?.ShowMaxIndicator();
        HandcuffCount = next;
        RefreshStack(handcuffStackRoot, handcuffPrefab, HandcuffCount);
    }

    public bool TakeHandcuff(int amount = 1)
    {
        if (HandcuffCount < amount) return false;
        HandcuffCount -= amount;
        RefreshStack(handcuffStackRoot, handcuffPrefab, HandcuffCount);
        return true;
    }

    public void AddDollar(int amount = 1)
    {
        DollarCount += amount;
        RefreshStack(dollarStackRoot, dollarPrefab, DollarCount);
        UIManager.Instance?.UpdateDollarsUI(DollarCount);
    }

    public bool TakeDollar(int amount = 1)
    {
        if (DollarCount < amount) return false;
        DollarCount -= amount;
        RefreshStack(dollarStackRoot, dollarPrefab, DollarCount);
        UIManager.Instance?.UpdateDollarsUI(DollarCount);
        return true;
    }

    // 오브젝트 풀링 방식으로 스택 비주얼 갱신
    // 초과분은 비활성화, 부족분만 새로 생성
    private void RefreshStack(Transform root, GameObject prefab, int count)
    {
        if (root == null || prefab == null) return;

        int current = root.childCount;

        for (int i = count; i < current; i++)
            root.GetChild(i).gameObject.SetActive(false);

        for (int i = 0; i < count; i++)
        {
            GameObject obj;
            if (i < current)
            {
                obj = root.GetChild(i).gameObject;
                obj.SetActive(true);
            }
            else
            {
                obj = Instantiate(prefab, root);
            }
            obj.transform.localPosition = Vector3.up * i * stackSpacing;
            obj.transform.localRotation = Quaternion.identity;
        }
    }

    private void HandleMovement()
    {
        Vector2 input = joystick != null ? joystick.Direction : Vector2.zero;
        Vector3 move = new Vector3(input.x, 0f, input.y);
        // 카메라가 45도 각도를 보고 있으므로 입력 벡터를 동일하게 회전
        move = Quaternion.Euler(0, 45f, 0) * move;

        if (_cc.isGrounded)
            _verticalVelocity = -1f;
        else
            _verticalVelocity += gravity * Time.deltaTime;

        move.y = _verticalVelocity;
        _cc.Move(move * stats.moveSpeed * Time.deltaTime);

        float mag = 0f;
        if (animator != null)
            mag = new Vector3(input.x, 0, input.y).magnitude;
            animator.SetFloat(VertHash, mag > 0.1f ? 1f : 0f);

        // Y 성분 제거 후 이동 방향으로 부드럽게 회전
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

    // drillLevel 0: 0.5초마다 정면 최근접 1개
    // drillLevel 1, 2: OnTriggerEnter에서 즉시 처리
    private void HandleMining()
    {
        if (stats == null || stats.drillLevel >= 1) return;

        if (_depositsInRange <= 0) { _miningTimer = 0f; return; }

        _miningTimer += Time.deltaTime;
        if (_miningTimer < stats.miningInterval) return;
        _miningTimer = 0f;

        RockController nearest = GetNearestDeposit();
        nearest?.Mine(this);
    }

    private RockController GetNearestDeposit()
    {
        Vector3 center = transform.position + transform.forward * 1f;
        Collider[] hits = Physics.OverlapSphere(center, stats.EffectiveMiningRange, miningLayerMask);

        RockController nearest = null;
        float nearestDist = float.MaxValue;

        foreach (Collider hit in hits)
        {
            RockController rock = hit.GetComponent<RockController>();
            if (rock == null) continue;

            float dist = Vector3.Distance(center, hit.transform.position);
            if (dist < nearestDist)
            {
                nearestDist = dist;
                nearest = rock;
            }
        }

        return nearest;
    }

    private void OnTriggerEnter(Collider other)
    {
        RockController rock = other.GetComponent<RockController>();
        if (rock != null)
        {
            _depositsInRange++;
            SetToolVisible(true);

            if (stats == null) return;

            if (stats.drillLevel == 1)
            {
                RockController nearest = GetNearestDeposit();
                nearest?.Mine(this);
            }
            else if (stats.drillLevel == 2)
            {
                Vector3 center = transform.position + transform.forward * 1f;
                Collider[] hits = Physics.OverlapSphere(center, stats.EffectiveMiningRange, miningLayerMask);
                foreach (Collider hit in hits)
                {
                    RockController nearby = hit.GetComponent<RockController>();
                    if (nearby != null)
                        nearby.Mine(this);
                }
            }
            return;
        }

        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null)
            _currentInteractable = interactable;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.GetComponent<RockController>() != null)
        {
            _depositsInRange = Mathf.Max(0, _depositsInRange - 1);
            if (_depositsInRange == 0)
                SetToolVisible(false);
            return;
        }

        IInteractable interactable = other.GetComponent<IInteractable>();
        if (interactable != null && _currentInteractable == interactable)
            _currentInteractable = null;
    }

    // Scene View에서 채굴 범위 시각화
    private void OnDrawGizmosSelected()
    {
        if (stats == null) return;
        Vector3 center = transform.position + transform.forward * 1f;
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
        Gizmos.DrawSphere(center, stats.EffectiveMiningRange);
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.8f);
        Gizmos.DrawWireSphere(center, stats.EffectiveMiningRange);
    }

    public void ResetRunState(Vector3 position, Quaternion rotation)
    {
        _currentInteractable = null;
        stats?.RuntimeReset();

        IronOreCount     = 0;
        HandcuffCount    = 0;
        DollarCount      = 0;
        _miningTimer     = 0f;
        _depositsInRange = 0;
        _verticalVelocity = 0f;

        RefreshStack(ironOreStackRoot, ironOrePrefab, 0);
        RefreshStack(handcuffStackRoot, handcuffPrefab, 0);
        RefreshStack(dollarStackRoot, dollarPrefab, 0);
        UIManager.Instance?.UpdateDollarsUI(0);

        RefreshTool();

        if (_cc != null)
        {
            _cc.enabled = false;
            transform.SetPositionAndRotation(position, rotation);
            _cc.enabled = true;
        }
        else
            transform.SetPositionAndRotation(position, rotation);
    }
}
