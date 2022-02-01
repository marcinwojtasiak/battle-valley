using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.UI;
using System.Linq;
using System;
using TMPro;

[RequireComponent(typeof(NetworkMatch))]
public class GameController : NetworkBehaviour, IGameController
{
    public static IGameController instance;

    [Header("Assigned References")]
    [SerializeField] private GameObject gameEndMenu;
    [SerializeField] private Button playAgainButton;
    [SerializeField] private Animator endGameAnimation;
    [SerializeField] private TMP_Text endGamePlayerText;
    [SerializeField] private PlayerState netPlayerPrefab;
    [SerializeField] private InputHandler playerInterfacePrefab;
    [SerializeField] private ComputerPlayer botPrefab;
    [SerializeField] private GameObject unitsHolder;
    [SerializeField] private GameObject buildingsHolder;
    [SerializeField] private Unit generalUnitPrefab;

    [Header("Matchmaking")]
    private ILobbyController lobbyController;
    private Guid matchId;
    public Dictionary<NetworkIdentity, PlayerState> playerIdentities = new Dictionary<NetworkIdentity, PlayerState>(); // players NetIds and their coresponding PlayerStates
    private List<PlayerInfo> matchPlayers = new List<PlayerInfo>();
    private List<BotInfo> matchBots = new List<BotInfo>();

    // local variables
    private MapManager mapManager;
    private InputHandler localPlayer;

    // synced variables
    [SyncVar] private PlayerState currentPlayer;
    private readonly SyncList<PlayerState> players = new SyncList<PlayerState>();
    private readonly SyncList<Building> buildings = new SyncList<Building>();
    private readonly SyncList<Unit> units = new SyncList<Unit>();
    [SyncVar] private Unit movingUnit = null;
    [SyncVar] private bool isPlayingAnimation;
    [SyncVar] private int round;
    [SyncVar] private bool isReady;

    // server variables
    private bool localGame;
    private int playAgainVotes = 0;
    private PlayerState dummyPlayer;
    private List<IAiOpponent> aiOpponents = new List<IAiOpponent>();
    private MapGenerator mapGenerator = new MapGenerator();

    private void Awake()
    {
        instance = this;
        mapManager = GetComponentInChildren<MapManager>();
    }

    private void Update()
    {
        if (!isServer)
            return;
        if (movingUnit != null)
        {
            if (movingUnit.IsMoving())
            {
                movingUnit.Move();
            }
            else
            {
                movingUnit.RpcOnStopMoving();
                movingUnit = null;
            }
        }
    }

    private void OnDestroy()
    {
        if (AudioManager.instance != null)
            AudioManager.instance.PlayMenuMusic();
    }

    #region GameStartBehaviour

    [Server]
    public IEnumerator InitializeGame(List<NetworkIdentity> identities)
    {
        isReady = false;

        Clear();
        System.Random rand = new System.Random();

        dummyPlayer = SpawnPlayer(-1, "dummy", false, 4, matchId);

        int i = 0;
        foreach (PlayerInfo playerInfo in matchPlayers)
        {
            PlayerState playerState = SpawnPlayer(playerInfo.playerIndex, playerInfo.playerName, false, players.Count, matchId);
            players.Insert(rand.Next(0, players.Count + 1), playerState);
            if (!localGame)
            {
                NetworkIdentity identity = identities[i++];
                playerIdentities[identity] = playerState;
                TileType[][] map = mapGenerator.GetMap();
                TileType[] flatMap = map.SelectMany(t => t).ToArray(); // flatten map representation for serialization
                InitializeClient(identity.connectionToClient, playerState, flatMap, map.Length, map[0].Length);
                yield return null; // wait for client to initialize
            }
        }

        i = 1;
        foreach (BotInfo botInfo in matchBots)
        {
            PlayerState playerState = SpawnPlayer(botInfo.botIndex, $"Bot {i}", true, players.Count, matchId);
            playerState.Initialize(botInfo.botIndex, $"Bot {i}", true, players.Count);
            players.Insert(rand.Next(0, players.Count + 1), playerState);
            i++;
            IAiOpponent bot = Instantiate(botPrefab, transform);
            bot.Initialize(playerState, (DifficultyType)(botInfo.difficulty + 2));
            aiOpponents.Add(bot);
        }

        currentPlayer = players[players.Count - 1]; // last player from list, because we call NextTurn in the begining

        if (localGame)
        {
            LocalInitialize(currentPlayer, mapGenerator.GetMap());
        }

        SpawnEntities();

        round = 0;

        yield return null; // wait to finish initialization
        StartCoroutine(ServerNextTurn());

        isReady = true;
    }

