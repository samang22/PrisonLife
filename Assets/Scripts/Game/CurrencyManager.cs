using UnityEngine;
using UnityEngine.Events;

public class CurrencyManager : MonoBehaviour
{
    public static CurrencyManager Instance { get; private set; }

    [Header("초기 달러")]
    public int startingDollars = 0;

    public int CurrentDollars { get; private set; }

    public UnityEvent<int> onDollarsChanged;

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
        CurrentDollars = startingDollars;
        onDollarsChanged?.Invoke(CurrentDollars);
    }

    public void AddDollars(int amount)
    {
        if (amount <= 0) return;
        CurrentDollars += amount;
        onDollarsChanged?.Invoke(CurrentDollars);
    }

    public bool TrySpendDollars(int amount)
    {
        if (CurrentDollars < amount) return false;
        CurrentDollars -= amount;
        onDollarsChanged?.Invoke(CurrentDollars);
        return true;
    }

    public bool HasEnough(int amount) => CurrentDollars >= amount;
}
