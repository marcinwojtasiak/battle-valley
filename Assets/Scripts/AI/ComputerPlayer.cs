using System;
using System.Collections.Generic;
using UnityEngine;
using Random = System.Random;

public class ComputerPlayer : MonoBehaviour, IAiOpponent
{
    public PlayerState PlayerState { get; private set; }
    private IGameController gameController;
    private DifficultyType difficulty;
    [SerializeField] private List<UnitStats> buyableUnitsBarracks;
    [SerializeField] private List<UnitStats> buyableUnitsMercenaryCamp;
    [SerializeField] private States currentState = States.CHOOSE;
    private List<Unit> unitsToMove = null;
    private int numberOfTriesToBuy = 3;
    private Unit movingUnit = null;
    private object target = null;
    private Random rand = new Random();
    private bool hasTurn = false;
    List<Vector3Int> directions = new List<Vector3Int>() { Vector3Int.up, Vector3Int.down, Vector3Int.left, Vector3Int.right };

    public void Initialize(PlayerState playerState, DifficultyType difficulty)
    {
        this.gameController = GameController.instance;
        this.PlayerState = playerState;
        this.difficulty = difficulty;
    }

    private void Update()
    {
        if (hasTurn && gameController.AllowInput())
        {
            MakePlay();
        }
    }

    #region GameControllerActions
    public bool UnitMove(Unit ally, Vector3Int position)
    {
        gameController.MoveUnit(ally, position, true);

        return true;
    }

    public bool UnitAttack(Unit ally, Unit enemy)
    {
        gameController.UnitAttackUnit(ally, enemy, true);
        return true;
    }

    public bool UnitInteract(Unit ally, Building building)
    {
        if (building.owner.playerID == -1)
            gameController.CaptureBuilding(ally, building, true);
        else if (building.owner.playerID != gameController.GetCurrentPlayer().playerID)
            gameController.UnitAttackBuilding(ally, building, true);
        else
            gameController.UnitInteractBuilding(ally, building, true);
        return true;
    }
    public void EndTurn()
    {
        hasTurn = false;
        gameController.NextTurn(true);
    }

    #endregion

    #region CheckGamestate

    public Vector3Int PointToPosition(Vector3 point)
    {
        return Vector3Int.FloorToInt(point);
    }
    public List<Unit> GetAllUnits()
    {
        return gameController.GetUnits();
    }
    public List<Building> GetAllBuildings()
    {
        return gameController.GetBuildings();
    }
    public int GetAllyGold()
    {
        return gameController.GetCurrentPlayer().gold;
    }

    public object GetObjectAtLocation(Vector3Int position)
    {
        if (gameController.GetUnitAt(position) != null)
            return gameController.GetUnitAt(position);
        else if (gameController.GetBuildingAt(position) != null)
            return gameController.GetBuildingAt(position);
        else
            return null;
    }

    public Vector3Int GetLocationOfObject(object obj)
    {
        if (obj is Unit)
            return Vector3Int.FloorToInt(((Unit)obj).transform.position);
        else
            return Vector3Int.FloorToInt(((Building)obj).transform.position);

    }

    public List<Vector3Int> GetPossibleWalkLocations(Unit ally)
    {
        return gameController.GetWalkRangeLocations(ally);
    }

    #endregion

