using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGameController
{
    MapManager GetMapManager();

    PlayerState GetCurrentPlayer();

    bool IsCurrentPlayer(PlayerState playerState);

    List<PlayerState> GetPlayers();

    List<Building> GetBuildings();

    List<Unit> GetUnits();

    bool AllowInput();

    void NextTurn(bool isAI);

    int GetRound();

    bool IsReady();
    
    void ExitGame(PlayerState playerState);

    Building GetBuildingAt(Vector3Int point);

    Unit GetUnitAt(Vector3Int point);

    void MoveUnit(Unit unit, Vector3Int to, bool isAI);

    void UnitInteractBuilding(Unit unit, Building building, bool isAI);

    void InteractBuilding(Building building, bool isAI);

    void UnitAttackBuilding(Unit attacking, Building defending, bool isAI);

    void UnitAttackUnit(Unit attacking, Unit defending, bool isAI);

    void CaptureBuilding(Unit unit, Building building, bool isAI);

    void RecruitUnit(UnitStats unitStats, Vector3Int position, Building usedBuilding, bool isAI);

    List<Vector3Int> GetWalkRangeLocations(Unit unit);

    List<Vector3Int> GetAttackRangeLocations(Unit unit);

    List<Vector3Int> GetEmptyNeighbours(Vector3Int position);

    List<Vector3Int> GetPath(Vector3Int from, Vector3Int to);

    List<Vector3Int> GetPositionToAttackPoint(Vector3Int pointToAttack, Unit unitAttacking);
    (int, int) UnitPossibleDamageUnit(Unit attacking, Unit defending);
    int UnitPossibleDamageBuilding(Unit attacking);

}
