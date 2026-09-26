using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Player/Skills/Solis/Bash Shield")]
public class BashShield_Skill : Skill
{
    public float dashSpeed;
    public float duration;

    public HitData hitData;
    public Vector3 hitBoxSize;
    public Vector3 hitBoxOffset;
    [Header("Trail Variables")]
    [SerializeField] private float refreshRate;
    [SerializeField] private int trailAmount;
    [SerializeField] private string shaderVarRef;
    [SerializeField] private float shaderVarRate;
    [SerializeField] private float shaderVarRefreshRate;
    [SerializeField] private List<SkinnedMeshRenderer> skinnedMeshRenderers;
    [SerializeField] private List<GameObject> gameObjects;
    [SerializeField] public Material shaderMaterial;

    // ==================== NUEVOS: DAÑO Y ESCALADO ====================
    public override string GetPhysicalScaling() => hitData != null ? $"{hitData.physicalScale * 100:F0}%" : "";
    public override string GetMagicScaling() => hitData != null ? $"{hitData.magicalScale * 100:F0}%" : "";

    public override void ExecuteSkill(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        if (lockTargetPos != Vector3.zero)
        {
            Vector3 dir = (lockTargetPos - player.transform.position).normalized;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.01f)
            {
                player.Model.rotation = Quaternion.LookRotation(dir);
            }
        }

        //if (actionSound != null)
        //{
        //    AudioManager.Instance.PlaySFX(actionSound);
        //}
        FindModel(player);
        player.StartCoroutine(BashRoutine(player));
        player.StartCoroutine(ActiveTrail(trailAmount, refreshRate, player));
    }

    private IEnumerator BashRoutine(PlayerControl player)
    {
        player.blockVelocity = false;
        player.PlayAudio(actionSound, 0.8f);

        float timer = duration;

        while (timer > 0f)
        {
            timer -= Time.fixedDeltaTime;

            Vector3 velocity = player.Model.forward * dashSpeed;
            velocity.y = player.Rb.linearVelocity.y;
            player.Rb.linearVelocity = velocity;

            if (CheckHits(player)) yield break;

            yield return new WaitForFixedUpdate();
        }

        player.blockVelocity = false;
    }
    void FindModel(PlayerControl player)
    {
        gameObjects.Clear();
        skinnedMeshRenderers.Clear();
        gameObjects.Add(player.gameObject.transform.Find("ModelHolder").Find("Solis-FBX_Final").Find("Solis").Find("Hands").Find("Hand_L").gameObject);
        gameObjects.Add(player.gameObject.transform.Find("ModelHolder").Find("Solis-FBX_Final").Find("Solis").Find("Hands").Find("Hand_R").gameObject);
        gameObjects.Add(player.gameObject.transform.Find("ModelHolder").Find("Solis-FBX_Final").Find("Solis").Find("Knight").gameObject);
        gameObjects.Add(player.gameObject.transform.Find("ModelHolder").Find("Solis-FBX_Final").Find("Solis").Find("Wing").gameObject);
        skinnedMeshRenderers.Add(gameObjects[0].GetComponent<SkinnedMeshRenderer>());
        skinnedMeshRenderers.Add(gameObjects[1].GetComponent<SkinnedMeshRenderer>());
        skinnedMeshRenderers.Add(gameObjects[2].GetComponent<SkinnedMeshRenderer>());
        skinnedMeshRenderers.Add(gameObjects[3].GetComponent<SkinnedMeshRenderer>());
    }
    IEnumerator ActiveTrail(int effectAmount, float timeBetweenTrail, PlayerControl player)
    {
        for (int i1 = 0; i1 < effectAmount; i1++)
        {
            for (int i = 0; i < skinnedMeshRenderers.Count; i++)
            {
                //GameObject parentObj = new GameObject();
                GameObject gObj = new GameObject();
                gObj.transform.SetPositionAndRotation(gameObjects[i].transform.position, gameObjects[i].transform.rotation);
                /*parentObj.transform.position = player.Model.position + player.Model.forward * hitBoxOffset.z + Vector3.up * hitBoxOffset.y;
                parentObj.transform.rotation = player.Model.rotation;
               // parentObj.name = "Trail";
                parentObj.transform.SetParent(gObj.transform);*/
                MeshRenderer mr = gObj.AddComponent<MeshRenderer>();
                MeshFilter mf = gObj.AddComponent<MeshFilter>();

                Mesh mesh = new Mesh();
                skinnedMeshRenderers[i].BakeMesh(mesh);

                mf.mesh = mesh;
                mr.material = shaderMaterial;

                player.StartCoroutine(AnimateMaterialFloat(mr.material, 0, shaderVarRate, shaderVarRefreshRate));
                Destroy(gObj, duration + 0.2f);
            }
            yield return new WaitForSeconds(timeBetweenTrail);
        }
        gameObjects.Clear();
        skinnedMeshRenderers.Clear();
        yield return new WaitForSeconds(timeBetweenTrail);        
    }
    IEnumerator AnimateMaterialFloat(Material mat, float goal, float rate, float refreshRate)
    {
        float valueToAnimate = mat.GetFloat(shaderVarRef);

        while (valueToAnimate > goal)
        {
            valueToAnimate -= rate;
            mat.SetFloat(shaderVarRef, valueToAnimate);
            yield return new WaitForSeconds(refreshRate);
        }
    }
    private bool CheckHits(PlayerControl player)
    {
        Vector3 center = player.Model.position + player.Model.forward * hitBoxOffset.z + Vector3.up * hitBoxOffset.y;

        Collider[] hits = Physics.OverlapBox(center, hitBoxSize * 0.5f, player.Model.rotation);

        player.ShowHitbox(center, hitBoxSize, player.Model.rotation);

        foreach (Collider hit in hits)
        {
            if (!hit.CompareTag("Enemy")) continue;

            if (!hit.TryGetComponent(out IDamageable damageable)) continue;

            DamageInfo info = new DamageInfo
            {
                damage = (player.PlayerStatsManager.GetActualValue(StatType.DañoFísico) * hitData.physicalScale) + (player.PlayerStatsManager.GetActualValue(StatType.DañoMágico) * hitData.magicalScale),
                hitDirection = player.Model.forward,
                throwType = hitData.throwType,
                stunDuration = hitData.stunDuration,
                keepInAir = hitData.keepInAir,
                airHangDuration = hitData.airHangDuration,
                airLiftForce = hitData.airLiftForce,
                pushForce = hitData.pushForce,
                knockDownForce = hitData.knockDownForce,
                knockDownForwardScale = hitData.knockDownForwardScale,
                staggerBuild = hitData.staggerCharge
            };

            damageable.TakeDamage(info);
            return true;
        }

        return false;
    }
}