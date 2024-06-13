using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerUiController : MonoBehaviour
{
  [SerializeField] private GameObject i_ColorMenu;
  private bool i_ColorMenuOnOff = false; // false is off on is true

  void Start()
  {
    i_ColorMenu.SetActive(false);
  }

  public void ColorMenuActivator()
  {
    if (i_ColorMenuOnOff == false)
    {
      i_ColorMenuOnOff = true;
      i_ColorMenu.SetActive(true);
    } else if (i_ColorMenuOnOff == true)
    {
      i_ColorMenuOnOff = false;
      i_ColorMenu.SetActive(false);
    }
  }


}