    [Server]
    public void CreateGame(Guid matchId, ILobbyController lobbyController, List<NetworkIdentity> identities, List<PlayerInfo> matchPlayers, List<BotInfo> matchBots, bool localGame, MapSettings mapSettings)
    {
        this.localGame = localGame;
        this.matchId = matchId;
        this.lobbyController = lobbyController;
        this.matchPlayers = matchPlayers;
        this.matchBots = matchBots;

        GetComponent<NetworkMatch>().matchId = matchId;
        NetworkServer.Spawn(gameObject);

        mapGenerator.GenerateMap((MapSize)mapSettings.mapSize, matchPlayers.Count + matchBots.Count);
        mapManager.InitMapVisualization(mapGenerator.GetMap());

        StartCoroutine(InitializeGame(identities));
    }

    [TargetRpc]
    private void InitializeClient(NetworkConnection target, PlayerState playerState, TileType[] mapRepr, int mapWidth, int mapHeight)
    {
        LocalInitialize(playerState, Expand(mapRepr, mapWidth, mapHeight));
    }

    private void LocalInitialize(PlayerState playerState, TileType[][] mapRepr)
    {
        // clear after previous game
        if (localPlayer != null)
            Destroy(localPlayer.gameObject);

        // prepare new game
        gameEndMenu.SetActive(false);

        endGameAnimation.SetTrigger("Hide");
        endGameAnimation.gameObject.SetActive(false);

        localPlayer = Instantiate(playerInterfacePrefab, transform);

        localPlayer.thisPlayer = playerState;

        mapManager.InitMapVisualization(mapRepr);

        CameraController cameraController = Camera.main.GetComponent<CameraController>();
        if (cameraController != null)
            cameraController.EnableCameraControlls(mapManager.baseTilemap);

        AudioManager.instance.PlayInGameMusic();
    }

    private T[][] Expand<T>(T[] array, int rows, int columns)
    {
        if (rows * columns != array.Length)
            throw new ArgumentException();
        T[][] result = new T[rows][];
        for (int i = 0; i < rows; i++)
        {
            result[i] = new T[columns];
            for (int j = 0; j < columns; j++)
            {
                result[i][j] = array[i * columns + j];
            }
        }
        return result;
    }

    [Server]
    private void Clear()
    {
        if (dummyPlayer != null)
            NetworkServer.Destroy(dummyPlayer.gameObject);
        foreach (PlayerState player in players)
            NetworkServer.Destroy(player.gameObject);
        players.Clear();
        foreach (Building building in buildings)
            NetworkServer.Destroy(building.gameObject);
        buildings.Clear();
        foreach (Unit unit in units)
            NetworkServer.Destroy(unit.gameObject);
        units.Clear();
    }

    #endregion

    #region SpawningEntities

    [Server]
    private PlayerState SpawnPlayer(int playerID, string playerName, bool isAi, int colorIndex, Guid matchId)
    {
        PlayerState playerState = Instantiate(netPlayerPrefab, transform);
        playerState.GetComponent<NetworkMatch>().matchId = matchId;
        NetworkServer.Spawn(playerState.gameObject);
        playerState.Initialize(playerID, playerName, isAi, colorIndex);
        return playerState;
    }

