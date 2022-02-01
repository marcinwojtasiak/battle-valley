using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class RecruitmentRow : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text priceText;
    [SerializeField] private Button buyButton;

    [HideInInspector] public UnitStats unit; // should be set from Recruitment script
    [HideInInspector] public int index;
    private Recruitment parent;
    private void Start()
    {
        nameText.text = unit.name;
        priceText.text = unit.cost.ToString();
        parent = GetComponentInParent<Recruitment>();
    }

    public void RecruitUnit()
    {
        parent.RecruitUnit(index);
    }

    public void ShowInfo()
    {
        parent.ShowInfo(index);
    }

    public void SetDisabled(bool disabled)
    {
        buyButton.interactable = !disabled;
    }
}
