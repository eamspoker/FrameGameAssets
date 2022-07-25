using System;
using System.Collections.Generic;
using UnityEngine;

#region Serializable classes
[Serializable]
public class PlayerInfo
{
    // id, (username, coins)
    public string id;
    public string username;
    public int coins;

}

[Serializable]
public class AnnotationInfo
{
    public string author_id;
    public string fe_id;
    public int startIndex;
    public int length;
    public bool isEditor;
    public string text;
}

[Serializable]
public class FrameList
{
    public FrameInfo[] Frames;
}


[Serializable]
public class FEInfo
{
    public string abbreviation;
    public string description;
    public string[] excludes;
    public string name;
    public string[] requires;
    public string color;

}

[Serializable]
public class Example
{
    public Label[] labels;
    public string text;
}

[Serializable]
public class Label
{
    public string title;
    public int start;
    public int length;
}

[Serializable]
public class FrameInfo
{
    public FEInfo[] FEs;
    public string ID;
    public string[] LUs;
    public string[] allLUs;
    public string def;
    public Example[] examples;
    public string name;
}

[Serializable]
public class TargetCheck
{
    public bool isTarget;
}

[Serializable]
public class DisplayText
{
    public string text;
    public string author;
    public Label[] labels;
}

[Serializable]
public class DisplayTextList
{
    public DisplayText[] sentences;
}
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
    public string[] requires;
    public string[] excludes;

    public FE(string name, string description, string[] requires, string[] excludes)
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
        this.excludes = null;
        this.requires = null;
    }
}


struct Frame 
{
    public string name;
    public string id;
    public string def;
    public AnnotatedSentence[] examples;
    public FE[] FEs;

    public string[] LUs;
        public string[] allLUs;


    public Frame(string name, string id, string def, AnnotatedSentence[] examples, string[] LUs, string[] allLUs, FE[] FEs)
    {
        this.name = name;
        this.id = id;
        this.def = def;
        this.examples = examples;
        this.LUs = LUs;
        this.allLUs = allLUs;
        this.FEs = FEs;
    }
}

struct FGame
{
    public Frame[] frames;

    public bool editorMode;

    public FGame(Frame[] frames)
    {
        this.frames = frames;
        this.editorMode = false;
    }
    public FGame(Frame[] frames, bool editorMode)
    {
        this.frames = frames;
        this.editorMode = editorMode;
    }

}

#endregion