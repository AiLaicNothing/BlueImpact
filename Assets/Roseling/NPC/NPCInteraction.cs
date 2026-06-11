using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class NPCInteraction : MonoBehaviour
{
    public DialogueSequence dialogue;
    public GameObject pressEText;

    private bool playerNear = false;

    void Update()
    {
        if (playerNear && Keyboard.current.eKey.wasPressedThisFrame)
        {
            Debug.Log("INTERACTUANDO");

            pressEText.SetActive(false);
            DialogueManager.instance.StartDialogue(dialogue);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNear = true;
            pressEText.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerNear = false;
            pressEText.SetActive(false);
        }
    }
}
