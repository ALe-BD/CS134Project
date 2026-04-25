using UnityEngine;

public class EnemyAnimationDriver : MonoBehaviour
{
    public EnemyDamage damage;

    public void DealDamage()
    {
        if (damage != null)
        {
            damage.DealDamage();
        }
    }
}