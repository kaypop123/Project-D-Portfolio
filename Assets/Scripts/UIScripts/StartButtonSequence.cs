using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class StartButtonSequence : MonoBehaviour
{
    public Camera mainCamera;
    public Transform targetSprite; // 왼쪽 구석 스프라이트
    public float zoomSize = 3f;
    public float moveSpeed = 5f;
    public float zoomSpeed = 5f;

    public Image fadeImage; // 검은색 Image
    public float fadeDuration = 1f;
    public string nextScene = "NextSceneName";

    public GameObject gameTitle; // 게임 제목 UI
    public GameObject startButton; // Start 버튼

    private Vector3 targetPosition;
    private bool isZooming = false;

    void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        targetPosition = mainCamera.transform.position;

        // 페이드 이미지 초기화 및 비활성화
        if (fadeImage != null)
        {
            fadeImage.gameObject.SetActive(false);
            fadeImage.color = new Color(0, 0, 0, 0);
        }
    }

    void Update()
    {
        if (isZooming)
        {
            // 카메라 이동
            mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position,
                targetPosition, Time.deltaTime * moveSpeed);

            // 카메라 줌
            mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize,
                zoomSize, Time.deltaTime * zoomSpeed);
        }
    }

    // Start 버튼 OnClick 연결
    public void OnStartButtonClicked()
    {
        // 타겟 위치 설정
        targetPosition = new Vector3(targetSprite.position.x, targetSprite.position.y, mainCamera.transform.position.z);
        isZooming = true;

        // 버튼/게임제목 비활성화
        if (gameTitle != null) gameTitle.SetActive(false);
        if (startButton != null) startButton.SetActive(false);

        // 페이드 이미지 활성화
        if (fadeImage != null) fadeImage.gameObject.SetActive(true);

        // 페이드 후 씬 전환 코루틴 실행
        StartCoroutine(FadeAndLoadScene());
    }

    private IEnumerator FadeAndLoadScene()
    {
        // 카메라 이동/줌 완료 대기 (조정 가능)
        yield return new WaitForSeconds(0.5f);

        // 검은색 페이드
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            if (fadeImage != null)
                fadeImage.color = new Color(0, 0, 0, Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }

        // 씬 전환
        SceneManager.LoadScene(nextScene);
    }
}
