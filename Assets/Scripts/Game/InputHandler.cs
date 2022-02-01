using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// This class recognizes input from a player, validates it and communicates player's intentions to other components of the game (game controller, map manager).
/// It shouldn't make any changes to the state of the game.
/// </summary>
public class InputHandler : MonoBehaviour
{
    public PlayerState thisPlayer;
    private PlayerState spectatedPlayer;
    private IGameController gameController;
    private Unit selectedUnit;
    private static IHoverInfo hoveredEntity;
    private List<Vector3Int> walkRangeLocations;
    private List<Vector3Int> attackRangeLocations;

    private void Start()
    {
        gameController = GameController.instance;
    }

    private void Update()
    {
        if (!gameController.IsReady())
            return;

        if (thisPlayer.hasLost)
        {
            spectatedPlayer = gameController.GetCurrentPlayer();
        }
        else
        {
            spectatedPlayer = thisPlayer;
        }

        UpdateUI();

        if (Input.GetKeyDown(KeyCode.Tab))
        {
            HandleShowGameStats();
        }

        if (gameController.AllowInput() && !thisPlayer.hasLost)
        {
            if (Input.GetMouseButtonDown(0))
            {
                if (EventSystem.current.IsPointerOverGameObject())
                    return;

                PlayerLeftClick();
            }

            if (Input.GetMouseButtonDown(1))
            {
                if (EventSystem.current.IsPointerOverGameObject())
                    return;

                PlayerRightClick();
            }

            MouseHover();
        }
        else
        {
            gameController.GetMapManager().ClearHoverIndicator();
        }
    }

    public void ExitGame()
    {
        gameController.ExitGame(thisPlayer);
    }

    public void OpenSettings()
    {
        GameUIReferences.instance.settings.OpenUI();
    }

    public void NextTurn()
    {
        if (gameController.AllowInput() && gameController.IsCurrentPlayer(thisPlayer))
            gameController.NextTurn(false);
    }

    public void UpdateUI()
    {
        float rangeScale = 1.5f;
        if(Vector3.Distance(Input.mousePosition, GameUIReferences.instance.mainUI.position) < GameUIReferences.instance.mainUI.sizeDelta.magnitude * rangeScale)
        {
            Vector2 lastPos = new Vector2(GameUIReferences.instance.mainUI.localPosition.x, GameUIReferences.instance.mainUI.localPosition.y);
            GameUIReferences.instance.mainUI.anchorMin = new Vector2(1 - GameUIReferences.instance.mainUI.anchorMin.x, GameUIReferences.instance.mainUI.anchorMin.y);
            GameUIReferences.instance.mainUI.anchorMax = new Vector2(1 - GameUIReferences.instance.mainUI.anchorMax.x, GameUIReferences.instance.mainUI.anchorMax.y);
            GameUIReferences.instance.mainUI.pivot = new Vector2(1 - GameUIReferences.instance.mainUI.pivot.x, GameUIReferences.instance.mainUI.pivot.y);
            GameUIReferences.instance.mainUI.localPosition = new Vector2(-lastPos.x, lastPos.y);
        }

        GameUIReferences.instance.endTurnButton.interactable = gameController.IsCurrentPlayer(thisPlayer);
        GameUIReferences.instance.playerName.text = spectatedPlayer.playerName;

        Color[] teamColors = GameUIReferences.instance.teamPalletes.palettes[spectatedPlayer.colorIndex].color;
        //GameUIReferences.instance.playerName.color = teamColors[teamColors.Length - 1];
        GameUIReferences.instance.playerNameBackground.color = teamColors[teamColors.Length - 1];

        GameUIReferences.instance.goldText.text = spectatedPlayer.gold.ToString();
        GameUIReferences.instance.turnText.text = "Turn: " + gameController.GetRound().ToString().PadLeft(2, '0');
    }

