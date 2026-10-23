using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class ItemEntry_UI : MonoBehaviour
{
    [SerializeField] private Button itemButton;
    [SerializeField] private Image icon;
    [SerializeField] private TextMeshProUGUI itemName;

    private Item_Data data;

    public event Action<Item_Data> OnSelect;

    public void Initialize(Item_Data itemData)
    {
        data = itemData;

        if (icon != null)
        {
            icon.sprite = data.icon;
            icon.enabled = data.icon != null;
        }

        if (itemName != null) itemName.text = data.itemName;

        itemButton.onClick.RemoveListener(HandleClick);
        itemButton.onClick.AddListener(HandleClick);
    }

    public Item_Data ReturnData()
    {
        return data;
    }

    private void HandleClick()
    {
        OnSelect?.Invoke(data);
    }

    private void OnDestroy()
    {
        if (itemButton != null) itemButton.onClick.RemoveListener(HandleClick);
    }
}
