using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

public class TutorialPopUp : MonoBehaviour
{
    [SerializeField] private List<PopUpPage> pages = new List<PopUpPage>();

    private bool hasActivated = false;
    private UIPopUp ui;

    private void Awake()
    {
        ui = FindAnyObjectByType<UIPopUp>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (hasActivated) return;

        if (other.gameObject.CompareTag("Player"))
        {
            hasActivated = true;

            if (ui != null) ui.ShowPopUp(pages);
        }
    }
}
