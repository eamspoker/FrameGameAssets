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

namespace ICSI.FrameNet.FrameGame {
public class ExampleController : MonoBehaviour
{
    #region callback definitions
    public delegate void OnReceivedCallback(string json);
    #endregion

    #region helper variables
    private int currIndex = 0;
    private SentenceList sentenceList;

    public TMP_InputField displayExamples;
    public TMP_Text displayTextBox;

    public TMP_Text authorName;
    public TMP_Text FrameName;
    public GameObject leftArrow;
    public GameObject rightArrow;

    private Dictionary<int,Dictionary<string, Color>> colors = new Dictionary<int, Dictionary<string, Color>>();

    public PlayerInfo playerObject;
    public TMP_Text PlayerText;
    private CustomTextSelector customText;
    private bool hasStarted;

    public GameObject Error;
    public TMP_Text ErrorText;

    private bool isFading = false;

    #endregion
    
    #region basic functions
    void Awake()
    {
        customText = displayTextBox.GetComponent<CustomTextSelector>();
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

    // Update is called once per frame
    public void onEditModeEnable()
    {
        if(hasStarted)
        {
            SceneManager.LoadScene("SRLGame");
        } else {
            ErrorText.text = "Cannot switch modes while sentences are still loading.";
            if(!isFading)
            {
                StartFadeIn(Error);
                StartTextFadeIn(Error, ErrorText);
                StartFadeOut(Error);
                StartTextFadeOut(Error, ErrorText);
            }
        }
    }

    public void onStartEnable()
    {
        if(hasStarted)
        {
            SceneManager.LoadScene("StartScreen");
        } else {
            ErrorText.text = "Cannot switch modes while frames are still loading.";
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

    #region callbacks

    private void UpdatePlayerInfo(string jsonData)
    {
        object resultValue = JsonUtility.FromJson<PlayerInfo>(jsonData);
        playerObject = (PlayerInfo)Convert.ChangeType(resultValue, typeof(PlayerInfo));
        PlayerText.text = playerObject.username + "'s Cafe";
        StartCoroutine(GetRequest("https://frame-game-backend.herokuapp.com/getSentences", RetrieveSentences));
    }

    private void RetrieveSentences(string jsonData)
    {
        object resultValue = JsonUtility.FromJson<SentenceList>(jsonData);
        sentenceList = (SentenceList)Convert.ChangeType(resultValue, typeof(SentenceList));
        if(sentenceList.list.Length > 0)
        {
            UpdateExamples();
        } else {
            displayExamples.text = "No sentences found. Switch to story creation mode to make some!";
            hasStarted = true;
        }
    }


    #endregion

    #region increment/decrement examples

    void UpdateExamples()
    {
        SentenceInfo sentence = sentenceList.list[currIndex];
        FrameName.text = "<b><u>Frame: " + sentenceList.list[currIndex].frame_name + "</u></b>\n";
        string sent = sentence.text;
        displayExamples.text = sent;
        (int, int) targetIndices = (-1,-1);
        
        if(!colors.ContainsKey(currIndex))
        {
            colors[currIndex] = new Dictionary<string, Color>();
            foreach(LabelInfo label in sentence.labels)
            {
                colors[currIndex][label.fe_id] = UnityEngine.Random.ColorHSV(0f, 1.0f, 1.0f, 1.0f, 0.75f, 1.0f, 0.25f, 0.25f);

            }
        }
            foreach(LabelInfo label in sentence.labels)
            { 
                if(label.startIndex >= 0 && label.fe_id != "Target" && label.fe_id != "Support")
                {
                    customText.addHighlight(sentenceList.list[currIndex].frame_name, label.fe_id, colors[currIndex][label.fe_id], label.startIndex, label.startIndex + label.length -1);

                } else if (label.fe_id == "Target")
                {
                    targetIndices = (label.startIndex, label.length);
                }
                authorName.text = "<b><u>Author: " + label.author_id + "</u></b>\n";

            }
        string original = sentence.text;
        

        if(targetIndices.Item1 >= 0)
        {
            string targetText = original.Substring(targetIndices.Item1, targetIndices.Item2);
            sent = sent.Insert(sent.IndexOf(targetText), "<u>");
            sent = sent.Insert(sent.IndexOf(targetText)+targetText.Length, "</u>");
        }

        sent += "\n<u>Target</u>";

        foreach(LabelInfo label in sentence.labels)
        {
            if(label.fe_id != "Target") sent += "\n<mark=#" + ColorUtility.ToHtmlStringRGBA(colors[currIndex][label.fe_id])+">" + label.fe_id + "</mark>" ;
        }
        displayExamples.text = sent;
        hasStarted = true;
        leftArrow.SetActive(true);
        rightArrow.SetActive(true);

    }

    public void onLeftPress()
    {
        if(currIndex == 0)  currIndex = sentenceList.list.Length-1;
        else currIndex--;
        customText.destroyAllHighlights();
        UpdateExamples();
    }

    public void onRightPress()
    {
        if(currIndex == sentenceList.list.Length-1)  currIndex = 0;
        else currIndex++;
        customText.destroyAllHighlights();
        UpdateExamples();
    }
    #endregion

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
}