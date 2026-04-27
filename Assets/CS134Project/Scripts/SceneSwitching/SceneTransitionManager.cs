using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

//Used on static Interactables that switcher the player scene
public class SceneTransitionManager : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 1f;
    public void ChangeScene(string sceneName, string targetSpawnPoint)
    {
        SceneSpawnSetter.spawnPointName = targetSpawnPoint;
        StartCoroutine(LoadSceneRoutine(sceneName));
    }

    private IEnumerator LoadSceneRoutine(string sceneName)
    {
        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.FadeOut();
            yield return new WaitForSeconds(fadeDuration);
        }

        yield return SceneManager.LoadSceneAsync(sceneName);
    }
}