    #region BasicGameControl
    public void StartTurn()
    {
        hasTurn = true;
    }
    public Node NoMoveTree()
    {
        /*
	     *
         *          Root
         *            |
         *          IDLE
         * 
         */
        Func<Unit, bool> alwaysTrue = x => true;
        return new Node(alwaysTrue, null, null, rightA: ActionType.STAY);
    }
    public Node EasyTree()
    {
        /*
         *                 Root
         *              /      \
         *             LNode    HELP 
         *           /      \
         *      LLNode        ATTACK 
         *      /   \                 
         *  ADVANCE  CONQUER           
         */

        Node LLNode = new Node(IsBuildingInRange, null, null, ActionType.ADVANCE, ActionType.CONQUER);
        Node LNode = new Node(IsEnemyInRange, LLNode, null, rightA: ActionType.ATTACK);
        Node root = new Node(ShouldUseHospital, LNode, null, rightA: ActionType.HELP);
        return root;
    }
    private Node MediumTree()
    {
        /*
         *                    Root
         *                  /      \
         *                 LNode    KILL 
         *               /      \
         *           LLNode   <-  LRNode
         *           /   \              \   
         *        LLLNode   ATTACK        HEAL 
         *       /       \
         *     ADVANCE   CONQUER
         */

        Node LLLNode = new Node(BestBuildingInRange, null, null, ActionType.ADVANCE, ActionType.CONQUER);
        Node LLNode = new Node(BestCanDamage, LLLNode, null, rightA: ActionType.ATTACK);
        Node LRNode = new Node(ShouldHeal, LLNode, null, rightA: ActionType.HELP);
        Node LNode = new Node(CouldHeal, LLNode, LRNode);
        Node root = new Node(ShouldKill, LNode, null, rightA: ActionType.KILL);
        return root;
    }
    private Node HardTree()
    {
        Node LRLRLLNode =   new Node(BestBuildingInRange, null, null, leftA: ActionType.STAY, rightA: ActionType.CONQUER);
        Node LRLRLNode =    new Node(BestCanDamage, LRLRLLNode, null, rightA: ActionType.ATTACK);
        Node LRLRRNode =    new Node(ShouldHeal, LRLRLNode, null, rightA: ActionType.HELP);
        Node LRLRNode =     new Node(CouldHeal, LRLRLNode, LRLRRNode);
        Node LRLLLLNode =   new Node(FindBestPlaceToStay, null, null, leftA: ActionType.STAY, rightA: ActionType.ADVANCE);
        Node LRLLLNode =    new Node(BestBuildingInRange, LRLLLLNode, null, rightA: ActionType.CONQUER);
        Node LRLLNode =     new Node(BestCanDamage, LRLLLNode, null, rightA: ActionType.ATTACK);
        Node LRLNode =      new Node(ImportantTarget, LRLLNode, LRLRNode);
        Node LLLLNode =     new Node(BestBuildingInRange, null, null, leftA: ActionType.ADVANCE, rightA: ActionType.CONQUER);
        Node LLLNode =      new Node(BestCanDamage, LLLLNode, null, rightA: ActionType.ATTACK);
        Node LRNode =       new Node(ShouldKillHard, LRLNode, null, rightA: ActionType.KILL);
        Node LLNode =       new Node(ShouldGoToHospital, LLLNode, null, rightA: ActionType.TRAVEL);
        Node LNode =        new Node(AtTagretLocation, LLNode, LRNode);
        Node root =         new Node(CanTakeover, LNode, null, rightA: ActionType.CONQUER); 
        return root;
    }
    public Node ChooseTree()
    {
        switch (difficulty)
        {
            case DifficultyType.NO_MOVE:
                return NoMoveTree();
            case DifficultyType.EASY:
                return EasyTree();
            case DifficultyType.MEDIUM:
                return MediumTree();
            case DifficultyType.HARD:
                return HardTree();
        }
        throw new NotImplementedException("That difficulty is not implemented!");
    }
    public void MakePlay()
    {
        Debug.Log("Current state: " + currentState);
        switch (currentState)
        {
            case States.BUY:
                currentState = MachineBuyUnitState();
                break;
            case States.CHOOSE:
                currentState = MachineChooseUnitState();
                break;
            case States.HEAL:
                currentState = MachineHealState();
                break;
            case States.ATTACK:
                currentState = MachineAttackState();
                break;
            case States.MOVE:
                currentState = MachineMoveState();
                break;
            case States.FINISH:
                currentState = MachineFinishState();
                break;
        }
        return;
    }

    #endregion

    private States MachineMoveState()
    {
        if (unitsToMove.Count == 0)
            return States.FINISH;


        movingUnit = unitsToMove[0];
        if (movingUnit == null || movingUnit.actions == 0)
        {
            unitsToMove.RemoveAt(0);
            if (unitsToMove.Count == 0)
                return States.FINISH;
            movingUnit = unitsToMove[0];
        }

        ActionType currentAction = ChooseTree().Choice(movingUnit);

        Debug.Log("Current action: " + currentAction);
        States newState = States.FINISH;
        switch (currentAction)
        {
            case ActionType.HELP:
                newState = MoveToHospital();
                break;
            case ActionType.ATTACK:
                newState = MoveToEnemy();
                break;
            case ActionType.KILL:
                newState = MoveToEnemy();
                break;
            case ActionType.ADVANCE:
                newState = TryToAdvance();
                break;
            case ActionType.CONQUER:
                newState = MoveToEnemy();
                break;
            case ActionType.TRAVEL:
                newState = TravelToBuilding();
                break;
            case ActionType.ESCAPE:
                newState = MoveToEscape();
                break;
            case ActionType.STAY:
                newState = Idle();
                break;
        }
        return newState;

    }


    #region PerformActionFunctions

