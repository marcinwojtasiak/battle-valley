using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MapGeneratorParams
{
    public int mapWidth;
    public int mapHeight;
    public int numberOfBlurs;
    public Dictionary<TileType, float> blurDepthDecreaseFactor;
    public Dictionary<TileType, float> blurDrawThreshold;
    public Dictionary<TileType, int> tileWeigths;
    public int nextSegmentAreaSize;
    public int middleAreaSize;
    public int buildingMaxRange; //max range where building can be spawn from stronghold
    public int maxNumberOfRivers;
    public int minLengthOfRiver;
    public int maxLengthOfRiver;
    public List<BuildingType> buildingsForPlayer;
    public List<BuildingType> buildingInMiddle;

    public static MapGeneratorParams GetParams(MapSize mapSize)
    {
        return paramsBySize[mapSize];
    }

    private static Dictionary<MapSize, MapGeneratorParams> paramsBySize = new Dictionary<MapSize, MapGeneratorParams>()
    {

        //Settings for small map
        {
            MapSize.SMALL, new MapGeneratorParams()
            {
                mapWidth = 15,
                mapHeight = 13,
                nextSegmentAreaSize = 3,
                numberOfBlurs = 6,
                middleAreaSize = 0,
                buildingMaxRange = 4,
                maxNumberOfRivers = 3,
                minLengthOfRiver = 3,
                maxLengthOfRiver = 5,

                //Weights for tiles
                tileWeigths = new Dictionary<TileType, int>()
                {
                    { TileType.FOREST,   10 },
                    { TileType.GRASS,    0 },
                    { TileType.MOUNTAIN, 5 },
                    { TileType.PATH,     0 },
                    { TileType.ROCKS,    7 },
                    { TileType.WATER,    0 }
                },

                blurDepthDecreaseFactor = new Dictionary<TileType, float>()
                {
                    { TileType.FOREST,   2f },
                    { TileType.GRASS,    0f },
                    { TileType.MOUNTAIN, 1.4f },
                    { TileType.PATH,     0f },
                    { TileType.ROCKS,    1.3f },
                    { TileType.WATER,    0f }
                },

                blurDrawThreshold = new Dictionary<TileType, float>()
                {
                    { TileType.FOREST,   1f },
                    { TileType.GRASS,    0f },
                    { TileType.MOUNTAIN, 0.6f },
                    { TileType.PATH,     0 },
                    { TileType.ROCKS,    0.5f },
                    { TileType.WATER,    0f }
                },

                buildingsForPlayer = new List<BuildingType>()
                {
                    BuildingType.SPAWNINGBUILDING,
                    BuildingType.GOLDMINE,
                    BuildingType.RANDOMBUILDING
                },

                buildingInMiddle = new List<BuildingType>()
                {

                }
            }
        },

        {
            MapSize.MEDIUM, new MapGeneratorParams()
            {
                mapWidth = 20,
                mapHeight = 17,
                nextSegmentAreaSize = 3,
                numberOfBlurs = 12,
                middleAreaSize = 3,
                buildingMaxRange = 6,
                maxNumberOfRivers = 4,
                minLengthOfRiver = 4,
                maxLengthOfRiver = 7,

                 //Weights for tiles
                tileWeigths = new Dictionary<TileType, int>()
                {
                    { TileType.FOREST,   10 },
                    { TileType.GRASS,    0 },
                    { TileType.MOUNTAIN, 5 },
                    { TileType.PATH,     0 },
                    { TileType.ROCKS,    7 },
                    { TileType.WATER,    0 }
                },

                blurDepthDecreaseFactor = new Dictionary<TileType, float>()
                {
                    { TileType.FOREST,   2f },
                    { TileType.GRASS,    0f },
                    { TileType.MOUNTAIN, 1.4f },
                    { TileType.PATH,     0f },
                    { TileType.ROCKS,    1.3f },
                    { TileType.WATER,    0f }
                },

                blurDrawThreshold = new Dictionary<TileType, float>()
                {
                    { TileType.FOREST,   1f },
                    { TileType.GRASS,    0f },
                    { TileType.MOUNTAIN, 0.7f },
                    { TileType.PATH,     0 },
                    { TileType.ROCKS,    0.5f },
                    { TileType.WATER,    0f }
                },

                buildingsForPlayer = new List<BuildingType>()
                {
                    BuildingType.BARRACKS,
                    BuildingType.MERCENARYCAMP,
                    BuildingType.GOLDMINE,
                },

                 buildingInMiddle = new List<BuildingType>()
                {
                     BuildingType.SUPPORTBUILDING,
                    BuildingType.GOLDMINE,
                }
            }
        },

        {
            MapSize.LARGE, new MapGeneratorParams()
            {
                mapWidth = 25,
                mapHeight = 22,
                nextSegmentAreaSize = 3,
                numberOfBlurs = 20,
                middleAreaSize = 4,
                buildingMaxRange = 8,
                maxNumberOfRivers = 5,
                minLengthOfRiver = 4,
                maxLengthOfRiver = 10,

                 //Weights for tiles
                tileWeigths = new Dictionary<TileType, int>()
                {
                    { TileType.FOREST,   10 },
                    { TileType.GRASS,    0 },
                    { TileType.MOUNTAIN, 5 },
                    { TileType.PATH,     0 },
                    { TileType.ROCKS,    7 },
                    { TileType.WATER,    0 }
                },

                blurDepthDecreaseFactor = new Dictionary<TileType, float>()
                {
                    { TileType.FOREST,   2f },
                    { TileType.GRASS,    0f },
                    { TileType.MOUNTAIN, 1.4f },
                    { TileType.PATH,     0f },
                    { TileType.ROCKS,    1.3f },
                    { TileType.WATER,    0f }
                },

                blurDrawThreshold = new Dictionary<TileType, float>()
                {
                    { TileType.FOREST,   1f },
                    { TileType.GRASS,    0f },
                    { TileType.MOUNTAIN, 0.8f },
                    { TileType.PATH,     0 },
                    { TileType.ROCKS,    0.5f },
                    { TileType.WATER,    0f }
                },

                buildingsForPlayer = new List<BuildingType>()
                {
                    BuildingType.MERCENARYCAMP,
                    BuildingType.BARRACKS,
                    BuildingType.GOLDMINE,
                    BuildingType.HOSPITAL
                },

                buildingInMiddle = new List<BuildingType>()
                {
                    BuildingType.SUPPORTBUILDING,
                    BuildingType.GOLDMINE,
                }
            }
        }
    };
}

public enum MapSize
{
    SMALL,
    MEDIUM,
    LARGE
}

public enum BuildingType
{
    STRONGHOLD,
    GOLDMINE,
    HOSPITAL,
    BARRACKS,
    MERCENARYCAMP,
    SUPPORTBUILDING,
    SPAWNINGBUILDING,
    RANDOMBUILDING
}