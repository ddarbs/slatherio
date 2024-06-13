using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class InputNameHandler : MonoBehaviour
{
    [SerializeField] private PlayerData i_PlayerData;
    [SerializeField] private TextMeshProUGUI i_PlaceHolderNameText;

    private void Start()
    {
        if (i_PlayerData.p_CurrentName != "")
        {
            i_PlaceHolderNameText.text = i_PlayerData.p_CurrentName;
        }
    }

    public void OnValueChanged(string _value)
    {
        i_PlayerData.p_CurrentName = _value;
    }
}
