using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Realtime;
using Photon.Pun;
using TMPro;
using UnityEngine.Events;
using UnityEngine.EventSystems;
public class GameScript : MonoBehaviour
{
    #region items in scene
   
    [SerializeField] private PhotonView photonView;
    [SerializeField] private TMP_Text PlayerText;

    [SerializeField] private Camera mainCamera;

    public GameObject FrameDesc;

    // Frame Description
    private TMP_Text frameName;
    private TMP_Text frameCount;
    private TMP_Text frameSaved;

    // FE Description

    private TMP_Text feName;
    private TMP_Text feCount;
    private TMP_Text feSaved;


    #endregion

    #region helper variables
    private bool isTesting = true;
    private int currentFrame = 0;
    private int currentFE = 0;
    private int completeFrame = 0;
    private int completeFE = 0;
    private FGame game;
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
        public AnnotatedSentence[] examples;
        public FE[] FEs;

        public Dictionary<AnnotatedSentence, FramePlayer[]> sentences;

        public Frame(string name, AnnotatedSentence[] examples, FE[] FEs, Dictionary<AnnotatedSentence, FramePlayer[]> sentences)
        {
            this.name = name;
            this.examples = examples;
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
        FE co_part = new FE("Co-participant", "Co-participant is the accompanying entity (person or object).", "", "Participants");
        FE part = new FE("Participant", "Participant is the accompanied entity (person or object).", "", "Participants");
        FE parts = new FE("Participants", "Two or more entities construed as symmetrically and usually equally participating in an event or relation.");

        AnnotatedSentence sent1 = new AnnotatedSentence("The mayor was killed ALONG WITH three bodyguards and his driver.", new Dictionary<string, (int, int)>());
        sent1.labels["Co-participant"] = (32, 31);
        sent1.labels["Participant"] = (0,9);

        // Debug.Log(sent1.text.Substring(sent1.labels["Participant"].Item1, sent1.labels["Participant"].Item2));
        // Debug.Log(sent1.text.Substring(sent1.labels["Co-participant"].Item1, sent1.labels["Co-participant"].Item2));

        AnnotatedSentence sent2 = new AnnotatedSentence("The doctor told me to take my regular pill IN COMBINATION with the new drug and I will be cured of my symptoms.");
        sent2.labels["Co-participant"] = (27, 16);
        sent2.labels["Participant"] = (58,18);
        // Debug.Log(sent2.text.Substring(sent2.labels["Participant"].Item1, sent2.labels["Participant"].Item2));
        // Debug.Log(sent2.text.Substring(sent2.labels["Co-participant"].Item1,sent2.labels["Co-participant"].Item2));

        AnnotatedSentence sent3 = new AnnotatedSentence("Lao Tzu and Confucious built the house TOGETHER.");
        sent3.labels["Participants"] = (0,22);
        // Debug.Log(sent3.text.Substring(sent3.labels["Participants"].Item1, sent3.labels["Participants"].Item2));

        AnnotatedSentence[] examples = new AnnotatedSentence[3]{sent1, sent2, sent3};

        FE[] fes = new FE[3]{co_part, part, parts};

        Frame Accompaniment = new Frame("Accompaniment", examples, fes, new Dictionary<AnnotatedSentence, FramePlayer[]>());

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
         // Frame Description
        frameName = (GameObject.Find("FrameName")).GetComponent<TMP_Text>();
        frameCount = (GameObject.Find("FrameCount")).GetComponent<TMP_Text>();
        frameSaved = (GameObject.Find("FrameSaved")).GetComponent<TMP_Text>();

        // FE Description

        feName = (GameObject.Find("FEName")).GetComponent<TMP_Text>();
        feCount = (GameObject.Find("FECount")).GetComponent<TMP_Text>();
        feSaved = (GameObject.Find("FESaved")).GetComponent<TMP_Text>();
     }

    // Start is called before the first frame update
    void Start()
    {
        PlayerText.text = PhotonNetwork.NickName +"'s Cafe";
        PlayGame();
    }

    // Update is called once per frame
    void Update()
    {
           
    }

    void UpdateText()
    {
        frameName.text = (game.frames[currentFrame]).name;
        frameCount.text = "" + (currentFrame+1) +"/"+ game.frames.Length;
        frameSaved.text = "" + completeFrame + " done";

        feName.text = game.frames[currentFrame].FEs[currentFE].name;
        feCount.text = "" + (currentFE+1) +"/"+ game.frames[currentFrame].FEs.Length;
        feSaved.text = "" + completeFE + " done";
    }

    #endregion


    #region general gameplay


    public void PlayGame()
    {
        
        // Generate game
        if(!isTesting)
        {
            // reading in stuff here
            game = createTestGame();
        } else {
            game = createTestGame();
            UpdateText();
        }
    }

    #endregion


    #region increment/decrement frames/FEs
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

        UpdateText();
    }
    public void onRightFEClick()
    {
        Debug.Log("Clicked!");
        if(currentFE== game.frames[currentFrame].FEs.Length-1) currentFE = 0;
        else currentFE++;

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

    #region regular fade in/out
    private void StartFadeOut(GameObject obj)
    {
        IEnumerator coroutine = FadeOut(obj);
        StartCoroutine(coroutine);
    }

    IEnumerator FadeOut(GameObject obj)
    {
        for (float f = 1; f > 0.0; f-=0.02f)
        {
            Color c = obj.GetComponent<Renderer> ().material.color;
            c.a = f;
            obj.GetComponent<Renderer> ().material.color = c;
            yield return new WaitForSeconds(0.05f);

        }
        obj.SetActive(false);
    }

    private void StartFadeIn(GameObject obj)
    {
        Color c = obj.GetComponent<Renderer> ().material.color;
        c.a = 0.0f;
        obj.GetComponent<Renderer> ().material.color = c;

        obj.SetActive(true);
        IEnumerator coroutine = FadeIn(obj);
        StartCoroutine(coroutine);
    }

    IEnumerator FadeIn(GameObject obj)
    {
        
        for (float f = 0.00f; f <= 1.0f; f+=0.05f)
        {
            Color c = obj.GetComponent<Renderer> ().material.color;
            c.a = f;
            obj.GetComponent<Renderer> ().material.color = c;
            yield return new WaitForSeconds(0.05f);

        }
    }

    #endregion
    


    
}
