using Unity.Cinemachine;
using Unity.VisualScripting;
using UnityEngine;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private string sceneNameLoad;
    [SerializeField] private string sceneNameUnload;

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
        {
            SceneLoaderManager.Instance.LoadScene(sceneNameLoad);
            SceneLoaderManager.Instance.UnLoadScene(sceneNameUnload);
        }
    }
}
