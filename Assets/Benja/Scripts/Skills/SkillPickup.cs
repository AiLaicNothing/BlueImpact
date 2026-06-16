using UnityEngine;

public class SkillPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private Skill skillToUnlock;
    [SerializeField] private bool destroyOnPickup = true;

    private bool pickedUp = false;

    public void Interact()
    {
        if (pickedUp || skillToUnlock == null)
            return;

        var player = GameObject.FindGameObjectWithTag("Player")?.GetComponent<PlayerControl>();

        if (player == null)
            return;

        player.UnlockSkill(skillToUnlock);
        pickedUp = true;

        if (destroyOnPickup)
            gameObject.SetActive(false);
    }

    public string GetInteractionText()
    {
        if (skillToUnlock == null)
            return "Desbloquear";
        return $"Desbloquear: {skillToUnlock.skillName}";
    }
}