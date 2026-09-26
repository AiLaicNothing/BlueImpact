using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class SceneLoader : MonoBehaviour
{
    public static SceneLoader Instance;

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
        }
    }

    private void Start()
    {
        LoadScene("Zone_01_Tutorial");
        LoadScene("Zone_02");
        LoadScene("Testing4");
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation operation = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            Debug.Log("Loading: " + progress * 100f + "%");

            yield return null;
        }

        Debug.Log("Scene loaded: " + sceneName);
    }

    public void UnLoadScene(string sceneName)
    {
        StartCoroutine (UnloadSceneAsync(sceneName));
    }

    private IEnumerator UnloadSceneAsync(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);

        if (!scene.isLoaded)
        {
            yield break;
        }

        AsyncOperation operation = SceneManager.UnloadSceneAsync(sceneName);

        while (!operation.isDone)
        {
            yield return null;
        }

        Debug.Log("scene Unloaded" + sceneName);
    }
}

