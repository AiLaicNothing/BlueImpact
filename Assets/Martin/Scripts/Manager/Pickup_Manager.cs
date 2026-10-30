using System.Collections.Generic;
using UnityEngine;

public class Pickup_Manager : MonoBehaviour
{
    public static Pickup_Manager Instance { get; private set; }

    private readonly HashSet<string> collectedPickups = new();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if(SaveLoadManager.canLoad)
        {
            LoadData();
        }
    }
    void LoadData()
    {
        SaveData loadedData = SaveLoadManager.Instance.LoadGame();
        if (loadedData != null)
        {
            collectedPickups.Clear();
            foreach (string pickupID in loadedData.collectedPickups)
            {
                collectedPickups.Add(pickupID);
            }
        }
    }

    public bool IsCollected(Pickup_Data pickup)
    {
        if (!IsValid(pickup)) return false;

        return collectedPickups.Contains(pickup.PickupID);
    }

    // Returns true only if the pickup was not already recorded.
    public bool MarkCollected(Pickup_Data pickup)
    {
        if (!IsValid(pickup)) return false;

        bool collected = collectedPickups.Add(pickup.PickupID);

        if (collected)
        {
            SaveLoadManager.Instance.SaveGame("collectedPickups", collectedPickups);
        }

        return collected;
    }

    public void RemoveCollected(Pickup_Data pickup)
    {
        if (!IsValid(pickup)) return;

        collectedPickups.Remove(pickup.PickupID);
    }

    public void ClearAll()
    {
        collectedPickups.Clear();
    }

    // Future Save_Manager can serialize this list.
    public List<string> GetCollectedIDs()
    {
        return new List<string>(collectedPickups);
    }

    // Future Save_Manager uses this when loading a save.
    public void LoadCollectedIDs(IEnumerable<string> savedIDs)
    {
        collectedPickups.Clear();

        if (savedIDs == null) return;

        foreach (string id in savedIDs)
        {
            if (!string.IsNullOrWhiteSpace(id)) collectedPickups.Add(id);
        }
    }

    private bool IsValid(Pickup_Data pickup)
    {
        if (pickup != null &&
            !string.IsNullOrWhiteSpace(pickup.PickupID))
        {
            return true;
        }

        Debug.LogWarning(
            "[Pickup_Manager] Invalid Pickup_Data or empty pickup ID."
        );

        return false;
    }
}
