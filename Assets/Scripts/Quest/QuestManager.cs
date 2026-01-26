using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class QuestManager : MonoBehaviour
{
    public static QuestManager Instance;

    public Queue<Quest> questQueue = new Queue<Quest>();
    public Quest currentQuest;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // QuestManager는 씬 전환 시 유지
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    private void Start()
    {
        LoadQuests();
        NextQuest();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    // 씬이 불려졌을 때 처리
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 1. 씬마다 Canvas 안의 QuestUI를 찾아 싱글턴 Instance 재설정
        QuestUI newUI = FindObjectOfType<QuestUI>();
        if (newUI != null)
        {
            QuestUI.Instance = newUI;  // 새 QuestUI로 Instance 재설정
            if (currentQuest != null)
                QuestUI.Instance.UpdateUI(currentQuest); // 현재 퀘스트 UI 갱신
        }

        // 2. 씬이 현재 퀘스트 클리어 씬인지 체크
        if (currentQuest != null && scene.name == currentQuest.clearSceneName)
        {
            CompleteQuest();
        }
    }

   public void LoadQuests()
    {
        // 1. 타임라인 / 대기맵 1 - 사이렌 끄기
        questQueue.Enqueue(new Quest
        {
            questName = "사이렌을 정지하라",
            description = "홍채 인식 장치를 사용해 사이렌을 정지시키시오.",
            clearSceneName = "StealthMap"
        });

        // 2. 스텔스 맵 1 - 몬스터 회피
        questQueue.Enqueue(new Quest
        {
            questName = "감시를 피해 이동",
            description = "몬스터에게 발각되지 않고 구역을 통과하시오.",
            clearSceneName = "WaitingMap2"
        });

        // 3. 대기맵 2 - 파이프 획득
        questQueue.Enqueue(new Quest
        {
            questName = "탈출 경로 확보",
            description = "탈출에 필요한 배관 파이프를 획득하시오.",
            clearSceneName = "LadderMap"
        });

        // 4. 사다리 맵 - 아래로 이동
        questQueue.Enqueue(new Quest
        {
            questName = "하부 구역으로 이동",
            description = "사다리를 이용해 아래 구역으로 내려가시오.",
            clearSceneName = "SewerMap1"
        });

        // 5. 하수도 맵 - 단서 기반 진행
        questQueue.Enqueue(new Quest
        {
            questName = "탈출구를 찾아라",
            description = "여러 단서를 수집해 하수도의 출구를 찾으시오.",
            clearSceneName = "PipeMap"
        });

        // 6. 배관 맵 - 시체 조사
        questQueue.Enqueue(new Quest
        {
            questName = "남겨진 흔적",
            description = "배관 구역에서 시체를 조사하시오.",
            clearSceneName = "Map"
        });
    }

    public void NextQuest()
    {
        if (questQueue.Count > 0)
        {
            currentQuest = questQueue.Dequeue();
            if (QuestUI.Instance != null)
                QuestUI.Instance.UpdateUI(currentQuest);
        }
        else
        {
            currentQuest = null;
            if (QuestUI.Instance != null)
                QuestUI.Instance.ClearUI();
        }
    }

    void CompleteQuest()
    {
        Debug.Log($"퀘스트 클리어: {currentQuest.questName}");
        NextQuest();
    }
}
