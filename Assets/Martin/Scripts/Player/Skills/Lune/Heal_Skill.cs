using UnityEngine;

[CreateAssetMenu(menuName = "Player/Skills/Lune/Heal")]
public class Heal_Skill : Skill
{
    [Header("Buff")]
    [SerializeField] private int ammount;

    [Header("Sfx")]
    [SerializeField] private GameObject sfx;

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

        Debug.Log($"Healed -> {ammount}");


        if (sfx != null)
        {
            GameObject obj = Instantiate(sfx, player.transform.position, Quaternion.identity);
            Destroy(obj, 3f);
        }
    }
}
