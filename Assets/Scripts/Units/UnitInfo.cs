using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitInfo : MonoBehaviour, IUIWindow
{
    public Recruitment barracksRecruitment;
    public Recruitment mercenaryCampRecruitment;
    public GameObject statsList;
    public InfoRow statsRowPrefab;
    public List<InfoRow> statsRows = new List<InfoRow>();
    public Image unitImage;

    public bool IsActive()
    {
        return gameObject.activeSelf;
    }

    public void OpenUI(Unit clickedUnit, PlayerState player)
    {
        gameObject.SetActive(true);

        unitImage.sprite = Unit.GetSprite(clickedUnit.stats, player);

        KeyValuePair<string, string>[] infoRows = new KeyValuePair<string, string>[]{
            PairOf("Name: ", clickedUnit.stats.name),
            PairOf("Health: ", clickedUnit.stats.hp.ToString()),
            PairOf("Armor: ", clickedUnit.stats.armor.ToString()),
            PairOf("Attack: ", clickedUnit.stats.attack.ToString()),
            PairOf("Movement: ", clickedUnit.stats.movementSpeed.ToString()),
            PairOf("Actions: ", clickedUnit.stats.actions.ToString()),
            PairOf("Attack range: ", clickedUnit.stats.attackRange.ToString()),
            PairOf("Counter attack: ", clickedUnit.stats.counterAttack ? "yes" : "no"),
            PairOf("Cost: ", clickedUnit.stats.cost.ToString()),
            PairOf("Available at: ", (
                barracksRecruitment.buyableUnits.Contains(clickedUnit.stats) ?
                "Barracks" :
                (
                    mercenaryCampRecruitment.buyableUnits.Contains(clickedUnit.stats) ?
                    "Camp" :
                    "None"
                )
            ))
        };

        for (int i = 0; i < infoRows.Length; i++)
        {
            InfoRow row = Instantiate<InfoRow>(statsRowPrefab, statsList.transform);
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
