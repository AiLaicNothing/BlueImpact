using UnityEngine;

public class ArrowController : MonoBehaviour
{
    [SerializeField] private bool unlock;
    [SerializeField] public bool wantToUnlock;
    void Update()
    {
        if (wantToUnlock)
        {
            WantToUnlockArrow();
        }
        else
        {
            if (Input.GetKeyUp(KeyCode.LeftAlt))
            {
                unlock = false;
                WantTolockArrow();
            }
            if (Time.timeScale == 1 && !unlock)
            {
                WantTolockArrow();
            }
            if (Input.GetKeyDown(KeyCode.LeftAlt))
            {
                unlock = true;
                WantToUnlockArrow();
            }
            if (Time.timeScale == 0)
            {
                WantToUnlockArrow();
            }
        }


    }

    void WantToUnlockArrow()
    {
        Cursor.lockState = CursorLockMode.None;
    }
    void WantTolockArrow()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }
}
