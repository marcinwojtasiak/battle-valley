using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class ActionsDisplay : MonoBehaviour
{
    [SerializeField] private TMP_Text actionsText;

    public void SetActions(int actions)
    {
        actionsText.text = actions.ToString();
    }

    internal void Show()
    {
        gameObject.SetActive(true);
    }

    internal void Hide()
    {
        gameObject.SetActive(false);
    }
}
