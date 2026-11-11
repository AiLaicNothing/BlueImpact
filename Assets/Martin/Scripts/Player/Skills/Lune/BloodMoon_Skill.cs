using UnityEngine;
using System.Collections;
using System.Collections.Generic;
[CreateAssetMenu(menuName = "Player/Skills/Lune/Blood Moon")]
public class BloodMoon_Skill : Skill
{
    [SerializeField] private GameObject moonPrefab;

    [Header("Spawn")]
    [SerializeField] private float maxRange = 20f;
    [SerializeField] private float height;
    [SerializeField] private Vector3 offset;
    [SerializeField] private HitData hitData;

    [Header("Projection")]
    [SerializeField] private GameObject projectionObject;
    GameObject projectionObjectSave;
    [SerializeField] private Vector3 projectionOffset;
    [Header("Sfx")]
    [SerializeField] private GameObject sfx;
    [Header("Vfx")]
    [SerializeField] private GameObject vfx;
    [SerializeField] private float sumEdgeVfx;
    [SerializeField] private float durationVfx;
    [SerializeField] private float firstPause;
    [SerializeField] private float secondPause;
    [SerializeField] private float thirdPause;
    [SerializeField] private float maxTimer;

    // ==================== NUEVOS: DAÑO Y ESCALADO ====================
    public override string GetPhysicalScaling() => hitData != null ? $"{hitData.physicalScale * 100:F0}%" : "";
    public override string GetMagicScaling() => hitData != null ? $"{hitData.magicalScale * 100:F0}%" : "";

    public override void ExecuteSkill(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
       player.StartCoroutine( SummonMoon(player, targetPoint, lockTargetPos));
    }

    private IEnumerator SummonMoon(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        DestroyProjectionObject();
        player.blockVelocity = true;

        Vector3 finalTarget = lockTargetPos != Vector3.zero ? lockTargetPos : targetPoint;

        // clamp range
        Vector3 dir = (finalTarget - player.transform.position);
        float dist = dir.magnitude;

        if (dist > maxRange)
        {
            finalTarget = player.transform.position + dir.normalized * maxRange;
        }

        Vector3 spawnPos = finalTarget + Vector3.up * height;
        //spawnPos = player.transform.position + player.Model.forward * offset.z + player.Model.right * offset.x + Vector3.up * height;

        ParticleSystem firstVfx = Instantiate(vfx, spawnPos + projectionOffset, Quaternion.identity).GetComponent<ParticleSystem>();
        firstVfx.Play(false);
        ParticleSystem secondVfx = firstVfx.gameObject.transform.Find("VortexBloodMoon").GetComponent<ParticleSystem>();
        ParticleSystem thirdVfx = secondVfx.gameObject.transform.Find("MagicCircleBloodMoon").GetComponent<ParticleSystem>();
        yield return new WaitForSeconds(firstPause);
        firstVfx.Pause(false);
        secondVfx.Play(false);
        yield return new WaitForSeconds(secondPause);
        secondVfx.Pause(false);
        thirdVfx.Play(false);
        yield return new WaitForSeconds(thirdPause);
        thirdVfx.Pause(false);
        GameObject moon = Instantiate(moonPrefab, spawnPos + offset, Quaternion.identity);
        player.PlayAudio(actionSound, 0.8f);
        Vector3 circlePosition = new Vector3(0, firstVfx.transform.position.y + sumEdgeVfx, 0);
        BloodMoon moonProj = moon.GetComponent<BloodMoon>();
        Destroy(firstVfx.gameObject, durationVfx);
        if (moonProj != null)
        {
            moonProj.Initialize(player, hitData, finalTarget, circlePosition, maxTimer);
        }
    }

    public override void CreateProjection(PlayerControl player)
    {
        //projectionObject.GetComponent<Collider>().enabled = false;
        projectionObjectSave = Instantiate(projectionObject);
        Renderer[] renderers = projectionObjectSave.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            Material mat = renderer.material;
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
        Vector3 spawnPos = finalTarget + Vector3.up * height;
        spawnPos.y = spawnPos.y + projectionOffset.y;
        projectionObjectSave.transform.position = spawnPos;

        //SetProjectionColor(new Color(1f, 1f, 1f, 0.2f));
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
    public override void DestroyProjectionObject()
    {
        Destroy(projectionObjectSave);
    }
}