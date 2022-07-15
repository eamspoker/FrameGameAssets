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
public class GameScript : MonoBehaviour
{
    #region items in scene
   
    [SerializeField] private PhotonView photonView;
    [SerializeField] private TMP_Text PlayerText;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private TMP_Text Result;
    [SerializeField] private GameObject Field;
    [SerializeField] private TMP_InputField WritingBox;
    private CustomTextSelector customText;


    public GameObject FrameDesc;
    public GameObject FrameInfo;
    public GameObject StoryBG;
    public GameObject InfoBG;

    public TextMeshProUGUI Story;
    public TextMeshProUGUI Info;

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

    private List<Dictionary<string, Color>> FEColors;

    private Dictionary<FE, bool> isComplete = new Dictionary<FE, bool>();

    private AnnotatedSentence currentSentence;


    #endregion

    #region structures
    struct FramePlayer
    {
        public string ID;
        public int points;
        public int approvals;
        public FramePlayer(string ID)
        {
            this.ID = ID;
            this.points = 0;
            this.approvals = 0;
        }

    }

    struct AnnotatedSentence
    {
        public string text;

        // Name of FE, (start index, length of substring)
        public Dictionary<string, (int, int)> labels;

        public AnnotatedSentence(string text)
        {
            this.text = text;
            this.labels = new Dictionary<string, (int, int)>();
        }

        public AnnotatedSentence(string text, Dictionary<string, (int, int)> labels)
        {
            this.text = text;
            this.labels = labels;
        }
    }

    struct FE
    {
        public string name;
        public string description;
        public string requires;
        public string excludes;

        public FE(string name, string description, string requires, string excludes)
        {
            this.name = name;
            this.description = description;
            this.excludes = excludes;
            this.requires = requires;
        }

        public FE(string name, string description)
        {
            this.name = name;
            this.description = description;
            this.excludes = "";
            this.requires = "";
        }
    }


    struct Frame 
    {
        public string name;
        public string def;
        public AnnotatedSentence[] examples;
        public FE[] FEs;

        public string[] LUs;

        public Dictionary<AnnotatedSentence, FramePlayer[]> sentences;

        public Frame(string name, string def, AnnotatedSentence[] examples, string[] LUs, FE[] FEs, Dictionary<AnnotatedSentence, FramePlayer[]> sentences)
        {
            this.name = name;
            this.def = def;
            this.examples = examples;
            this.LUs = LUs;
            this.FEs = FEs;
            this.sentences = sentences;
        }
    }

    struct FGame
    {
        public Frame[] frames;
        public FramePlayer fplayer;

        public bool editorMode;

        public FGame(Frame[] frames, FramePlayer fplayer)
        {
            this.frames = frames;
            this.fplayer = fplayer;
            this.editorMode = false;
        }
        public FGame(Frame[] frames, FramePlayer fplayer, bool editorMode)
        {
            this.frames = frames;
            this.fplayer = fplayer;
            this.editorMode = editorMode;
        }

    }

    #endregion

    #region example initializations
    private FGame createTestGame()
    {

        //FEs
        FE target = new FE("Target", "");
        FE co_part = new FE("Co-participant", "The Co-participant is the accompanying entity (person or object).", "", "Participants");
        FE part = new FE("Participant", "The Participant is the accompanied entity (person or object).", "", "Participants");
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
        Frame Accompaniment = new Frame("Accompaniment", "A Co-participant fills the same role as the Participant in an event or relation.", examples, LUs, fes, new Dictionary<AnnotatedSentence, FramePlayer[]>());

        // Player
        FramePlayer fplayer = new FramePlayer("Person");

        // Game
        FGame game = new FGame(new Frame[1]{Accompaniment}, fplayer);

        return game;
    }
   
    #endregion


     #region basic functions

     // Initialize components