    private States MoveToEscape()
    {
        Debug.LogWarning("Will escape now!");
        Vector3Int toEscape;
        toEscape = (Vector3Int)target;
        UnitMove(movingUnit, toEscape);
        movingUnit.actions = 0; // Zmuszenie do zakoñczenia dzia³ania
        return States.MOVE;

    }

    private States TravelToBuilding()
    {
        Vector3Int toAttack;
        toAttack = PointToPosition(((Building)target).transform.position);
        List<Vector3Int> walkPositions = gameController.GetWalkRangeLocations(movingUnit);

        foreach (var position in gameController.GetEmptyNeighbours(toAttack))
        {
            List<Vector3Int> path = gameController.GetPath(PointToPosition(movingUnit.transform.position), position);
            if (path.Count > 0)
            {
                for (int i = path.Count - 1; i >= 0; i--)
                    if (walkPositions.Contains(path[i]))
                    {
                        UnitMove(movingUnit, path[i]);
                        return States.MOVE;
                    }
            }
        }
        foreach (var direction in directions)
        {
            foreach (var metaPosition in gameController.GetEmptyNeighbours(toAttack + direction))
            {
                List<Vector3Int> path = gameController.GetPath(PointToPosition(movingUnit.transform.position), metaPosition);
                if (path.Count > 0)
                {
                    for (int i = path.Count - 1; i >= 0; i--)
                        if (walkPositions.Contains(path[i]))
                        {
                            UnitMove(movingUnit, path[i]);
                            return States.MOVE;
                        }

                }
            }
        }

        if (walkPositions.Count > 0)
            UnitMove(movingUnit, walkPositions[rand.Next(0, walkPositions.Count)]);
        else
            movingUnit.actions = 0; // Zmuszenie do zakoñczenia dzia³ania
        return States.MOVE;
    }
    private States Idle()
    {
        Debug.LogWarning("Will stay now!");
        movingUnit.actions = 0; // Zmuszenie do zakoñczenia dzia³ania
        return States.MOVE;
    }

    private States TryToAdvance()
    {
        List<Vector3Int> walkPositions = gameController.GetWalkRangeLocations(movingUnit);
        List<Building> enemyBuildings = GetAllBuildings().FindAll(x => x.owner != gameController.GetCurrentPlayer());
        Vector3Int enemyPoz = new Vector3Int();
        foreach (var aB in enemyBuildings)
            if (aB is Stronghold)
            {
                enemyPoz = PointToPosition(((Building)aB).transform.position);
                break;
            }
        
        foreach (var position in gameController.GetEmptyNeighbours(enemyPoz))
        {
            List<Vector3Int> path = gameController.GetPath(PointToPosition(movingUnit.transform.position), position);
            if (path.Count > 0)
            {
                for (int i = path.Count - 1; i >= 0; i--)
                    if (walkPositions.Contains(path[i]))
                    {
                        UnitMove(movingUnit, path[i]);
                        return States.MOVE;
                    }

            }
        }
        foreach (var direction in directions)
        {
            foreach (var metaPosition in gameController.GetEmptyNeighbours(enemyPoz + direction))
            {
                List<Vector3Int> path = gameController.GetPath(PointToPosition(movingUnit.transform.position), metaPosition);
                if (path.Count > 0)
                {
                    for (int i = path.Count - 1; i >= 0; i--)
                        if (walkPositions.Contains(path[i]))
                        {
                            UnitMove(movingUnit, path[i]);
                            return States.MOVE;
                        }

                }
            }
        }
        if (walkPositions.Count > 0)
            UnitMove(movingUnit, walkPositions[rand.Next(0, walkPositions.Count)]);
        else
            movingUnit.actions = 0; // Zmuszenie do zakoñczenia dzia³ania
        return States.MOVE;
    }

    private States MoveToEnemy()
    {
        Vector3Int toAttack;
        if (target is Unit)
            toAttack = PointToPosition(((Unit)target).transform.position);
        else if (target is Building)
        {
            toAttack = PointToPosition(((Building)target).transform.position);
            if (((Building)target).owner.playerID == -1)
            {
                UnitMove(movingUnit, gameController.GetEmptyNeighbours(toAttack)[0]);
                return States.ATTACK;
            }
        }
        else
            throw errorThrow();

        

        List<Vector3Int> damagePositions = gameController.GetPositionToAttackPoint(toAttack, movingUnit);
        damagePositions.Remove(toAttack);
        List<Vector3Int> walkPositions = gameController.GetWalkRangeLocations(movingUnit);
        walkPositions.Add(PointToPosition(movingUnit.transform.position));
        foreach (var move in walkPositions)
        {
            if (damagePositions.Contains(move))
            {
                UnitMove(movingUnit, move);
                return States.ATTACK;
            }
        }
        throw errorThrow();


    }

