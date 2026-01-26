using UnityEngine;

public class CallCCTVMove : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator.TryGetComponent(out CCTVMove pc))
        {
            pc.InMove2();
            return;
        }
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator.TryGetComponent(out CCTVMove pc))
        {
            pc.OutMove2();
            return;
        }
    }


}
