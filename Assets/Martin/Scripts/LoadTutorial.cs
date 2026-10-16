using UnityEngine;

public class LoadTutorial : MonoBehaviour
{
    [SerializeField] private string sceneNameLoad;

    private void Start()
    {

        if (SceneLoaderManager.Instance == null)
        {
            Debug.LogError("[SceneLoader] SceneLoaderManager.Instance is missing.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(sceneNameLoad))
        {
            Debug.Log($"[SceneLoader] Requesting load: {sceneNameLoad}");
            SceneLoaderManager.Instance.LoadScene(sceneNameLoad);
        }
    }
}
