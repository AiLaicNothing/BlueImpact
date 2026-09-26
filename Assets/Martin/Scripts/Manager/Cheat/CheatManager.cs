using UnityEngine;
using UnityEngine.SceneManagement;

public class CheatManager : MonoBehaviour
{
    [SerializeField] private Transform level1Pos;
    [SerializeField] private Transform level1_5Pos;
    [SerializeField] private Transform level2Pos;
    [SerializeField] private Transform level2_5;
    [SerializeField] private Transform level3Pos;
    [SerializeField] private Transform level3_5;

    [SerializeField] private Transform level_Tuto;
    [SerializeField] private Transform level_1_2;
    [SerializeField] private Transform level_Tuto1;

    private void Update()
    {
        if (Input.GetKey(KeyCode.P))
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                TeleportToLevel1();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                TeleportToLevel1_5();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                TeleportToLevel2();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha4))
            {
                TeleportToLevel2_5();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha5))
            {
                TeleportToLevel3();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha6))
            {
                TeleportToLevel3_5();
            }
        }

        if (Input.GetKeyDown(KeyCode.Alpha7))
        {
            SceneManager.LoadScene("Testing_Scene");
        }

        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            Transform player = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();

            player.transform.position = level_Tuto.position;
        }
        if (Input.GetKeyDown(KeyCode.Alpha8))
        {
            Transform player = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();

            player.transform.position = level_1_2.position;
        }


        if (Input.GetKeyDown(KeyCode.Alpha0))
        {
            Transform player = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();

            player.transform.position = level_Tuto1.position;
        }
    }

    private void TeleportToLevel1()
    {
        Transform player = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();

        player.transform.position = level1Pos.position;
    }

    private void TeleportToLevel1_5()
    {
        Transform player = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();

        player.transform.position = level1_5Pos.position;
    }

    private void TeleportToLevel2()
    {
        Transform player = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();

        player.transform.position = level2Pos.position;
    }

    private void TeleportToLevel2_5()
    {
        Transform player = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();

        player.transform.position = level2_5.position;
    }

    private void TeleportToLevel3()
    {
        Transform player = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();

        player.transform.position = level3Pos.position;
    }

    private void TeleportToLevel3_5()
    {
        Transform player = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();

        player.transform.position = level3_5.position;
    }

    private void UnlockSkills()
    {

    }

    private void GetPoints()
    {

    }

}
