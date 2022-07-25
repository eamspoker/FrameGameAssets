using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ICSI.FrameNet.FrameGame {
    public class ViewOthersScript : MonoBehaviour
    {
        #region display sentences
        public void displaySentences(string jsonData)
        {
            // object resultValue = JsonUtility.FromJson<DisplayTextList>(jsonData);
            // DisplayTextList dlist = (DisplayTextList)Convert.ChangeType(resultValue, typeof(DisplayTextList));
            // string desc = "";
            // foreach(DisplayText a in dlist.sentences)
            // {
            //     desc += "Author: " + a.author;
            //     List<(int, int)> shifts = new List<(int, int)>();
            //     string sent = a.text;
            //     string original = a.text;
            //     int totalShift = 0;
            //     // foreach(KeyValuePair<string, (int,int)> entry in a.labels)
            //     // {
            //     //     bool isCore = false;
            //     //     foreach(FE fe in game.frames[infoFrame].FEs)
            //     //     {
            //     //         if(fe.name == entry.Key) isCore = true;
            //     //     }

            //     //     if(isCore && entry.Key != "Target" && entry.Key != "Support")
            //     //     {
            //     //         int start = entry.Value.Item1+totalShift;
            //     //         int len = entry.Value.Item2;
            //     //         string color = ColorUtility.ToHtmlStringRGBA(colors[entry.Key]);
            //     //         Debug.Log(sent + " Label: " + entry.Key + " Start: " + start);
            //     //         sent = sent.Insert(start, "<mark=#" + color + ">");
            //     //         sent = sent.Insert(start+len+("<mark=#" + color + ">").Length, "</mark>");
            //     //         totalShift += ("<mark=#" + color + ">").Length + "</mark>".Length;
            //     //     }  
            //     // }

            //     // if(a.labels.Keys.Contains("Target"))
            //     // {
            //     //     string targetText = original.Substring(a.labels["Target"].Item1, a.labels["Target"].Item2);
            //     //     sent = sent.Insert(sent.IndexOf(targetText), "<b>");
            //     //     sent = sent.Insert(sent.IndexOf(targetText)+targetText.Length, "</b>");
            //     // }

            //     // if(a.labels.Keys.Contains("Support"))
            //     // {
            //     //     string targetText = original.Substring(a.labels["Support"].Item1, a.labels["Support"].Item2);
            //     //     sent = sent.Insert(sent.IndexOf(targetText), "<i>");
            //     //     sent = sent.Insert(sent.IndexOf(targetText)+targetText.Length, "</i>");
            //     // }
            }
        }
    }
#endregion