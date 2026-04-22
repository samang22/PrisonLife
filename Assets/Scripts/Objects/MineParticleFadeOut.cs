using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

// duration 동안 스케일 1 -> 2 -> 거의 0 으로 변화 후 오브젝트 제거
// RockController.mineParticlePrefab 루트에 붙여서 사용
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
