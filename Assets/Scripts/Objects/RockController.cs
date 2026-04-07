using UnityEngine;

public class RockController : MonoBehaviour, IInteractable
{
    [Header("설정")]
    public float maxHealth = 100f;
    public int dollarsPerHit = 5;
    public GameObject destroyParticlePrefab;
    public bool respawn = true;
    public float respawnTime = 10f;

    private float _currentHealth;
    private bool _isDead;
    private float _respawnTimer;
    private MeshRenderer _meshRenderer;
    private Collider _collider;

    private void Awake()
    {
        _meshRenderer = GetComponentInChildren<MeshRenderer>();
        _collider = GetComponent<Collider>();
        _currentHealth = maxHealth;
    }

    private void Update()
    {
        if (_isDead && respawn)
        {
            _respawnTimer -= Time.deltaTime;
            if (_respawnTimer <= 0f)
                Revive();
        }
    }

    // 플레이어가 트리거 범위 내에 있는 동안 매 프레임 호출
    public void OnInteract(PlayerController player)
    {
        if (_isDead) return;
        float damage = player.stats.miningDamagePerSecond * Time.deltaTime;
        TakeDamage(damage);
    }

    public void TakeDamage(float amount)
    {
        if (_isDead) return;

        _currentHealth -= amount;

        // 체력 비율에 따른 시각적 크기 축소
        float ratio = Mathf.Clamp01(_currentHealth / maxHealth);
        transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 1f, ratio);

        CurrencyManager.Instance?.AddDollars(Mathf.RoundToInt(amount * dollarsPerHit * 0.1f));

        if (_currentHealth <= 0f)
            Die();
    }

    private void Die()
    {
        _isDead = true;
        _respawnTimer = respawnTime;

        if (destroyParticlePrefab != null)
            Instantiate(destroyParticlePrefab, transform.position, Quaternion.identity);

        if (_meshRenderer != null) _meshRenderer.enabled = false;
        if (_collider != null) _collider.enabled = false;
    }

    private void Revive()
    {
        _isDead = false;
        _currentHealth = maxHealth;
        transform.localScale = Vector3.one;

        if (_meshRenderer != null) _meshRenderer.enabled = true;
        if (_collider != null) _collider.enabled = true;
    }

    public float GetHealthRatio() => Mathf.Clamp01(_currentHealth / maxHealth);
}
