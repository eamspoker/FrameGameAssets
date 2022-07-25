using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Photon.Realtime;
using Photon.Pun;
using TMPro;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using TMPro.Examples;
using System.Linq;
using System.IO;
using System.Text;
using UnityEngine.Networking;

namespace ICSI.FrameNet.FrameGame {
public class GameScript : MonoBehaviour
{
    #region callback definitions
        public delegate void OnReceivedCallback(string json);
    #endregion
    #region items in scene
   
    [SerializeField] private PhotonView photonView;
    [SerializeField] private TMP_Text PlayerText;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private TMP_Text Result;
    [SerializeField] private GameObject Field;
    [SerializeField] private TMP_InputField WritingBox;
    public TMP_InputField ReadingBox;
    private CustomTextSelector customText;


    public GameObject FrameDesc;
    public GameObject FrameInfo;
    public GameObject StoryBG;
    public GameObject InfoBG;

    public TextMeshProUGUI Story;
    public TextMeshProUGUI Info;

    public GameObject InfoTab;

    // Frame Description
    private TMP_Text frameName;
    private TMP_Text frameCount;
    private TMP_Text frameSaved;

    // Info tab
    public TMP_Text frameNameI;
    public TMP_Text frameCountI;
    public TMP_InputField descI;

    // FE Description

    private TMP_Text feName;
    private TMP_Text feCount;
    private TMP_Text feSaved;

    // Alert boxes
    public GameObject Alert;
    public TMP_Text AlertText;
    public GameObject Error;
    public TMP_Text ErrorText;

    // Save buttons
    public GameObject UnlockedFE;
    public GameObject LockedFE;
    public GameObject UnlockedF;
    public GameObject LockedF;

    // text boxes
    public GameObject WriteBoxObj;
    public GameObject ReadBoxObj;
    public GameObject Annotate;
    public GameObject FrameMenu;

    // Loading
    public GameObject FrameBox;
    public GameObject Loading;

    


    #endregion

    #region helper variables
    private bool isTesting = true;
    private int currentFrame = 0;
    private int infoFrame = 0;
    private int currentFE = 0;
    private int completeFrame = 0;
    private int completeFE = 0;
    private FGame game;

    private bool isInfo = false;

    private Dictionary<string, Dictionary<string, Color>> FEColors;

    private Dictionary<FE, bool> isComplete = new Dictionary<FE, bool>();

    private AnnotatedSentence currentSentence;
    
    private PlayerInfo playerObject;

    private bool isFading = false;

    private bool hasStarted = false;

    private bool isAnnotating = false;
    string story;

    // highlighting
    // Dictionary is organized first by frame name
    // within each entry, there is a dictionary organized by FE name
    // each entry of the sub-dictionary has start/end tag as well as start/end in marked up version
    public Dictionary<string, Dictionary<string, ((int, int), (int, int))>> highlights = new Dictionary<string, Dictionary<string, ((int, int), (int, int))>>();

    #endregion

