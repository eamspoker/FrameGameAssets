using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Text;
using System.Linq;
using TMPro;
using TMPro.Examples;
using UnityEngine.Networking;
using UnityEngine.UI;

public class HomeScreenController : MonoBehaviour
{
    #region callback definitions
    public delegate void OnReceivedCallback(string json);
    #endregion

    #region helper variables
    private bool hasStarted;
    private bool isFading = false;
    #endregion

    #region items in scene
    public PlayerInfo playerObject;
    public TMP_Text PlayerText;
    public TMP_Text WelcomeText;
    public GameObject Error;
    public TMP_Text ErrorText;

    public GameObject StartingScreen;

    public TMP_InputField infoText;

    

    public GameObject Copyright;

    public GameObject MoreInfo;
    public GameObject Coffee;
    public GameObject Computer;

    public GameObject desc;
    public GameObject instr;
    public GameObject ReturningPlayer;
    public GameObject scrollbar;




    #endregion
    
    #region basic functions

    // Get player information from backend
    void Awake()
    {
        string PID = PlayerPrefs.GetString("player_id");
        StartCoroutine(GetRequest("https://frame-game-backend.herokuapp.com/players/get/"+PID, UpdatePlayerInfo));
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    

    #endregion

    #region callbacks]

    // Parse the user's information, if the user is new, activate walkthrough
    private void UpdatePlayerInfo(string jsonData)
    {
        object resultValue = JsonUtility.FromJson<PlayerInfo>(jsonData);
        playerObject = (PlayerInfo)Convert.ChangeType(resultValue, typeof(PlayerInfo));
        PlayerText.text = playerObject.username + "'s Cafe";
        WelcomeText.text = "Welcome, " + playerObject.username + "!"; 
        if(playerObject.isFirstTime)
        {
          StartingScreen.SetActive(true); 
          infoText.text = "In this game, you can write a story and then earn points by analyzing what you have written.  You can analyze your story by marking up parts of each sentence to show different kinds of events and situations and the roles that characters and objects play in them.  We'll give you a few marked-up sentences to show how it works and get you started. What would you like to do?";

        } else {
            infoText.text = "Instructions:\n- Click on the coffee cup to get started \n- Write your story based on the frames given to you (read the \"Info\" tab for details)\n- Click the \"Finish\" story button to finish the story\n- Press the shift key and hover to select words \n- Lock in frame elements and save your annotated sentences";
            ReturningPlayer.SetActive(true);
            hasStarted = true;
        }

        
    }
    


    #endregion

    #region begin other scenes
    // Loads the game
    public void onEditModeEnable()
    {
        if(hasStarted)
        {
            SceneManager.LoadScene("SRLGame");
        } else {
            ErrorText.text = "Cannot switch modes before agreeing to terms and conditions.";
            if(!isFading)
            {
                StartFadeIn(Error);
                StartTextFadeIn(Error, ErrorText);
                StartFadeOut(Error);
                StartTextFadeOut(Error, ErrorText);
            }
        }
    }

    // Loads the view of other people's sentences
    public void onViewerModeEnable()
    {
        if(hasStarted)
        {
            SceneManager.LoadScene("ViewerMode");
        } else {
            ErrorText.text = "Cannot switch modes before agreeing to terms and conditions.";
            if(!isFading)
            {
                StartFadeIn(Error);
                StartTextFadeIn(Error, ErrorText);
                StartFadeOut(Error);
                StartTextFadeOut(Error, ErrorText);
            }
        }
    }
    #endregion

    #region returning player

    // Show the information about FrameNet
    public void onDescPress()
    {
        infoText.text = "In addition to providing you with an fun way to write stories and think carefully about what you have written, this game is intended to collect data to expand <u><b><link=\"ID\">the FrameNet</link> project</b></u>. \n\nThe <u><b><link=\"ID\">FrameNet</link></b></u> project is building a database of English that is both human- and machine-readable, based on annotating examples of how words are actually used. For students, it can serve as a dictionary, with more than 13,000 word senses.  Most of them have annotated examples that show the meaning and use of the word. For Natural Language Processing developers, the 200,000+ manually annotated sentences  provide a training dataset for semantic role labeling.  FrameNet has been used in applications like information extraction, machine translation, event recognition, and sentiment analysis. \n\nFrameNet is based on the theory that various words can make us think of the same type of situation, which is called a semantic frame. For example, admire, appreciate, disapprove, and scorn are all in the Judgment frame, even though some are positive and some are negative, because all of them involve a person who makes a judgment about someone (or something) else.  FrameNet gives these roles formal names; in the Judgment frame they are called Cognizer (for the person who judges) and Evaluee (for the person or thing that is judged.";
         desc.SetActive(false);
         instr.SetActive(true);
         scrollbar.SetActive(true);
    }

    // Show the gameplay instructions
    public void onInstrPress()
    {
         infoText.text = "Instructions:\n- Click on the coffee cup to get started \n- Write your story based on the frames given to you (read the \"Info\" tab for details)\n- Click the \"Finish\" story button to finish the story\n- Press the shift key and hover to select words \n- Lock in frame elements and save your annotated sentences";
         desc.SetActive(true);
         instr.SetActive(false);
         scrollbar.SetActive(false);
    }
    #endregion

    #region walkthrough tutorial

    // Takes them to the copyright information
    public void onExamplePress()
    {
        MoreInfo.SetActive(false);
        loadCopyright();
    }

    // If they decide to see more information, show them information about framenet
    public void onMoreInfoPress()
    {

        infoText.text = "In addition to providing you with an fun way to write stories and think carefully about what you have written, this game is intended to collect data to expand <u><b><link=\"ID\">the FrameNet</link> project</b></u>. \n\nThe <u><b><link=\"ID\">FrameNet</link></b></u> project is building a database of English that is both human- and machine-readable, based on annotating examples of how words are actually used. For students, it can serve as a dictionary, with more than 13,000 word senses.  Most of them have annotated examples that show the meaning and use of the word. For Natural Language Processing developers, the 200,000+ manually annotated sentences  provide a training dataset for semantic role labeling.  FrameNet has been used in applications like information extraction, machine translation, event recognition, and sentiment analysis. \n\nFrameNet is based on the theory that various words can make us think of the same type of situation, which is called a semantic frame. For example, admire, appreciate, disapprove, and scorn are all in the Judgment frame, even though some are positive and some are negative, because all of them involve a person who makes a judgment about someone (or something) else.  FrameNet gives these roles formal names; in the Judgment frame they are called Cognizer (for the person who judges) and Evaluee (for the person or thing that is judged.";
        MoreInfo.SetActive(true);
        scrollbar.SetActive(true);
        StartingScreen.SetActive(false);
    }

    // Show copyright information
    public void loadCopyright()
    {
        scrollbar.SetActive(false);
        StartingScreen.SetActive(false);
        infoText.text = "Copyright and permissions:";
        Copyright.SetActive(true);
        
    }

    // If they agree to terms and conditions, load returning player mode
    public void onSignCopyright()
    {
        AgreementInfo inf = new AgreementInfo();
        inf.id = playerObject.id;
        inf.time = DateTime.Now.ToString();
        string json = JsonUtility.ToJson(inf);
        StartCoroutine(PostRequest("https://frame-game-backend.herokuapp.com//players/agreement", json));
        playerObject.isFirstTime = false;
        string player_json = JsonUtility.ToJson(playerObject);
        StartCoroutine(PostRequest("https://frame-game-backend.herokuapp.com/players/update", player_json));

        Copyright.SetActive(false);
        infoText.text = "Instructions:\n- Click on the coffee cup to get started \n- Write your story based on the frames given to you (read the \"Info\" tab for details)\n- Click the \"Finish\" story button to finish the story\n- Press the shift key and hover to select words \n- Lock in frame elements and save your annotated sentences";
        ReturningPlayer.SetActive(true);
        hasStarted = true;
    }

    
    #endregion

    // See MenuScript.cs for further description on these generic helpers

    #region get and post requests
    IEnumerator GetRequest(string uri, OnReceivedCallback callback)
        {
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
                    // Send back
                    webRequest.downloadHandler.Dispose();
                    callback(jsonData);
                }
            }
            
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
                }
            }
            }

    #endregion

    #region text fade in/out
    private void StartTextFadeOut(GameObject g_obj, TMP_Text obj)
    {
        IEnumerator coroutine = TextFadeOut(g_obj, obj);
        StartCoroutine(coroutine);
    }

    IEnumerator TextFadeOut(GameObject g_obj, TMP_Text to_fade)
    {
        for (float f = 1; f >= 0.0; f-=0.02f)
        {
            Color c = to_fade.color;
            c.a = f;
            to_fade.color = c;
            yield return new WaitForSeconds(0.05f);

        }
        g_obj.SetActive(false);
    }

    private void StartTextFadeIn(GameObject g_obj, TMP_Text obj)
    {
        Color c = obj.color;
        c.a = 0;
        obj.color = c;
        g_obj.SetActive(true);
        IEnumerator coroutine = TextFadeIn(g_obj, obj);
        StartCoroutine(coroutine);
    }

    IEnumerator TextFadeIn(GameObject g_obj, TMP_Text to_fade)
    {
        
        for (float f = 0.00f; f <= 1.0f; f+=0.05f)
        {
            Color c = to_fade.color;
            c.a = f;
            to_fade.color = c;
            yield return new WaitForSeconds(0.05f);

        }
    }

    #endregion

    #region image fade in/out
    private void StartFadeOut(GameObject obj)
    {
        IEnumerator coroutine = FadeOut(obj);
        StartCoroutine(coroutine);
    }

    IEnumerator FadeOut(GameObject obj)
    {
        for (float f = 1; f >= 0.0; f-=0.02f)
        {
            Color c = obj.GetComponent<Image> ().color;
            c.a = f;
            obj.GetComponent<Image> ().color = c;
            yield return new WaitForSeconds(0.05f);
        }
        isFading = false;
        obj.SetActive(false);

    }

    private void StartFadeIn(GameObject obj)
    {
        Color c = obj.GetComponent<Image> ().color;
        c.a = 0.0f;
        obj.GetComponent<Image> ().color = c;

        obj.SetActive(true);
        IEnumerator coroutine = FadeIn(obj);
        StartCoroutine(coroutine);
    }

    IEnumerator FadeIn(GameObject obj)
    {
        isFading = true;
        for (float f = 0.00f; f <= 1.0f; f+=0.05f)
        {
            Color c = obj.GetComponent<Image> ().color;
            c.a = f;
            obj.GetComponent<Image> ().color = c;
            yield return new WaitForSeconds(0.05f);

        }
    }

    #endregion
    
}