    private void PlayerRightClick()
    {
        Vector3Int clickedPos = Vector3Int.FloorToInt(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        clickedPos.z = 0;

        Building clickedBuilding = gameController.GetBuildingAt(clickedPos);
        Unit clickedUnit = gameController.GetUnitAt(clickedPos);

        if(clickedUnit != null)
        {
            HandleUnitInfo(clickedUnit);
        }
        else if(clickedBuilding != null)
        {
            HandleBuildingInfo(clickedBuilding);
        }
    }

    private void HandleBuildingInfo(Building building)
    {
        BuildingInfo statsInfo = GameUIReferences.instance.buildingInfo;
        statsInfo.OpenUI(building, thisPlayer);
    }

    private void HandleUnitInfo(Unit unit)
    {
        UnitInfo statsInfo = GameUIReferences.instance.unitInfo;
        statsInfo.OpenUI(unit, thisPlayer);
    }

    private void HandleShowGameStats()
    {
        GameStateInfo statsInfo = GameUIReferences.instance.gameStateInfo;
        statsInfo.OpenUI();
    }

    private void MouseHover()
    {
        Vector3Int mousePosition = Vector3Int.FloorToInt(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        mousePosition.z = 0;
        gameController.GetMapManager().SetHoverIndicator(mousePosition);

        Unit hoveredUnit = gameController.GetUnitAt(mousePosition);
        Building hoveredBuilding = gameController.GetBuildingAt(mousePosition);

        if (hoveredUnit != null)
        {
            OnHoverEntity(hoveredUnit);
        }
        else if (hoveredBuilding != null)
        {
            OnHoverEntity(hoveredBuilding);
        }
        else
        {
            OnUnhoverEntity();
        } 

    }

    private void PlayerLeftClick()
    {
        Vector3Int clickedPos = Vector3Int.FloorToInt(Camera.main.ScreenToWorldPoint(Input.mousePosition));
        clickedPos.z = 0;

        Building clickedBuilding = gameController.GetBuildingAt(clickedPos);
        Unit clickedUnit = gameController.GetUnitAt(clickedPos);

        if (selectedUnit == null)
        {
            if (clickedBuilding != null)
            {
                HandleBuildingSelect(clickedBuilding);
            }
            else if (clickedUnit != null)
            {
                HandleUnitSelect(clickedUnit);
            }
        }
        else
        {
            if (clickedBuilding != null)
            {
                HandleUnitBuldingInteraction(clickedBuilding, clickedPos);
            }
            else if (clickedUnit != null)
            {
                HandleUnitUnitInteraction(clickedUnit, clickedPos);
            }
            else
            {
                HandleUnitMove(clickedPos);
            }
        }
    }

    private void HandleBuildingSelect(Building clickedBuilding)
    {
        if (gameController.IsCurrentPlayer(thisPlayer) && thisPlayer.Equals(clickedBuilding.owner))
        {
            gameController.InteractBuilding(clickedBuilding, false);
        }
    }

    private void HandleUnitSelect(Unit clickedUnit)
    {
        DeselectUnit(); // to clear a potential previous selection
        selectedUnit = clickedUnit;

        walkRangeLocations = gameController.GetWalkRangeLocations(clickedUnit);
        attackRangeLocations = gameController.GetAttackRangeLocations(clickedUnit);

        gameController.GetMapManager().SetMoveIndicator(walkRangeLocations);
        gameController.GetMapManager().SetAttackIndicator(attackRangeLocations);

    }

    private void HandleUnitMove(Vector3Int clickedPos)
    {
        if (thisPlayer.Equals(selectedUnit.owner) && gameController.IsCurrentPlayer(thisPlayer)) // if it's our unit and it's our turn
        {
            if (walkRangeLocations.Contains(clickedPos))
            {
                gameController.MoveUnit(selectedUnit, clickedPos, false);
            }
        }
        DeselectUnit();
    }

    private void HandleUnitUnitInteraction(Unit unit, Vector3Int clickedPos)
    {
        if (unit == selectedUnit) // if clicked the same unit again then deselect
        {
            DeselectUnit();
        }
        else if (!thisPlayer.Equals(selectedUnit.owner)) // can't attack with someone else's unit
        {
            Debug.Log("You can't attack with someone else's unit");
            DeselectUnit();
        }
        else if (thisPlayer.Equals(unit.owner)) // can't attack allied unit
        {
            Debug.Log("You can't attack allied units");
            DeselectUnit();
        }
        else if (!gameController.IsCurrentPlayer(thisPlayer)) // can't attack when it's not our turn
        {
            Debug.Log("You can only attack during your turn");
            DeselectUnit();
        }
        else
        {
            if (selectedUnit.actions > 0 && attackRangeLocations.Contains(clickedPos))
            {
                gameController.UnitAttackUnit(selectedUnit, unit, false);
            }
            DeselectUnit();
        }
    }

    private void HandleUnitBuldingInteraction(Building building, Vector3Int clickedPos)
    {
        if (selectedUnit.actions > 0 && gameController.IsCurrentPlayer(thisPlayer) && selectedUnit.owner.Equals(thisPlayer)) // unit has action and it's our turn and it's our unit
        {
            Vector3Int selectedUnitPosition = Vector3Int.FloorToInt(selectedUnit.transform.localPosition);
            Vector3Int buildingPosition = Vector3Int.FloorToInt(building.transform.localPosition);
            if (building.owner.playerID == -1) // building is neutral
            {
                if (Vector3Int.Distance(selectedUnitPosition, buildingPosition) <= 1.1)
                {
                    gameController.CaptureBuilding(selectedUnit, building, false);
                }
            }
            else if (thisPlayer.Equals(building.owner)) // we own the building
            {
                if (Vector3Int.Distance(selectedUnitPosition, buildingPosition) <= 1.1)
                {
                    gameController.UnitInteractBuilding(selectedUnit, building, false);
                }
            }
            else // building is owned by an enemy
            {
                if (attackRangeLocations.Contains(clickedPos))
                {
                    gameController.UnitAttackBuilding(selectedUnit, building, false);
                }
            }
        }
        DeselectUnit();
    }

    private void DeselectUnit()
    {
        if(selectedUnit != null)
        {
            selectedUnit = null;
        }
        walkRangeLocations = null;
        attackRangeLocations = null;
        gameController.GetMapManager().ClearMoveIndicator();
        gameController.GetMapManager().ClearAttackIndicator();  
    }

    private void OnHoverEntity(IHoverInfo hoveredEntity)
    {
        if (hoveredEntity != InputHandler.hoveredEntity)
        {
            OnUnhoverEntity();
            hoveredEntity.OnHover();
            InputHandler.hoveredEntity = hoveredEntity;
        }
    }

    private void OnUnhoverEntity()
    {
        if (hoveredEntity != null)
        {
            hoveredEntity.OnUnhover();
            hoveredEntity = null;
        }
    }

    public static void EntityKilled(IHoverInfo entity)
    {
        if (hoveredEntity == entity)
            hoveredEntity = null;
    }
}
