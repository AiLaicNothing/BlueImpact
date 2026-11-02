using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory_Manager : MonoBehaviour
{
    public static Inventory_Manager Instance;

    [Header("List of item")]
    [SerializeField] private List<Item_Data> inventory = new();

    public event Action OnInventoryChange;

    public IReadOnlyList<Item_Data> listOfItemInInventory => inventory;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (SaveLoadManager.canLoad)
        {
            LoadData();
        }
    }

    private void LoadData()
    {
        if (SaveLoadManager.Instance == null) return;

        SaveData loadedData = SaveLoadManager.Instance.LoadGame();
        if (loadedData == null || loadedData.inventory == null) return;

        inventory.Clear();
        foreach (Item_Data item in loadedData.inventory)
        {
            // Los Item_Data llegan null desde JsonUtility (ver SaveData). Se omiten.
            if (item != null) inventory.Add(item);
        }
    }

    public void AddItem(Item_Data item)
    {
        if (item == null) return;

        inventory.Add(item);
        OnInventoryChange?.Invoke();
        SaveInventory();
    }

    public void RemoveItem(Item_Data item)
    {
        if (item == null) return;
        if (!inventory.Remove(item)) return;

        OnInventoryChange?.Invoke();
        SaveInventory();
    }

    private void SaveInventory()
    {
        if (SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.SaveGame("inventory", inventory);
        }
    }
}
