using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class StoredInfo
{
    // id, (username, coins)
    public Dictionary<string, (string, int)> players;

    // text, frame id, list of: (author id, FE id, (start index, length), isEditor)
    public Dictionary<(string, int), (string, string, (int, int), bool)[]> annotatedSentences;

}
