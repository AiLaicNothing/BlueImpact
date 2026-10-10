using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class SceneLoaderManager : MonoBehaviour
{
    public static SceneLoaderManager Instance { get; private set; }

    private readonly Dictionary<string, AsyncOperation> activeLoads = new();

    private readonly HashSet<string> activeUnloads = new();

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

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public void LoadScene(string sceneName)
    {
        StartCoroutine(LoadSceneAndWait(sceneName));
    }

    public IEnumerator LoadSceneAndWait(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError("[SceneLoader] Scene name is empty.");
            yield break;
        }

        Scene scene = SceneManager.GetSceneByName(sceneName);

        if (scene.isLoaded) yield break;

        // Wait for an existing request instead of starting a duplicate.
        if (activeLoads.TryGetValue(sceneName, out AsyncOperation existing))
        {
            while (!existing.isDone) yield return null;

            yield break;
        }

        // Avoid loading while the same scene is being unloaded.
        while (activeUnloads.Contains(sceneName)) yield return null;

        scene = SceneManager.GetSceneByName(sceneName);

        if (scene.isLoaded) yield break;

        AsyncOperation operation =
            SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);

        if (operation == null)
        {
            Debug.LogError( $"[SceneLoader] Could not load scene '{sceneName}'. " + "Check the scene name and Build Profile scene list.");
            yield break;
        }

        activeLoads[sceneName] = operation;

        while (!operation.isDone) yield return null;

        activeLoads.Remove(sceneName);

        Debug.Log($"[SceneLoader] Loaded scene: {sceneName}");
    }

    public void UnLoadScene(string sceneName)
    {
        StartCoroutine(UnloadSceneAndWait(sceneName));
    }

    public IEnumerator UnloadSceneAndWait(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) yield break;

        while (activeLoads.TryGetValue(sceneName, out AsyncOperation loading))
        {
            while (!loading.isDone) yield return null;
        }

        Scene scene = SceneManager.GetSceneByName(sceneName);

        if (!scene.isLoaded) yield break;

        // Unity cannot unload the only loaded scene.
        if (SceneManager.sceneCount <= 1)
        {
            Debug.LogError("[SceneLoader] Cannot unload the only loaded scene.");
            yield break;
        }

        activeUnloads.Add(sceneName);

        AsyncOperation operation =
            SceneManager.UnloadSceneAsync(sceneName);

        if (operation == null)
        {
            activeUnloads.Remove(sceneName);
            Debug.LogError($"[SceneLoader] Could not unload scene '{sceneName}'.");
            yield break;
        }

        while (!operation.isDone) yield return null;

        activeUnloads.Remove(sceneName);

        Debug.Log($"[SceneLoader] Unloaded scene: {sceneName}");
    }
}