// RollStateExit.cs
using UnityEngine;

public class RollStateExit : StateMachineBehaviour
{
    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        // PlayerAnimationBinder 스크립트를 찾아서 _isRolling을 false로 바꿔달라고 요청
        PlayerAnimationBinder binder = animator.GetComponent<PlayerAnimationBinder>();
        if (binder != null)
        {
            binder.OnRollAnimationEnd();
        }
    }
}