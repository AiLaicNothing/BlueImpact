using System.Collections.Generic;
using System.Collections;
using UnityEngine;

[CreateAssetMenu(menuName = "Player/Skills/Lune/Sir Moon Beams")]
public class SirMoonBeams_Skill : Skill
{
    [Header("Time")]
    [SerializeField] private float duration = 2f;

    [Header("Damage")]
    [SerializeField] private HitData hitData;

    private GameObject debugBox;
    [Header("Vfx")]
    [SerializeField] private GameObject vfx;
    [SerializeField] private Vector3 vfxOffset;
    [SerializeField] float invokeTimer;
    [SerializeField] float upTimer;
    [SerializeField] float rotateTimer;
    [SerializeField] float speedRotation;
    [SerializeField] float beamSpeed;
    [SerializeField] GameObject sir;
    [SerializeField] ParticleSystem sirf;
    [SerializeField] ParticleSystem[] beams;
    [SerializeField] Transform[] tBeams;
    [Header("Sfx")]
    [SerializeField] private GameObject sfx;
    public override string GetPhysicalScaling() => hitData != null ? $"{hitData.physicalScale * 100:F0}%" : "";
    public override string GetMagicScaling() => hitData != null ? $"{hitData.magicalScale * 100:F0}%" : "";

    public override void ExecuteSkill(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        player.StartCoroutine(InvokeSir(player, targetPoint, lockTargetPos));
    }

    IEnumerator InvokeSir(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        player.blockVelocity = true;
        GameObject sirs = Instantiate(vfx, player.Model.position + vfxOffset, player.Model.rotation);
        ParticleSystem sir = sirs.GetComponent<ParticleSystem>();
        this.sir = sirs;
        player.PlayAudio(actionSound, 0.8f); // ? justo después del Instantiate
        MoonBeamCollision[] proj = sir.gameObject.GetComponentsInChildren<MoonBeamCollision>(true);


        yield return new WaitForSeconds(invokeTimer);
        sir.Pause();
        List<ParticleSystem> beams = new List<ParticleSystem>();
        foreach(ParticleSystem b in sir.gameObject.GetComponentsInChildren<ParticleSystem>(true))
        {
            if (b.gameObject.activeSelf) continue;
            beams.Add(b);
        }
        List<Transform> tBeams = new List<Transform>();
        foreach(Transform t in sir.gameObject.GetComponentsInChildren<Transform>(true))
        {
            if (t.gameObject.activeSelf) continue;
            tBeams.Add(t);
        }
        for (int i = 0; i < beams.Count; i++)
        {
            beams[i].gameObject.SetActive(true);

            proj[i].Initialize(hitData, player, sir.gameObject, tBeams, speedRotation, beamSpeed);
        }

        yield return new WaitForSeconds(upTimer);
        for (int i = 0; i < proj.Length; i++)
        {
            proj[i].canDmg = true;
        }
        foreach (ParticleSystem ps in beams)
        {
            if(ps != null)
            {
                ps.Pause(true);
            }
        }

        for (int i = 0; i < proj.Length; i++)
        {
            proj[i].canRotate = true;
        }
        yield return new WaitForSeconds(rotateTimer);


        for (int i = 0; i < proj.Length; i++)
        {
            proj[i].canRotate = false;
            proj[i].canMove = true;
        }

        sir.Play(false);
        Destroy(sir.gameObject, duration);
        player.blockVelocity = false;
    }
}