    [Server]
    private void SpawnEntities()
    {
        for (int i = 0; i < players.Count; i++)
        {
            if (i < mapGenerator.GetSpawners().Count)
            {
                Spawner spawner = mapGenerator.GetSpawners()[i];

                foreach (SpawnerEntry<UnitStats> unitSpawn in spawner.units)
                {
                    SpawnUnit(unitSpawn.entity, (Vector3Int)unitSpawn.position, players[i]);
                }

                foreach (SpawnerEntry<BuildingStats> buildingSpawn in spawner.buildings)
                {
                    SpawnBuilding(buildingSpawn.entity, (Vector3Int)buildingSpawn.position, players[i]);
                }
            }
        }

        Spawner neutralSpawner = mapGenerator.GetSpawners()[mapGenerator.GetSpawners().Count - 1];

        foreach (SpawnerEntry<BuildingStats> buildingSpawn in neutralSpawner.buildings)
        {
            SpawnBuilding(buildingSpawn.entity, (Vector3Int)buildingSpawn.position, dummyPlayer);
        }
    }

    public void RecruitUnit(UnitStats unitStats, Vector3Int position, Building usedBuilding, bool isAI)
    {
        if (isAI)
            ServerRecruitUnit(unitStats, position, usedBuilding);
        else
            CmdRecruitUnit(unitStats, position, usedBuilding);
    }

    [Command(requiresAuthority = false)]
    private void CmdRecruitUnit(UnitStats unitStats, Vector3Int position, Building usedBuilding)
    {
        ServerRecruitUnit(unitStats, position, usedBuilding);
    }

    [Server]
    private void ServerRecruitUnit(UnitStats unitStats, Vector3Int position, Building usedBuilding)
    {
        if (usedBuilding.isAlreadyUsedInTurn || GetCurrentPlayer().gold < unitStats.cost)
            return;
        usedBuilding.isAlreadyUsedInTurn = true;
        GetCurrentPlayer().gold -= unitStats.cost;
        SpawnUnit(unitStats, position, GetCurrentPlayer());
    }

    [Server]
    private void SpawnUnit(UnitStats unitStats, Vector3Int position, PlayerState player)
    {
        Unit unit = Instantiate(generalUnitPrefab, unitsHolder.transform, false);
        unit.GetComponent<NetworkMatch>().matchId = matchId;
        units.Add(unit);
        NetworkServer.Spawn(unit.gameObject);
        unit.ServerInitialize(player, unitStats, position);
        unit.RpcClientInitialize(player, unitStats, unit.transform.position);
    }

    [Server]
    private void SpawnBuilding(BuildingStats buildingStats, Vector3Int position, PlayerState player)
    {
        Building newBuilding = Instantiate(buildingStats.prefab, buildingsHolder.transform, false);
        newBuilding.GetComponent<NetworkMatch>().matchId = matchId;
        buildings.Add(newBuilding);
        NetworkServer.Spawn(newBuilding.gameObject);
        newBuilding.ServerInitialize(player, position);
        newBuilding.RpcClientInitialize(newBuilding.transform.position);
    }

    #endregion

    #region GameStateChecks

    public MapManager GetMapManager()
    {
        return mapManager;
    }

    public PlayerState GetCurrentPlayer()
    {
        return currentPlayer;
    }

    public bool IsCurrentPlayer(PlayerState playerState)
    {
        return currentPlayer.Equals(playerState);
    }

    public Building GetBuildingAt(Vector3Int point)
    {
        return buildings.Find(building => Vector3Int.FloorToInt(building.transform.position).Equals(point));
    }

    public Unit GetUnitAt(Vector3Int point)
    {
        return units.Find(unit => Vector3Int.FloorToInt(unit.transform.position).Equals(point));
    }

    public bool AllowInput()
    {
        return (movingUnit == null || !movingUnit.IsMoving()) && !isPlayingAnimation;
    }

    public List<PlayerState> GetPlayers()
    {
        return players.ToList();
    }

    public List<Building> GetBuildings()
    {
        return buildings.ToList();
    }

    public List<Unit> GetUnits()
    {
        return units.ToList();
    }

