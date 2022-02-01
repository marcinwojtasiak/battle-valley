using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Recruitment : MonoBehaviour, IUIWindow
{
    public List<UnitStats> buyableUnits;
    public GameObject buyList;
    public RecruitmentRow buyRowPrefab;
    public Image unitImage;

    private IGameController gameController;
    private PlayerState player;
    private Building openedFrom;

    public bool IsActive()
    {
        return gameObject.activeSelf;
    }

    public void OpenUI(PlayerState player, Building openedFrom)
    {
        gameObject.SetActive(true);

        this.player = player;
        this.openedFrom = openedFrom;
        gameController = GameController.instance;

        for (int i = 0; i < buyableUnits.Count; i++)
        {
            GameObject row = Instantiate(buyRowPrefab.gameObject, buyList.transform);
            RecruitmentRow rowScript = row.GetComponent<RecruitmentRow>();
            rowScript.unit = buyableUnits[i];
            rowScript.index = i;
            rowScript.SetDisabled(openedFrom.isAlreadyUsedInTurn || player.gold < buyableUnits[i].cost || !NextSpawnPosition().HasValue);
        }

        ShowInfo(0);
    }

    public void CloseUI()
    {
        foreach (Transform row in buyList.transform)
        {
            Destroy(row.gameObject);
        }

        gameObject.SetActive(false);

        player = null;
        openedFrom = null;
        gameController = null;
    }

    public void RecruitUnit(int index)
    {
        UnitStats unit = buyableUnits[index];
        if (player.gold < unit.cost)
        {
            Debug.Log("Too little gold to buy this unit: " + unit.name);
            return;
        }
        Vector3Int? spawnPos = NextSpawnPosition();
        if (!spawnPos.HasValue)
        {
            Debug.Log("No place to put new unit");
            return;
        }

        gameController.RecruitUnit(unit, spawnPos.Value, openedFrom, false);
        
        Debug.Log("recruited " + unit.name);
        CloseUI();
    }

    private Vector3Int? NextSpawnPosition()
    {
        Vector3Int buildingPosition = Vector3Int.FloorToInt(openedFrom.transform.position);
        List<Vector3Int> spawningPositions = gameController.GetEmptyNeighbours(buildingPosition);
        foreach (Vector3Int position in spawningPositions)
        {
            if (CanBeSpawned(position))
            {
                return position;
            }
        }
        return null;
    }

    private bool CanBeSpawned(Vector3Int position)
    {
        TileType? tile = gameController.GetMapManager().GetTileAt(position.x, position.y);
        return tile.HasValue && gameController.GetMapManager().GetTileCost(tile.Value) != -1;
    }

    public void ShowInfo(int index)
    {
        UnitStats unit = buyableUnits[index];
        unitImage.sprite = Unit.GetSprite(unit, player);
    }
}
