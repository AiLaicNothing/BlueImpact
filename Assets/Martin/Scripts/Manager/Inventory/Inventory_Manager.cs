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
        else
        {
            SaveInventory();
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

    // Lo usa el final secreto para saber si el jugador consiguio el objeto.
    // Compara por referencia (los Item_Data son assets) y, como respaldo, por ID y nombre.
    public bool HasItem(Item_Data item)
    {
        if (item == null) return false;

        if (inventory.Contains(item)) return true;

        for (int i = 0; i < inventory.Count; i++)
        {
            Item_Data owned = inventory[i];
            if (owned == null) continue;

            if (owned.ID != 0 && owned.ID == item.ID) return true;

            if (!string.IsNullOrEmpty(item.itemName) && owned.itemName == item.itemName) return true;
        }

        return false;
    }

    private void SaveInventory()
    {
        if (SaveLoadManager.Instance != null)
        {
            SaveLoadManager.Instance.SaveGame("inventory", inventory);
        }
    }
}
