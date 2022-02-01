using Mirror;
using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "ScriptableObjects/UnitStats", order = 1)]
public class UnitStats : ScriptableObject
{
    public int hp;
    public int armor;
    public int attack;
    public int attackRange;
    public bool counterAttack;
    public int movementSpeed;
    public int actions;
    public int cost;
    public Unit prefab;
    public AudioClip[] attackUnitSFXs;
    public AudioClip[] attackBuildingSFXs;
}

public static class UnitStatsSerializer
{
    public static void WriteUnitStats(this NetworkWriter writer, UnitStats unitStats)
    {
        writer.WriteString(unitStats.name);
    }
    public static UnitStats ReadUnitStats(this NetworkReader reader)
    {
        return (UnitStats)Resources.Load("ScriptableObjects/Units/" + reader.ReadString());
    }
}
