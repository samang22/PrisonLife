using UnityEngine;
using UnityEngine.Events;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    // 수감자 수 변경 이벤트 (현재 수, 최대 수)
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

    public void NotifyPrisonerAdded(int current, int max)
    {
        onPrisonerCountChanged?.Invoke(current, max);
    }
}
