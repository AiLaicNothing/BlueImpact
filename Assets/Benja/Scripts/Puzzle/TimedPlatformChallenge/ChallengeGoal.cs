using UnityEngine;

public class ChallengeGoal : MonoBehaviour, IInteractable
{
    [SerializeField]
    private TimedPlatformChallenge challenge;

    public void Interact()
    {
        challenge?.CompleteChallenge();
    }

    public string GetInteractionText()
    {
        return "Completar desafío";
    }
}