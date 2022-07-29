using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;


namespace TMPro.Examples
{

// This is referenced by the in-game scripts to highlight words and select words
// It is modeled after TMPro's Text Selector B. Please note it must stay in the
// TMPro Example namespace and folder.

public class CustomTextSelector : MonoBehaviour, IPointerExitHandler, IPointerClickHandler
{
    #pragma warning disable 0618 // Disabled warning due to SetVertices being deprecated until new release with SetMesh() is available.

        public RectTransform TextPopup_Prefab_01;

        private RectTransform m_TextPopup_RectTransform;
        private TextMeshProUGUI m_TextPopup_TMPComponent;
        private const string k_LinkText = "You have selected link <#ffff00>";
        private const string k_WordText = "Word Index: <#ffff00>";

        // The indices of the string the user has highlighted (by word, not char)
        private (int, int) selectedIndices = (-1, -1);

        // Contains the amount the text box has scrolled at the last redraw
        private Dictionary<string, Vector2> offsets = new Dictionary<string, Vector2>();

        // Contains the original position of each highlight
        private Dictionary<string, Vector3> origins = new Dictionary<string, Vector3>();

        // Stores the color of each highlight
        private Dictionary<string, Color> colors = new Dictionary<string, Color>();

        // Stores the frame name and frame element name of each highlight
        private Dictionary<string, (string, string)> fnames = new Dictionary<string, (string, string)>();

        // The slot in which the highlighted text is displayed
        public TMP_Text Result;

        // Start and end used for GameScript.cs to access
        public int startIndex = -1;
        public int endIndex = -1;

        // The parent is the canvas, the position is the frame element 
        // i.e. input field that the text exists under
        public GameObject parentGameObject;
        public GameObject positionGameObject;

        // Variables used by original Text Selector B
        private TextMeshProUGUI m_TextMeshPro;
        private Canvas m_Canvas;
        private Camera m_Camera;

    

        private Matrix4x4 m_matrix;

        private TMP_MeshInfo[] m_cachedMeshInfoVertexData;

        // Helper variables used for selecting the text
        private int m_selectedWord = -1;
        private int lastdeSelected = -1;
        private string lastString = "Not in sentence";
        private string story = "";
        private Canvas canvas;
        
        // Indices 
        private Dictionary<string, (int, int)> FrameIndices = new Dictionary<string, (int,int)>();

        // The last scroll value of the text box
        Vector2 last_offset;

        // The top of the text box (to cause the highlights to diappear)
        float top;

        void Awake()
        {
            m_TextMeshPro = gameObject.GetComponent<TextMeshProUGUI>();

            // set scroll offset, top of text box
            last_offset = gameObject.GetComponent<RectTransform>().offsetMin;
            top = (gameObject.GetComponent<RectTransform>().rect.height/2);

            m_Canvas = gameObject.GetComponentInParent<Canvas>();

            // Get a reference to the camera if Canvas Render Mode is not ScreenSpace Overlay.
            if (m_Canvas.renderMode == RenderMode.ScreenSpaceOverlay)
                m_Camera = null;
            else
                m_Camera = m_Canvas.worldCamera;

            // Create pop-up text object which is used to show the link information.
            m_TextPopup_RectTransform = Instantiate(TextPopup_Prefab_01) as RectTransform;
            m_TextPopup_RectTransform.SetParent(m_Canvas.transform, false);
            m_TextPopup_TMPComponent = m_TextPopup_RectTransform.GetComponentInChildren<TextMeshProUGUI>();
            m_TextPopup_RectTransform.gameObject.SetActive(false);
        }


        void OnEnable()
        {
            // Subscribe to event fired when text object has been regenerated.
            TMPro_EventManager.TEXT_CHANGED_EVENT.Add(ON_TEXT_CHANGED);
            canvas = parentGameObject.GetComponent<Canvas>();
        }

        void OnDisable()
        {
            // UnSubscribe to event fired when text object has been regenerated.
            TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(ON_TEXT_CHANGED);
        }