    #region example initializations
    private FGame createTestGame()
    {

        //FEs
        FE target = new FE("Target", "");
        FE co_part = new FE("Co-participant", "The Co-participant is the accompanying entity (person or object).", null, new string[1]{"Participants"});
        FE part = new FE("Participant", "The Participant is the accompanied entity (person or object).", null, new string[1]{"Participants"});
        FE parts = new FE("Participants", "Two or more entities construed as symmetrically and usually equally participating in an event or relation.");

        AnnotatedSentence sent1 = new AnnotatedSentence("The mayor was killed ALONG WITH three bodyguards and his driver.", new Dictionary<string, (int, int)>());
        sent1.labels["Target"] = (21, 10);
        sent1.labels["Co-participant"] = (32, 31);
        sent1.labels["Participant"] = (0,9);

        // Debug.Log(sent1.text.Substring(sent1.labels["Target"].Item1, sent1.labels["Target"].Item2));
        // Debug.Log(sent1.text.Substring(sent1.labels["Co-participant"].Item1, sent1.labels["Co-participant"].Item2));

        AnnotatedSentence sent2 = new AnnotatedSentence("The doctor told me to take my regular pill IN COMBINATION with the new drug and I will be cured of my symptoms.");
        sent2.labels["Co-participant"] = (27, 16);
        sent2.labels["Participant"] = (58,18);
        sent2.labels["Target"] = (43, 14);
        // Debug.Log(sent2.text.Substring(sent2.labels["Target"].Item1, sent2.labels["Target"].Item2));

        // Debug.Log(sent2.text.Substring(sent2.labels["Participant"].Item1, sent2.labels["Participant"].Item2));
        // Debug.Log(sent2.text.Substring(sent2.labels["Co-participant"].Item1,sent2.labels["Co-participant"].Item2));

        AnnotatedSentence sent3 = new AnnotatedSentence("Lao Tzu and Confucious built the house TOGETHER.");
        sent3.labels["Participants"] = (0,22);
        sent3.labels["Target"] = (39, 8);
        // Debug.Log(sent3.text.Substring(sent3.labels["Target"].Item1, sent3.labels["Target"].Item2));
        // Debug.Log(sent3.text.Substring(sent3.labels["Participants"].Item1, sent3.labels["Participants"].Item2));

        AnnotatedSentence[] examples = new AnnotatedSentence[3]{sent1, sent2, sent3};

        FE[] fes = new FE[4]{target, co_part, part, parts};

        string[] LUs = new string[6]{"alone", "along with", "in combination", "singly", "together", "with"};
        Frame Accompaniment = new Frame("Accompaniment", "258", "A Co-participant fills the same role as the Participant in an event or relation.", examples, LUs, null, fes);


        // Game
        FGame game = new FGame(new Frame[1]{Accompaniment});

        return game;
    }
   
    #endregion

    #region actual initializations
    public void GetFrames()
    {
        StartCoroutine(GetRequest("http://127.0.0.1:5000/lookup/!", CreateFrameArray));
    }
    public void CreateFrameArray(string jsonData)
    {
        FEColors = new Dictionary<string, Dictionary<string, Color>>();
        object resultValue = JsonUtility.FromJson<FrameList>(jsonData);
        FrameList flist = (FrameList)Convert.ChangeType(resultValue, typeof(FrameList));
        Frame[] frames = new Frame[flist.Frames.Length];
        for(int j = 0; j < flist.Frames.Length; j++)
        {
            FrameInfo f = flist.Frames[j];
            AnnotatedSentence[] examples = new AnnotatedSentence[f.examples.Length];
            for(int i = 0; i < f.examples.Length; i++)
            {
                Example ex = f.examples[i];
                AnnotatedSentence annot = new AnnotatedSentence(ex.text);
                annot.labels = new Dictionary<string, (int, int)>();
                for(int k = 0; k < ex.labels.Length; k++)
                {
                    Label entry = ex.labels[k];
                    annot.labels[entry.title] = (entry.start, entry.length);
                }
                examples[i] = annot;
            }

            FE[] FEs = new FE[f.FEs.Length+1];
            FEs[0] = new FE("Target", f.name);
            FEColors[f.name] = new Dictionary<string, Color>();
            for(int i = 1; i < f.FEs.Length+1; i++)
            {
                FEInfo inf = f.FEs[i-1];
                FE fe = new FE(inf.name, inf.description, inf.requires, inf.excludes);
                FEs[i] = fe;
                Color c;
                if (!ColorUtility.TryParseHtmlString("#" + inf.color + "FF", out c))
                {
                    c = UnityEngine.Random.ColorHSV(0f, 1.0f, 1.0f, 1.0f, 0.75f, 1.0f, 0.25f, 0.25f);
                }
                c.a = 0.25f;
                FEColors[f.name][fe.name] = c;
            }

            frames[j] = new Frame(f.name, f.ID, f.def, examples, f.LUs, f.allLUs, FEs);

        }

        FGame fGame = new FGame(frames);

        game = fGame;
        Loading.SetActive(false);
        FrameBox.SetActive(true);
        FrameMenu.SetActive(true);
        frameName = (GameObject.Find("FrameName")).GetComponent<TMP_Text>();
        InfoTab.SetActive(true);
        PlayGame();
    }
    #endregion

    #region basic functions

     // Initialize components
     void Awake()
     {

        
     }

    // Start is called before the first frame update
    void Start()
    {
        GetFrames();
    }

