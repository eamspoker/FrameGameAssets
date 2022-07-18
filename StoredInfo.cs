using System;
using System.Collections.Generic;
using UnityEngine;

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

