using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerSpawnerManager : MonoBehaviour
{
    public static string spawnPointToUse;

    private void Awake()
    {
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
        if (string.IsNullOrEmpty(spawnPointToUse)) return;

        GameObject spawnPoint = GameObject.Find(spawnPointToUse);

        if (spawnPoint != null)
        {
            transform.position = spawnPoint.transform.position;
        }
    }
}

