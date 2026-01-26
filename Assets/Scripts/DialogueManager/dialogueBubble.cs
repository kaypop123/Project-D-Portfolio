using System.Collections;
using UnityEngine;
using TMPro;

public class DialogueBubble : MonoBehaviour
{
    public TextMeshProUGUI textUI;
    public RectTransform bubbleRect;

    public Transform target; // 캐릭터 Transform
    public Vector3 offset = new Vector3(0, 1.5f, 0);

    public float typingSpeed = 0.05f;
    public float autoPassDelay = 1.5f;

    [Header("다이어로그 텍스트")]
    public string[] sentences;
    private int index = 0;
    private bool isTyping = false;
    private Coroutine typingCoroutine;

    void Update()
    {
        // 2D 전용: 캐릭터 머리 위치 → 스크린 좌표로 변환
        if (target != null)
        {
            Vector3 screenPos = Camera.main.WorldToScreenPoint(target.position + offset);
            bubbleRect.position = screenPos;
        }
    }

    public void ShowDialogue(string[] lines)
    {
        sentences = lines;
        index = 0;
        gameObject.SetActive(true);

        TypeSentence(sentences[index]);
    }

    void TypeSentence(string sentence)
    {
        if (typingCoroutine != null) StopCoroutine(typingCoroutine);
        typingCoroutine = StartCoroutine(Typing(sentence));
    }

    IEnumerator Typing(string sentence)
    {
        isTyping = true;
        textUI.text = "";

        foreach (char c in sentence)
        {
            textUI.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        yield return new WaitForSeconds(autoPassDelay);
        NextSentence();
    }

    void NextSentence()
    {
        index++;

        if (index >= sentences.Length)
        {
            EndDialogue();
            return;
        }

        TypeSentence(sentences[index]);
    }

    void EndDialogue()
    {
        gameObject.SetActive(false);
    }
}
