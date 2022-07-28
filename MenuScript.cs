using System;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
// using Photon.Realtime;
// using Photon.Pun;
using Facebook.Unity;
using TMPro;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

namespace ICSI.FrameNet.FrameGame {
public class MenuScript : MonoBehaviour // PunCallbacks
{

    [SerializeField] private GameObject StartButton;
    private string username;
    private string id;

    // For testing without Facebook authentication
    private bool IsMultTesting = false;
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
        }
        // } else {
        //     PhotonNetwork.ConnectUsingSettings();
        //     PhotonNetwork.GameVersion = VersionName;
        // }

    }

    void Start()
    {
    }

    IEnumerator GetRequest(string uri)
    {
        string json = "";
        using (UnityWebRequest webRequest = UnityWebRequest.Get(uri))
        {
            yield return webRequest.SendWebRequest();
            if(webRequest.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log("Network Error: " + webRequest.error);
            } else {
                //Convert to json string
                 string jsonData = Encoding.ASCII.GetString(webRequest.downloadHandler.data);
                // Debug.Log(jsonData);
                    //Convert to Object
                    object resultValue = JsonUtility.FromJson<PlayerInfo>(jsonData);
                    PlayerInfo inf = (PlayerInfo)Convert.ChangeType(resultValue, typeof(PlayerInfo));
                    if(inf.id == "")
                    {
                        inf.id = id;
                        inf.username = username;
                        inf.coins = 0;
                    } else {
                        inf.username = username;
                    }

                // save ID for next scene
                PlayerPrefs.SetString("player_id", id);

                // Send back
                json = JsonUtility.ToJson(inf);
                webRequest.downloadHandler.Dispose();
            }
        }

        // Debug.Log(json);
        StartCoroutine(PostRequest("https://frame-game-backend.herokuapp.com/players/update", json));
         
    }

    IEnumerator PostRequest(string url, string json)
    {
        
        using (UnityWebRequest uwr = new UnityWebRequest(url, "POST"))
        {
            byte[] jsonToSend = new System.Text.UTF8Encoding().GetBytes(json);
            uwr.uploadHandler = (UploadHandler)new UploadHandlerRaw(jsonToSend);
            uwr.downloadHandler = (DownloadHandler)new DownloadHandlerBuffer();
            uwr.SetRequestHeader("Content-Type", "application/json");

            //Send the request then wait here until it returns
            yield return uwr.SendWebRequest();

            if (uwr.result == UnityWebRequest.Result.ConnectionError)
            {
                Debug.Log("Error While Sending: " + uwr.error);
            }
            else
            {
                // Debug.Log("Received: " + uwr.downloadHandler.text);
                // Enable start button
                Connecting.SetActive(false);
                StartButton.SetActive(true);
            }
        }
        }

    

    // Update is called once per frame
    void Update()
    {
        
    }
    public void ConnectSRL() 
    {
        
        // if(IsMultTesting)
        // {
        //   PhotonNetwork.NickName = TestUsername.text;  
        // }
        // PhotonNetwork.JoinRandomRoom();

        SceneManager.LoadScene("StartScreen");
    }


    // Callbacks: these are called after a certain event

    // public override void OnConnectedToMaster()
    // {
    //     if(!IsMultTesting)
    //     {
    //         FB.API("me?fields=name", HttpMethod.GET, NameCallBack);
    //     } else {
    //         TestUsernameG.SetActive(true);
    //     }
        
    //     PhotonNetwork.JoinLobby(TypedLobby.Default);
    //     Connecting.SetActive(false);
    //     StartButton.SetActive(true);
    //     Debug.Log("PUN Basics Tutorial/Launcher: OnConnectedToMaster() was called by PUN");
    // }


    // public override void OnDisconnected(DisconnectCause cause)
    // {
    //     Debug.LogWarningFormat("PUN Basics Tutorial/Launcher: OnDisconnected() was called by PUN with reason {0}", cause);
    // }

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

    

    // public override void OnCustomAuthenticationFailed(string debugMessage)
    // {
    //     Debug.LogErrorFormat("Error authenticating to Photon using Facebook: {0}", debugMessage);
    // }
    private void OnFacebookLoggedIn()
    {
        // AccessToken class will have session details
        // string aToken = AccessToken.CurrentAccessToken.TokenString;
        string facebookId = AccessToken.CurrentAccessToken.UserId;
        id = facebookId;
        FB.API("me?fields=name", HttpMethod.GET, NameCallBack);
        // PhotonNetwork.AuthValues = new AuthenticationValues();
        // PhotonNetwork.AuthValues.AuthType = CustomAuthenticationType.FacebookGaming;
        // PhotonNetwork.AuthValues.UserId = facebookId; // alternatively set by server
        // PhotonNetwork.AuthValues.AddAuthParameter("token", aToken);
        // PhotonNetwork.ConnectUsingSettings();
        // PhotonNetwork.GameVersion = VersionName;
    }

    private void NameCallBack(IResult result)
    {
       if (result.Error == null)
        {
            string name = "" + result.ResultDictionary["name"];
            username = name;
        }
        else
        {
            Debug.Log(result.Error);
        }

        StartCoroutine(GetRequest("https://frame-game-backend.herokuapp.com/players/get/"+id));
    }

    // Callbacks for joining a random room

    // public override void OnJoinRandomFailed(short returnCode, string message)
    // {
    //     Debug.Log("PUN Basics Tutorial/Launcher:OnJoinRandomFailed() was called by PUN. No random room available, so we create one.\nCalling: PhotonNetwork.CreateRoom");
    //     PhotonNetwork.CreateRoom(null, new RoomOptions() {MaxPlayers = 2}, null);
    // }

    // public override void OnJoinedRoom()
    // {
    //     PhotonNetwork.LoadLevel("SRLGame");
    // }



}

}