    private States MoveToHospital()
    {
        List<Building> allyBuildings = GetAllBuildings().FindAll(x => x.owner != null && x.owner == gameController.GetCurrentPlayer());
        List<Vector3Int> walkPositions = GetPossibleWalkLocations(movingUnit);
        walkPositions.Insert(0, GetLocationOfObject(movingUnit));
        foreach (var allyBuild in allyBuildings)
        {
            if (allyBuild is Hospital)
            {
                foreach (var direction in directions)
                {
                    if (walkPositions.Contains(PointToPosition(allyBuild.transform.position + direction)))
                    {
                        UnitMove(movingUnit, PointToPosition(allyBuild.transform.position + direction));
                        return States.HEAL;
                    }
                }
            }
        }
        throw errorThrow();
    }

    private bool CanBeSpawned(Vector3Int position)
    {
        TileType? tile = gameController.GetMapManager().GetTileAt(position.x, position.y);
        return tile.HasValue && gameController.GetMapManager().GetTileCost(tile.Value) != -1;
    }

    #endregion

    #region MinorMachineStates

    private States MachineChooseUnitState()
    {
        List<Unit> allyUnits = GetAllUnits().FindAll(x => x.owner == gameController.GetCurrentPlayer());
        unitsToMove = allyUnits;

        return States.BUY;
    }
    private States MachineBuyUnitState()
    {
        List<Building> allyBuildings = GetAllBuildings().FindAll(x => x.owner != null && x.owner == gameController.GetCurrentPlayer());
        List<Unit> allyUnits = GetAllUnits().FindAll(x => x.owner == gameController.GetCurrentPlayer());
        if (allyUnits.Count == 0)
        {
            foreach (var aB in allyBuildings)
            {
                if (aB is Stronghold)
                {
                    for (int i = 0; i < buyableUnitsBarracks.Count; i++)
                    {
                        if (buyableUnitsBarracks[i].name == "Scout")
                        {
                            Vector3Int buildingPosition = PointToPosition(aB.transform.position);
                            List<Vector3Int> spawningPositions = gameController.GetEmptyNeighbours(buildingPosition);
                            foreach (Vector3Int position in spawningPositions)
                            {
                                if (CanBeSpawned(position))
                                {
                                    gameController.RecruitUnit(buyableUnitsBarracks[i], position, aB, true);
                                    Debug.Log("recruited " + buyableUnitsBarracks[i].name);
                                    return States.MOVE;
                                }
                            }
                            break;
                        }
                    }

                }
            }
        }
        foreach (var aB in allyBuildings)
        {
            
            Debug.Log(aB.isAlreadyUsedInTurn);
            if (aB.isAlreadyUsedInTurn)
                continue;
            if (aB is Stronghold || aB is Barracks)
            {
                for (int i = 0; i < Math.Max(numberOfTriesToBuy, 2* numberOfTriesToBuy - allyUnits.Count); i++)
                {
                    int id = rand.Next(0, buyableUnitsBarracks.Count);
                    if (GetAllyGold() >= buyableUnitsBarracks[id].cost)
                    {
                        Vector3Int buildingPosition = PointToPosition(aB.transform.position);
                        List<Vector3Int> spawningPositions = gameController.GetEmptyNeighbours(buildingPosition);
                        foreach (Vector3Int position in spawningPositions)
                        {
                            if (CanBeSpawned(position))
                            {
                                gameController.RecruitUnit(buyableUnitsBarracks[id], position, aB, true);
                                Debug.Log("recruited " + buyableUnitsBarracks[id].name);
                                break;
                            }
                        }
                        break;
                    }
                }

            }
            else if (aB is MercenaryCamp)
            {
                for (int i = 0; i < Math.Max(numberOfTriesToBuy, 2 * numberOfTriesToBuy - allyUnits.Count); i++)
                {
                    int id = rand.Next(0, buyableUnitsMercenaryCamp.Count);
                    if (GetAllyGold() >= buyableUnitsMercenaryCamp[id].cost)
                    {
                        Vector3Int buildingPosition = PointToPosition(aB.transform.position);
                        List<Vector3Int> spawningPositions = gameController.GetEmptyNeighbours(buildingPosition);
                        foreach (Vector3Int position in spawningPositions)
                        {
                            if (CanBeSpawned(position))
                            {
                                gameController.RecruitUnit(buyableUnitsMercenaryCamp[id], position, aB, true);
                                Debug.Log("recruited " + buyableUnitsMercenaryCamp[id].name);
                                break;
                            }
                        }
                        break;
                    }
                }
            }
        }
        return States.MOVE;
    }
    private States MachineFinishState()
    {

        EndTurn();
        return States.CHOOSE;
    }
    private States MachineAttackState()
    {

        if (target is Unit)
            UnitAttack(movingUnit, (Unit)target);
        else if (target is Building)
            UnitInteract(movingUnit, (Building)target);
        else
            throw errorThrow();

        return States.MOVE;
    }
    private States MachineHealState()
    {
        List<Building> allyBuildings = GetAllBuildings().FindAll(x => x.owner != null && x.owner == gameController.GetCurrentPlayer());
        foreach (var allyBuild in allyBuildings)
        {
            if (allyBuild is Hospital)
            {
                foreach (var direction in directions)
                {
                    if (PointToPosition(allyBuild.transform.position + direction).Equals(PointToPosition(movingUnit.transform.position)))
                    {
                        UnitInteract(movingUnit, allyBuild);
                        movingUnit.actions = 0;
                        return States.MOVE;
                    }
                }
            }
        }
        throw errorThrow();
    }

