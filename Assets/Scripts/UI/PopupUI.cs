using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class PopupUI : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private TMP_Text topText;
    [SerializeField] private TMP_Text bottomText;

    private void Start()
    {
        gameObject.SetActive(false);
    }

    public void PlayAnimation(PlayerState player, string bottomText)
    {
        gameObject.SetActive(true);
        topText.text = player.playerName;
        Color[] playerColors = GameUIReferences.instance.teamPalletes.palettes[player.colorIndex].color;
        topText.color = playerColors[playerColors.Length - 1];
        this.bottomText.text = bottomText;
        animator.SetTrigger("Play");
    }

    public void OnEndAnimation()
    {
        gameObject.SetActive(false);
    }
}