    public int GetRound()
    {
        return round;
    }

    public bool IsReady()
    {
        return isReady;
    }

    private List<Vector3Int> ReconstructPath(Dictionary<Vector3Int, Vector3Int> cameFrom, Vector3Int current)
    {
        List<Vector3Int> result = new List<Vector3Int>();
        Vector3Int last = current;
        while (cameFrom.ContainsKey(current))
        {
            result.Add(last);
            current = cameFrom[current];
            last = current;
        }
        result.Add(last);
        result.Reverse();
        return result;
    }

    public List<Vector3Int> GetPath(Vector3Int from, Vector3Int to)
    {
        Dictionary<Vector3Int, Vector3Int> cameFrom = new Dictionary<Vector3Int, Vector3Int>();
        List<Vector3Int> openList = new List<Vector3Int>();
        openList.Add(from);

        Dictionary<Vector3Int, int> gScore = new Dictionary<Vector3Int, int>();
        gScore[from] = 0;

        Dictionary<Vector3Int, int> fScore = new Dictionary<Vector3Int, int>();
        fScore[from] = Mathf.Abs(from.x - to.x) + Mathf.Abs(from.y - to.y);


        while (openList.Count > 0)
        {
            Vector3Int minPoint = openList[0];
            foreach (Vector3Int v in openList)
            {
                if (fScore[v] < fScore[minPoint])
                {
                    minPoint = v;
                }
            }

            if (minPoint.x == to.x && minPoint.y == to.y)
            {
                return ReconstructPath(cameFrom, minPoint);
            }

            openList.Remove(minPoint);
            List<Vector3Int> neighbours = GetEmptyNeighbours(minPoint);
            foreach (Vector3Int n in neighbours)
            {
                TileType? neighbourTileType = mapManager.GetTileAt(n.x, n.y);
                int costToOtherTile = neighbourTileType.HasValue ? mapManager.GetTileCost(neighbourTileType.Value) : -1;

                if (costToOtherTile == -1)
                {
                    continue;
                }

                int tentative_gScore = gScore[minPoint] + costToOtherTile;
                int gScoreNeighb = gScore.ContainsKey(n) ? gScore[n] : int.MaxValue;
                if (tentative_gScore < gScoreNeighb)
                {
                    cameFrom[n] = minPoint;
                    gScore[n] = tentative_gScore;
                    fScore[n] = gScore[n] + Mathf.Abs(n.x - to.x) + Mathf.Abs(n.y - to.y);
                    if (!openList.Contains(n))
                    {
                        openList.Add(n);
                    }
                }
            }
        }
        Debug.Log("Did not find a path: " + from + " => " + to);
        return new List<Vector3Int>();
    }

    public List<Vector3Int> GetWalkRangeLocations(Unit unit)
    {
        UnityEngine.Profiling.Profiler.BeginSample("GetWalkRangeLocations");
        Vector3Int startPoint = Vector3Int.FloorToInt(unit.transform.position);
        int unitMovement = unit.movement;

        List<Vector3Int> pointsToCheck = new List<Vector3Int>();
        pointsToCheck.Add(startPoint);
        List<Vector3Int> result = new List<Vector3Int>();

        Dictionary<Vector3Int, int> gScore = new Dictionary<Vector3Int, int>();
        gScore[startPoint] = 0;

        while (pointsToCheck.Count > 0)
        {
            Vector3Int checkedPoint = pointsToCheck[0];
            List<Vector3Int> neighbours = GetEmptyNeighbours(checkedPoint);

            foreach (Vector3Int neighbourLocation in neighbours)
            {
                TileType? neighbourTileType = mapManager.GetTileAt(neighbourLocation.x, neighbourLocation.y);
                int costToOtherTile = neighbourTileType.HasValue ? mapManager.GetTileCost(neighbourTileType.Value) : -1;

                if (costToOtherTile == -1)
                {
                    continue;
                }
                int tentative_gScore = gScore[checkedPoint] + costToOtherTile;
                if (unitMovement >= tentative_gScore)
                {
                    result.Add(neighbourLocation);
                    int gScoreNeighb = gScore.ContainsKey(neighbourLocation) ? gScore[neighbourLocation] : int.MaxValue;
                    if (tentative_gScore < gScoreNeighb)
                    {
                        gScore[neighbourLocation] = tentative_gScore;
                        if (!pointsToCheck.Contains(neighbourLocation))
                        {
                            pointsToCheck.Add(neighbourLocation);
                        }
                    }
                }
            }
            pointsToCheck.RemoveAt(0);

        }
        UnityEngine.Profiling.Profiler.EndSample();
        return result;
    }