    #endregion

    #region EasyTreeNodes

    private bool ShouldUseHospital(Unit unit) // FOR: EASY TREE 
    {
        List<Vector3Int> movesEnemy = gameController.GetWalkRangeLocations(unit);
        movesEnemy.Add(PointToPosition(unit.transform.position));

        foreach (var move in movesEnemy)
        {
            foreach (var direction in directions)
            {
                object objectAtPosition = GetObjectAtLocation(move + direction);
                if (objectAtPosition is Hospital)
                {
                    Hospital hos = ((Hospital)objectAtPosition);
                    if (hos.owner != null && hos.owner.playerID == unit.owner.playerID && !hos.isAlreadyUsedInTurn)
                    {
                        if (unit.hp < unit.stats.hp)
                        {
                            target = hos;
                            return true;
                        }
                            
                        break;
                    }
                }

            }
        }


        return false;
    }
    private bool IsEnemyInRange(Unit unit) // FOR: EASY TREE 
    {
        List<Unit> enemyUnits = GetAllUnits().FindAll(x => x.owner != gameController.GetCurrentPlayer());
        List<Vector3Int> movesAlly = gameController.GetWalkRangeLocations(unit);
        movesAlly.Add(PointToPosition(unit.transform.position));
        int maxPossibleHPDiff = -100;
        Unit enemyToHpDiff = null;
        foreach (var enemy in enemyUnits)
        {
            Vector3Int enemyPos = Vector3Int.FloorToInt(enemy.transform.position);

            List<Vector3Int> attackDirections = gameController.GetPositionToAttackPoint(enemyPos, unit);
            foreach (var direction in attackDirections) // for every possible direction at location
            {
                if (movesAlly.Contains(direction))
                {
                    (int, int) dmg = gameController.UnitPossibleDamageUnit(unit, enemy);
                    if (maxPossibleHPDiff < dmg.Item1 - dmg.Item2)
                    {
                        maxPossibleHPDiff = dmg.Item1 - dmg.Item2;
                        enemyToHpDiff = enemy;

                    }
                }
            }
        }

        if (enemyToHpDiff != null & maxPossibleHPDiff > -100)
        {
            target = enemyToHpDiff;
            return true;
        }
        return false;
    }
    private bool IsBuildingInRange(Unit unit) // FOR: EASY TREE 
    {
        List<Building> enemyBuildings = GetAllBuildings().FindAll(x => x.owner == null || x.owner != gameController.GetCurrentPlayer());
        List<Vector3Int> movesAlly = gameController.GetWalkRangeLocations(unit);
        movesAlly.Add(PointToPosition(unit.transform.position));
        foreach (var enemy in enemyBuildings)
        {
            Vector3Int enemyPos = Vector3Int.FloorToInt(enemy.transform.position);

            List<Vector3Int> attackDirections = gameController.GetPositionToAttackPoint(enemyPos, unit);
            foreach (var direction in attackDirections) // for every possible direction at location
            {
                if (movesAlly.Contains(direction))
                {
                    target = enemy;
                    return true;
                }
            }
        }
        return false;
    }

    #endregion

    #region MediumTreeNodes



