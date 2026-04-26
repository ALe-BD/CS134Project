using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DamageZone : InteractionScript
{
    [SerializeField] private GameObject player;
    [SerializeField] private int damage;
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            player = other.transform.parent.gameObject;
            Interacting();
        }
    }

    public override void Interacting()
    {
        player.GetComponent<HealthManager>().Damaged(damage);
    }
}