        void ON_TEXT_CHANGED(Object obj)
        {
            if (obj == m_TextMeshPro)
            {
                // Update cached vertex data.
                m_cachedMeshInfoVertexData = m_TextMeshPro.textInfo.CopyMeshInfoVertexData();
            }
        }

        // Adds a new highlight to the dictionaries (supports multi-line comment)
        public void addHighlight(string f_name, string fe_name, Color color, int start_char, int end_char)
        {
            
            int start_line = m_TextMeshPro.textInfo.characterInfo[start_char].lineNumber;
            int end_line = m_TextMeshPro.textInfo.characterInfo[end_char].lineNumber;
            if(start_line == end_line)
            {
                createRectangle(start_char, end_char, f_name + "_"+ fe_name, color);
                fnames[ f_name + "_"+ fe_name] = (f_name, fe_name);

            } else
            {
                for(int i = start_line; i <= end_line; i++)
                {
                    int startOfLineChar = m_TextMeshPro.textInfo.lineInfo[i].firstVisibleCharacterIndex;
                    int endOfLineChar = m_TextMeshPro.textInfo.lineInfo[i].lastVisibleCharacterIndex;
                    if(i == start_line)
                    {
                        createRectangle(start_char, endOfLineChar, f_name + "_"+ fe_name+(i-start_line), color);

                    } else if(i == end_line)
                    {
                        createRectangle(startOfLineChar, end_char, f_name + "_"+ fe_name+(i-start_line), color);
                        
                    } else {
                        createRectangle(startOfLineChar, endOfLineChar, f_name + "_"+ fe_name+(i-start_line), color);

                    }

                    fnames[f_name + "_"+ fe_name+(i-start_line)] = (f_name, fe_name);
                }
            }
        }

        // Helper function that draws individual rectangles
        public void createRectangle(int first, int last, string gname, Color c)
        {
            GameObject g = GameObject.Find(gname);
            if(g != null)
            {
                Destroy(g);
            }
            Vector3 topLeft = m_TextMeshPro.textInfo.characterInfo[first].topLeft;
            Vector3 bottomRight = m_TextMeshPro.textInfo.characterInfo[last].bottomRight;
            float rWidth = bottomRight.x - topLeft.x;
            float rHeight = topLeft.y - bottomRight.y;
            Vector3 origin = Vector3.zero;
            origin.x = topLeft.x + (rWidth/2);
            origin.y = bottomRight.y + (rHeight/2);
            Vector2 offset = gameObject.GetComponent<RectTransform>().offsetMin;
            origin = origin + new Vector3(offset.x, offset.y, 0);
            
            Vector3 newScale = new Vector3(rWidth/100, rHeight/100, 0);
            Canvas canvas = positionGameObject.GetComponent<Canvas>();
            offsets[gname] = gameObject.GetComponent<RectTransform>().offsetMin;

            // create GameObject
            GameObject imageGameObject = new GameObject(gname);
            Image image = imageGameObject.AddComponent<Image>();

            //check if rectangle is within bounds
            colors[gname] = c;
            if(origin.y <= top && origin.y >= (top*-1.0F))
            {
                c.a = 0.25F;
            } else {
                c.a = 0.0F;
            }
            image.color = c;
            RectTransform r = imageGameObject.GetComponent<RectTransform>();
            FrameIndices[gname] = (first, last);

            // resize
            r.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rHeight);
            r.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rWidth);

