using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Slider slider;
    [SerializeField] private TMP_Text healthLabel;
    [SerializeField] private float onClickScale;

    private int maxHealth;
    public int MaxHealth { 
        set
        {
            maxHealth = value;
            slider.maxValue = value;
            slider.value = value;
            UpdateHealthLabel();
        }
        get
        {
            return maxHealth;
        }
    }

    private int health;
    public int Health
    {
        set
        {
            health = value;
            slider.value = value;
            UpdateHealthLabel();
        }
        get
        {
            return health;
        }
    }

    private void UpdateHealthLabel()
    {
        healthLabel.text = $"{Health} / {MaxHealth}";
    }

    public void ShowNumbers()
    {
        healthLabel.gameObject.SetActive(true);
        gameObject.transform.localScale = gameObject.transform.localScale * onClickScale;
    }

    public void HideNumbers()
    {
        healthLabel.gameObject.SetActive(false);
        gameObject.transform.localScale = gameObject.transform.localScale / onClickScale;
    }
}
