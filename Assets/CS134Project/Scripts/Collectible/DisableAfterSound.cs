using System.Collections;
using UnityEngine;

public class DisableAfterSound : MonoBehaviour
{
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private GameObject visualToHide;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        visualToHide = transform.Find("Cube").gameObject;
    }

    //Called when triggereed by collectible
    public void PlayAndDisable()
    {
        //only hides the visuals first
        if (visualToHide != null)
            visualToHide.SetActive(false); // hide graphics only

        //plays the sound effect then starts DisableWhenDone()
        audioSource.Play();
        StartCoroutine(DisableWhenDone());
    }
    
    //Coroutine that waits for length of the audioSource and then deactivates the main GameObject
    private IEnumerator DisableWhenDone()
    {
        yield return new WaitForSeconds(audioSource.clip.length);
        gameObject.SetActive(false);
    }
}
