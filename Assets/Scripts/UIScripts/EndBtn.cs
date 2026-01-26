using UnityEngine;

public class EndBtn : MonoBehaviour
{
    public void Quit()
    {
        // 빌드된 게임에서는 정상 종료
        Application.Quit();

        // 에디터에서 테스트용 (Unity 6 포함)
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
