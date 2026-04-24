using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
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
            yield return new WaitForSeconds(0.5f);
        }

        yield return SceneManager.LoadSceneAsync(sceneName);
    }
}