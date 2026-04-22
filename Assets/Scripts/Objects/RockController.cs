using System.Collections.Generic;
using UnityEngine;

public class RockController : MonoBehaviour, IResettable
{
    [Header("채굴 설정")]
    public bool respawn = true;
    public float respawnTime = 15f;

    [Header("이펙트")]
    public GameObject mineParticlePrefab;

    private bool _isDepleted;
    private float _respawnTimer;
    private Renderer[] _renderers;
    private Collider[] _colliders;
    // Awake에서 초기 enabled 상태를 저장해 재생성 시 그대로 복원
    private bool[] _colliderEnabledInitially;

    // 씬 내 모든 RockController를 캐싱 (FindObjectsOfType 반복 호출 방지)
    private static readonly List<RockController> _all = new List<RockController>();
    public static IReadOnlyList<RockController> All => _all;

    private void Awake()
    {
        _all.Add(this);
        ResetRegistry.Register(this);
        _renderers = GetComponentsInChildren<Renderer>();
        _colliders = GetComponentsInChildren<Collider>();
        _colliderEnabledInitially = new bool[_colliders.Length];
        for (int i = 0; i < _colliders.Length; i++)
            _colliderEnabledInitially[i] = _colliders[i].enabled;
    }

    private void OnDestroy()
    {
        _all.Remove(this);
        ResetRegistry.Unregister(this);
    }

    private void Update()
    {
        if (_isDepleted && respawn)
        {
            _respawnTimer -= Time.deltaTime;
            if (_respawnTimer <= 0f)
                Revive();
        }
    }

    public void Mine(PlayerController player)
    {
        if (_isDepleted) return;

        player.AddIronOre(1);

        if (mineParticlePrefab != null)
            Instantiate(mineParticlePrefab, transform.position, Quaternion.identity);

        Deplete();
    }

    public bool MineByWorker()
    {
        if (_isDepleted) return false;

        if (mineParticlePrefab != null)
            Instantiate(mineParticlePrefab, transform.position, Quaternion.identity);

        Deplete();
        return true;
    }

    public bool IsAvailable => !_isDepleted;

    private void Deplete()
    {
        _isDepleted = true;
        _respawnTimer = respawnTime;

        Debug.Log($"[RockController] 고갈 {gameObject.name} / {respawnTime}초 후 재생성");

        foreach (Renderer r in _renderers) r.enabled = false;
        foreach (Collider c in _colliders) c.enabled = false;
    }

    private void Revive()
    {
        _isDepleted = false;

        Debug.Log($"[RockController] 재생성 {gameObject.name}");

        foreach (Renderer r in _renderers) r.enabled = true;
        RestoreColliderStates();
    }

    public void ResetState() => ResetToAvailable();

    public void ResetToAvailable()
    {
        _isDepleted = false;
        _respawnTimer = 0f;
        foreach (Renderer r in _renderers) r.enabled = true;
        RestoreColliderStates();
    }

    private void RestoreColliderStates()
    {
        if (_colliders == null || _colliderEnabledInitially == null) return;
        int n = Mathf.Min(_colliders.Length, _colliderEnabledInitially.Length);
        for (int i = 0; i < n; i++)
            _colliders[i].enabled = _colliderEnabledInitially[i];
    }
}
