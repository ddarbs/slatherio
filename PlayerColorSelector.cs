using System.Collections;
using System.Collections.Generic;
using FishNet.Object;
using UnityEngine;

public class PlayerColorSelector : MonoBehaviour
{
    [SerializeField] private Color[] c_SnakeColors = new Color[9];
    [SerializeField] private GameObject[] i_CheckMarks = new GameObject[10];
    //public static int p_PlayerColorinput;
    //public static Color p_PlayerSelectedColor;

    [SerializeField] private PlayerData i_PlayerData;
    
    private void Awake()
    {
        /*if (IsServer)
        {
            this.enabled = false;
        }*/ 
        /*p_PlayerColorinput = Random.Range(0,9);
        p_PlayerSelectedColor = c_SnakeColors[p_PlayerColorinput];*/

        int l_Random = Random.Range(0, 9);
        i_PlayerData.p_SnakeColorIndex = l_Random;
        i_PlayerData.p_SnakeColor = c_SnakeColors[l_Random];
        i_CheckMarks[l_Random].SetActive(true);
    }

    public void SelectColor(int selection)
    {
        ResetCheckMarks();
        i_CheckMarks[selection].SetActive(true);
        //p_PlayerColorinput = selection;
        //p_PlayerSelectedColor = c_SnakeColors[selection];
        i_PlayerData.p_SnakeColorIndex = selection;
        if (selection < 9)
        {
            i_PlayerData.p_SnakeColor = c_SnakeColors[selection];
        }
    }
    
    private void ResetCheckMarks()
    {
        for (int i = 0; i < i_CheckMarks.Length; i++)
        {
            i_CheckMarks[i].SetActive(false);
        }
    }
}
