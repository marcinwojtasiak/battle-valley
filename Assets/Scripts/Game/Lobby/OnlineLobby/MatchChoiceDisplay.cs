using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MatchChoiceDisplay : MonoBehaviour
{
    private Guid matchId;

    [Header("GUI Elements")]
    public Image image;
    public Toggle toggleButton;
    public TMP_Text matchName;
    public TMP_Text playerCount;

    [Header("Diagnostics - Do Not Modify")]
    public OnlineLobbyController canvasController;

    public void Awake()
    {
        canvasController = FindObjectOfType<OnlineLobbyController>();
        toggleButton.onValueChanged.AddListener(delegate { OnToggleClicked(); });
    }

    public void OnToggleClicked()
    {
        canvasController.SelectMatch(toggleButton.isOn ? matchId : Guid.Empty);
        image.color = toggleButton.isOn ? new Color(0f, 0f, 0f, 0.2f) : new Color(1f, 1f, 1f, 0.0f);
    }

    public Guid GetMatchId()
    {
        return matchId;
    }

    public void SetMatchInfo(MatchInfo matchInfo)
    {
        matchId = matchInfo.matchId;
        matchName.text = matchInfo.matchName;
        playerCount.text = $"{matchInfo.players}/{matchInfo.maxPlayers}";
    }
}
