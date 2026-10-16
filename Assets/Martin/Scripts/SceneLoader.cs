using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private string sceneNameLoad;
    [SerializeField] private string sceneNameUnload;

    private bool isProcessing;

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player") || isProcessing)
            return;

        if (SceneLoaderManager.Instance == null)
        {
            Debug.LogError("[SceneLoader] SceneLoaderManager.Instance is missing.");
            return;
        }

        isProcessing = true;

        if (!string.IsNullOrWhiteSpace(sceneNameLoad))
        {
            Debug.Log($"[SceneLoader] Requesting load: {sceneNameLoad}");
            SceneLoaderManager.Instance.LoadScene(sceneNameLoad);
        }

        if (!string.IsNullOrWhiteSpace(sceneNameUnload))
        {
            Debug.Log($"[SceneLoader] Requesting unload: {sceneNameUnload}");
            SceneLoaderManager.Instance.UnLoadScene(sceneNameUnload);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) isProcessing = false;
    }
}
