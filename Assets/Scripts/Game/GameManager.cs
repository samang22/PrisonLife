using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("수감자 설정")]
    public int maxPrisonerCapacity = 20;

    public int CurrentPrisonerCount { get; private set; }

    public UnityEvent<int, int> onPrisonerCountChanged;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        CurrentPrisonerCount = 0;
        onPrisonerCountChanged?.Invoke(CurrentPrisonerCount, maxPrisonerCapacity);
    }

    public bool TryImprisonPrisoner()
    {
        if (CurrentPrisonerCount >= maxPrisonerCapacity)
        {
            UIManager.Instance?.ShowMaxIndicator();
            return false;
        }

        CurrentPrisonerCount++;
        onPrisonerCountChanged?.Invoke(CurrentPrisonerCount, maxPrisonerCapacity);
        return true;
    }
}
