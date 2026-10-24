using UnityEngine;

[CreateAssetMenu(fileName = "WorldEvent", menuName = ("World/Event"))]
public class WorldEvent_Data : ScriptableObject
{
    [Header("ID")]
    [SerializeField] private string event_ID;

    public string Event_ID => event_ID;
}
