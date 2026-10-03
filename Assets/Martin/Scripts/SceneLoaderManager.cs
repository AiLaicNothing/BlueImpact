using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class SceneLoaderManager : MonoBehaviour
{
    [SerializeField] private string tutorial;
    [SerializeField] private string level_1;
    [SerializeField] private string level_2;
    [SerializeField] private string level_3;
    [SerializeField] private string level_4;

    [SerializeField] private bool loadlLv_Tutorial;
    [SerializeField] private bool loadlLv_1;
    [SerializeField] private bool loadlLv_2;
    [SerializeField] private bool loadlLv_3;
    [SerializeField] private bool loadlLv_4;

    public static SceneLoaderManager Instance;

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
        StartCoroutine(LoadTestScenes());
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneAsync(sceneName));
    }

    public IEnumerator LoadSceneAndWait(string sceneName)
    {
        yield return StartCoroutine(LoadSceneAsync(sceneName));
    }

    private IEnumerator LoadTestScenes()
    {
        if (loadlLv_Tutorial)
            yield return StartCoroutine(LoadSceneAsync(tutorial));

        if (loadlLv_1)
            yield return StartCoroutine(LoadSceneAsync(level_1));

        if (loadlLv_2)
            yield return StartCoroutine(LoadSceneAsync(level_2));

        if (loadlLv_3)
            yield return StartCoroutine(LoadSceneAsync(level_3));

        if (loadlLv_4)
            yield return StartCoroutine(LoadSceneAsync(level_4));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        // Already loaded?
        Scene scene = SceneManager.GetSceneByName(sceneName);

        if (scene.isLoaded)
        {
            Debug.Log("Scene already loaded: " + sceneName);
            yield break;
        }

        AsyncOperation operation =
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        if (operation == null)
        {
            Debug.LogError("Could not load scene: " + sceneName);
            yield break;
        }

        while (!operation.isDone)
        {
            float progress = Mathf.Clamp01(operation.progress / 0.9f);

            Debug.Log("Loading " + sceneName + ": " +  progress * 100f + "%");

            yield return null;
        }

        Debug.Log("Scene loaded: " + sceneName);
    }

    public void UnLoadScene(string sceneName)
    {
        StartCoroutine(UnloadSceneAsync(sceneName));
    }

    public IEnumerator UnloadSceneAndWait(string sceneName)
    {
        yield return StartCoroutine(UnloadSceneAsync(sceneName));
    }

    private IEnumerator UnloadSceneAsync(string sceneName)
    {
        Scene scene = SceneManager.GetSceneByName(sceneName);

        if (!scene.isLoaded)
        {
            yield break;
        }

        AsyncOperation operation =
            SceneManager.UnloadSceneAsync(sceneName);

        if (operation == null)
        {
            Debug.LogError("Could not unload scene: " + sceneName);
            yield break;
        }

        while (!operation.isDone)
        {
            yield return null;
        }

        Debug.Log("Scene unloaded: " + sceneName);
    }
}