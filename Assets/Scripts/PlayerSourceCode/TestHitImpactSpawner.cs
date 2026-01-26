using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class TestHitImpactSpawner2D : MonoBehaviour
{
    public HitImpactManager fx;   // 같은 오브젝트나 임의 오브젝트의 HitEffect2D 참조

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current?.pKey.wasPressedThisFrame == true)
            fx?.Play();
#else
        if (Input.GetKeyDown(KeyCode.P))
            fx?.Play();
#endif
    }
}