    private bool ShouldKill(Unit unit) // FOR: MEDIUM TREE 
    {
        List<Unit> enemyUnits = GetAllUnits().FindAll(x => x.owner != gameController.GetCurrentPlayer());
        List<Vector3Int> movesAlly = gameController.GetWalkRangeLocations(unit);
        movesAlly.Add(PointToPosition(unit.transform.position));
        double enemyValue = 0;
        foreach (var enemy in enemyUnits)
        {
            Vector3Int enemyPos = Vector3Int.FloorToInt(enemy.transform.position);

            List<Vector3Int> attackDirections = gameController.GetPositionToAttackPoint(enemyPos, unit);
            foreach (var direction in attackDirections) // for every possible direction at location
            {
                if (movesAlly.Contains(direction))
                {
                    (int, int) dmg = gameController.UnitPossibleDamageUnit(unit, enemy);
                    if (enemy.hp < dmg.Item1)
                    {
                        if (enemy.hp / (double)enemy.stats.hp * enemy.stats.cost > enemyValue)
                        {
                            enemyValue = enemy.hp / (double)enemy.stats.hp * enemy.stats.cost;
                            target = enemy;
                        }
                    }
                }
            }
        }

        if (enemyValue > 0)
            return true;
        else
            return false;
    }

    private bool BestCanDamage(Unit unit) // FOR: MEDIUM TREE 
    {
        List<Unit> enemyUnits = GetAllUnits().FindAll(x => x.owner != gameController.GetCurrentPlayer());
        List<Vector3Int> movesAlly = gameController.GetWalkRangeLocations(unit);
        movesAlly.Add(PointToPosition(unit.transform.position));
        double enemyValue = -100000;
        foreach (var enemy in enemyUnits)
        {
            Vector3Int enemyPos = Vector3Int.FloorToInt(enemy.transform.position);
            List<Vector3Int> attackDirections = gameController.GetPositionToAttackPoint(enemyPos, unit);
            foreach (var direction in attackDirections) // for every possible direction at location
            {
                if (movesAlly.Contains(direction))
                {
                    (int, int) dmg = gameController.UnitPossibleDamageUnit(unit, enemy);
                    double value = dmg.Item1 * enemy.stats.cost - dmg.Item2 * unit.stats.cost;
                    if (value > enemyValue)
                    {
                        enemyValue = value;
                        target = enemy;
                    }
                }
            }
        }
        if (enemyValue > -100000)
            return true;
        else
            return false;
    }

    private bool CouldHeal(Unit unit) // FOR: MEDIUM TREE 
    {
        return ShouldUseHospital(unit);
    }

    private bool ShouldHeal(Unit unit) // FOR: MEDIUM TREE 
    {
        Hospital hos = (Hospital)target;
        double myHeal = Math.Min(unit.stats.hp - unit.hp, hos.healAmount) / (double)unit.stats.hp * unit.stats.cost;
        List<Unit> allyUnits = GetAllUnits().FindAll(x => x.owner == gameController.GetCurrentPlayer());
        foreach(var ally in allyUnits)
        {

            List<Vector3Int> movesAlly = gameController.GetWalkRangeLocations(unit);
            movesAlly.Add(PointToPosition(unit.transform.position));

            foreach (var direction in directions)
            {
                if (ally != unit && movesAlly.Contains(PointToPosition(hos.transform.position) + direction))
                {
                    if (Math.Min(ally.stats.hp - ally.hp, hos.healAmount) / (double)ally.stats.hp * ally.stats.cost > myHeal)
                        return false;
                }

            }
        }
        return true;
    }

    private bool BestBuildingInRange(Unit unit) // FOR: MEDIUM TREE 
    {
        
        List<Building> enemyBuildings = GetAllBuildings().FindAll(x => x.owner == null || x.owner != gameController.GetCurrentPlayer());
        List<Vector3Int> movesAlly = gameController.GetWalkRangeLocations(unit);
        movesAlly.Add(PointToPosition(unit.transform.position));
        int bestHP = 10000;
        foreach (var enemy in enemyBuildings)
        {
            Vector3Int enemyPos = Vector3Int.FloorToInt(enemy.transform.position);

            List<Vector3Int> attackDirections = gameController.GetPositionToAttackPoint(enemyPos, unit);
            List<Vector3Int> emptyNeighbours = gameController.GetEmptyNeighbours(enemyPos);
            foreach (var direction in attackDirections) // for every possible direction at location
            {
              
                if (movesAlly.Contains(direction))
                {
                    if (enemy.owner.playerID == -1 && emptyNeighbours.Contains(direction))
                    {
                        target = enemy;
                        return true;
                    }
                    else if (enemy.owner != gameController.GetCurrentPlayer() && enemy.hp < bestHP)
                    {
                        bestHP = enemy.hp;
                        target = enemy;
                    }
                }
            }
        }
        if (bestHP < 10000)
            return true;
        else
            return false;
    }