            // set position and store world position
            imageGameObject.transform.position = origin;
            imageGameObject.transform.SetParent(positionGameObject.transform, false);
            origins[gname] = imageGameObject.transform.position;
        }

        // Redraws a single highlight based on name
        public void updateRectangle(string gname)
        {
            int first = FrameIndices[gname].Item1;
            int last = FrameIndices[gname].Item2;
            GameObject g = GameObject.Find(gname);
            if(g != null)
            {
                Destroy(g);
            }
            // find origin
            Vector3 topLeft = m_TextMeshPro.textInfo.characterInfo[first].topLeft;
            Vector3 bottomRight = m_TextMeshPro.textInfo.characterInfo[last].bottomRight;
            float rWidth = bottomRight.x - topLeft.x;
            float rHeight = topLeft.y - bottomRight.y;
            Vector3 origin = Vector3.zero;
            origin.x = topLeft.x + (rWidth/2);
            origin.y = bottomRight.y + (rHeight/2);

            Vector2 offset = gameObject.GetComponent<RectTransform>().offsetMin;
            origin = origin + new Vector3(offset.x, offset.y, 0);

            // scale
            Vector3 newScale = new Vector3(rWidth/100, rHeight/100, 0);
            Canvas canvas = positionGameObject.GetComponent<Canvas>();
            offsets[gname] = gameObject.GetComponent<RectTransform>().offsetMin;
            // create GameObject
            GameObject imageGameObject = new GameObject(gname);
            Image image = imageGameObject.AddComponent<Image>();
            Color c = colors[gname];
            if(origin.y <= top && origin.y >= (top*-1.0F))
            {
                c.a = 0.25F;
            } else {
                c.a = 0.0F;
            }
            image.color = c;
            RectTransform r = imageGameObject.GetComponent<RectTransform>();

            // resize
            r.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, rHeight);
            r.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, rWidth);

            // set position and store world position
            imageGameObject.transform.position = origin;
            imageGameObject.transform.SetParent(positionGameObject.transform, false);
            origins[gname] = imageGameObject.transform.position;
        }

        // updates all highlights by using updateRectangle
        public void UpdateHighlights()
        {
            Vector2 offset = gameObject.GetComponent<RectTransform>().offsetMin;
            if(offset != last_offset)
            {
                foreach(string gname in FrameIndices.Keys)
                {
                    updateRectangle(gname);
                }
            }

            last_offset = offset;
        }

        // Destroys the highlights
        public void destroyHighlights(string f_name, string fe_name)
        {
            List<string> to_remove = new List<string>();
            foreach(string gname in FrameIndices.Keys)
            {
                if(gname.Contains(f_name+"_"+fe_name))
                {
                    GameObject g = GameObject.Find(gname);
                    if(g != null)
                    {
                        Destroy(g);
                    }
                    offsets.Remove(gname);
                    colors.Remove(gname);
                    to_remove.Add(gname);

                }
            }
            foreach(string remove in to_remove)
            {
                FrameIndices.Remove(remove);
            }
        }

        // Destroys all highlight game objects, resets dictionaries
        public void destroyAllHighlights()
        {
            foreach(string gname in FrameIndices.Keys)
            {
                GameObject g = GameObject.Find(gname);
                if(g != null)
                {
                    Destroy(g);
                }
            }

            fnames = new Dictionary<string, (string, string)>();
            offsets = new Dictionary<string, Vector2>();
            colors = new Dictionary<string, Color>();
            FrameIndices = new Dictionary<string, (int, int)>();
        }
        // Called every frame, updates the highlights
        public void FixedUpdate()
        {
            UpdateHighlights();
        }

        // Called by GameScript.cs, locks the text as it is for annotation
        public void SetString(string s)
        {
            story = s;
        }

        // Called by GameScript.cs in annotation mode, adds words to selected
        // range on hover
        public void CheckAndSelect()
        { 

            // updateRectangle(start_char, end_char, "frame_fe");        
            int wordIndex = TMP_TextUtilities.FindIntersectingWord(m_TextMeshPro, Input.mousePosition, m_Camera);
            int start = selectedIndices.Item1;
            int end = selectedIndices.Item2;

            
            if((lastString != (m_TextMeshPro.text)) || 
                (wordIndex == -1 && Input.GetMouseButtonDown(1)))
            {
                // Deselects everything if the user right clicks outside
                ClearText();
            } else if((Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))) {
                // Search for words to expand range on hover
                WordSelect();
            } else if(wordIndex != -1 && (wordIndex == start || wordIndex == end) && (Input.GetMouseButtonDown(1)))
            {
                // If the user right clicks above a word, deselect just that word
                ClearTextH(wordIndex);
                if(start == end) selectedIndices = (-1, -1);
                else if(wordIndex == start) selectedIndices = (start+1, end);
                else selectedIndices = (start, end-1);
                lastdeSelected = wordIndex;
            }
            // Used to check that the input hasn't changed
            lastString = m_TextMeshPro.text;

            // Updates the selected text in the box as seen by user 
            UpdateResult();
        }

        // Ensures that the user can reselect after deselecting
        public void OnPointerExit(PointerEventData eventData)
        {
            lastdeSelected = -1;
        }


        // open link to FrameNet frame page (link ID should be frame name)
        public void OnPointerClick (PointerEventData eventData) {  
            int linkIndex = TMP_TextUtilities.FindIntersectingLink(m_TextMeshPro, Input.mousePosition, m_Camera);  
            if (linkIndex == -1)    
                return;  
            TMP_LinkInfo linkInfo = m_TextMeshPro.textInfo.linkInfo[linkIndex];
            string selectedLink = linkInfo.GetLinkID();  
            if (selectedLink == "ID") {   
                Application.OpenURL ("https://framenet.icsi.berkeley.edu/");          
            } else {
                Application.OpenURL ("https://framenet.icsi.berkeley.edu"+selectedLink);

            }
        }

        // Used to broaden the range of the selected words on user hover
        void WordSelect()
        {
                #region Word Selection Handling
                //Check if Mouse intersects any words and if so assign a random color to that word.
                int wordIndex = TMP_TextUtilities.FindIntersectingWord(m_TextMeshPro, Input.mousePosition, m_Camera);
                int start = selectedIndices.Item1;
                int end = selectedIndices.Item2;

                if(wordIndex != lastdeSelected) lastdeSelected = -1;
                // Check if it is consecutive
                bool isConsecutive = (start < 0
                    || start == wordIndex+1
                    || end == wordIndex-1);

                bool isInRange = (start <= wordIndex) && (wordIndex <= end);

                // Word Selection Handling
                if (wordIndex != -1
                    && wordIndex != lastdeSelected
                    && isConsecutive
                    && !(isInRange) 
                    && !(Input.GetMouseButtonDown(1)))
                {
                    m_selectedWord = wordIndex;
                    lastdeSelected = -1;

                    // change color of text
                    string[] byWord = (m_TextMeshPro.text).Split(" ");
                    TMP_WordInfo wInfo = m_TextMeshPro.textInfo.wordInfo[wordIndex];
                    for (int j = 0; j < wInfo.characterCount; j++)
                    {
                        int characterIndex = wInfo.firstCharacterIndex + j;
                        // Get the index of the material / sub text object used by this character.
                        int meshIndex = m_TextMeshPro.textInfo.characterInfo[characterIndex].materialReferenceIndex;

                        int vertexIndex = m_TextMeshPro.textInfo.characterInfo[characterIndex].vertexIndex;

                        // Get a reference to the vertex color
                        Color32[] vertexColors = m_TextMeshPro.textInfo.meshInfo[meshIndex].colors32;

                        Color32 c = vertexColors[vertexIndex + 0].Tint(0.25f);

                        vertexColors[vertexIndex + 0] = c;
                        vertexColors[vertexIndex + 1] = c;
                        vertexColors[vertexIndex + 2] = c;
                        vertexColors[vertexIndex + 3] = c;
                    }

                    // Update Geometry
                    m_TextMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.All);

                    // Add to our range of selected indices, unless outside of sentence
                    if(start == -1)
                    {
                        selectedIndices = (wordIndex, wordIndex);
                    } else if(wordIndex < start)
                    {
                        selectedIndices = (wordIndex, end);
                    } else {
                        selectedIndices = (start, wordIndex);
                    }


                }
            
            #endregion
        }

        // Updates the result text in the window by converting from the indices
        // by word to string
        void UpdateResult()
        {
            int start = selectedIndices.Item1;
            int end = selectedIndices.Item2;
            string updated = "";

            // get array of words in story
            string[] byWord = (story).Split(" ");
            TMP_WordInfo[] wInfo = m_TextMeshPro.textInfo.wordInfo;
            startIndex = 0;
            if(start != -1){
                // Concatenate the selected words using the character count
                for(int i = start; i <= end; i++)
                {
                    updated += " ";
                    // update result window
                    for(int j = 0; j < wInfo[i].characterCount; j++)
                    {
                        updated += (story)[wInfo[i].firstCharacterIndex + j];
                    }
                }

                // Set the start and the end for GameScript.cs to use
                startIndex = wInfo[start].firstCharacterIndex;
                endIndex = wInfo[end].lastCharacterIndex + 1;
                updated = updated.Substring(1);
                    
            } else {
                // if no text is selected, display this to user
                startIndex = -1;
                endIndex = -1;
                updated = "Not in sentence";
            }

            Result.text = updated;
            
        }

        // Helper that restores the color of one word
        void ClearTextH(int m_selectedWord)
        {
                if (m_TextPopup_RectTransform != null && m_selectedWord != -1)
                {
                    TMP_WordInfo wInfo = m_TextMeshPro.textInfo.wordInfo[m_selectedWord];

                    // Iterate through each of the characters of the word.
                    for (int i = 0; i < wInfo.characterCount; i++)
                    {
                        int characterIndex = wInfo.firstCharacterIndex + i;

                        // Get the index of the material / sub text object used by this character.
                        int meshIndex = m_TextMeshPro.textInfo.characterInfo[characterIndex].materialReferenceIndex;

                        // Get the index of the first vertex of this character.
                        int vertexIndex = m_TextMeshPro.textInfo.characterInfo[characterIndex].vertexIndex;

                        // Get a reference to the vertex color
                        Color32[] vertexColors = m_TextMeshPro.textInfo.meshInfo[meshIndex].colors32;

                        Color32 c = vertexColors[vertexIndex + 0].Tint(4.0f);

                        vertexColors[vertexIndex + 0] = c;
                        vertexColors[vertexIndex + 1] = c;
                        vertexColors[vertexIndex + 2] = c;
                        vertexColors[vertexIndex + 3] = c;
                    }

                    // Update Geometry
                    m_TextMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.All);

                    m_selectedWord = -1;
                }
        }

        // Uses ClearTextH to restore the colors of every word in the text box
        public void ClearText()
        {
            int start = selectedIndices.Item1;
            int end = selectedIndices.Item2;
            for(int i = start; i <= end; i++)
            {
                ClearTextH(i);
            }
            // Sets indices and text displayed to user
            selectedIndices = (-1, -1);
            startIndex = -1;
            Result.text = "Not in sentence";
        }

        // (from Text Selector B) 
        //reverts back to original style of text, used as reference in creating
        // ClearTextH 
        void RestoreCachedVertexAttributes(int index)
        {
            if (index == -1 || index > m_TextMeshPro.textInfo.characterCount - 1) return;

            // Get the index of the material / sub text object used by this character.
            int materialIndex = m_TextMeshPro.textInfo.characterInfo[index].materialReferenceIndex;

            // Get the index of the first vertex of the selected character.
            int vertexIndex = m_TextMeshPro.textInfo.characterInfo[index].vertexIndex;

            // Restore Vertices
            // Get a reference to the cached / original vertices.
            Vector3[] src_vertices = m_cachedMeshInfoVertexData[materialIndex].vertices;

            // Get a reference to the vertices that we need to replace.
            Vector3[] dst_vertices = m_TextMeshPro.textInfo.meshInfo[materialIndex].vertices;

            // Restore / Copy vertices from source to destination
            dst_vertices[vertexIndex + 0] = src_vertices[vertexIndex + 0];
            dst_vertices[vertexIndex + 1] = src_vertices[vertexIndex + 1];
            dst_vertices[vertexIndex + 2] = src_vertices[vertexIndex + 2];
            dst_vertices[vertexIndex + 3] = src_vertices[vertexIndex + 3];

            // Restore Vertex Colors
            // Get a reference to the vertex colors we need to replace.
            Color32[] dst_colors = m_TextMeshPro.textInfo.meshInfo[materialIndex].colors32;

            // Get a reference to the cached / original vertex colors.
            Color32[] src_colors = m_cachedMeshInfoVertexData[materialIndex].colors32;

            // Copy the vertex colors from source to destination.
            dst_colors[vertexIndex + 0] = src_colors[vertexIndex + 0];
            dst_colors[vertexIndex + 1] = src_colors[vertexIndex + 1];
            dst_colors[vertexIndex + 2] = src_colors[vertexIndex + 2];
            dst_colors[vertexIndex + 3] = src_colors[vertexIndex + 3];

            // Restore UV0S
            // UVS0
            Vector2[] src_uv0s = m_cachedMeshInfoVertexData[materialIndex].uvs0;
            Vector2[] dst_uv0s = m_TextMeshPro.textInfo.meshInfo[materialIndex].uvs0;
            dst_uv0s[vertexIndex + 0] = src_uv0s[vertexIndex + 0];
            dst_uv0s[vertexIndex + 1] = src_uv0s[vertexIndex + 1];
            dst_uv0s[vertexIndex + 2] = src_uv0s[vertexIndex + 2];
            dst_uv0s[vertexIndex + 3] = src_uv0s[vertexIndex + 3];

            // UVS2
            Vector2[] src_uv2s = m_cachedMeshInfoVertexData[materialIndex].uvs2;
            Vector2[] dst_uv2s = m_TextMeshPro.textInfo.meshInfo[materialIndex].uvs2;
            dst_uv2s[vertexIndex + 0] = src_uv2s[vertexIndex + 0];
            dst_uv2s[vertexIndex + 1] = src_uv2s[vertexIndex + 1];
            dst_uv2s[vertexIndex + 2] = src_uv2s[vertexIndex + 2];
            dst_uv2s[vertexIndex + 3] = src_uv2s[vertexIndex + 3];


            // Restore last vertex attribute as we swapped it as well
            int lastIndex = (src_vertices.Length / 4 - 1) * 4;

            // Vertices
            dst_vertices[lastIndex + 0] = src_vertices[lastIndex + 0];
            dst_vertices[lastIndex + 1] = src_vertices[lastIndex + 1];
            dst_vertices[lastIndex + 2] = src_vertices[lastIndex + 2];
            dst_vertices[lastIndex + 3] = src_vertices[lastIndex + 3];

            // Vertex Colors
            src_colors = m_cachedMeshInfoVertexData[materialIndex].colors32;
            dst_colors = m_TextMeshPro.textInfo.meshInfo[materialIndex].colors32;
            dst_colors[lastIndex + 0] = src_colors[lastIndex + 0];
            dst_colors[lastIndex + 1] = src_colors[lastIndex + 1];
            dst_colors[lastIndex + 2] = src_colors[lastIndex + 2];
            dst_colors[lastIndex + 3] = src_colors[lastIndex + 3];

            // UVS0
            src_uv0s = m_cachedMeshInfoVertexData[materialIndex].uvs0;
            dst_uv0s = m_TextMeshPro.textInfo.meshInfo[materialIndex].uvs0;
            dst_uv0s[lastIndex + 0] = src_uv0s[lastIndex + 0];
            dst_uv0s[lastIndex + 1] = src_uv0s[lastIndex + 1];
            dst_uv0s[lastIndex + 2] = src_uv0s[lastIndex + 2];
            dst_uv0s[lastIndex + 3] = src_uv0s[lastIndex + 3];

            // UVS2
            src_uv2s = m_cachedMeshInfoVertexData[materialIndex].uvs2;
            dst_uv2s = m_TextMeshPro.textInfo.meshInfo[materialIndex].uvs2;
            dst_uv2s[lastIndex + 0] = src_uv2s[lastIndex + 0];
            dst_uv2s[lastIndex + 1] = src_uv2s[lastIndex + 1];
            dst_uv2s[lastIndex + 2] = src_uv2s[lastIndex + 2];
            dst_uv2s[lastIndex + 3] = src_uv2s[lastIndex + 3];

            // Need to update the appropriate 
            m_TextMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.All);
        }
}
}