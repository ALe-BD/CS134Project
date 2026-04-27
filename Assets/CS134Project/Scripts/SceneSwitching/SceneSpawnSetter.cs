using UnityEngine;
using UnityEngine.SceneManagement;

//This script finds the player in the scene and moves the player to the spawn point (Put on an object that pesists from the first level)
public class SceneSpawnSetter : MonoBehaviour
{
    public static SceneSpawnSetter Instance;
    public static string spawnPointName;
    public int score;
    private void Awake()
    {
        //clears the instances
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

    //Plays on every scene load that is not the title screen
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        //Debug.Log("Start" + spawnPointName + "ping");
        //if (string.IsNullOrEmpty(spawnPointName)) return;

        GameObject player = GameObject.Find("Player (1)");
        if(player != null)
        {   
            player.GetComponent<InputAdapter>().score = score;
        }
        //Debug.Log("Player Found");
        GameObject spawnPoint = GameObject.Find(spawnPointName);
        //Debug.Log("spawnPoint Found");

        if (player != null && spawnPoint != null)
        {
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
