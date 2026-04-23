using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSpawnSetter : MonoBehaviour
{
    public static Vector3 targetPosition;
    public static bool shouldSetPosition;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!shouldSetPosition) return;

        GameObject player = GameObject.FindWithTag("Player");
        if (player != null)
        {
            player.transform.position = targetPosition;
        }

        shouldSetPosition = false;
    }
}