    #endregion

    #region HardTreeNodes

    private bool CanTakeover(Unit unit)
    {
        List<Building> enemyBuildings = GetAllBuildings().FindAll(x => x.owner != gameController.GetCurrentPlayer());
        List<Vector3Int> movesAlly = gameController.GetWalkRangeLocations(unit);
        movesAlly.Add(PointToPosition(unit.transform.position));
        foreach (var enemy in enemyBuildings)
        {
            Vector3Int enemyPos = Vector3Int.FloorToInt(enemy.transform.position);

            List<Vector3Int> attackDirections = gameController.GetPositionToAttackPoint(enemyPos, unit);
            foreach (var direction in attackDirections) // for every possible direction at location
            {
                if (movesAlly.Contains(direction))
                {
                    if (enemy.owner.playerID == -1)
                    {
                        target = enemy;
                        return true;
                    }
                }
            }
        }
        return false;
    }
    private bool AtTagretLocation(Unit unit) // FOR: HARD TREE 
    {
        List<Building> enemyBuildings = GetAllBuildings().FindAll(x => x.owner == null || x.owner != gameController.GetCurrentPlayer());
        List<Unit> enemyUnits = GetAllUnits().FindAll(x => x.owner != gameController.GetCurrentPlayer());
        List<Vector3Int> movesAlly = gameController.GetWalkRangeLocations(unit);
        movesAlly.Add(PointToPosition(unit.transform.position));

        foreach(var eB in enemyBuildings)
        {
            Vector3Int enemyPos = Vector3Int.FloorToInt(eB.transform.position);
            List<Vector3Int> attackDirections = gameController.GetPositionToAttackPoint(enemyPos, unit);
            foreach(var poz in attackDirections)
            {
                if(movesAlly.Contains(poz))
                {
                    return true;
                }
            }
        }
        return false;
    }

    private bool AtDangerPosition(Unit unit) // FOR: HARD TREE 
    {
        List<Unit> enemyUnits = GetAllUnits().FindAll(x => x.owner != gameController.GetCurrentPlayer());
        Vector3Int unitPos = Vector3Int.FloorToInt(unit.transform.position);
        List<Vector3Int> savePositionsAlly = gameController.GetWalkRangeLocations(unit);
        savePositionsAlly.Add(unitPos);
        List<Vector3Int> notSavePositions = new List<Vector3Int>();
        foreach (var move in savePositionsAlly)
        {
            foreach (var enemy in enemyUnits)
            {
                List<Vector3Int> movesEnemy = gameController.GetWalkRangeLocations(enemy);
                List<Vector3Int> attackDirections = gameController.GetPositionToAttackPoint(move, enemy);
                foreach (var poz in attackDirections)
                {
                    if (movesEnemy.Contains(poz) && !notSavePositions.Contains(move))
                    {
                        notSavePositions.Add(move);
                    }
                }
                if (notSavePositions.Contains(move))
                    break;
            }
        }
        if(notSavePositions.Contains(unitPos) && notSavePositions.Count < savePositionsAlly.Count)
        {
            foreach (var move in savePositionsAlly)
            {
                if(!notSavePositions.Contains(move))
                {
                    target = move;
                    break;
                }
            }
            return true;
        }
        return false;
    }
    private bool ShouldGoToHospital(Unit unit) // FOR: HARD TREE 
    {
        List<Building> allyBuildings = GetAllBuildings().FindAll(x => x.owner == null || x.owner == gameController.GetCurrentPlayer());
        foreach(var aB in allyBuildings)
        {
            if(aB is Hospital && gameController.GetEmptyNeighbours(PointToPosition(((Hospital)aB).transform.position)).Count > 0)
            {
                target = (Hospital)aB;
                return unit.stats.hp - unit.hp > ((Hospital)aB).healAmount;
            }
                
        }
        return false;
    }

    private bool CanFindATarget(Unit unit) // FOR: HARD TREE 
    {
        List<Vector3Int> walkPositions = gameController.GetWalkRangeLocations(movingUnit);
        List<Vector3Int> movesAlly = gameController.GetWalkRangeLocations(movingUnit);


        List<Building> enemyBuildings = GetAllBuildings().FindAll(x => x.owner != null && x.owner != gameController.GetCurrentPlayer());
        Vector3Int enemyPoz = new Vector3Int();
        foreach (var aB in enemyBuildings)
            if (aB is Stronghold)
            {
                enemyPoz = PointToPosition(((Building)aB).transform.position);
                target = (Building)aB;
                break;
            }

        if (gameController.GetEmptyNeighbours(enemyPoz).Count > 0)
            return true;

        return false;
    }

