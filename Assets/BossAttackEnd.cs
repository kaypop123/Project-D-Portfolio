using UnityEngine;

public class BossAttackEnd : StateMachineBehaviour
{
    public BossActFunc a;

    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        a = animator.GetComponent<BossActFunc>();
        a.attackEnd = true;
    }
}
