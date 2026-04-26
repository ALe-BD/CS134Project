using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Controls Player Health Bar
public class HealthManager : MonoBehaviour
{
    [SerializeField] private GameObject HealthBar;
    [SerializeField] private Material HealthMaterial;
    [SerializeField] private float Radius = 0.2f;
    [SerializeField] private float LineWidth = 0.25f;
    [SerializeField] private Color Color;
    [SerializeField] private float Rotation = 0;
    [SerializeField] private float RemovedSegments = 0;
    [SerializeField] private float SegmentSpacing = 0.02f;
    [SerializeField] private float SegmentCount = 6;

    private Coroutine fadeRoutine;
    [SerializeField] private float fadeDuration = 2f;

    [SerializeField, TextArea]
    private string DEBUG_String;

    // Start is called before the first frame update
    void Start()
    {
        HealthBar = GameObject.Find("HealthBar");

        if(HealthBar != null)
        {
            DEBUG_String = this + "ping";
        }
        
        //get healthbar material
        HealthMaterial = HealthBar.GetComponent<Renderer>().material;
        HealthMaterial.SetFloat("_Radius", Radius);
        HealthMaterial.SetFloat("_LineWidth", LineWidth);
        HealthMaterial.SetFloat("_Rotation", Rotation);
        HealthMaterial.SetFloat("_RemoveSegments", RemovedSegments);
        HealthMaterial.SetFloat("_SegmentSpacing", SegmentSpacing);
        HealthMaterial.SetFloat("_SegmentCount", SegmentCount);

        Color = HealthMaterial.GetColor("_Color");
    }

    void Update()
    {
        // if (Input.GetKeyDown(KeyCode.I))
        //     StartFadeOut();

        // if (Input.GetKeyDown(KeyCode.O))
        //     StartFadeIn();

        // if (Input.GetKeyDown(KeyCode.R))
        //     Damaged(2);
        // if (Input.GetKeyDown(KeyCode.T))
        //     Healed(1);
    }

    public void Damaged(int damagePoints)
    {
        StartCoroutine(DamageRoutine(damagePoints));
    }

    //Damage Logic
    private IEnumerator DamageRoutine(int damagePoints)
    {
        StartFadeIn();

        RemovedSegments += damagePoints;
        RemovedSegments = Mathf.Clamp(RemovedSegments, 0, SegmentCount);
        HealthMaterial.SetFloat("_RemoveSegments", RemovedSegments);

        yield return new WaitForSeconds(1f);

        StartFadeOut();
    }

    public void Healed(int healPoints)
    {
        StartCoroutine(HealRoutine(healPoints));
    }

    //Healing Logic
    private IEnumerator HealRoutine(int healPoints)
    {
        StartFadeIn();

        RemovedSegments -= healPoints;
        RemovedSegments = Mathf.Clamp(RemovedSegments, 0, SegmentCount);
        HealthMaterial.SetFloat("_RemoveSegments", RemovedSegments);

        yield return new WaitForSeconds(1f);

        StartFadeOut();
    }

    //Fades the Healthbar out
    public void StartFadeOut()
    {
        StartFadeTo(0f);
    }

    //Fades the Healthbar in
    public void StartFadeIn()
    {
        StartFadeTo(1f);
    }

    private void StartFadeTo(float targetAlpha)
    {
        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeToRoutine(targetAlpha, fadeDuration));
    }

    //Fade Logic
    private IEnumerator FadeToRoutine(float targetAlpha, float fullDuration)
    {
        float startAlpha = Color.a;
        float alphaDistance = Mathf.Abs(targetAlpha - startAlpha);

        if (alphaDistance <= 0.001f)
        {
            Color.a = targetAlpha;
            HealthMaterial.SetColor("_Color", Color);
            fadeRoutine = null;
            yield break;
        }

        float adjustedDuration = fullDuration * alphaDistance;
        float elapsed = 0f;

        while (elapsed < adjustedDuration)
        {
            elapsed += Time.deltaTime;
            Color.a = Mathf.Lerp(startAlpha, targetAlpha, elapsed / adjustedDuration);
            HealthMaterial.SetColor("_Color", Color);
            yield return null;
        }

        Color.a = targetAlpha;
        HealthMaterial.SetColor("_Color", Color);
        fadeRoutine = null;
    }
}