    // Update is called once per frame
    void Update()
    {
        // Debug.Log("currentFrame: " + currentFrame + " currentFE: " + currentFE);
        if(isAnnotating && hasStarted && game.frames.Length > 0 && game.frames[currentFrame].FEs.Length > 0 && !isComplete[game.frames[currentFrame].FEs[currentFE]])
        {
            customText.CheckAndSelect();
            
        }  
    }

    // Helper callbacks for getting and saving
    public void UpdatePlayerInfo(string jsonData)
    {
        object resultValue = JsonUtility.FromJson<PlayerInfo>(jsonData);
        playerObject = (PlayerInfo)Convert.ChangeType(resultValue, typeof(PlayerInfo));
        PlayerText.text = playerObject.username + "'s Cafe";
        hasStarted = true;
    }

    #endregion

    #region change display text
    void UpdateText()
    {
        if(!isAnnotating)
        {
            frameName.text = (game.frames[currentFrame]).name;
        } else if(!isInfo && game.frames.Length > 0)
        {
            (GameObject.Find("FELeft")).SetActive(true);
            (GameObject.Find("FERight")).SetActive(true);
            frameName.text = (game.frames[currentFrame]).name;
            frameCount.text = "" + (currentFrame+1) +"/"+ game.frames.Length;
            frameSaved.text = "" + completeFrame + " done";

            FE fe = game.frames[currentFrame].FEs[currentFE];
            Dictionary<string, Color> colors = FEColors[(game.frames[currentFrame]).name];
            string nameFE = fe.name;
            if(nameFE != "Target")
            {
                string color = ColorUtility.ToHtmlStringRGBA(colors[nameFE]);
                feName.text = "<mark=#" + color + ">" + nameFE + "</mark>";
            } else
            {
                feName.text = "<b>" + nameFE + "</b>";
            }
            feCount.text = "" + (currentFE+1) +"/"+ game.frames[currentFrame].FEs.Length;
            feSaved.text = "" + completeFE + " done";

            if(isComplete[fe])
            {
                (int, int) indices = currentSentence.labels[fe.name];
                if(indices.Item1 >= 0)
                {
                    Result.text = (currentSentence.text).Substring(indices.Item1, indices.Item2);
                } else {
                    Result.text = "Not in sentence";
                }

                if(fe.name != "Target")
                {
                    Result.color = FEColors[(game.frames[currentFrame]).name][fe.name];
                    Color c = Result.color;
                    c.a = 1.0f;
                    c.r += 0.3f;
                    c.g += 0.3f;
                    c.b += 0.3f;
                    Result.color = c;
                } else {
                    Result.text = "<b>" + Result.text + "</b>";
                    Result.color = Color.white;
                }

                UnlockedFE.SetActive(false);
                LockedFE.SetActive(true);
            } else {
                UnlockedFE.SetActive(true);
                LockedFE.SetActive(false);
                Result.color = Color.white;
            }
        } else
        {
            frameName.text = "All Frames Completed";
            frameCount.text = "";
            frameSaved.text = "" + completeFrame + " done";;

            Result.text = "Not in sentence.";
            Result.color = Color.white;
            feCount.text = "";
            feSaved.text = "";
            UnlockedFE.SetActive(false);
            LockedFE.SetActive(false);

            (GameObject.Find("FELeft")).SetActive(false);
            (GameObject.Find("FERight")).SetActive(false);

        }
    }

