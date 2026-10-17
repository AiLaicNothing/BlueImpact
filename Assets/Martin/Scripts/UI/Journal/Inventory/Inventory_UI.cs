using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Inventory_UI : MonoBehaviour
{
    [Header("Inventory UI Information")]
    [SerializeField] private TextMeshProUGUI itemName;
    [SerializeField] private Image itemSprite;
    [SerializeField] private TextMeshProUGUI itemDescription;

    [Header("Category Buttons")]
    [SerializeField] private Button keyItems;
    [SerializeField] private Button collectablesItems;
    [SerializeField] private Button logItems;

    [Header("Scroll Items")]
    [SerializeField] private Transform itemContainer;
    [SerializeField] private GameObject itemEntry_Prefab;

    private readonly List<ItemEntry_UI> itemEntries = new();

    private Inventory_Manager inventory_Manager;
    private Item_Data currentItem;

    private ItemType currentType = ItemType.Key;

    private void OnEnable()
    {
        if (keyItems != null) keyItems.onClick.AddListener(OnKeyItems);

        if (collectablesItems != null) collectablesItems.onClick.AddListener(OnCollectablesItems);

        if (logItems != null) logItems.onClick.AddListener(OnLogItems);

        inventory_Manager = Inventory_Manager.Instance;

        if (inventory_Manager == null)
        {
            Debug.LogWarning("Inventory_UI: No Inventory_Manager found.");
            return;
        }

        inventory_Manager.OnInventoryChange += RefreshItemEntries;

        // Default category when opening the inventory.
        currentType = ItemType.Key;
        RefreshItemEntries();
    }

    private void OnDisable()
    {
        if (keyItems != null) keyItems.onClick.RemoveListener(OnKeyItems);

        if (collectablesItems != null) collectablesItems.onClick.RemoveListener(OnCollectablesItems);

        if (logItems != null) logItems.onClick.RemoveListener(OnLogItems);

        if (inventory_Manager != null) inventory_Manager.OnInventoryChange -= RefreshItemEntries;

        ClearItemEntries();
        ClearItemInfo();

        inventory_Manager = null;
    }

    #region Category Selection

    private void OnKeyItems()
    {
        ChangeCategory(ItemType.Key);
    }

    private void OnCollectablesItems()
    {
        ChangeCategory(ItemType.collectable);
    }

    private void OnLogItems()
    {
        ChangeCategory(ItemType.Lore);
    }

    private void ChangeCategory(ItemType type)
    {
        if (currentType == type)
        {
            RefreshItemEntries();
            return;
        }

        currentType = type;
        RefreshItemEntries();
    }

    #endregion

    #region Refresh Entries

    private void RefreshItemEntries()
    {
        ClearItemEntries();

        if (inventory_Manager == null)
        {
            ClearItemInfo();    
            return;
        }

        // Filter by category and sort by ID.
        List<Item_Data> filteredItems = inventory_Manager.listOfItemInInventory.Where(item => item != null && item.type == currentType).OrderBy(item => item.ID).ToList();

        foreach (Item_Data itemData in filteredItems)
        {
            GameObject instance = Instantiate(itemEntry_Prefab, itemContainer);
            UINavigation.DisableNavigation(instance.transform);
            ItemEntry_UI entry = instance.GetComponent<ItemEntry_UI>();

            if (entry == null)
            {
                Destroy(instance);
                continue;
            }

            entry.Initialize(itemData);
            entry.OnSelect += ShowItemInfo;

            itemEntries.Add(entry);
        }

        // Keep the selected item if it still exists in this category.
        if (currentItem != null && filteredItems.Contains(currentItem))
        {
            ShowItemInfo(currentItem);
        }
        else if (itemEntries.Count > 0)
        {
            // Select the first valid entry, already sorted by ID.
            Item_Data firstItem = itemEntries[0].ReturnData();
            ShowItemInfo(firstItem);
        }
        else
        {
            ClearItemInfo();
        }
    }

    private void ClearItemEntries()
    {
        foreach (ItemEntry_UI entry in itemEntries)
        {
            if (entry == null) continue;

            entry.OnSelect -= ShowItemInfo;
            Destroy(entry.gameObject);
        }

        itemEntries.Clear();
    }

    #endregion

    #region Item Information

    private void ShowItemInfo(Item_Data itemData)
    {
        if (itemData == null) return;

        currentItem = itemData;

        itemName.text = itemData.itemName;
        itemDescription.text = itemData.description;

        itemSprite.sprite = itemData.icon;
        itemSprite.enabled = itemData.icon != null;
    }

    private void ClearItemInfo()
    {
        currentItem = null;

        itemName.text = "";
        itemDescription.text = "";

        itemSprite.sprite = null;
        itemSprite.enabled = false;
    }

    #endregion
}
