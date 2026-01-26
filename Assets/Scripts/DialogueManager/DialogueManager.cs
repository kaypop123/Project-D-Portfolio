using System.Collections;
using UnityEngine;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    [Header("UI 컴포넌트 연결")]
    public TextMeshProUGUI dialogueTextUI;

    [Header("설정")]
    public float typingSpeed = 0.05f;
    public float autoPassDelay = 1.5f;

    [Header("대화 내용 작성")]
    [TextArea(3, 10)]
    public string[] sentences;

    private int currentSentenceIndex = 0;
    private bool isTyping = false;
    private Coroutine typeSentenceCoroutine;

    public System.Action onDialogueEnd;

    public bool isDialogueRunning = false;

    [Header("다이얼로그 종료 후 활성화할 몬스터")]
    public GameObject monsterToActivate;

    [Header("사운드")]
    public AudioClip typingSfx;
    public float typingSfxInterval = 0.03f;
    private float lastTypingSfxTime = 0f;

    void Start()
    {
        if (sentences.Length > 0)
        {
            StartDialogue(sentences);
        }
    }

    void Update()
    {
        if (!isDialogueRunning) return;

        //  지금 실행 중인 DialogueManager 오브젝트 태그가 DIA면 입력으로 스킵/넘기기 불가
        if (CompareTag("DIA"))
            return;

        if (Input.GetKeyDown(KeyCode.UpArrow))
        {
            if (isTyping)
            {
                SkipTyping();
            }
            else
            {
                if (dialogueTextUI.text == sentences[currentSentenceIndex])
                {
                    DisplayNextSentence();
                }
            }
        }
    }

    public void StartDialogue(string[] lines)
    {
        isDialogueRunning = true;
        sentences = lines;
        currentSentenceIndex = 0;

        if (sentences.Length > 0)
        {
            TypeNewSentence(sentences[currentSentenceIndex]);
        }
    }

    public void DisplayNextSentence()
    {
        currentSentenceIndex++;

        if (currentSentenceIndex >= sentences.Length)
        {
            EndDialogue();
            return;
        }

        TypeNewSentence(sentences[currentSentenceIndex]);
    }

    private void TypeNewSentence(string sentence)
    {
        if (typeSentenceCoroutine != null) StopCoroutine(typeSentenceCoroutine);
        typeSentenceCoroutine = StartCoroutine(TypeSentence(sentence));
    }

    IEnumerator TypeSentence(string sentence)
    {
        isTyping = true;
        dialogueTextUI.text = "";

        foreach (char letter in sentence.ToCharArray())
        {
            dialogueTextUI.text += letter;
            if (typingSfx != null && SoundManager.Instance != null)
            {
                if (Time.time - lastTypingSfxTime >= typingSfxInterval)
                {
                    SoundManager.Instance.PlaySFX(typingSfx);
                    lastTypingSfxTime = Time.time;
                }
            }
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        StartCoroutine(WaitAndNext());
    }

    IEnumerator WaitAndNext()
    {
        yield return new WaitForSeconds(autoPassDelay);
        DisplayNextSentence();
    }

    private void SkipTyping()
    {
        //  혹시 다른 곳에서 SkipTyping을 직접 호출해도 DIA면 막기
        if (CompareTag("DIA"))
            return;

        if (typeSentenceCoroutine != null) StopCoroutine(typeSentenceCoroutine);

        dialogueTextUI.text = sentences[currentSentenceIndex];
        isTyping = false;

        StartCoroutine(WaitAndNext());
    }

    public void EndDialogue()
    {
        dialogueTextUI.text = "";
        Debug.Log("대화 종료");
        isDialogueRunning = false;

        if (monsterToActivate != null)
            monsterToActivate.SetActive(true);

        onDialogueEnd?.Invoke();
    }
}
