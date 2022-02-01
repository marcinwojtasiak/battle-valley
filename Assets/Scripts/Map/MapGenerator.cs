using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGenerator
{
    MapGeneratorParams mapParams;
    
    private int numberOfPlayers;
    private MapSize mapSize;

    private List<Spawner> spawners;
    private TileType[][] map;
    private HashSet<Vector2Int> entityPositions;
    private List<Vector2Int> strongholdPositions;
    private List<Vector2Int> closesBuildingsToMiddle;

    private Vector2Int middlePoint;

    private BuildingStats stronghold, hospital, goldMine, barracks, mercenaryCamp;
    private List<BuildingStats> spawningBuildings, supportingBuildings, randomBuildings;


    public void GenerateMap(MapSize mapSize, int numberOfPlayers, int seed = -1)
    {
        this.mapSize = mapSize;

        stronghold = Resources.Load<BuildingStats>("ScriptableObjects/Buildings/Stronghold");
        hospital = Resources.Load<BuildingStats>("ScriptableObjects/Buildings/Hospital");
        goldMine = Resources.Load<BuildingStats>("ScriptableObjects/Buildings/GoldMine");
        barracks = Resources.Load<BuildingStats>("ScriptableObjects/Buildings/Barracks");
        mercenaryCamp = Resources.Load<BuildingStats>("ScriptableObjects/Buildings/MercenaryCamp");
        spawningBuildings = new List<BuildingStats>() { barracks, mercenaryCamp };
        supportingBuildings = new List<BuildingStats>() { goldMine, hospital };
        randomBuildings = new List<BuildingStats>() { barracks, mercenaryCamp, goldMine, hospital };

        mapParams = MapGeneratorParams.GetParams(mapSize);

        middlePoint = new Vector2Int(mapParams.mapWidth / 2, mapParams.mapHeight / 2);

        this.numberOfPlayers = numberOfPlayers;
        if (seed != -1)
        {
            Random.InitState(seed);
        }
        
        bool isPlayable = false;
        do
        {
            InitiateParams();
            InitiateMap();
            InitiateTeams();
            GenerateStrongholds();
            GenerateBuildings();
            GenerateBlurs();
            GenerateRivers();
            CleanBuildings();
            GeneratePaths();
            isPlayable = CheckIfPlayable();
        }
        while (!isPlayable);
        //Test buildings
        /*AddBuilding(stronghold, new Vector2Int(4, 3), 1);
        AddBuilding(hospital, new Vector2Int(3, 7), 5);
        AddBuilding(goldMine, new Vector2Int(13, 1), 5);
        AddBuilding(mercenaryCamp, new Vector2Int(9, 8), 5);

        AddBuilding(stronghold, new Vector2Int(11, 10), 2);
        AddBuilding(hospital, new Vector2Int(12, 6), 5);
        AddBuilding(goldMine, new Vector2Int(1, 12), 5);
        AddBuilding(barracks, new Vector2Int(5, 8), 5);


        AddBuilding(stronghold, new Vector2Int(3, 9), 3);


        AddBuilding(stronghold, new Vector2Int(9, 3), 4);
        //End test buildings

        for (int i = 0; i < mapParams.numberOfBlurs;  i++)
        {
            TileType xd = SelectTile();
            Vector2Int newPoint = GetEmptyPoint();
            GenerateBlurV2(xd, newPoint);
        }

        ConnectPoints(TileType.WATER, GetRandomPoint(1, mapParams.mapWidth, 1, mapParams.mapHeight), GetRandomPoint(1, mapParams.mapWidth, 1, mapParams.mapHeight));

        GenerateBlurV2(TileType.GRASS, new Vector2Int(4, 3));
        GenerateBlurV2(TileType.GRASS, new Vector2Int(11, 10));*/

    }

    public List<Spawner> GetSpawners()
    {
        return spawners;
    }

    public TileType[][] GetMap()
    {
        return map;
    }

    private void InitiateParams()
    {
        spawners = new List<Spawner>();
        strongholdPositions = new List<Vector2Int>();
        closesBuildingsToMiddle = new List<Vector2Int>();
        entityPositions = new HashSet<Vector2Int>();
    }

    private void InitiateMap()
    {
        int height = mapParams.mapHeight;
        int width = mapParams.mapWidth;
        
        map = new TileType[width][];

        for(int i = 0; i < width; i++)
        {
            map[i] = new TileType[height];
            for(int j = 0; j < height; j++)
            {          
                map[i][j] = TileType.GRASS;      
            }
        }
    }

    private void InitiateTeams()
    {
        for (int i = 0; i < numberOfPlayers; i++)
        {
            spawners.Add(new Spawner());
            strongholdPositions.Add(new Vector2Int());
            closesBuildingsToMiddle.Add(new Vector2Int());
        }

        spawners.Add(new Spawner() { isNeutral = true });
    }

    private void AddBuilding(BuildingStats building, Vector2Int position, int team)
    {
        spawners[team - 1].buildings.Add(new SpawnerEntry<BuildingStats> { entity = building, position = position });
        entityPositions.Add(position);
        if(building == stronghold)
        {
            strongholdPositions[team - 1] = position;
        }
    }


    private TileType SelectTile()
    {
        int sum = 0;
        foreach(int weight in mapParams.tileWeigths.Values)
        {
            sum += weight;
        }
        int randValue = Random.Range(0, sum);

        foreach (TileType tile in mapParams.tileWeigths.Keys)
        {
            if (randValue < mapParams.tileWeigths[tile])
            {
                return tile;
            }

            randValue -= mapParams.tileWeigths[tile];
        }

        return TileType.WATER;
    }
    private void GenerateBlur(TileType tile)
    {
        float scale = 0.1f;
        float offset = Random.Range(0.01f, 100000f);
        int biomeMinSizeX = 5;
        int biomeMinSizeY = 5;
        int biomeMaxSizeX = Mathf.Max(Mathf.RoundToInt(mapParams.mapWidth * 0.4f), biomeMinSizeX);
        int biomeMaxSizeY = Mathf.Max(Mathf.RoundToInt(mapParams.mapHeight * 0.4f), biomeMinSizeY);


        Vector2Int leftTop = new Vector2Int(
            Mathf.RoundToInt(Random.Range(-biomeMinSizeX + 1f, mapParams.mapWidth - 2)),
            Mathf.RoundToInt(Random.Range(-biomeMinSizeY + 1f, mapParams.mapHeight - 2)));

        Vector2Int rightBottom = new Vector2Int(
            Mathf.RoundToInt(Random.Range(leftTop.x + biomeMinSizeX, leftTop.x + biomeMaxSizeX)),
            Mathf.RoundToInt(Random.Range(leftTop.y + biomeMinSizeY, leftTop.y + biomeMaxSizeY)));

        Vector2Int middle = (leftTop + rightBottom)/2;

        for (int i = leftTop.x; i <= rightBottom.x; i++)
        {
            for(int j = leftTop.y; j <= rightBottom.y; j++)
            {
                if (IsInMap(i, j))
                {
                    Vector2Int actualVector = new Vector2Int(i, j);
                    float dist = Vector2Int.Distance(actualVector, middle);
                    float biomeUsedSize = Mathf.Max(rightBottom.x - leftTop.x, rightBottom.y - leftTop.y) / 2.0f;
                    float noiseScale = Mathf.Log10(Mathf.Max((biomeUsedSize - dist) / biomeUsedSize, 0.1f)) + 1.3f;
                    if (Mathf.PerlinNoise(i * scale + offset, j * scale + offset) * noiseScale > 0.4)
                    {
                        map[i][j] = tile;
                    }
                }
            }
        }
    }

    private void GenerateBlurV2(TileType tile, Vector2Int? position = null)
    {

        float blurDepthDecreaseFactor = mapParams.blurDepthDecreaseFactor[tile];
        float blurDrawThreshold = mapParams.blurDrawThreshold[tile];
        Vector2Int startingPoint;
        if (position.HasValue)
        {
            startingPoint = position.Value;
        }
        else
        {
            startingPoint = GetRandomPoint(1, mapParams.mapWidth - 2, 1, mapParams.mapHeight - 2);
        }
        float perlinScale = 0.1f;
        float perlinOffset = Random.Range(0.01f, 100000f);
        HashSet<Vector2Int> blurPoints = new HashSet<Vector2Int>();
        blurPoints.Add(startingPoint);
        List<(Vector2Int point, float depth)> pointsToCheck = new List<(Vector2Int point, float depth)>();
        var pointWithDepth = (startingPoint, 1);
        pointsToCheck.Add(pointWithDepth);
        float startingPerlinNoise = GetPerlinNoise(startingPoint.x, startingPoint.y, perlinScale, perlinOffset);
        Vector2Int[] directionVectors = { Vector2Int.up, Vector2Int.down, Vector2Int.right, Vector2Int.left };

        while (pointsToCheck.Count != 0)
        {
            (Vector2Int point, float depth) = pointsToCheck[0];
            pointsToCheck.RemoveAt(0);
            if(IsInMap(point.x, point.y) && Mathf.Abs(GetPerlinNoise(point.x, point.y, perlinScale, perlinOffset) - startingPerlinNoise) / depth < blurDrawThreshold) 
            {
                map[point.x][point.y] = tile;
                float newDepth = depth - depth / blurDepthDecreaseFactor;
                foreach(Vector2Int dirVector in directionVectors)
                {
                    Vector2Int neighbour = point + dirVector;
                    if(!blurPoints.Contains(neighbour))
                    {
                        blurPoints.Add(neighbour);
                        pointsToCheck.Add((neighbour, newDepth));
                    }
                }
            }
        }
    }

    private void ConnectPoints(TileType tile, Vector2Int startingPoint, Vector2Int endPoint, bool clockwise = false)
    {
        int nextSegmentAreaSize = mapParams.nextSegmentAreaSize;
        Vector2Int actualPoint = startingPoint;
        DirectionType direction = GetDirection(startingPoint, endPoint);
        int nextPointMinX, nextPointMaxX, nextPointMinY, nextPointMaxY;
        while(actualPoint != endPoint)
        {
            if(direction == DirectionType.NW)
            {
                nextPointMinX = Mathf.Max(0, actualPoint.x - nextSegmentAreaSize, endPoint.x);
                nextPointMaxX = actualPoint.x;
                nextPointMinY = actualPoint.y;
                nextPointMaxY = Mathf.Min(mapParams.mapHeight - 1, actualPoint.y + nextSegmentAreaSize, endPoint.y);
            }
            else if (direction == DirectionType.NE)
            {
                nextPointMinX = actualPoint.x;
                nextPointMaxX = Mathf.Min(mapParams.mapWidth - 1, actualPoint.x + nextSegmentAreaSize, endPoint.x);
                nextPointMinY = actualPoint.y;
                nextPointMaxY = Mathf.Min(mapParams.mapHeight - 1, actualPoint.y + nextSegmentAreaSize, endPoint.y);
            }
            else if (direction == DirectionType.SW)
            {
                nextPointMinX = Mathf.Max(0, actualPoint.x - nextSegmentAreaSize, endPoint.x);
                nextPointMaxX = actualPoint.x;
                nextPointMinY = Mathf.Max(0, actualPoint.y - nextSegmentAreaSize, endPoint.y);
                nextPointMaxY = actualPoint.y;
            }
            else
            {
                nextPointMinX = actualPoint.x;
                nextPointMaxX = Mathf.Min(mapParams.mapWidth - 1, actualPoint.x + nextSegmentAreaSize, endPoint.x);
                nextPointMinY = Mathf.Max(0, actualPoint.y - nextSegmentAreaSize, endPoint.y);
                nextPointMaxY = actualPoint.y;
            }

            Vector2Int nextPoint = GetRandomPoint(nextPointMinX, nextPointMaxX, nextPointMinY, nextPointMaxY);
            DrawShortestRoute(tile, actualPoint, nextPoint, clockwise);
            actualPoint = nextPoint;
        }
    }

    private void DrawShortestRoute(TileType tile, Vector2Int startPoint, Vector2Int endPoint, bool clockwise = false)
    {
        bool changeHorizontalFirst = Random.value > 0.5;
        DirectionType direction = GetDirection(startPoint, endPoint);
        if (clockwise)
        {
            if (direction == DirectionType.NE || direction == DirectionType.SW)
            {
                changeHorizontalFirst = false;
            }
            else
            {
                changeHorizontalFirst = true;
            }
        }

        if(changeHorizontalFirst)
        {
            for (int i = startPoint.x; i != endPoint.x; i += (int)Mathf.Sign(endPoint.x - startPoint.x))
            {
                map[i][startPoint.y] = tile;
            }
            for (int i = startPoint.y; i != endPoint.y; i += (int)Mathf.Sign(endPoint.y - startPoint.y))
            {
                map[endPoint.x][i] = tile;
            }
        }
        else
        {
            for (int i = startPoint.y; i != endPoint.y; i += (int)Mathf.Sign(endPoint.y - startPoint.y))
            {
                map[startPoint.x][i] = tile;
            }
            for (int i = startPoint.x; i != endPoint.x; i += (int)Mathf.Sign(endPoint.x - startPoint.x))
            {
                map[i][endPoint.y] = tile;
            }
        }

        map[endPoint.x][endPoint.y] = tile;
    }

    private DirectionType GetDirection(Vector2Int start, Vector2Int end)
    {
        bool isNorth = end.y > start.y;
        bool isWest = end.x < start.x;
        return (isNorth, isWest) switch
        {
            (true, true) => DirectionType.NW,
            (true, false) => DirectionType.NE,
            (false, true) => DirectionType.SW,
            _ => DirectionType.SE,
        };
    }

    private bool IsInMap(int i, int j)
    {
        return i >= 0 && j >= 0 && i < mapParams.mapWidth && j < mapParams.mapHeight;
    }

    private float GetPerlinNoise(int x, int y, float scale, float offset)
    {
        return Mathf.PerlinNoise(x * scale + offset, y * scale + offset);
    }


    private void Border(TileType tile)
    {
        for(int i = 0; i < mapParams.mapHeight; i++)
        {
            for(int j = 0; j < mapParams.mapWidth; j += mapParams.mapWidth - 1)
            {
                map[i][j] = TileType.MOUNTAIN;
            }
        }
        for (int j = 0; j < mapParams.mapWidth; j++)
        {
            for (int i = 0; i < mapParams.mapHeight; i += mapParams.mapHeight - 1)
            {
                map[i][j] = tile;
            }
        }
    }

    private Vector2Int GetRandomPoint(int minX, int maxX, int minY, int maxY)
    {
        minX = Mathf.Max(minX, 0);
        maxX = Mathf.Min(maxX, mapParams.mapWidth - 1);
        minY = Mathf.Max(minY, 0);
        maxY = Mathf.Min(maxY, mapParams.mapHeight - 1);
        return new Vector2Int(
           Mathf.RoundToInt(Random.Range(minX, maxX + 1)),
           Mathf.RoundToInt(Random.Range(minY, maxY + 1)));
    }

    private bool IsPossibleToReach(Vector2Int startPoint, Vector2Int endPoint)
    {
        List<Vector2Int> pointsToCheck = new List<Vector2Int>();
        pointsToCheck.Add(startPoint);
        HashSet<Vector2Int> possiblePoints = new HashSet<Vector2Int>();
        possiblePoints.Add(startPoint);
        Vector2Int[] directionVectors = { Vector2Int.up, Vector2Int.down, Vector2Int.right, Vector2Int.left };
        while (pointsToCheck.Count > 0)
        {
            Vector2Int point = pointsToCheck[0];
            pointsToCheck.RemoveAt(0);
            if(IsInMap(point.x, point.y))
            {
                foreach (Vector2Int dir in directionVectors)
                {
                    Vector2Int neighbour = point + dir;
                    if (neighbour.Equals(endPoint))
                    {
                        return true;
                    }

                    if (!possiblePoints.Contains(neighbour) && CanWalkOn(neighbour))
                    {
                        pointsToCheck.Add(neighbour);
                        possiblePoints.Add(neighbour);
                    }
                }
            }
        }
        return false;
    }

    private bool CanWalkOn(Vector2Int point)
    {
        return IsInMap(point.x, point.y) && (MapManager.tileCosts[map[point.x][point.y]] != -1) && (!entityPositions.Contains(point));
    }

    private bool IsEmptyTile(Vector2Int point)
    {
        return IsInMap(point.x, point.y) && (map[point.x][point.y] == TileType.GRASS) && (!entityPositions.Contains(point));
    }

    private Vector2Int GetEmptyPoint(int mapQuater = -1)
    {
        int minX = 0, maxX = mapParams.mapWidth - 1, minY = 0, maxY = mapParams.mapHeight - 1;
        switch (mapQuater)
        {
            case 0:
                minX = middlePoint.x;
                minY = middlePoint.y;
                break;
            case 1:
                maxX = middlePoint.x;
                minY = middlePoint.y;
                break;
            case 2:
                maxX = middlePoint.x;
                maxY = middlePoint.y;
                break;
            case 3:
                minX = middlePoint.x;
                maxY = middlePoint.y;
                break;
            default: 
                break;
        }

        Vector2Int point = GetRandomPoint(minX, maxX, minY, maxY);

        int maxIter = 100;
        int iter = 0;
        while(!IsEmptyTile(point) && iter++ < maxIter)
        {
            point = GetRandomPoint(minX, maxX, minY, maxY);
        }
        return point;
    }

    private void GenerateStrongholds() {

        int edgeDistance = 1;

        switch (numberOfPlayers)
        {
            case 2:
                AddBuilding(stronghold, new Vector2Int(edgeDistance, edgeDistance), 1);
                AddBuilding(stronghold, new Vector2Int(mapParams.mapWidth - 1 - edgeDistance, mapParams.mapHeight - 1 - edgeDistance), 2);
                break;
            case 3:
                AddBuilding(stronghold, new Vector2Int(edgeDistance, edgeDistance), 1);
                AddBuilding(stronghold, new Vector2Int(mapParams.mapWidth - 1 - edgeDistance, edgeDistance), 2);
                AddBuilding(stronghold, new Vector2Int(mapParams.mapWidth / 2, mapParams.mapHeight - 1 - edgeDistance), 3);
                break;
            default:
                AddBuilding(stronghold, new Vector2Int(edgeDistance, edgeDistance), 1);
                AddBuilding(stronghold, new Vector2Int(mapParams.mapWidth - 1 - edgeDistance, edgeDistance), 2);
                AddBuilding(stronghold, new Vector2Int(edgeDistance, mapParams.mapHeight - 1 - edgeDistance), 3);
                AddBuilding(stronghold, new Vector2Int(mapParams.mapWidth - 1 - edgeDistance, mapParams.mapHeight - 1 - edgeDistance), 4);
                break;
        }

    }

    private void GenerateBuildings()
    {
        GenerateStrongholdBuildings();
        GenerateMiddleBuildings();
    }

    private void GenerateStrongholdBuildings()
    {
        List<BuildingType> localPlayerBuildings = mapParams.buildingsForPlayer.ConvertAll(building => building);
        if (numberOfPlayers == 4)
        {
            localPlayerBuildings.Remove(BuildingType.RANDOMBUILDING);
        }

        foreach (BuildingType buildingType in localPlayerBuildings)
        {
            BuildingStats building = GetBuildingFromType(buildingType);
            int maxSpawnDistance = mapParams.buildingMaxRange;
            int minSpawnDistance = 2;
            int middleSpawnDistance = (maxSpawnDistance + minSpawnDistance) / 2;

            int maxDist, minDist;
            if (spawningBuildings.Contains(building))
            {
                maxDist = middleSpawnDistance;
                minDist = minSpawnDistance;
            }
            else
            {
                maxDist = maxSpawnDistance;
                minDist = middleSpawnDistance;
            }

            for (int i = 0; i < strongholdPositions.Count; i++)
            {
                Vector2Int strongholdPosition = strongholdPositions[i];
                Vector2Int position = Vector2Int.RoundToInt(Random.insideUnitCircle * maxDist);
                position += strongholdPosition;
                while (!ProximityCheck(position) || !IsInMap(position.x, position.y) || Vector2Int.Distance(position, strongholdPosition) < minDist)
                {
                    position = Vector2Int.RoundToInt(Random.insideUnitCircle * maxDist);
                    position += strongholdPosition;
                }
                if(Vector2Int.Distance(position, middlePoint) < Vector2Int.Distance(closesBuildingsToMiddle[i], middlePoint))
                {
                    closesBuildingsToMiddle[i] = position;
                }

                AddBuilding(building, position, numberOfPlayers + 1);
            }
        }
    }

    private void GenerateMiddleBuildings()
    {
        if(numberOfPlayers == 3)
        {
            middlePoint.y -= 2;
        }

        int numberOfBuildings = mapSize switch
        {
            (MapSize.SMALL) => 0,
            (MapSize.MEDIUM) => numberOfPlayers - 1,
            (MapSize.LARGE) => numberOfPlayers + 1,
            _ => 0
        };
        for(int i = 0; i < numberOfBuildings; i++)
        {
            BuildingStats building = supportingBuildings[Random.Range(0, supportingBuildings.Count)];
            Vector2Int position = Vector2Int.RoundToInt(Random.insideUnitCircle * mapParams.middleAreaSize);
            position += middlePoint;
            while (!ProximityCheck(position) || !IsInMap(position.x, position.y))
            { 
                position = Vector2Int.RoundToInt(Random.insideUnitCircle * mapParams.middleAreaSize);
                position += middlePoint;
            }
            AddBuilding(building, position, numberOfPlayers + 1);
            while (!ProximityCheck(position) || !IsInMap(position.x, position.y))
            {
                position = Vector2Int.RoundToInt(Random.insideUnitCircle * mapParams.middleAreaSize);
                position += middlePoint;
            }
            AddBuilding(goldMine, position, numberOfPlayers + 1);
        }
    }

    private BuildingStats GetBuildingFromType(BuildingType buildingType)
    {
        return buildingType switch
        {
            (BuildingType.GOLDMINE) => goldMine,
            (BuildingType.BARRACKS) => barracks,
            (BuildingType.HOSPITAL) => hospital,
            (BuildingType.MERCENARYCAMP) => mercenaryCamp,
            (BuildingType.SPAWNINGBUILDING) => spawningBuildings[Random.Range(0, spawningBuildings.Count)],
            (BuildingType.SUPPORTBUILDING) => supportingBuildings[Random.Range(0, supportingBuildings.Count)],
            (BuildingType.RANDOMBUILDING) => randomBuildings[Random.Range(0, randomBuildings.Count)],
            (BuildingType.STRONGHOLD) => stronghold,
            _ => null
        };
    }

    private bool ProximityCheck(Vector2Int position)
    {
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (entityPositions.Contains(new Vector2Int(position.x + i, position.y + j)))
                {
                    return false;
                }
            }
        }
        return true;
    }

    private void GeneratePaths()
    {
        if(mapSize != MapSize.SMALL) GenerateMiddlePath();
        if (Random.value < 0.5) GeneratePathToFurthestBuilding();
    }

    private void GenerateMiddlePath()
    {
        Vector2Int rigthPoint = middlePoint + Vector2Int.right * mapParams.middleAreaSize;
        Vector2Int leftPoint = middlePoint + Vector2Int.left * mapParams.middleAreaSize;
        Vector2Int downPoint = middlePoint + Vector2Int.down * mapParams.middleAreaSize;
        Vector2Int upPoint = middlePoint + Vector2Int.up * mapParams.middleAreaSize;

        ConnectPoints(TileType.PATH, rigthPoint, downPoint, true);
        ConnectPoints(TileType.PATH, downPoint, leftPoint, true);
        ConnectPoints(TileType.PATH, leftPoint, upPoint, true);
        ConnectPoints(TileType.PATH, upPoint, rigthPoint, true);
    }

    private void GeneratePathToFurthestBuilding()
    {
        for(int i = 0; i < strongholdPositions.Count; i++)
        {
            ConnectPoints(TileType.PATH, strongholdPositions[i] + Vector2Int.down, closesBuildingsToMiddle[i]);
        }
    }

    private void GenerateBlurs()
    {
        List<TileType> blurTiles = CreateTileListWithPositiveWeigths();
        int initialCount = blurTiles.Count;
        int i = 0;
        while(blurTiles.Count > 0 && i++ < mapParams.numberOfBlurs)
        {
            TileType tile = blurTiles[Random.Range(0, blurTiles.Count)];
            blurTiles.Remove(tile);
            Vector2Int position = GetEmptyPoint(i % 4);
            GenerateBlurV2(tile, position);
        }

        while(i++ < mapParams.numberOfBlurs)
        {
            TileType tile = SelectTile();
            Vector2Int position = GetEmptyPoint(i % 4);
            GenerateBlurV2(tile, position);
        }
    }

    private List<TileType> CreateTileListWithPositiveWeigths()
    {
        List<TileType> list = new List<TileType>();

        foreach(TileType tile in mapParams.tileWeigths.Keys)
        {
            if(mapParams.tileWeigths[tile] > 0)
            {
                list.Add(tile);
            }
        }
        return list;
    }

    private void GenerateRivers()
    {

        int numberOfRivers = Random.Range(1, mapParams.maxNumberOfRivers + 1);
        for (int i = 0; i < numberOfRivers; i++)
        {
            Vector2Int startPoint = GetRandomPoint(0, mapParams.mapWidth, 0, mapParams.mapHeight);

            Vector2Int endPoint = startPoint + Vector2Int.RoundToInt(Random.insideUnitCircle * mapParams.maxLengthOfRiver);
            while(!IsInMap(endPoint.x, endPoint.y) || Vector2Int.Distance(endPoint, startPoint) < mapParams.minLengthOfRiver)
            {
                endPoint = startPoint + Vector2Int.RoundToInt(Random.insideUnitCircle * mapParams.maxLengthOfRiver);
            }
            ConnectPoints(TileType.WATER, startPoint, endPoint);
        }
    }

    private void CleanBuildings()
    {
        for(int i = -1; i <= 1; i++)
        {
            for(int j = -1; j <= 1; j++)
            {
                foreach(Vector2Int building in entityPositions)
                {
                    int x = building.x + i;
                    int y = building.y + j;
                    if(IsInMap(x,y))
                    {
                        if(MapManager.tileCosts[map[x][y]] == -1 || (i == 0 && j == 0))
                        {
                            map[x][y] = TileType.GRASS;
                        }
                    }
                }
            }
        }
    }

    private bool CheckIfPlayable()
    {
        foreach(Vector2Int strongholdBuilding in strongholdPositions)
        {
            foreach (Vector2Int building in entityPositions)
            {
                if (!IsPossibleToReach(strongholdBuilding, building))
                {
                    return false;
                }
            }
        }
        return true;
    }
}
