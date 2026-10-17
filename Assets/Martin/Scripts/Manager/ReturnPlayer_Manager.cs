using UnityEngine;

public class ReturnPlayer_Manager : MonoBehaviour
{
    public static ReturnPlayer_Manager Instance;
    public GameObject playerObj;
    public GameObject playerFather;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void FixedUpdate()
    {
        if (playerObj == null)
        {
            playerObj = Player_Manager.Instance.GetPlayer();
        }
    }

    public void ReturnPlayer()
    {
        playerObj.transform.SetParent(playerFather.transform);
    }
}
