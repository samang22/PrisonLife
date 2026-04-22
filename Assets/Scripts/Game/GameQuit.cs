using UnityEngine;

// UI Button의 OnClick()에 연결해서 사용
// 빌드에서는 앱 종료, 에디터에서는 플레이 모드 중지
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
