using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterStats_UI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI characterName;
    [SerializeField] private Image characterIcon;

    [SerializeField] private Transform stats_Container;
    [SerializeField] private GameObject statValueRow_Prefab;

    private readonly List<StatValueRow_UI> statValueRows = new();
    private CharacterStatsManager statsManager;

    private void OnEnable()
    {
        // Se obtiene aquí porque OnEnable corre antes que Start en la primera activación.
        statsManager = CharacterStatsManager.Instance;

        if (statsManager == null) return;

        CreateEntries();
    }

    private void OnDisable()
    {
        ClearEntries();
    }
    private void CreateEntries()
    {
        var player = Player_Manager.Instance.GetCharacterData();

        characterName.text = player.data.characterName;

        ClearEntries();

        foreach (StatsType stat in Enum.GetValues(typeof(StatsType)))
        {
            GameObject statRow = Instantiate(statValueRow_Prefab, stats_Container);
            var content = statRow.GetComponent<StatValueRow_UI>();

            int value = statsManager.GetCurrentStat(stat);

            content.Initialize(null, stat, value);
            statValueRows.Add(content);
        }
    }

    private void ClearEntries()
    {
        foreach (StatValueRow_UI row in statValueRows)
        {
            if (row != null) Destroy(row.gameObject);
        }

        statValueRows.Clear();
    }
}