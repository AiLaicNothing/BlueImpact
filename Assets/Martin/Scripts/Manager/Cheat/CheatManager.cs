using UnityEngine;

public class CheatManager : MonoBehaviour
{
    [SerializeField] private Transform level1Pos;
    [SerializeField] private Transform level2Pos;
    [SerializeField] private Transform level3Pos;

    private void Update()
    {
        if (Input.GetKey(KeyCode.LeftShift))
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
            {
                TeleportToLevel1();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha2))
            {
                TeleportToLevel2();
            }
            else if (Input.GetKeyDown(KeyCode.Alpha3))
            {
                TeleportToLevel3();
            }
        }
    }

    private void TeleportToLevel1()
    {
        Transform player = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();

        player.transform.position = level1Pos.position;
    }

    private void TeleportToLevel2()
    {
        Transform player = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();

        player.transform.position = level2Pos.position;
    }

    private void TeleportToLevel3()
    {
        Transform player = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();

        player.transform.position = level3Pos.position;
    }

    private void UnlockSkills()
    {

    }

    private void GetPoints()
    {

    }

}