    void UpdateInfo()
    {
        frameNameI.text = (game.frames[infoFrame]).name;
        frameCountI.text = "" + (infoFrame+1) +"/"+ game.frames.Length;
        string desc = "<u><b>Examples</b></u>\n";
        Dictionary<string, Color> colors = FEColors[(game.frames[infoFrame]).name];

        foreach(AnnotatedSentence a in game.frames[infoFrame].examples)
        {
            List<(int, int)> shifts = new List<(int, int)>();
            string sent = a.text;
            string original = a.text;
            int totalShift = 0;
            foreach(KeyValuePair<string, (int,int)> entry in a.labels)
            {
                bool isCore = false;
                foreach(FE fe in game.frames[infoFrame].FEs)
                {
                    if(fe.name == entry.Key) isCore = true;
                }

                if(isCore && entry.Key != "Target" && entry.Key != "Support")
                {
                    int start = entry.Value.Item1+totalShift;
                    int len = entry.Value.Item2;
                    string color = ColorUtility.ToHtmlStringRGBA(colors[entry.Key]);
                    sent = sent.Insert(start, "<mark=#" + color + ">");
                    sent = sent.Insert(start+len+("<mark=#" + color + ">").Length, "</mark>");
                    totalShift += ("<mark=#" + color + ">").Length + "</mark>".Length;
                }  
            }

            if(a.labels.Keys.Contains("Target"))
            {
                string targetText = original.Substring(a.labels["Target"].Item1, a.labels["Target"].Item2);
                sent = sent.Insert(sent.IndexOf(targetText), "<b>");
                sent = sent.Insert(sent.IndexOf(targetText)+targetText.Length, "</b>");
            }

            if(a.labels.Keys.Contains("Support"))
            {
                string targetText = original.Substring(a.labels["Support"].Item1, a.labels["Support"].Item2);
                sent = sent.Insert(sent.IndexOf(targetText), "<i>");
                sent = sent.Insert(sent.IndexOf(targetText)+targetText.Length, "</i>");
            }

            desc += sent + "\n\n";

        }

        desc += "\n<u><b>Target</b></u>\n";
        desc += "The target is the word or phrase that evokes this frame. Acceptable targets for this frame include:\n\n";
        foreach(string LU in game.frames[infoFrame].LUs)
        {
            desc += LU + "\n";
        }


        desc += "\n<u><b>Frame Elements</b></u>\n";
        foreach(FE fe in game.frames[infoFrame].FEs)
        {
            if(fe.name != "Target")
            {
                string color = ColorUtility.ToHtmlStringRGBA(colors[fe.name]);
                desc += "<mark=#" + color + ">" + fe.name + "</mark>" + ": " +  fe.description +"\n\n";
            }
        }

        desc += "\n<u><b>Description</b></u>\n" + game.frames[infoFrame].def + "\n";

        descI.text = desc;
    }

    #endregion

    #region general gameplay

    public void PlayGame()
    {
        if(!isInfo)
        {
            foreach(Frame frame in game.frames)
            {
                highlights[frame.name] = new Dictionary<string, ((int, int), (int, int))>();
                foreach(FE fe in frame.FEs)
                {
                    isComplete[fe] = false;
                }
            }
            UpdateText();
        } 
        string PID = PlayerPrefs.GetString("player_id");
        StartCoroutine(GetRequest("http://127.0.0.1:5000/players/get/"+PID, UpdatePlayerInfo));
        customText = Field.GetComponent<CustomTextSelector>();

        
    }

    #endregion

