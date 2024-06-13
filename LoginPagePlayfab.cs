using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using PlayFab;
using PlayFab.ClientModels;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginPagePlayfab : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI TopText;
    [SerializeField] private TextMeshProUGUI MessageText;

    [Header("Login")] 
    [SerializeField] private TMP_InputField EmailLoginInput;
    [SerializeField] private TMP_InputField PasswordLoginput;
    [SerializeField] private GameObject LoginPage;

    [Header("Register")] 
    [SerializeField] private TMP_InputField EmailRegisterInput;
    [SerializeField] private TMP_InputField PasswordRegisterinput;
    [SerializeField] private GameObject RegisterPage;

    [Header("Recovery")] 
    [SerializeField] private TMP_InputField EmailRecoveryInput;
    [SerializeField] private GameObject RecoveryPage;
    
    [Header("Data Storage")] 
    [SerializeField] private PlayerData i_PlayerData;

    [Header("Post-Login")] 
    [SerializeField] private GameObject i_LoginCanvas;
    [SerializeField] private GameObject i_LoggedPage;
    [SerializeField] private TextMeshProUGUI i_LoggedInAsText;
    [SerializeField] private TextMeshProUGUI i_ServerStatusText;
    [SerializeField] private Button i_PlayButton;
    [SerializeField] private PlayFabLeaderboardDisplay i_LeaderboardDisplay;
    [SerializeField] private TMP_InputField i_NameInput;

    private Coroutine i_CheckServerStatusThread;
    private bool i_DisplayNameUpdated;
    private bool i_RequestingPlay;

    private void Start()
    {
        if (!PlayFabClientAPI.IsClientLoggedIn())
        {
            i_LoginCanvas.SetActive(true);
            EventSystem.current.SetSelectedGameObject(EmailLoginInput.gameObject, new PointerEventData(EventSystem.current));
            return;
        }
        
        i_LeaderboardDisplay.OnPlayFabLogin();
        
        PlayFabClientAPI.GetTitleData(new GetTitleDataRequest(), OnTitleDataReceived, OnError);

        i_LoggedInAsText.text = "Logged in as: " + i_PlayerData.p_PlayFabLoginName;
        
        i_LoggedPage.SetActive(true);
        EventSystem.current.SetSelectedGameObject(i_NameInput.gameObject, new PointerEventData(EventSystem.current));
    }

    public void OpenLoginPage()
    {
        LoginPage.SetActive(true);
        RegisterPage.SetActive(false);
        RecoveryPage.SetActive(false);
        TopText.text = "Login";
    }

    public void OpenRegisterPage()
    {
        LoginPage.SetActive(false);
        RegisterPage.SetActive(true);
        RecoveryPage.SetActive(false);
        TopText.text = "Register";
    }

    public void OpenRecoveryPage()
    {
        LoginPage.SetActive(false);
        RegisterPage.SetActive(false);
        RecoveryPage.SetActive(true);
        TopText.text = "Recover";
    }

    //play fab functions
    public void RegisterUser()
    {
        // if statement if pasdsowrd is less than 6 error that password too short
        Debug.Log(EmailLoginInput.text);
        Debug.Log(PasswordRegisterinput.text);
        var request = new RegisterPlayFabUserRequest
        {
            Email = EmailRegisterInput.text,
            Password = PasswordRegisterinput.text,
            DisplayName = EmailRegisterInput.text,
            RequireBothUsernameAndEmail = false
        };
        Debug.Log(request);
        PlayFabClientAPI.RegisterPlayFabUser(request, OnRegisterSuccess, OnError);
    }

    public void Login()
    {
        var request = new LoginWithEmailAddressRequest
        {
            Email = EmailLoginInput.text,
            Password = PasswordLoginput.text,
            
        };
        PlayFabClientAPI.LoginWithEmailAddress(request, OnLoginSuccess, OnError);
    }

    private void OnLoginSuccess(LoginResult result)
    {
        MessageText.text = "logged in";

        i_PlayerData.p_PlayFabLoginName = EmailLoginInput.text;
        i_PlayerData.p_PlayFabLogin = result;
        
        PlayFabClientAPI.GetTitleData(new GetTitleDataRequest(), OnTitleDataReceived, OnError);

        i_LeaderboardDisplay.OnPlayFabLogin();
        
        i_LoggedInAsText.text = "Logged in as: " + i_PlayerData.p_PlayFabLoginName;
        
        i_LoginCanvas.SetActive(false);
        i_LoggedPage.SetActive(true);
        EventSystem.current.SetSelectedGameObject(i_NameInput.gameObject, new PointerEventData(EventSystem.current));
    }

    private void OnTitleDataReceived(GetTitleDataResult _result)
    {
        if (_result.Data == null || !_result.Data.ContainsKey("PlayFlowIP"))
        {
            i_PlayerData.p_PlayFlowIP = "";
            i_ServerStatusText.color = Color.red;
            i_PlayButton.interactable = false;
            Debug.LogError("Cannot find the title data for PlayFlowIP");
            return;
        }

        if (_result.Data["PlayFlowIP"] == "")
        {
            Debug.LogError("IP is not set in the title data for PlayFlowIP");
            if (i_CheckServerStatusThread == null)
            {
                i_PlayerData.p_PlayFlowIP = "";
                i_ServerStatusText.color = Color.red;
                i_PlayButton.interactable = false;
                i_CheckServerStatusThread = StartCoroutine(CheckServerStatus());
            }
            return;
        }
        
        i_PlayerData.p_PlayFlowIP = _result.Data["PlayFlowIP"];
        i_ServerStatusText.color = Color.green;
        if (!i_RequestingPlay)
        {
            i_PlayButton.interactable = true;
        }
    }

    private IEnumerator CheckServerStatus()
    {
        while (i_PlayerData.p_PlayFlowIP == "")
        {
            yield return new WaitForSeconds(9f);
            
            PlayFabClientAPI.GetTitleData(new GetTitleDataRequest(), OnTitleDataReceived, OnError);

            yield return new WaitForSeconds(1f);
        }

        i_CheckServerStatusThread = null;
    }

    public void RecoverUser()
    {
        var request = new SendAccountRecoveryEmailRequest
        {
            Email = EmailRecoveryInput.text,
            TitleId = "36699",
        };
        PlayFabClientAPI.SendAccountRecoveryEmail(request, OnRecoverySuccess, OnError);
    }

    private void OnRecoverySuccess(SendAccountRecoveryEmailResult obj)
    {
        OpenLoginPage();
        MessageText.text = "Recovery Email Sent";
    }

    private void OnError(PlayFabError Error)
    {
        MessageText.text = Error.ErrorMessage;
        Debug.Log(Error.GenerateErrorReport());
    }
    private void OnRegisterSuccess(RegisterPlayFabUserResult Result)
    {
        MessageText.text = "Account Created Successfully";
        OpenLoginPage();
    }
    
    public void Button_SwapToSlitherScene()
    {
        i_RequestingPlay = true;
        i_PlayButton.interactable = false;
        if (i_NameInput.text != "")
        {
            PlayFabClientAPI.UpdateUserTitleDisplayName(new UpdateUserTitleDisplayNameRequest
            {
                DisplayName = i_NameInput.text
            }, result=> OnDisplayNameChanged(result), OnError);
        }
        else
        {
            i_DisplayNameUpdated = true;
        }
        
        i_PlayerData.p_PlayFlowIP = "";
        PlayFabClientAPI.GetTitleData(new GetTitleDataRequest(), OnTitleDataReceived, OnError);
        StartCoroutine(SwapToSlitherSceneCheck());
    }

    private void OnDisplayNameChanged(UpdateUserTitleDisplayNameResult _result)
    {
        i_DisplayNameUpdated = true;
    }

    private IEnumerator SwapToSlitherSceneCheck()
    {
        while (!i_DisplayNameUpdated)
        {
            yield return new WaitForSeconds(0.25f);
        }
        yield return new WaitForSeconds(1f);
        if (i_PlayerData.p_PlayFlowIP != "")
        {
            SceneManager.LoadScene(1);
        }
        yield return new WaitForSeconds(1f);
        i_PlayButton.interactable = true;
        i_RequestingPlay = false;
    }
}
