using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Spawner
{
    public List<SpawnerEntry<UnitStats>> units = new List<SpawnerEntry<UnitStats>>();
    public List<SpawnerEntry<BuildingStats>> buildings = new List<SpawnerEntry<BuildingStats>>();
    public bool isNeutral;
}

[Serializable]
public class SpawnerEntry<T>
{
    public T entity;
    public Vector2Int position;
}
