using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class BuildingInfo : MonoBehaviour, IUIWindow
{
    public GameObject statsList;
    public InfoRow statsRowPrefab;
    private List<InfoRow> statsRows = new List<InfoRow>();
    public Image image;

    public bool IsActive()
    {
        return gameObject.activeSelf;
    }

    public void OpenUI(Building building, PlayerState player)
    {
        gameObject.SetActive(true);

        image.sprite = Building.GetSprite(building.stats, player);

        List<KeyValuePair<string, string>> infoRows = new List<KeyValuePair<string, string>>{
            PairOf("Name: ", building.stats.name),
            PairOf("Max HP: ", building.stats.hp.ToString()),
            PairOf("HP regen: ", building.stats.regen.ToString() + " HP/round"),
            PairOf("Income: ", building.stats.income.ToString())
        };
        if (building is Hospital)
        {
            Hospital hospital = building as Hospital;
            infoRows.Add(PairOf("Healing: ", hospital.healAmount.ToString()));
            infoRows.Add(PairOf("Heal cost: ", hospital.healCost.ToString()));
        }

        for (int i = 0; i < infoRows.Count; i++)
        {
            InfoRow row = Instantiate(statsRowPrefab, statsList.transform);
            row.SetStats(infoRows[i].Key, infoRows[i].Value);
            statsRows.Add(row);
        }
    }


    public void CloseUI()
    {
        statsRows.ForEach(row => Destroy(row.gameObject));
        statsRows.Clear();
        gameObject.SetActive(false);
    }

    private KeyValuePair<string, string> PairOf(string key, string value)
    {
        return new KeyValuePair<string, string>(key, value);
    }
}
