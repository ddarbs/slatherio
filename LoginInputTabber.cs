using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LoginInputTabber : MonoBehaviour
{
    private bool i_InputOneFocused = true; // INFO: auto set to focused on start
    
    [SerializeField] private TMP_InputField i_InputFieldOne, i_InputFieldTwo;
    [SerializeField] private Button i_Button;
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            if (!i_InputOneFocused)
            {
                i_InputOneFocused = true;
                EventSystem.current.SetSelectedGameObject(i_InputFieldOne.gameObject, new PointerEventData(EventSystem.current));
            }
            else
            {
                i_InputOneFocused = false;
                EventSystem.current.SetSelectedGameObject(i_InputFieldTwo.gameObject, new PointerEventData(EventSystem.current));
            }
        }

        if (Input.GetKeyDown(KeyCode.Return))
        {
            if (i_InputFieldOne.text != "" && i_InputFieldTwo.text != "")
            {
                i_Button.OnPointerClick(new PointerEventData(EventSystem.current));
            }
        }
        
    }
}