    public List<Vector3Int> GetAttackRangeLocations(Unit unit)
    {
        Vector3Int startPoint = Vector3Int.FloorToInt(unit.transform.position);
        int unitAttackRange = unit.stats.attackRange;

        List<Vector3Int> result = new List<Vector3Int>();

        for (int x = startPoint.x - unitAttackRange; x <= startPoint.x + unitAttackRange; x++)
        {
            for (int y = startPoint.y - unitAttackRange; y <= startPoint.y + unitAttackRange; y++)
            {
                Vector3Int position = new Vector3Int(x, y, 0);
                if (Vector3Int.Distance(startPoint, position) <= unitAttackRange)
                {
                    mapManager.AddPointIfInsideMap(position, result);
                }
            }
        }
        return result;
    }

    public List<Vector3Int> GetPositionToAttackPoint(Vector3Int pointToAttack, Unit unitAttacking)
    {
        int unitAttackRange = unitAttacking.stats.attackRange;

        List<Vector3Int> result = new List<Vector3Int>();

        for (int x = pointToAttack.x - unitAttackRange; x <= pointToAttack.x + unitAttackRange; x++)
        {
            for (int y = pointToAttack.y - unitAttackRange; y <= pointToAttack.y + unitAttackRange; y++)
            {
                Vector3Int position = new Vector3Int(x, y, 0);
                if (Vector3Int.Distance(pointToAttack, position) <= unitAttackRange)
                {
                    if(Vector3Int.FloorToInt(unitAttacking.transform.position).Equals(position) || !IsOccupied(position))
                        mapManager.AddPointIfInsideMap(position, result);
                }
            }
        }
        return result;
    }

    public List<Vector3Int> GetEmptyNeighbours(Vector3Int point)
    {
        List<Vector3Int> neighbours = new List<Vector3Int>();

        List<Vector3Int> directions = new List<Vector3Int> { Vector3Int.down, Vector3Int.up, Vector3Int.left, Vector3Int.right };
        foreach (Vector3Int dir in directions)
        {
            if (!IsOccupied(point + dir))
            {
                mapManager.AddPointIfInsideMap(point + dir, neighbours);
            }
        }
        return neighbours;
    }

    public (int, int) UnitPossibleDamageUnit(Unit attacking, Unit defending)
    {
        int damageAttacker = 0;
        int damageDefender = 0;
        for(int i = 0; i < attacking.stats.actions; i++)
        {
            damageAttacker += attacking.stats.attack - defending.stats.armor;
            if (damageAttacker < 5)
                damageAttacker = 5;

            if (defending.stats.counterAttack && attacking.stats.attackRange > 1)
            {
                damageDefender += defending.stats.attack - attacking.stats.armor;
                if (damageDefender < 5)
                    damageDefender = 5;
            }
        }

        return (damageAttacker, damageDefender);
    }

    public int UnitPossibleDamageBuilding(Unit attacking)
    {
        int damage = 0;
        for (int i = 0; i < attacking.stats.actions; i++)
        {
            damage += attacking.stats.attack;
            if (damage < 5)
                damage = 5;
        }

        return damage;
    }

    private bool IsOccupied(Vector3Int point)
    {
        return GetBuildingAt(point) != null || GetUnitAt(point) != null;
    }


    #endregion

    #region GameStateChange