    #region create user annotation
    public void onFESave()
    {
        if(!isComplete[game.frames[currentFrame].FEs[currentFE]])
        {
            string FEName = game.frames[currentFrame].FEs[currentFE].name;
            string annotation = Result.text;
            (int, int) newLabel = (customText.startIndex, customText.endIndex);
            if(FEName == "Target")
            {
                string noPunct = "";
                foreach(char c in annotation)
                {
                    if(char.IsLetter(c) || c == ' ')
                    {
                        noPunct += c;
                    }
                }
                noPunct = noPunct.ToLower();

                // Check if the string (punctuation removed) is an acceptable LU
                foreach(string lu in game.frames[currentFrame].allLUs)
                {
                    if(lu == noPunct)
                    {
                        AlertText.text = "Saved " + game.frames[currentFrame].FEs[currentFE].name;
                        if(!isFading)
                        {
                            StartFadeIn(Alert);
                            StartTextFadeIn(Alert, AlertText);
                            StartFadeOut(Alert);
                            StartTextFadeOut(Alert, AlertText);
                        }
                        Alert.SetActive(true);
                        isComplete[game.frames[currentFrame].FEs[currentFE]] = true;
                        createLabel(newLabel);
                        Result.text = annotation;
                    }
                }

                // if not, issue an alert
                if(!isComplete[game.frames[currentFrame].FEs[currentFE]])
                {
                    ErrorText.text = "Not a valid target. Click info tab for more. ";
                    if(!isFading)
                    {
                        StartFadeIn(Error);
                        StartTextFadeIn(Error, ErrorText);
                        StartFadeOut(Error);
                        StartTextFadeOut(Error, ErrorText);
                    }
                    customText.ClearText();
                }

            } else {
                createLabel(newLabel);
                Result.text = annotation;
            }
        } else {
            isComplete[game.frames[currentFrame].FEs[currentFE]] = false;
            if(completeFE <= 1)
            {
                currentSentence = default(AnnotatedSentence);
            }
            customText.ClearText();
            string f_name = game.frames[currentFrame].name;
            string fe_name = game.frames[currentFrame].FEs[currentFE].name;
            customText.destroyHighlights(f_name, fe_name);
            completeFE--;
        }

        UpdateText();
    }
    public void onFrameSave()
    {
        if(game.frames.Length > 0 && completeFE == game.frames[currentFrame].FEs.Length)
            {
                UnlockedF.SetActive(false);
                LockedF.SetActive(false);
                AnnotationInfo[] annotations = new AnnotationInfo[game.frames[currentFrame].FEs.Length];
                foreach(KeyValuePair<string, (int,int)> entry in currentSentence.labels)
                {
                    AnnotationInfo annot = new AnnotationInfo();
                    annot.author_id = playerObject.id;
                    annot.fe_id = entry.Key;
                    annot.startIndex = entry.Value.Item1;
                    annot.length = entry.Value.Item2;
                    annot.isEditor = false;
                    annot.text = currentSentence.text;
                    string json = JsonUtility.ToJson(annot);
                    Debug.Log(json);
                    StartCoroutine(PostRequest("http://127.0.0.1:5000/frames/update/"+game.frames[currentFrame].id, json));
                }
                
                List<Frame> frames = new List<Frame>(game.frames);
                foreach(FE fe in game.frames[currentFrame].FEs)
                {
                    isComplete[fe] = false;
                }
                frames.RemoveAt(currentFrame);
                game.frames = frames.ToArray();
                currentFE = 0;
                completeFrame++;
                completeFE = 0;
                Result.text = "Not in sentence";
                onRightFClick();
                
                UpdateText();
                currentSentence = default(AnnotatedSentence);

                UnlockedF.SetActive(true);
                LockedF.SetActive(true);
            } else {
                ErrorText.text = "All FEs must be completed before annotation is sent off.";
                if(!isFading)
                {
                    StartFadeIn(Error);
                    StartTextFadeIn(Error, ErrorText);
                    StartFadeOut(Error);
                    StartTextFadeOut(Error, ErrorText);
                }
            }
            
    }

    private void createLabel((int, int) label)
    {
        string name = game.frames[currentFrame].FEs[currentFE].name;
        Dictionary<string, Color> colors = FEColors[(game.frames[currentFrame]).name];
        if(label.Item1 < 0)
        {
            if(currentSentence.Equals(default(AnnotatedSentence)))
            {
                currentSentence = new AnnotatedSentence("");
            }

            isComplete[game.frames[currentFrame].FEs[currentFE]] = true;
            currentSentence.labels[name] = (-1, -1);
            completeFE++;
            
        } else {
        // Find the start and end of the sentence
        int[] potentialStart = new int[3]{story.LastIndexOf('.',label.Item1), 
                                story.LastIndexOf('?', label.Item1), 
                                story.LastIndexOf('!', label.Item1)};

        int start;
        if(story.Length > potentialStart.Max()+1 && story[potentialStart.Max()+1] == ' ')
        {
            start = potentialStart.Max()+2;
        } else if(potentialStart.Max() < 0)
        {
            start = 0;
        } else {
            start = potentialStart.Max()+1;
        }


        int[] potentialEnd = new int[3]{story.IndexOf('.',label.Item2), 
                                story.IndexOf('?', label.Item2), 
                                story.IndexOf('!', label.Item2)};

        int[] filtered = potentialEnd.Where(e => e >= 0).ToArray();

        int end;
        
        if(filtered.Length == 0)
        {
            end = story.Length;
        } else {
            end = filtered.Min()+1;
        }

        string sent = story.Substring(start, end-start);
        if(currentSentence.Equals(default(AnnotatedSentence)) || currentSentence.text == "")
        {
            customText.addHighlight(game.frames[currentFrame].name, name, colors[name], customText.startIndex, customText.endIndex);
            if(currentSentence.Equals(default(AnnotatedSentence))) currentSentence = new AnnotatedSentence(sent);
            currentSentence.text = sent;
            currentSentence.labels[name] = (label.Item1-start, label.Item2-label.Item1);
            isComplete[game.frames[currentFrame].FEs[currentFE]] = true;
            customText.ClearText();
            completeFE++;
            
        } else
        {
            if(currentSentence.text == sent)
            {
                customText.addHighlight(game.frames[currentFrame].name, name, colors[name], customText.startIndex, customText.endIndex);
                currentSentence.labels[name] = (label.Item1-start, label.Item2-label.Item1);
                isComplete[game.frames[currentFrame].FEs[currentFE]] = true;
                customText.ClearText();
                completeFE++;
            } else {
                ErrorText.text = "Frame elements must be in the same sentence. Click info tab for more.";
                if(!isFading)
                {
                    StartFadeIn(Error);
                    StartTextFadeIn(Error, ErrorText);
                    StartFadeOut(Error);
                    StartTextFadeOut(Error, ErrorText);
                }
                customText.ClearText();
                
            }
        }
        }

    }
    #endregion

