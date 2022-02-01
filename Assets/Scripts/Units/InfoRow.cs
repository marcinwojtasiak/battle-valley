using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InfoRow : MonoBehaviour
{
    [SerializeField] private TMP_Text keyField;
    [SerializeField] private TMP_Text valueField;

    public void SetStats(string key, string value)
    {
        keyField.text = key;
        valueField.text = value;
    }
}
