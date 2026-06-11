using System.Collections;
using TMPro;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager instance;

    public CinemachineControllerDialogue cinemachineControllerDialogue;
    public AudioSource audioSource;
    public TextMeshProUGUI dialogueText;

    public CinemachineVirtualCamera playerCamera;

    private DialogueSequence currentSequence;
    private int currentIndex;
    private bool isPlaying = false;

    public Image leftImageUI;
    public Image rightImageUI;
    public Image centerImageUI;

    void Awake()
    {
        instance = this;
    }

    public void StartDialogue(DialogueSequence sequence)
    {
        if (isPlaying) return;

        currentSequence = sequence;
        currentIndex = 0;
        isPlaying = true;

        StartCoroutine(PlayDialogue());
    }

    IEnumerator PlayDialogue()
    {
        while (currentIndex < currentSequence.steps.Count)
        {
            DialogueStep step = currentSequence.steps[currentIndex];

            //  camara
            if (step.virtualCamera != null)
            {
                cinemachineControllerDialogue.SwitchCamera(step.virtualCamera);
            }

            // texto
            dialogueText.text = step.text;

            // imagen izquierda
            if (step.leftImage != null)
            {
                leftImageUI.gameObject.SetActive(true);
                leftImageUI.sprite = step.leftImage;
            }
            else
            {
                leftImageUI.gameObject.SetActive(false);
            }

            // imagen derecha
            if (step.rightImage != null)
            {
                rightImageUI.gameObject.SetActive(true);
                rightImageUI.sprite = step.rightImage;
            }
            else
            {
                rightImageUI.gameObject.SetActive(false);
            }

            // imagen centro o fondo de texto podemos usarlo de cualquiera de esos modos
            if (step.centerImage != null)
            {
                centerImageUI.gameObject.SetActive(true);
                centerImageUI.sprite = step.centerImage;
            }
            else
            {
                centerImageUI.gameObject.SetActive(false);
            }

            // audio
            if (step.audio != null)
            {
                audioSource.clip = step.audio;
                audioSource.Play();

                yield return new WaitForSeconds(step.audio.length);
            }
            else
            {
                yield return new WaitForSeconds(3f);
            }

            currentIndex++;
        }

        EndDialogue();
    }

    void EndDialogue()
    {
        dialogueText.text = "";
        isPlaying = false;
        leftImageUI.gameObject.SetActive(false);
        rightImageUI.gameObject.SetActive(false);
        centerImageUI.gameObject.SetActive(false);

        // regresa a la camara del jugador
        if (playerCamera != null)
        {
            cinemachineControllerDialogue.SwitchCamera(playerCamera);
        }

        Debug.Log("Diálogo terminado");
    }
}