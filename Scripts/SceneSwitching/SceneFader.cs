using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

//Placed on seperate SceneFader gameObject
public class SceneFader : MonoBehaviour
{
    public static SceneFader Instance;

    [SerializeField] private Image fadeImage;
    [SerializeField] private float fadeDuration = 0.5f;

    private Coroutine currentFade;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (fadeImage != null)
        {
            // Prevent the fade image from blocking UI clicks
            fadeImage.raycastTarget = false;

            // Make sure it exists and starts visible/enabled
            if (!fadeImage.gameObject.activeSelf)
                fadeImage.gameObject.SetActive(true);
        }
    }

    private void Start()
    {
        if (fadeImage != null && !fadeImage.gameObject.activeSelf)
        {
            fadeImage.gameObject.SetActive(true);
        }
    }
    
    //Usually on Scene Entry
    public void FadeIn()
    {
        //stops current fade
        if (currentFade != null)
            StopCoroutine(currentFade);

        currentFade = StartCoroutine(Fade(1f, 0f));
    }

    //Usually on Scene Exit
    public void FadeOut()
    {
        //stops current fade
        if (currentFade != null)
            StopCoroutine(currentFade);

        currentFade = StartCoroutine(Fade(0f, 1f));
    }

    //Fades the fadeImages in or out based on the fadeDuration
    public IEnumerator Fade(float startAlpha, float endAlpha)
    {
        float time = 0f;
        Color color = fadeImage.color;
        color.a = startAlpha;
        fadeImage.color = color;

        // Never let the overlay block button presses
        fadeImage.raycastTarget = false;

        while (time < fadeDuration)
        {
            time += Time.deltaTime;
            float t = time / fadeDuration;

            color.a = Mathf.Lerp(startAlpha, endAlpha, t);
            fadeImage.color = color;

            yield return null;
        }

        color.a = endAlpha;
        fadeImage.color = color;
    }
}
