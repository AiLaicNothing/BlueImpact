using UnityEngine;

public class ChallengeStarter : MonoBehaviour, IInteractable
{
    [SerializeField]
    private TimedPlatformChallenge challenge;

    public void Interact()
    {
        challenge?.StartChallenge();
    }

    public string GetInteractionText()
    {
        return "Iniciar desafío";
    }
}