     void Awake()
     {
         if(!isInfo)
         {
         // Frame Description
        frameName = (GameObject.Find("FrameName")).GetComponent<TMP_Text>();
        frameCount = (GameObject.Find("FrameCount")).GetComponent<TMP_Text>();
        frameSaved = (GameObject.Find("FrameSaved")).GetComponent<TMP_Text>();

        // FE Description

        feName = (GameObject.Find("FEName")).GetComponent<TMP_Text>();
        feCount = (GameObject.Find("FECount")).GetComponent<TMP_Text>();
        feSaved = (GameObject.Find("FESaved")).GetComponent<TMP_Text>();
         }
     }

    // Start is called before the first frame update
    void Start()
    {
        // PlayerText.text = username + "'s Cafe";
        customText = Field.GetComponent<CustomTextSelector>();

        PlayGame();
    }

    // Update is called once per frame
    void Update()
    {
        if(!isComplete[game.frames[currentFrame].FEs[currentFE]])
        {
            customText.CheckAndSelect();
            WritingBox.ActivateInputField();
        }  
    }

    #endregion

  #region change display text
    void UpdateText()
    {
        frameName.text = (game.frames[currentFrame]).name;
        frameCount.text = "" + (currentFrame+1) +"/"+ game.frames.Length;
        frameSaved.text = "" + completeFrame + " done";

        FE fe = game.frames[currentFrame].FEs[currentFE];
        Dictionary<string, Color> colors = FEColors[currentFrame];
        string nameFE = fe.name;
        string color = ColorUtility.ToHtmlStringRGBA(colors[nameFE]);
        if(nameFE != "Target")
        {
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
            Result.text = (currentSentence.text).Substring(indices.Item1, indices.Item2);

            if(fe.name != "Target")
            {
                Result.color = FEColors[currentFrame][fe.name];
                Color c = Result.color;
                c.a = 1.0f;
                c.r += 0.3f;
                c.g += 0.3f;
                c.b += 0.3f;
                Result.color = c;
            } else {
                Result.text = "<b>" + Result.text + "</b>";
            }
        } else {
            Result.color = Color.white;
        }
    }

    void UpdateInfo()
    {
        frameNameI.text = (game.frames[infoFrame]).name;
        frameCountI.text = "" + (infoFrame+1) +"/"+ game.frames.Length;
        string desc = "<u><b>Examples</b></u>\n";
        Dictionary<string, Color> colors = FEColors[infoFrame];

        foreach(AnnotatedSentence a in game.frames[infoFrame].examples)
        {
            List<(int, int)> shifts = new List<(int, int)>();
            string sent = a.text;
            foreach(KeyValuePair<string, (int, int)> entry in a.labels)
            {
                if(entry.Key != "Target")
                {
                    int start = entry.Value.Item1;
                    int end = entry.Value.Item1 + entry.Value.Item2;

                    foreach((int, int) shift in shifts)
                    {
                        if(start >= shift.Item1 + shift.Item2)
                        {
                            start += shift.Item2;
                        }

                        if(end >= shift.Item1 + shift.Item2)
                        {
                            end += shift.Item2;
                        }
                    }

                    string color = ColorUtility.ToHtmlStringRGBA(colors[entry.Key]);
                    sent = sent.Insert(start, "<mark=#" + color + ">");
                    shifts.Add((start, 16));

                    if(end >= start)
                    {
                        end += 16;
                    }

                    sent = sent.Insert(end, "</mark>");
                    shifts.Add((end, 7));
                }     
            }

            desc += sent + "\n";

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
            // Generate game
            if(!isTesting)
            {
                // reading in stuff here
                game = createTestGame();
            } else {
                game = createTestGame();
                FEColors = new List<Dictionary<string, Color>>();
                foreach(Frame frame in game.frames)
                {
                    Dictionary<string, Color> dict = new Dictionary<string, Color>();
                    foreach(FE fe in frame.FEs)
                    {
                        Color c = Random.ColorHSV(0f, 1.0f, 1.0f, 1.0f, 0.5f, 1.0f, 0.25f, 0.25f);
                        dict[fe.name] = c;
                        isComplete[fe] = false;
                    }
                    FEColors.Add(dict);
                }
                UpdateText();
            }
        } else {
            return;
        }
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
                foreach(string lu in game.frames[currentFrame].LUs)
                {
                    if(lu == noPunct)
                    {
                        AlertText.text = "Saved " + game.frames[currentFrame].FEs[currentFE].name;
                        StartFadeIn(Alert);
                        StartTextFadeIn(Alert, AlertText);
                        StartFadeOut(Alert);
                        StartTextFadeOut(Alert, AlertText);
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
                    StartFadeIn(Error);
                    StartTextFadeIn(Error, ErrorText);
                    StartFadeOut(Error);
                    StartTextFadeOut(Error, ErrorText);
                    customText.ClearText();
                }

            } else {
                createLabel(newLabel);
                Result.text = annotation;
            }
        } else {
            isComplete[game.frames[currentFrame].FEs[currentFE]] = false;
            if(currentSentence.labels.Count <= 1)
            {
                currentSentence = default(AnnotatedSentence);
            }
            customText.ClearText();
            completeFE--;
            UnlockedFE.SetActive(true);
            LockedFE.SetActive(false);
        }

