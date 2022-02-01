using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameStateRow : MonoBehaviour
{
    [SerializeField] private TMP_Text playerNameText;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text incomeText;
    [SerializeField] private TMP_Text unitsCountText;
    [SerializeField] private Image playerNameBackgroud;

    public void SetStats(string playerName, int gold, int income, int unitsCount, Color teamColor)
    {
        playerNameText.text = playerName;
        playerNameBackgroud.color = teamColor;
        goldText.text = gold.ToString();
        incomeText.text = income.ToString();
        unitsCountText.text = unitsCount.ToString();
    }
}
