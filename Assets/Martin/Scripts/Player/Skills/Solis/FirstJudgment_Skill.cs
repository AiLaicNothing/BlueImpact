using UnityEngine;

[CreateAssetMenu(menuName = "Player/Skills/Solis/First Judgment")]
public class FirstJudgment_Skill : Skill
{
    [Header("Prefab")]
    public GameObject swordPrefab;

    public HitData hitData;

    [Header("Targeting")]
    public float maxRange = 20f;
    public float spawnHeight = 15f;

    [Header("Projection")]
    [SerializeField] private GameObject projectionObject;
    GameObject projectionObjectSave;
    [SerializeField] private Vector3 projectionOffset;

    [Header("SFX")]
    public GameObject spawnSFX;

    // ==================== NUEVOS: DAÑO Y ESCALADO ====================
    public override string GetPhysicalScaling() => hitData != null ? $"{hitData.physicalScale * 100:F0}%" : "";
    public override string GetMagicScaling() => hitData != null ? $"{hitData.magicalScale * 100:F0}%" : "";

    public override void ExecuteSkill(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        SummonSword(player, targetPoint, lockTargetPos);
    }

    private void SummonSword(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        DestroyProjectionObject();
        Vector3 finalTarget = lockTargetPos != Vector3.zero ? lockTargetPos : targetPoint;

        // clamp range
        Vector3 dir = (finalTarget - player.transform.position);
        float dist = dir.magnitude;

        if (dist > maxRange)
        {
            finalTarget = player.transform.position + dir.normalized * maxRange;
        }

        // spawn above target
        Vector3 spawnPos = finalTarget + Vector3.up * spawnHeight;

        GameObject sword = Instantiate(swordPrefab, spawnPos, Quaternion.identity);
        player.PlayAudio(actionSound, 0.8f); // ✅ justo después del Instantiate

        FirstJudgmentSword proj = sword.transform.Find("FireSwordVfx").GetComponent<FirstJudgmentSword>();

        proj.Initialize(hitData, player);

        if (spawnSFX != null)
        {
        }
    }
    public override void CreateProjection(PlayerControl player)
    {
        //projectionObject.GetComponent<Collider>().enabled = false;
        projectionObjectSave = Instantiate(projectionObject);
        Renderer[] renderers = projectionObjectSave.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            Material mat = renderer.sharedMaterial;
            Color color = mat.color;
            color.a = 0.5f;
            mat.color = color;

            mat.SetFloat("_Mode", 2);
            mat.SetInt("_ScrBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }
    }
    public override void UpdateProjectionPosition(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        Vector3 finalTarget = lockTargetPos != Vector3.zero ? lockTargetPos : targetPoint;

        // clamp range
        Vector3 dir = (finalTarget - player.transform.position);
        float dist = dir.magnitude;

        if (dist > maxRange)
        {
            finalTarget = player.transform.position + dir.normalized * maxRange;
        }

        // spawn above target
        Vector3 spawnPos = finalTarget + Vector3.up * spawnHeight;
        spawnPos.y = spawnPos.y + projectionOffset.y;
        projectionObjectSave.transform.position = spawnPos;

        SetProjectionColor(new Color(1f, 1f, 1f, 0.2f));
        if (!player.PlayerStatsManager.CanConsume(resourceType, cost))
        {
            //DestroyProjectionObject();
        }
    }
    void SetProjectionColor(Color color)
    {
        Renderer[] renderers = projectionObjectSave.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            Material mat = renderer.sharedMaterial;
            mat.color = color;
        }
    }
    void DestroyProjectionObject()
    {
        Destroy(projectionObjectSave);
    }
}