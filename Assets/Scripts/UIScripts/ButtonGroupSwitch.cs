using UnityEngine;

public class ButtonGroupSwitch : MonoBehaviour
{
    public GameObject First; 
    public GameObject Second;
    public void OnClickSwitchSecondGroup()
    {
        First.SetActive(false); // First 그룹 비활성화
        Second.SetActive(true);  // Second 그룹 활성화
    }
    public void OnClickSwitchFirstGroup()
    {
        First.SetActive(true); // First 그룹 비활성화
        Second.SetActive(false);  // Second 그룹 활성화
    }
}
