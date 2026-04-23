using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerInteractScript : MonoBehaviour
{
    [SerializeField] private GameObject parentObj;
    private InputAdapter ia;

    [Header("Interaction")]
    [SerializeField] private GameObject interactionHitbox;
    private Collider interactionCollider;
    private Rigidbody interactionRB;

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Collectible")) 
        {
           //AudioSource audioSource = other.GetComponent<AudioSource>();

           //audioSource.Play();
           //other.gameObject.SetActive(false);
           //set colliables into the floor to play audo from collectible
           other.GetComponent<CollectibleAnim>().startPos = other.GetComponent<CollectibleAnim>().startPos + new Vector3(0, -2, 0);
           Debug.Log("point gained");
           ia.score += 1;
           //SetCountText();
        }
        if (other.gameObject.CompareTag("Interactable") && ia.isInteracting)
        {
            Debug.Log(other.gameObject.name);
            other.gameObject.GetComponent<InteractionScript>().Interacting();
        }
    }
    // Start is called before the first frame update
    void Start()
    {
        parentObj = transform.parent.gameObject;
        ia = parentObj.GetComponent<InputAdapter>();
        interactionCollider = GetComponent<CapsuleCollider>();
        interactionRB = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