    public void MoveUnit(Unit unit, Vector3Int to, bool isAI)
    {
        if (isAI)
            ServerMoveUnit(unit, to);
        else
            CmdMoveUnit(unit, to);
    }

    [Command(requiresAuthority = false)]
    private void CmdMoveUnit(Unit unit, Vector3Int to)
    {
        ServerMoveUnit(unit, to);
    }

    [Server]
    private void ServerMoveUnit(Unit unit, Vector3Int to)
    {
        Vector3Int from = Vector3Int.FloorToInt(unit.transform.position);
        List<Vector3Int> path = GetPath(from, to);
        if (path.Count == 0)
            return;

        int moveCost = path
            .GetRange(1, path.Count - 1)
            .Sum(pos => mapManager.GetTileCost((TileType)mapManager.GetTileAt(pos.x, pos.y)));
        unit.movement -= moveCost;

        movingUnit = unit;
        unit.SetMovePath(path);
        movingUnit.RpcOnStartMoving();
    }

    public void NextTurn(bool isAI)
    {
        if (isAI)
            StartCoroutine(ServerNextTurn());
        else
            CmdNextTurn();
    }

    [Command(requiresAuthority = false)]
    public void CmdNextTurn()
    {
        StartCoroutine(ServerNextTurn());
    }

    [Server]
    private IEnumerator ServerNextTurn()
    {
        // reset buildings and units of the previous player 
        buildings.ToList().ForEach(building => building.Refresh());
        units.ToList().ForEach(unit => unit.Refresh());

        // change current player
        int currentPlayerIdx = (players.IndexOf(currentPlayer) + 1) % players.Count;
        currentPlayer = players[currentPlayerIdx];
        // add gold for the current player
        buildings.FindAll(building => building.owner.Equals(currentPlayer)).ForEach(building =>
        {
            currentPlayer.gold += building.stats.income;
        });
        // apply buildings regen
        buildings.FindAll(building => building.owner.Equals(currentPlayer)).ForEach(building =>
        {
            building.hp += building.stats.regen;
            if (building.hp > building.stats.hp)
                building.hp = building.stats.hp;
        });

        Debug.Log("Current player: " + GetCurrentPlayer());

        if(currentPlayerIdx == 0)
        {
            round++;
            Debug.Log("New round");
        }
        isPlayingAnimation = true;
        RpcNextTurnAnim(currentPlayer, round);
        yield return new WaitForSeconds(2.5f);
        isPlayingAnimation = false;

        RpcCurrentPlayerDisplay(currentPlayer);

        if (currentPlayer.isAi)
        {
            aiOpponents.Find(bot => bot.PlayerState.Equals(currentPlayer)).StartTurn();
        }
        if (localGame)
        {
            localPlayer.thisPlayer = currentPlayer;
        }
    }

    [ClientRpc]
    private void RpcCurrentPlayerDisplay(PlayerState currentPlayer)
    {
        GameUIReferences.instance.currentPlayerText.text = currentPlayer.playerName;
        Color[] teamColors = GameUIReferences.instance.teamPalletes.palettes[currentPlayer.colorIndex].color;
        GameUIReferences.instance.currentPlayerBackground.color = teamColors[teamColors.Length - 1];
    }

    [ClientRpc]
    private void RpcNextTurnAnim(PlayerState player, int round)
    {
        GameUIReferences.instance.popupUI.PlayAnimation(player, $"Round {round}");
    }

    public void UnitInteractBuilding(Unit unit, Building building, bool isAI)
    {
        if (isAI)
            ServerUnitInteractBuilding(unit, building);
        else
            CmdUnitInteractBuilding(unit, building);
    }

    [Command(requiresAuthority = false)]
    public void CmdUnitInteractBuilding(Unit unit, Building building)
    {
        ServerUnitInteractBuilding(unit, building);
    }

