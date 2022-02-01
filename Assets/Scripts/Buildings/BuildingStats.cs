using Mirror;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/BuildingStats", order = 1)]
public class BuildingStats : ScriptableObject
{
    public int hp;
    public int regen;
    public int income;
    public Building prefab;
}

public static class BuildingStatsSerializer
{
    public static void WriteBuildingStats(this NetworkWriter writer, BuildingStats buildingStats)
    {
        writer.WriteString(buildingStats.name);
    }
    public static BuildingStats ReadBuildingStats(this NetworkReader reader)
    {
        return (BuildingStats)Resources.Load("ScriptableObjects/Buildings/" + reader.ReadString());
    }
}
