using UnityEngine;

[CreateAssetMenu(fileName = "Pickup", menuName = "World/Pickup Data")]
public class Pickup_Data : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string pickupID;
    [SerializeField] private PickupType pickupType;

    public string PickupID => pickupID;
    public PickupType Type => pickupType;
    public enum PickupType
    {
        UpgradePoint,
        Skill,
        Item,
        Log,
        Inspection,
        Other
    }
}
