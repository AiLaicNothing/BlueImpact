using UnityEngine;

[CreateAssetMenu(menuName = "Player/Skills/Lune/Heal")]
public class Heal_Skill : Skill
{
    [Header("Buff")]
    [SerializeField] private int ammount;

    [Header("Vfx")]
    [SerializeField] private GameObject vfx;
    [SerializeField] private Vector3 offset = Vector3.zero;
    [Header("Sfx")]
    [SerializeField] private GameObject sfx;

    // ==================== NUEVOS: DAÑO Y ESCALADO ====================
    public override int GetBaseDamage() => ammount; // Mostramos el monto de heal

    /// <summary>
    /// Override para mostrar descripción especial de Heal
    /// </summary>
    public override string GetDamageDescription()
    {
        return $"<b>Restaura:</b> {ammount} HP";
    }

    public override void ExecuteSkill(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        HealPlayer(player);
    }

    private void HealPlayer(PlayerControl player)
    {
        //float maxHealth = player.PlayerStatsManager.GetAllStat.Max;

        // prevent overheal
        //int finalHealth = Mathf.Clamp(20, 0, maxHealth);

        // apply heal
        player.Heal(ammount);
        player.PlayAudio(actionSound, 0.8f);

        Debug.Log($"Healed -> {ammount}");


        if (vfx != null)
        {
            GameObject obj = Instantiate(vfx, player.transform.position + offset, Quaternion.identity, player.transform);
            Destroy(obj, 1.25f);
        }
    }
}