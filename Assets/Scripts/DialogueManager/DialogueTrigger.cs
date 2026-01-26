using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueTrigger : MonoBehaviour
{
    public DialogueManager dialogueManager;
    public GameObject dialoguePanel;

    private PlayerInput playerInput;
    private GameObject player;

    private void Awake()
    {
        //  Player 태그 자동 탐색
        player = GameObject.FindGameObjectWithTag("Player");

        if (player == null)
        {
            Debug.LogError("Player 태그를 가진 오브젝트를 찾을 수 없습니다.");
            return;
        }

        playerInput = player.GetComponent<PlayerInput>();

        if (playerInput == null)
        {
            Debug.LogError("Player 오브젝트에 PlayerInput 컴포넌트가 없습니다.");
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        //  태그 비교는 유지 (다른 오브젝트 오작동 방지)
        if (!other.CompareTag("Player")) return;

        // 인풋 비활성화
        if (playerInput != null)
        {
            Debug.Log("인풋 비활성화");
            playerInput.DeactivateInput();
        }

        // 패널 활성화
        if (dialoguePanel != null)
            dialoguePanel.SetActive(true);

        // 대화 시작
        dialogueManager.gameObject.SetActive(true);
        dialogueManager.StartDialogue(dialogueManager.sentences);

        // 이벤트 중복 방지
        dialogueManager.onDialogueEnd -= OnDialogueFinished;
        dialogueManager.onDialogueEnd += OnDialogueFinished;
    }

    private void OnDialogueFinished()
    {
        // 인풋 복구
        if (playerInput != null)
        {
            Debug.Log("인풋 활성화");
            playerInput.ActivateInput();
        }

        // 패널 비활성화
        if (dialoguePanel != null)
            dialoguePanel.SetActive(false);

        dialogueManager.onDialogueEnd -= OnDialogueFinished;

        // 트리거 비활성화
        gameObject.SetActive(false);
    }
}
