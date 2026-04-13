using UnityEngine;

/// <summary>
/// UI 버튼 OnClick()에 연결 — 빌드에서는 게임 종료, 에디터에서는 플레이 모드 중지.
/// </summary>
public class GameQuit : MonoBehaviour
{
    public void QuitApplication()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
