using UnityEngine;

public class EnemyAnimationDriver : MonoBehaviour
{
    [SerializeField] public EnemyDamage damage;

    //DealDamage proxy
    public void DealDamage()
    {
        if (damage != null)
        {
            damage.DealDamage();
        }
    }
}