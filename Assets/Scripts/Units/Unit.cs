using UnityEngine;
using System.Collections.Generic;
using System;
using Mirror;
using UnityEngine.Experimental.U2D.Animation;

public class Unit : NetworkBehaviour, IHoverInfo
{
    public UnitStats stats;

    public GameObject hitAnimationPrefab;
    public DeathAnimation deathAnimation;
    public GameObject rangedHitAnimationPrefab;

    [SyncVar]
    public PlayerState owner;

    [SyncVar(hook = nameof(UpdateHealthBar))]
    public int hp;

    [SyncVar(hook = nameof(UpdateActionsDisplay))]
    public int actions;

    [SyncVar]
    public int movement;

    [SyncVar]
    public bool usedCounter;

    public HealthBar healthBar;
    public SpriteRenderer spriteRenderer;
    public SpriteLibrary spriteLibrary;
    public Animator animator;
    public ActionsDisplay actionsDisplay;

    private List<Vector3Int> pathToWalk = null;
    private static float moveSpeedScale = 0.35f;

    private void OnDestroy()
    {
        InputHandler.EntityKilled(this);
    }

    [Server]
    public void ServerInitialize(PlayerState owner, UnitStats stats, Vector3 position)
    { 
        this.stats = stats;

        // update SyncVars only on server
        this.owner = owner;

        hp = stats.hp;
        movement = 0;
        actions = 0;

        transform.localPosition = position;
    }

    [ClientRpc]
    public void RpcClientInitialize(PlayerState owner, UnitStats stats, Vector3 position)
    {
        this.stats = stats;

        spriteLibrary.spriteLibraryAsset = GetSpriteLibrary(stats, owner.colorIndex);
        spriteRenderer.sprite = GetSprite(stats, owner);
        healthBar.MaxHealth = stats.hp;

        transform.position = position;

        UpdateActionsDisplay(0, 0);
    }

    [Server]
    public void Refresh()
    {
        movement = stats.movementSpeed;
        actions = stats.actions;
        usedCounter = false;
    }

    [Server]
    public void SetMovePath(List<Vector3Int> path)
    {
        pathToWalk = path;
    }

    [Server]
    public void Move()
    {
        if (Vector3.Distance(pathToWalk[0], transform.localPosition) < 0.001f)
        {
            pathToWalk.RemoveAt(0);
        }
        if (pathToWalk.Count > 0)
        {
            if (transform.localPosition.x > pathToWalk[0].x)
            {
                RpcTurnUnit(true);
            }
            else if (transform.localPosition.x < pathToWalk[0].x)
            {
                RpcTurnUnit(false);
            }
            transform.localPosition = Vector3.MoveTowards(transform.localPosition, pathToWalk[0], stats.movementSpeed * Time.deltaTime * moveSpeedScale);
            RpcMoveUnit(transform.position);
        }
    }

    [ClientRpc]
    public void RpcMoveUnit(Vector3 position)
    {
        transform.position = position;
    }

    [ClientRpc]
    public void RpcOnStartMoving()
    {
        animator.SetBool("isMoving", true);
        AudioManager.instance.PlayFootstepsSFX();
    }

    [ClientRpc]
    public void RpcOnStopMoving()
    {
        animator.SetBool("isMoving", false);
        AudioManager.instance.StopFootstepsSFX();
    }

    public bool IsMoving()
    {
        return pathToWalk != null && pathToWalk.Count > 0;
    }

    [ClientRpc]
    private void RpcTurnUnit(bool left)
    {
        if (left)
        {
            spriteRenderer.transform.localScale = new Vector3(-1, spriteRenderer.transform.localScale.y);
        }
        else
        {
            spriteRenderer.transform.localScale = new Vector3(1, spriteRenderer.transform.localScale.y);
        }
    }

    [Server]
    public void ApplyDamage(Unit damaging)
    {
        int damage = damaging.stats.attack - stats.armor;
        if (damage < 5)
            damage = 5;

        hp -= damage;

        Debug.Log(damaging + " dealt " + damage + " to " + this);
    }

    [ClientRpc]
    public void RpcOnAttack(Vector3 at, bool buildingSound, bool isRanged)
    {
        AudioClip attackSFX;
        if (buildingSound)
            attackSFX = stats.attackBuildingSFXs[new System.Random().Next(0, stats.attackBuildingSFXs.Length)];
        else
            attackSFX = stats.attackUnitSFXs[new System.Random().Next(0, stats.attackUnitSFXs.Length)];
        AudioManager.instance.PlayAttackSFX(attackSFX);
        if (at.x < transform.position.x)
            spriteRenderer.transform.localScale = new Vector3(-1, spriteRenderer.transform.localScale.y);
        else if (at.x > transform.position.x)
            spriteRenderer.transform.localScale = new Vector3(1, spriteRenderer.transform.localScale.y);
        GameObject animationPrefab;
        GameObject animObject;
        if (isRanged)
        {
            animationPrefab = rangedHitAnimationPrefab;
            float AngleRad = Mathf.Atan2(at.y - transform.position.y, at.x - transform.position.x); // angle in radians
            float AngleDeg = (180 / Mathf.PI) * AngleRad; // angle in degrees

            animObject = Instantiate(animationPrefab, at, Quaternion.Euler(0, 0, AngleDeg));
        }
        else
        {
            animationPrefab = hitAnimationPrefab;
            animObject = Instantiate(animationPrefab, at, Quaternion.identity);
            animObject.transform.localScale = spriteRenderer.transform.localScale;
        }
        Destroy(animObject, 0.5f);
    }

    [ClientRpc]
    public void RpcOnDeath(Vector3 from)
    {
        deathAnimation.PlayAnimation(spriteRenderer.sprite, transform.position - from);
    }

    public static Sprite GetSprite(UnitStats unit, PlayerState forPlayer)
    {
        string pathToSprite = $"GeneratedSprites/Team{forPlayer.colorIndex}/{unit.name}{forPlayer.colorIndex}";
        return Resources.Load<Sprite>(pathToSprite);
    }

    public static SpriteLibraryAsset GetSpriteLibrary(UnitStats unit, int colorIndex)
    {
        string pathToLib = $"Units/Team{colorIndex}Libs/{unit.name}Lib";
        return Resources.Load<SpriteLibraryAsset>(pathToLib);
    }

    private void UpdateHealthBar(int _, int newHP)
    {
        healthBar.Health = newHP;
    }

    private void UpdateActionsDisplay(int _, int newActions)
    {
        actionsDisplay.SetActions(newActions);
        if (newActions < 1)
        {
            spriteRenderer.color = Color.gray;
        }
        else
        {
            spriteRenderer.color = Color.white;
        }
    }

    public void OnHover()
    {
        healthBar.ShowNumbers();
        actionsDisplay.Show();
    }

    public void OnUnhover()
    {
        healthBar.HideNumbers();
        actionsDisplay.Hide();
    }
}
