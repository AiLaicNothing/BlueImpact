using UnityEngine;

[System.Serializable]
public class SkillEntry
{
    public string characterName;  // ← Nombre del personaje (debe coincidir con CharacterInfo)
    public Skill skill;
}

public class SkillPickup : MonoBehaviour, IInteractable
{
    [SerializeField] private SkillEntry[] skillsByCharacter;  // ✅ ARRAY DE SKILLS
    [SerializeField] private bool destroyOnPickup = true;

    private bool pickedUp = false;
    private Skill currentSkill;  // ✅ LA SKILL DEL PERSONAJE ACTUAL
    private PlayerControl currentPlayer;

    private void OnEnable()
    {
        // ✅ SUSCRIBIRSE CUANDO SE SPAWNEA EL PLAYER
        PlayerSpawn_Manager.OnPlayerSpawned += OnPlayerSpawned;
    }

    private void OnDisable()
    {
        PlayerSpawn_Manager.OnPlayerSpawned -= OnPlayerSpawned;
    }

    private void OnPlayerSpawned(PlayerControl player)
    {
        currentPlayer = player;

        // ✅ USAR LA REFERENCIA GUARDADA
        if (player.CurrentCharacterInfo == null)
        {
            Debug.LogWarning("⚠️ CurrentCharacterInfo es null");
            return;
        }

        // ✅ BUSCAR SKILL QUE COINCIDA
        foreach (var entry in skillsByCharacter)
        {
            if (entry.characterName == player.CurrentCharacterInfo.characterName)
            {
                currentSkill = entry.skill;
                gameObject.SetActive(true);
                pickedUp = false;
                Debug.Log($"✅ SkillPickup para: {player.CurrentCharacterInfo.characterName}");
                return;
            }
        }

        gameObject.SetActive(false);
    }

    public void Interact()
    {
        if (pickedUp || currentSkill == null || currentPlayer == null)
            return;

        currentPlayer.UnlockSkill(currentSkill);
        pickedUp = true;

        Debug.Log($"✅ Skill desbloqueada: {currentSkill.skillName}");

        if (destroyOnPickup)
            gameObject.SetActive(false);
    }

    public string GetInteractionText()
    {
        if (currentSkill == null)
            return "Desbloquear";
        return $"Desbloquear: {currentSkill.skillName}";
    }
}