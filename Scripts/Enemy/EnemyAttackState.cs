using UnityEngine;

public class EnemyAttackState : StateMachineBehaviour
{
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        EnemyAnimationDriver driver = animator.GetComponent<EnemyAnimationDriver>();

        if (driver == null)
        {
            return;
        }

        if (driver.enemyDamage == null)
        {
            return;
        }

        driver.enemyDamage.SetCanDamage(true);
    }

    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        EnemyAnimationDriver driver = animator.GetComponent<EnemyAnimationDriver>();

        if (driver != null && driver.enemyDamage != null)
        {
            driver.enemyDamage.SetCanDamage(false);
        }
    }
}