    [Server]
    public void ServerUnitInteractBuilding(Unit unit, Building building)
    {
        IUnitInteractable unitInteractable = building.GetComponent<IUnitInteractable>();
        if (unitInteractable != null && unitInteractable.Interact(currentPlayer, unit))
        {
            unit.actions--;
            building.isAlreadyUsedInTurn = true;
            Debug.Log(unit + " interacted with " + building);
        }
    }

    public void InteractBuilding(Building building, bool isAI)
    {
        IInteractable interactable = building.GetComponent<IInteractable>();
        if (interactable != null)
        {
            interactable.Interact(GetCurrentPlayer());
        }
    }

    public void UnitAttackUnit(Unit attacking, Unit defending, bool isAI)
    {
        if (isAI)
            StartCoroutine(ServerUnitAttackUnit(attacking, defending));
        else
            CmdUnitAttackUnit(attacking, defending);
    }

    [Command(requiresAuthority = false)]
    public void CmdUnitAttackUnit(Unit attacking, Unit defending)
    {
        StartCoroutine(ServerUnitAttackUnit(attacking, defending));
    }

    [Server]
    private IEnumerator ServerUnitAttackUnit(Unit attacking, Unit defending)
    {
        isPlayingAnimation = true;

        attacking.actions--;

        attacking.RpcOnAttack(defending.transform.position, false, attacking.stats.attackRange > 1);

        defending.ApplyDamage(attacking);

        yield return new WaitForSeconds(1f);

        if (defending.hp <= 0)
        {
            KillUnit(defending, attacking);
        }
        else if (defending.stats.counterAttack && !defending.usedCounter)
        {
            if (Vector3Int.Distance(Vector3Int.FloorToInt(defending.transform.localPosition), Vector3Int.FloorToInt(attacking.transform.localPosition)) <= 1.1)
            {
                defending.usedCounter = true;

                defending.RpcOnAttack(attacking.transform.position, false, defending.stats.attackRange > 1);

                attacking.ApplyDamage(defending);

                if (attacking.hp <= 0)
                {
                    KillUnit(attacking, defending);
                }
            }
        }

        isPlayingAnimation = false;
    }

    [Server]
    private void KillUnit(Unit unit, Unit killer)
    {
        unit.RpcOnDeath(killer.transform.position);
        units.Remove(unit);
        NetworkServer.Destroy(unit.gameObject);
    }

    public void UnitAttackBuilding(Unit attacking, Building defending, bool isAI)
    {
        if (isAI)
            ServerUnitAttackBuilding(attacking, defending);
        else
            CmdUnitAttackBuilding(attacking, defending);
    }

    [Command(requiresAuthority = false)]
    public void CmdUnitAttackBuilding(Unit attacking, Building defending)
    {
        ServerUnitAttackBuilding(attacking, defending);
    }

    [Server]
    public void ServerUnitAttackBuilding(Unit attacking, Building defending)
    {
        attacking.actions--;
        attacking.RpcOnAttack(defending.transform.position, true, attacking.stats.attackRange > 1);
        defending.ApplyDamage(attacking);

        if (defending.hp <= 0)
        {
            if (defending is Stronghold)
            {
                RemovePlayer(defending.owner, false);
            }
            else
            {
                SetBuildingNeutral(defending);
            }
        }
    }

    [Server]
    private void SetBuildingNeutral(Building building)
    {
        building.hp = 0;
        building.owner = dummyPlayer;
    }

    public void CaptureBuilding(Unit unit, Building building, bool isAI)
    {
        if (isAI)
            ServerCaptureBuilding(unit, building);
        else
            CmdCaptureBuilding(unit, building);
    }

    [Command(requiresAuthority = false)]
    public void CmdCaptureBuilding(Unit unit, Building building)
    {
        ServerCaptureBuilding(unit, building);
    }

    [Server]
    public void ServerCaptureBuilding(Unit unit, Building building)
    {
        unit.actions--;
        PlayerState player = unit.owner;
        building.owner = player;
        building.hp = (int)(building.stats.hp * 0.5);
        Debug.Log("player " + player.playerID + " captured " + building);
    }

