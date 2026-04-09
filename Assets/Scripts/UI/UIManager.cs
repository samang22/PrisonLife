using UnityEngine;
using TMPro;
using System.Collections;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("달러 UI")]
    public TextMeshProUGUI dollarsText;

    [Header("수감자 카운터 UI")]
    public TextMeshProUGUI prisonerCountText;

    [Header("MAX 표시")]
    public GameObject maxIndicator;
    public float maxIndicatorDuration = 1.5f;

    [Header("사운드 버튼")]
    public GameObject soundOnIcon;
    public GameObject soundOffIcon;
    private bool _isMuted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        if (maxIndicator != null) maxIndicator.SetActive(false);

        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.onDollarsChanged.AddListener(UpdateDollarsUI);

        if (GameManager.Instance != null)
            GameManager.Instance.onPrisonerCountChanged.AddListener(UpdatePrisonerCountUI);
    }

    private void OnDestroy()
    {
        if (CurrencyManager.Instance != null)
            CurrencyManager.Instance.onDollarsChanged.RemoveListener(UpdateDollarsUI);

        if (GameManager.Instance != null)
            GameManager.Instance.onPrisonerCountChanged.RemoveListener(UpdatePrisonerCountUI);
    }

    public void UpdateDollarsUI(int amount)
    {
        if (dollarsText != null)
            dollarsText.text = $"${amount}";
    }

    private void UpdatePrisonerCountUI(int current, int max)
    {
        if (prisonerCountText != null)
            prisonerCountText.text = $"{current}/{max}";
    }

    public void ShowMaxIndicator()
    {
        if (maxIndicator == null) return;
        StopCoroutine(nameof(HideMaxIndicatorAfterDelay));
        maxIndicator.SetActive(true);
        StartCoroutine(HideMaxIndicatorAfterDelay());
    }

    private IEnumerator HideMaxIndicatorAfterDelay()
    {
        yield return new WaitForSeconds(maxIndicatorDuration);
        if (maxIndicator != null) maxIndicator.SetActive(false);
    }

    public void ToggleSound()
    {
        _isMuted = !_isMuted;
        AudioListener.volume = _isMuted ? 0f : 1f;

        if (soundOnIcon != null) soundOnIcon.SetActive(!_isMuted);
        if (soundOffIcon != null) soundOffIcon.SetActive(_isMuted);
    }
}