    #region info screen
    public void onInfoClick()
    {
        FrameDesc.SetActive(false);
        FrameInfo.SetActive(true);
        StoryBG.SetActive(false);
        InfoBG.SetActive(true);
        Info.color = Color.black;
        Story.color = Color.white;
        UpdateInfo();
    }

    public void onHomeClick()
    {
        FrameDesc.SetActive(true);
        FrameInfo.SetActive(false);
        StoryBG.SetActive(true);
        InfoBG.SetActive(false);
        Info.color = Color.white;
        Story.color = Color.black;
        UpdateText();
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
                    Debug.Log(jsonData);
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
                    Debug.Log("Received: " + uwr.downloadHandler.text);
                }
            }
            }

    #endregion

    #region writing to annotating
    public void onStoryFinish()
    {
        isAnnotating = true;
        ReadBoxObj.SetActive(true);
        ReadingBox.text = WritingBox.text; 
        WriteBoxObj.SetActive(false);
        Annotate.SetActive(true);
        FrameMenu.SetActive(false);
        frameCount = (GameObject.Find("FrameCount")).GetComponent<TMP_Text>();
        frameSaved = (GameObject.Find("FrameSaved")).GetComponent<TMP_Text>();
        feName = (GameObject.Find("FEName")).GetComponent<TMP_Text>();
        feCount = (GameObject.Find("FECount")).GetComponent<TMP_Text>();
        feSaved = (GameObject.Find("FESaved")).GetComponent<TMP_Text>();
        story = Field.GetComponent<TMP_Text>().text;
        customText.SetString(story);
        UpdateText();
    }
    #endregion

    #region increment/decrement frames/FEs

    public void onLeftFIClick()
    {
        if(infoFrame == 0) infoFrame = game.frames.Length-1;
        else infoFrame--;

        UpdateInfo();
    }
    public void onRightFIClick()
    {
        if(infoFrame == game.frames.Length-1) infoFrame = 0;
        else infoFrame++;

        UpdateInfo();
    }
    public void onLeftFClick()
    {
        if(currentFrame <= 0) currentFrame = game.frames.Length-1;
        else currentFrame--;
        currentFE = 0;
        completeFE = 0;
        foreach(FE fe in game.frames[currentFrame].FEs)
        {
            if(isComplete[fe]) completeFE++;
        }
        UpdateText();
    }
    public void onRightFClick()
    {
        currentFE = 0;
        if(currentFrame >= game.frames.Length-1) currentFrame = 0;
        else currentFrame++;

        completeFE = 0;
        if(game.frames.Length > 0)
        {
            foreach(FE fe in game.frames[currentFrame].FEs)
            {
                if(isComplete[fe]) completeFE++;
            }
        }


        UpdateText();
    }

    public void onLeftFEClick()
    {
        if(currentFE <= 0) currentFE = game.frames[currentFrame].FEs.Length-1;
        else currentFE--;
        customText.ClearText();
        UpdateText();
    }
    public void onRightFEClick()
    {
        if(currentFE >= game.frames[currentFrame].FEs.Length-1) currentFE = 0;
        else currentFE++;

        customText.ClearText();
        UpdateText();
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
