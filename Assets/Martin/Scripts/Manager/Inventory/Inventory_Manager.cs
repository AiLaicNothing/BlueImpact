using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;

public class Inventory_Manager : MonoBehaviour
{
    public static Inventory_Manager Instance;

    [Header("List of item")]
    [SerializeField] private List<Item_Data> inventory = new ();

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
    void LoadData()
    {
        SaveData loadedData = SaveLoadManager.Instance.LoadGame();
        if (loadedData != null)
        {
            inventory.Clear();
            foreach (Item_Data item in loadedData.inventory)
            {
                inventory.Add(item);
            }
        }
    }

    public void AddItem(Item_Data item)
    {
        if (item == null) return;

        inventory.Add(item);
        OnInventoryChange?.Invoke();
        SaveLoadManager.Instance.SaveGame("inventory", inventory);
    }

    public void RemoveItem(Item_Data item)
    {
        if (item == null) return;

        inventory.Remove(item);
        OnInventoryChange?.Invoke();
    }
}