    private bool ImportantTarget(Unit unit) // FOR: HARD TREE 
    {
        List<Building> allyBuildings = GetAllBuildings().FindAll(x => x.owner == null || x.owner != gameController.GetCurrentPlayer());
        List<Vector3Int> movesAlly = gameController.GetWalkRangeLocations(unit);
        movesAlly.Add(PointToPosition(unit.transform.position));

        foreach (var aB in allyBuildings)
        {
            Vector3Int buildingPos = Vector3Int.FloorToInt(aB.transform.position);
            if(aB is Stronghold || aB is Barracks || aB is MercenaryCamp)
            {
                List<Vector3Int> attackDirections = gameController.GetPositionToAttackPoint(buildingPos, unit);
                foreach (var poz in attackDirections)
                {
                    if (movesAlly.Contains(poz))
                    {
                        return true;
                    }
                }
            }
            
        }
        return false;
    }

    private bool ShouldKillHard(Unit unit) // FOR: HARD TREE 
    {
        return ShouldKill(unit);
    }

    private bool FindBestPlaceToStay(Unit unit) // FOR: HARD TREE 
    {
        List<Unit> enemyUnits = GetAllUnits().FindAll(x => x.owner != gameController.GetCurrentPlayer());
        List<Unit> allyUnits = GetAllUnits().FindAll(x => x.owner != gameController.GetCurrentPlayer());
        Vector3Int unitPos = Vector3Int.FloorToInt(unit.transform.position);
        List<Vector3Int> movesUnit = gameController.GetWalkRangeLocations(unit);
        movesUnit.Add(unitPos);
        Dictionary<Vector3Int, double> powerDiff = new Dictionary<Vector3Int, double>();
        Dictionary<Vector3Int, double> currentPowerDiff = new Dictionary<Vector3Int, double>();
        
        foreach (var move in movesUnit)
            if(!powerDiff.ContainsKey(move))
                powerDiff.Add(move, 0);

        foreach (var enemy in enemyUnits)
        {
            currentPowerDiff = new Dictionary<Vector3Int, double>();
            List<Vector3Int> movesEnemy = gameController.GetWalkRangeLocations(enemy);
            foreach(var enemyMove in movesEnemy)
            {
                List<Vector3Int> attackDirections = gameController.GetPositionToAttackPoint(enemyMove, enemy);
                foreach(var poz in attackDirections)
                {
                    currentPowerDiff[poz] = gameController.UnitPossibleDamageUnit(enemy, unit).Item1;
                }
            }
            foreach (var key in currentPowerDiff.Keys)
                if(powerDiff.ContainsKey(key))
                    powerDiff[key] += currentPowerDiff[key];
            
        }

        foreach (var ally in allyUnits)
        {
            currentPowerDiff = new Dictionary<Vector3Int, double>();
            List<Vector3Int> movesAlly = gameController.GetWalkRangeLocations(ally);
            foreach (var enemyAlly in movesAlly)
            {
                List<Vector3Int> attackDirections = gameController.GetPositionToAttackPoint(enemyAlly, ally);
                foreach (var poz in attackDirections)
                {
                    currentPowerDiff[poz] = gameController.UnitPossibleDamageUnit(ally, unit).Item1;
                }
            }
            foreach (var key in currentPowerDiff.Keys)
                if (powerDiff.ContainsKey(key))
                    powerDiff[key] -= currentPowerDiff[key];

        }

        double minDanger = powerDiff[unitPos];
        Vector3Int minDangerPos = unitPos;
        foreach (var key in powerDiff.Keys)
        {
            if (powerDiff[key] < minDanger)
            {
                minDanger = powerDiff[key];
                minDangerPos = key;
            }
        }
        target = minDangerPos;
        return minDanger < powerDiff[unitPos];
    }

    #endregion
    public Exception errorThrow()
    {
        if (target is Unit)
            return new ApplicationException("This should never happen for AI!"
                + movingUnit.transform.position.ToString() + " | " + ((Unit)target).transform.position.ToString());
        else if (target is Building)
            return new ApplicationException("This should never happen for AI!"
                + movingUnit.transform.position.ToString() + " | " + ((Building)target).transform.position.ToString());
        else
            return new ApplicationException("This should never happen for AI!" + movingUnit.transform.position.ToString());
    }

}
