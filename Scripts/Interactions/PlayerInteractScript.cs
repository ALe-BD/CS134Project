using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerInteractScript : MonoBehaviour
{
    [SerializeField] private GameObject parentObj;
    private InputAdapter ia;

    [Header("Interaction")]
    [SerializeField] private GameObject interactionHitbox;
    [SerializeField] private GameObject UI;
    [SerializeField] private ParticleSystem QM;
    private Collider interactionCollider;
    private Rigidbody interactionRB;

    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Collectible")) 
        {
            other.transform.parent.Find("ParticleSystem").GetComponent<ParticleSystem>().Play();
            other.transform.parent.GetComponent<DisableAfterSound>().PlayAndDisable();
            Debug.Log("point gained");
            ia.score += 1;
            //SetCountText();
            UI.transform.Find("Score").GetComponent<TextMeshProUGUI>().text = ia.score.ToString("D4");
        }
        if (other.gameObject.CompareTag("Interactable"))
        {
            //spawn question mark above head
            QM.Play();
        }
        if (other.gameObject.CompareTag("Transition"))
        {
            other.gameObject.GetComponent<InteractionScript>().Interacting();
        }
    }

    void OnTriggerStay(Collider other)
    {
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
        UI = parentObj.transform.parent.Find("UI").gameObject;
        QM = parentObj.transform.Find("QuestionMark").GetComponent<ParticleSystem>();
        ia = parentObj.GetComponent<InputAdapter>();
        interactionCollider = GetComponent<CapsuleCollider>();
        interactionRB = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
