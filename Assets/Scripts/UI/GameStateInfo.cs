using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;

public class GameStateInfo : MonoBehaviour, IUIWindow
{
    [SerializeField] private GameStateRow gameStatePlayerRow;
    [SerializeField] private GameObject statsRowsContainer;
    private IGameController gameController;
    [SerializeField] private TeamPalletes teamPalletes;

    void Update()
    {
        if (Input.GetKeyUp(KeyCode.Tab))
        {
            CloseUI();
        }
        if(gameController != null)
        {
            UpdateUI();
        }
    }

    public bool IsActive()
    {
        return gameObject.activeSelf;
    }

    public void OpenUI()
    {
        gameController = GameController.instance;
        gameObject.SetActive(true);

        UpdateUI();
    }

    public void CloseUI()
    {
        DestroyRows();

        gameObject.SetActive(false);
        gameController = null;
    }

    private void UpdateUI()
    {
        DestroyRows();

        List<PlayerState> players = gameController.GetPlayers();

        for (int i = 0; i < players.Count; i++)
        {
            GameObject row = Instantiate(gameStatePlayerRow.gameObject, statsRowsContainer.transform);

            GameStateRow rowScript = row.GetComponent<GameStateRow>();

            int income = gameController.GetBuildings()
                .FindAll(building => building.owner.Equals(players[i]))
                .ConvertAll<int>(building => building.stats.income)
                .Sum();

            int unitsCount = gameController.GetUnits()
                .FindAll(unit => unit.owner.Equals(players[i]))
                .Count;
            Color[] teamColors = teamPalletes.palettes[players[i].colorIndex].color;
            rowScript.SetStats(players[i].playerName.ToString(), players[i].gold, income, unitsCount, teamColors[teamColors.Length - 1]);
        }
    }

    private void DestroyRows()
    {
        foreach (Transform row in statsRowsContainer.transform)
        {
            Destroy(row.gameObject);
        }
    }
}
