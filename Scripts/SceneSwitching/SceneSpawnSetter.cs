using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSpawnSetter : MonoBehaviour
{
    public static SceneSpawnSetter Instance;
    public static string spawnPointName;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

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
        //Debug.Log("Start" + spawnPointName + "ping");
        //if (string.IsNullOrEmpty(spawnPointName)) return;

        GameObject player = GameObject.Find("Player (1)");
        //Debug.Log("Player Found");
        GameObject spawnPoint = GameObject.Find(spawnPointName);
        //Debug.Log("spawnPoint Found");

        if (player != null && spawnPoint != null)
        {
            //Debug.Log("Not found");
            player.transform.position = spawnPoint.transform.position;
        }
        //Debug.Log("End");
        spawnPointName = null;

        //Fade In
        if (SceneFader.Instance != null)
        {
            SceneFader.Instance.FadeIn();
        }
    }
}
