using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(menuName = "Player/Skills/Lune/Taunt Illusion")]
public class TauntIllusion_Skill : Skill
{
    [Header("Projection")]
    [SerializeField] GameObject objectToPlace;
    [SerializeField] private GameObject projectionObject;
    GameObject projectionObjectSave;
    private HashSet<Vector3> occupiedPositions = new HashSet<Vector3>();    
    bool exist = false;
    [SerializeField] private Vector3 projectionOffset;
    public override void ExecuteSkill(PlayerControl player, Vector3 targetPoint, Vector3 lockTargetPos)
    {
        PlaceObject(player);
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
    public override void UpdateProjectionPosition(PlayerControl player)
    {
        if (projectionObjectSave == null) return;
        Vector3 point = player.transform.position + player.Model.right * projectionOffset.x + player.Model.up * projectionOffset.y + player.Model.forward * projectionOffset.z;


        projectionObjectSave.transform.position = point;
        projectionObjectSave.transform.rotation = player.Model.rotation;
        if (occupiedPositions.Contains(point))
        {
            SetProjectionColor(Color.red);
        }

        else
        {
            SetProjectionColor(new Color(1f, 1f, 1f, 0.3f));                
        }
        if (!player.PlayerStatsManager.CanConsume(resourceType, cost))
        {
            DestroyProjectionObject();
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
    void PlaceObject(PlayerControl player)
    {
        Vector3 placementPosition = projectionObjectSave.transform.position;
        if (!occupiedPositions.Contains(placementPosition))
        {
            var prefab = Instantiate(objectToPlace, placementPosition, player.Model.rotation);
            occupiedPositions.Add(placementPosition);
            DestroyObject(placementPosition, prefab);
            exist = true;
            DestroyProjectionObject();
        }
    }
    void DestroyProjectionObject()
    {
        Destroy(projectionObjectSave); 
    }
    void DestroyObject(Vector3 placementPosition, GameObject prefab)
    {
        occupiedPositions.Remove(placementPosition);
        Destroy(prefab, 6);
    }
}
