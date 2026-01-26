using UnityEngine;

public class CallCCTVMove_Flip : StateMachineBehaviour
{
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (animator.TryGetComponent(out CCTVMove pm))
        {
            if (!pm.hit)
            {
                pm.Flip();
            } 
            else if(pm.hit2){
                pm.Flip();
            }

            return;
        }
    }
}
