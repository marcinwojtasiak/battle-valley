using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapManager : MonoBehaviour
{
    private string tileInput =
        "5 5 5 5 1 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5\n" +
        "5 5 5 2 2 2 2 2 2 2 2 2 2 2 2 2 2 2 3 3 3 1 5\n" +
        "5 4 4 1 1 2 1 0 0 0 0 0 0 0 2 2 2 1 1 3 3 5 5\n" +
        "4 4 1 1 1 1 0 0 3 3 0 3 3 0 1 1 1 1 1 1 5 5 5\n" +
        "4 4 1 1 1 1 0 5 3 1 0 0 0 0 0 0 0 0 0 1 1 5 5\n" +
        "5 5 1 1 1 1 0 2 1 1 1 1 1 1 2 2 2 2 0 1 1 1 5\n" +
        "5 5 1 1 1 1 0 5 5 1 1 1 1 1 2 2 1 5 0 5 1 1 5\n" +
        "5 5 1 1 0 0 0 2 2 2 2 1 1 4 4 4 4 2 0 2 1 1 5\n" +
        "5 5 2 1 1 1 2 2 2 2 2 2 2 4 4 4 1 1 0 2 2 5 5\n" +
        "5 5 2 1 1 1 1 2 2 2 2 2 2 2 4 4 4 4 1 1 1 5 5\n" +
        "5 1 1 3 3 5 5 5 2 2 1 1 1 4 4 4 4 4 1 5 5 1 5\n" +
        "5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5 5"; 

    private TileType[][] tileMapValues;

    public Tilemap baseTilemap;
    public Tilemap moveIndicatorTilemap;
    public Tilemap attackIndicatorTilemap;
    public Tilemap gridOutlineTilemap;
    public Tilemap hoverIndicatorTilemap;

    private Dictionary<TileType, TileBase> tileTypeToTile = new Dictionary<TileType, TileBase>();
    public TileBase pathTile;
    public TileBase grassTile;
    public TileBase forestTile;
    public TileBase rocksTile;
    public TileBase waterTile;
    public TileBase mountainTile;
    public TileBase moveIndicatorTile;
    public TileBase attackIndicatorTile;
    public TileBase gridOutlineTile;
    public TileBase hoverIndicatorTile;

    public static Dictionary<TileType, int> tileCosts = new Dictionary<TileType, int>();

    public int MapHeight
    {
        get { return tileMapValues[0].Length; }
    }

    public int MapWidth
    {
        get { return tileMapValues.Length; }
    }

    public void Awake()
    {
        tileCosts.Clear();

        tileCosts.Add(TileType.PATH, 1);
        tileCosts.Add(TileType.GRASS, 2);
        tileCosts.Add(TileType.FOREST, 3);
        tileCosts.Add(TileType.ROCKS, 4);
        tileCosts.Add(TileType.MOUNTAIN, -1);
        tileCosts.Add(TileType.WATER, -1);
    }
   
    public void InitMapVisualization(TileType[][] map)
    {
        tileMapValues = map;
        SetupTiles();
        GenerateOutline();
        if (SettingsManager.instance)
            gridOutlineTilemap.gameObject.SetActive(SettingsManager.instance.GridEnabled);
    }

    public void SetGridOutlineActive(bool value)
    {
        gridOutlineTilemap.gameObject.SetActive(value);
    }

    public void SetMoveIndicator(List<Vector3Int> locations)
    {
        ClearMoveIndicator();
        foreach (Vector3Int location in locations)
        {
            moveIndicatorTilemap.SetTile(location, moveIndicatorTile);
        }
    }

    public void SetAttackIndicator(List<Vector3Int> locations)
    {
        ClearAttackIndicator();
        foreach (Vector3Int location in locations)
        {
            attackIndicatorTilemap.SetTile(location, attackIndicatorTile);
        }
    }

    public void SetHoverIndicator(Vector3Int mouseLocation)
    {
        if (hoverIndicatorTilemap.GetTile(mouseLocation) != hoverIndicatorTile)
        {
            ClearHoverIndicator();

            if (GetTileAt(mouseLocation.x, mouseLocation.y).HasValue)
            {
                hoverIndicatorTilemap.SetTile(mouseLocation, hoverIndicatorTile);
            }
        }
    }

    public void ClearMoveIndicator()
    {
        moveIndicatorTilemap.ClearAllTiles();
    }

    public void ClearAttackIndicator()
    {
        attackIndicatorTilemap.ClearAllTiles();
    }

    public void ClearHoverIndicator()
    {
        hoverIndicatorTilemap.ClearAllTiles();
    }

    public TileType? GetTileAt(int x, int y)
    {
        if (MapHeight <= y || y < 0)
        {
            return null;
        }
        if (MapWidth <= x || x < 0)
        {
            return null;
        }
        return tileMapValues[x][y];
    }

    public int GetTileCost(TileType tileType)
    {
        return tileCosts[tileType];
    }

    public void AddPointIfInsideMap(Vector3Int point, List<Vector3Int> resultList)
    {
        Vector3Int maxPoint = new Vector3Int(MapWidth - 1, MapHeight - 1, 0);
        Vector3Int minPoint = Vector3Int.zero;

        if (!Vector3Int.Max(maxPoint, point).Equals(maxPoint))
        {
            return;
        }
        if (!Vector3Int.Min(minPoint, point).Equals(minPoint))
        {
            return;
        }
        resultList.Add(point);
    }

    private void SetupTiles()
    {
        tileTypeToTile.Clear();

        tileTypeToTile.Add(TileType.PATH, pathTile);
        tileTypeToTile.Add(TileType.GRASS, grassTile);
        tileTypeToTile.Add(TileType.FOREST, forestTile);
        tileTypeToTile.Add(TileType.ROCKS, rocksTile);
        tileTypeToTile.Add(TileType.WATER, waterTile);
        tileTypeToTile.Add(TileType.MOUNTAIN, mountainTile);

        SetupTilemap();
    }

    private void SetupTilemap()
    {
        for (int i = 0; i < MapWidth; i++)
        {
            for (int j = 0; j < MapHeight; j++)
            {
                baseTilemap.SetTile(new Vector3Int(i, j, 0), tileTypeToTile[tileMapValues[i][j]]);
            }
        }
    }

    private TileType[][] ConvertToTileMap(string tileMapInput)
    {
        string[] rows = tileMapInput.Split('\n');
        TileType[][] tileTypes = new TileType[rows.Length][];
        for (int i = 0; i < rows.Length; i++)
        {
            string[] cols = rows[i].Split(' ');
            tileTypes[i] = new TileType[cols.Length];
            for (int j = 0; j < cols.Length; j++)
            {
                tileTypes[i][j] = ConvertToTile(cols[j]);
            }
        }
        return tileTypes;
    }

    private TileType ConvertToTile(string character)
    {
        return (TileType)int.Parse(character);
    }

    private void GenerateOutline()
    {
        for (int i = 0; i < MapWidth; i++)
        {
            for (int j = 0; j < MapHeight; j++)
            {
                gridOutlineTilemap.SetTile(new Vector3Int(i, j, 0), gridOutlineTile);
            }
        }
    }
}
