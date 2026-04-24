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

    public void PlayAndDisable()
    {
        if (visualToHide != null)
            visualToHide.SetActive(false); // hide graphics only

        audioSource.Play();
        StartCoroutine(DisableWhenDone());
    }

    private IEnumerator DisableWhenDone()
    {
        yield return new WaitForSeconds(audioSource.clip.length);
        gameObject.SetActive(false);
    }
}