        UpdateText();
    }
    public void onFrameSave()
    {
        UnlockedF.SetActive(false);
        LockedF.SetActive(true);
        completeFrame++;
    }

    private void createLabel((int, int) label)
    {

        // Find the start and end of the sentence
        string story = Field.GetComponent<TMP_Text>().text;
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
        string name = game.frames[currentFrame].FEs[currentFE].name;
        if(currentSentence.Equals(default(AnnotatedSentence)))
        {
            currentSentence = new AnnotatedSentence(sent);
            currentSentence.labels[name] = (label.Item1, label.Item2-label.Item1);
            UnlockedFE.SetActive(false);
            LockedFE.SetActive(true);
            isComplete[game.frames[currentFrame].FEs[currentFE]] = true;
            completeFE++;
            
        } else
        {
            if(currentSentence.text == sent)
            {
                currentSentence.labels[name] = (label.Item1, label.Item2-label.Item1);
                UnlockedFE.SetActive(false);
                LockedFE.SetActive(true);
                isComplete[game.frames[currentFrame].FEs[currentFE]] = true;
                customText.ClearText();
                completeFE++;
            } else {
                ErrorText.text = "Frame elements must be in the same sentence. Click info tab for more.";
                StartFadeIn(Error);
                StartTextFadeIn(Error, ErrorText);
                StartFadeOut(Error);
                StartTextFadeOut(Error, ErrorText);
                customText.ClearText();
                
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


    #region increment/decrement frames/FEs

    public void onLeftFIClick()
    {
        if(infoFrame == 0) infoFrame = game.frames.Length-1;
        else infoFrame--;

        UpdateText();
    }
    public void onRightFIClick()
    {
        if(infoFrame == game.frames.Length-1) infoFrame = 0;
        else infoFrame++;

        UpdateText();
    }
    public void onLeftFClick()
    {
        if(currentFrame == 0) currentFrame = game.frames.Length-1;
        else currentFrame--;

        UpdateText();
    }
    public void onRightFClick()
    {
        if(currentFrame == game.frames.Length-1) currentFrame = 0;
        else currentFrame++;

        UpdateText();
    }

    public void onLeftFEClick()
    {
        if(currentFE == 0) currentFE = game.frames[currentFrame].FEs.Length-1;
        else currentFE--;
        customText.ClearText();
        UpdateText();
    }
    public void onRightFEClick()
    {
        if(currentFE== game.frames[currentFrame].FEs.Length-1) currentFE = 0;
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
        for (float f = 1; f > 0.0; f-=0.02f)
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
        for (float f = 1; f > 0.0; f-=0.02f)
        {
            Color c = obj.GetComponent<Image> ().color;
            c.a = f;
            obj.GetComponent<Image> ().color = c;
            yield return new WaitForSeconds(0.05f);

        }
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
