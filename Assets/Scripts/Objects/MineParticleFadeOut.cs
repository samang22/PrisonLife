using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 채굴 이펙트 프리팹용 — duration(기본 0.5초) 동안 배율 1→2→매우 작게 변화 후 오브젝트를 제거합니다.
/// RockController의 mineParticlePrefab 루트에 붙여 사용합니다.
/// </summary>
public class MineParticleFadeOut : MonoBehaviour
{
    [SerializeField, FormerlySerializedAs("fadeDuration")]
    float duration = 0.5f;

    [Tooltip("마지막에 도달하는 크기 배율 (초기 대비)")]
    [SerializeField] float endScaleMultiplier = 0.02f;

    private void Start()
    {
        StartCoroutine(ScaleAndDestroy());
    }

    private IEnumerator ScaleAndDestroy()
    {
        float total = Mathf.Max(0.01f, duration);
        Vector3 baseScale = transform.localScale;
        float elapsed = 0f;

        while (elapsed < total)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / total);
            float mult;
            if (t <= 0.5f)
                mult = Mathf.Lerp(1f, 2f, t / 0.5f);
            else
                mult = Mathf.Lerp(2f, Mathf.Max(0.001f, endScaleMultiplier), (t - 0.5f) / 0.5f);

            transform.localScale = baseScale * mult;
            yield return null;
        }

        Destroy(gameObject);
    }
}
