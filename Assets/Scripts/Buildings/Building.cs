using Mirror;
using UnityEngine;

public class Building : NetworkBehaviour, IHoverInfo
{
    public BuildingStats stats;

    [SyncVar(hook = nameof(UpdateSprite))]
    public PlayerState owner;

    [SyncVar(hook = nameof(IsAlreadyUsedInTurn))]
    public bool isAlreadyUsedInTurn;

    [SyncVar(hook = nameof(UpdateHealthBar))]
    public int hp;

    public HealthBar healthBar;
    public SpriteRenderer spriteRenderer;

    private void OnDestroy()
    {
        InputHandler.EntityKilled(this);
    }

    [Server]
    public void ServerInitialize(PlayerState owner, Vector3 position)
    {
        this.owner = owner;

        hp = stats.hp;

        transform.localPosition = position;
        isAlreadyUsedInTurn = false;
    }

    [ClientRpc]
    public void RpcClientInitialize(Vector3 position)
    {
        healthBar.MaxHealth = stats.hp;

        transform.position = position;
    }

    public static Sprite GetSprite(BuildingStats building, PlayerState forPlayer)
    {
        string pathToSprite = $"GeneratedSprites/Team{forPlayer.colorIndex}/{building.name}{forPlayer.colorIndex}";
        return Resources.Load<Sprite>(pathToSprite);
    }

    public void ApplyDamage(Unit damaging)
    {
        int damage = damaging.stats.attack;
        if (damage < 5)
            damage = 5;

        hp -= damage;

        Debug.Log(damaging + " dealt " + damage + " to " + this);
    }

    private void UpdateHealthBar(int oldHP, int newHP)
    {
        healthBar.Health = newHP;
    }

    private void IsAlreadyUsedInTurn(bool _, bool value)
    {
        isAlreadyUsedInTurn = value;
        if (isAlreadyUsedInTurn)
        {
            spriteRenderer.color = Color.gray;
        }
        else
        {
            spriteRenderer.color = Color.white;
        }
    }

    private void UpdateSprite(PlayerState _, PlayerState owner)
    {
        spriteRenderer.sprite = GetSprite(stats, owner);
    }

    [Server]
    public void Refresh()
    {
        isAlreadyUsedInTurn = false;
    }

    public void OnHover()
    {
        healthBar.ShowNumbers();
    }

    public void OnUnhover()
    {
        healthBar.HideNumbers();
    }
}