    [Server]
    private void RemovePlayer(PlayerState player, bool disconnected)
    {
        if (disconnected)
        {
            Debug.Log("Player " + player + " disconnected!");
        }
        else
        {
            Debug.Log("Player " + player + " lost!");
        }

        players.Remove(player);

        units.FindAll(unit => unit.owner == player).ForEach(unit => NetworkServer.Destroy(unit.gameObject));
        units.RemoveAll(unit => unit.owner == player);

        Stronghold stronghold = buildings.FindAll(b => b is Stronghold).Find(b => b.owner.Equals(player)) as Stronghold;

        buildings.FindAll(building => building.owner == player).ForEach(building => SetBuildingNeutral(building));

        if (stronghold != null) // null if not spawned properly
        {
            NetworkServer.Destroy(stronghold.gameObject);
            buildings.Remove(stronghold);
        }

        if (players.Count == 1)
        {
            RpcShowWinner(players[0], playerIdentities.Count > 1 || localGame);
        }
        else
        {
            RpcPlayerLostAnimation(player);
        }
    }

    [ClientRpc]
    private void RpcPlayerLostAnimation(PlayerState player)
    {
        GameUIReferences.instance.popupUI.PlayAnimation(player, "Has lost!");
    }

    #endregion

    #region GameEndBehaviour

    [ClientRpc]
    private void RpcShowWinner(PlayerState player, bool canPlayAgain)
    {
        StartCoroutine(ShowWinner(player, canPlayAgain));
    }

    private IEnumerator ShowWinner(PlayerState player, bool canPlayAgain)
    {
        Debug.Log("Player " + players[0] + " won!");
        Destroy(localPlayer.gameObject);
        Color[] playerColors = GameUIReferences.instance.teamPalletes.palettes[player.colorIndex].color;
        endGamePlayerText.text = player.playerName;
        endGamePlayerText.color = playerColors[playerColors.Length - 1];
        endGameAnimation.gameObject.SetActive(true);
        endGameAnimation.SetTrigger("Show");
        AudioManager.instance.StopMusic();
        AudioManager.instance.PlayFanfarsSFX();
        playAgainButton.interactable = true; // could have been disabled in previous game
        if (!canPlayAgain)
            playAgainButton.interactable = false;
        yield return new WaitForSeconds(1.5f);
        gameEndMenu.SetActive(true);
    }

    // Assigned in inspector to ReplayButton::OnClick
    [Client]
    public void RequestPlayAgain()
    {
        playAgainButton.interactable = false;
        CmdPlayAgain();
    }

    [Command(requiresAuthority = false)]
    public void CmdPlayAgain(NetworkConnectionToClient sender = null)
    {
        playAgainVotes++;
        if (playAgainVotes == playerIdentities.Count || localGame)
        {
            playAgainVotes = 0;
            StartCoroutine(InitializeGame(playerIdentities.Keys.ToList()));
        }
    }

    [Server]
    public void OnPlayerDisconnected(NetworkConnection conn)
    {
        // Check that the disconnecting client is a player in this match
        if (playerIdentities.ContainsKey(conn.identity))
        {
            PlayerState playerState = playerIdentities[conn.identity];
            playerIdentities.Remove(conn.identity);

            if (playerIdentities.Count == 0)
            {
                lobbyController.EndMatch(this);
            }

            if (players.Contains(playerState))
                RemovePlayer(playerState, true);
        }
    }

    // Assigned in inspector to BackButton::OnClick
    [Client]
    public void RequestEndGame()
    {
        gameEndMenu.SetActive(false);
        CmdEndGame();
    }

    [Command(requiresAuthority = false)]
    private void CmdEndGame()
    {
        lobbyController.EndMatch(this);
    }

    [Client]
    public void ExitGame(PlayerState playerState)
    {
        Debug.Log($"Player {playerState} has quit");
        if (localGame)
        {
            NetworkManager.singleton.StopHost();
        }
        else
        {
            NetworkManager.singleton.StopClient();
        }
    }

    #endregion
}
