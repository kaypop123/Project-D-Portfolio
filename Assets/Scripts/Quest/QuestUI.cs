using UnityEngine;
using TMPro;

public class QuestUI : MonoBehaviour
{
    public static QuestUI Instance;

    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descText;

    private void Awake()
    {
        // ½Ì±ÛÅÏ Ã³¸®
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // ¾À ÀüÈ¯ ½Ã ÆÄ±« ¹æÁö
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void UpdateUI(Quest quest)
    {
        if (quest == null)
        {
            ClearUI();
            return;
        }

        titleText.text = quest.questName;
        descText.text = quest.description;
    }

    public void ClearUI()
    {
        titleText.text = "";
        descText.text = "";
    }
}
