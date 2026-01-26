using UnityEngine;

public class end2 : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        RebootGame();
    }

    // Update is called once per frame
    public void RebootGame()
    {
#if UNITY_STANDALONE
        string[] executableEndings = new string[] { "exe", "x86", "x86_64", "app" };
        string executablePath = Application.dataPath + "/../";

        // 실행 파일 찾기
        foreach (string file in System.IO.Directory.GetFiles(executablePath))
        {
            foreach (string ending in executableEndings)
            {
                if (file.ToLower().EndsWith("." + ending))
                {
                    Debug.Log("[Reboot] 실행 시도: " + file);
                    System.Diagnostics.Process.Start(file); // 새 인스턴스 실행
                    Application.Quit(); // 현재 게임 종료
                    return;
                }
            }
        }

        Debug.LogError("[Reboot] 실행 파일을 찾을 수 없습니다.");
#else
        Debug.LogWarning("RebootGame()은 Standalone 환경에서만 작동합니다.");
#endif
    }
}
