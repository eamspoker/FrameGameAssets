using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using Photon.Pun;
using Facebook.Unity;
using TMPro;

namespace ICSI.FrameNet.FrameGame {
public class MenuScript : MonoBehaviourPunCallbacks
{

    [SerializeField] private string VersionName = "1";
    [SerializeField] private GameObject StartButton;

    // For testing without Facebook authentication
    private bool IsMultTesting = true;
    [SerializeField] private TMP_InputField TestUsername;
    [SerializeField] private GameObject TestUsernameG;
    [SerializeField] private GameObject Connecting;
    [SerializeField] private GameObject LoginButton;


    // Start is called before the first frame update
    public void Login()
    {
        LoginButton.SetActive(false);
        Connecting.SetActive(true);
        if(!IsMultTesting)
        {
            if (!FB.IsInitialized)
            {
                // Initialize the Facebook SDK 
                // Most recent (GraphApiVersion) causes errors in HTTP requests
                FB.GraphApiVersion = "v13.0";
                FB.Init(InitCallback, OnHideUnity);
            }
            else
            {
                FB.ActivateApp();
                FacebookLogin();
            }
        } else {
            PhotonNetwork.ConnectUsingSettings();
            PhotonNetwork.GameVersion = VersionName;
        }

    }

    void Start()
    {
    
    }

    

    // Update is called once per frame
    void Update()
    {
        
    }
    public void ConnectSRL() 
    {
        
        if(IsMultTesting)
        {
          PhotonNetwork.NickName = TestUsername.text;  
        }
        PhotonNetwork.JoinRandomRoom();
    }


    // Callbacks: these are called after a certain event

    public override void OnConnectedToMaster()
    {
        if(!IsMultTesting)
        {
            FB.API("me?fields=name", HttpMethod.GET, NameCallBack);
        } else {
            TestUsernameG.SetActive(true);
        }
        
        PhotonNetwork.JoinLobby(TypedLobby.Default);
        Connecting.SetActive(false);
        StartButton.SetActive(true);
        Debug.Log("PUN Basics Tutorial/Launcher: OnConnectedToMaster() was called by PUN");
    }


    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogWarningFormat("PUN Basics Tutorial/Launcher: OnDisconnected() was called by PUN with reason {0}", cause);
    }

    // Callbacks and helpers for Facebook login

    private void InitCallback()
    {
        if (FB.IsInitialized)
        {
            FB.ActivateApp();
            FacebookLogin();
        }
        else
        {
            Debug.Log("Failed to initialize the Facebook SDK");
        }
    }

    private void OnHideUnity (bool isGameShown)
    {
        if (!isGameShown) {
            // Pause the game - we will need to hide
            Time.timeScale = 0;
        } else {
            // Resume the game - we're getting focus again
            Time.timeScale = 1;
        }
    }

    private void FacebookLogin()
    {
        if (FB.IsLoggedIn)
        {
            OnFacebookLoggedIn();
        }
        else
        {
            var perms = new List<string>(){"gaming_profile", "email"};
            FB.LogInWithReadPermissions(perms, AuthCallback);
        }
    }

    private void AuthCallback(ILoginResult result)
    {
        if (FB.IsLoggedIn)
        {
            OnFacebookLoggedIn();
        }
        else
        {
            Debug.LogErrorFormat("Error in Facebook login {0}", result.Error);
        }
    }

    public override void OnCustomAuthenticationFailed(string debugMessage)
    {
        Debug.LogErrorFormat("Error authenticating to Photon using Facebook: {0}", debugMessage);
    }
    private void OnFacebookLoggedIn()
    {
        // AccessToken class will have session details
        string aToken = AccessToken.CurrentAccessToken.TokenString;
        string facebookId = AccessToken.CurrentAccessToken.UserId;
        PhotonNetwork.AuthValues = new AuthenticationValues();
        PhotonNetwork.AuthValues.AuthType = CustomAuthenticationType.FacebookGaming;
        PhotonNetwork.AuthValues.UserId = facebookId; // alternatively set by server
        PhotonNetwork.AuthValues.AddAuthParameter("token", aToken);
        PhotonNetwork.ConnectUsingSettings();
        PhotonNetwork.GameVersion = VersionName;
    }

    private void NameCallBack(IResult result)
    {
       if (result.Error == null)
        {
            string name = "" + result.ResultDictionary["name"];
            PhotonNetwork.NickName = name;
        }
        else
        {
            Debug.Log(result.Error);
        }
    }

    // Callbacks for joining a random room

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("PUN Basics Tutorial/Launcher:OnJoinRandomFailed() was called by PUN. No random room available, so we create one.\nCalling: PhotonNetwork.CreateRoom");
        PhotonNetwork.CreateRoom(null, new RoomOptions() {MaxPlayers = 2}, null);
    }

    public override void OnJoinedRoom()
    {
        PhotonNetwork.LoadLevel("SRLGame");
    }



}

}
