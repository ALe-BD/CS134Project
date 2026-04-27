using System.Collections;
using System.Collections.Generic;
using UnityEngine;


public class FootstepManager : MonoBehaviour
{
    public List<AudioClip> grassSteps = new List<AudioClip>();
    public List<AudioClip> waterSteps = new List<AudioClip>();
    public List<AudioClip> caveSteps = new List<AudioClip>();
    [SerializeField] private float rayDistance = 2f;
    [SerializeField] private LayerMask groundMask = ~0;

    private enum Surface { grass, water, cave};
    private Surface surface;

    private List<AudioClip> currentList;

    private AudioSource source;

    private void Start()
    {
        source = GetComponent<AudioSource>();            
    }

    public void PlayStep ()
    {
        UpdateSurfaceFromRaycast();
        // Debug.Log("Step played");

        if(currentList == null || currentList.Count == 0)
            return;
        
        // Debug.Log("Step played2");
        AudioClip clip = currentList[Random.Range(0, currentList.Count)];
        source.PlayOneShot(clip);
    }

    private void SelectStepList ()
    {
        switch (surface)
        {
            case Surface.grass:
                currentList = grassSteps;
                break;
            case Surface.water:
                currentList = waterSteps;
                break;
            case Surface.cave:
                currentList = caveSteps;
                break;
            default:
                currentList = null;
                break;
        }
    }

    private void UpdateSurfaceFromRaycast()
    {
    RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, rayDistance, groundMask);

    if (hit.collider != null)
    {
        if (hit.collider.CompareTag("Grass"))
        {
            surface = Surface.grass;
        }
        else if (hit.collider.CompareTag("Water"))
        {
            surface = Surface.water;
        }
        else if (hit.collider.CompareTag("Cave"))
        {
            surface = Surface.cave;
        }

        SelectStepList();
    }
}

}
