# The Frame Game
[![Open Issues](https://img.shields.io/github/issues-raw/eamspoker/FrameGameAssets?style=flat-square)](https://github.com/eamspoker/FrameGameAssets/issues)

The Frame Game is an in-progress crowdsourcing game that seeks to provide users with prompts for creative writing while encouraging them to create semantic role labelling annotations of their sentences.

## Structure of Game
![Blank diagram - Page 1](https://user-images.githubusercontent.com/46005655/182261102-3a64a5ff-b613-4df3-990e-94d316062543.png)

### Scripts
<table>
  <tr>
    <td>Filename</td>
      <td>Type</td>
     <td>Scene</td>
     <td>Description</td>
  </tr>
  <tr>
    <td>MenuScript.cs</td>
    <td>C# Script</td>
    <td>MainMenu</td>
    <td>Initiates the game, facilitates login with Facebook SDK</td>
  </tr>
   <tr>
    <td>HomeScreenController.cs</td>
    <td>C# Script</td>
    <td>StartScreen (see green on flowchart)</td>
    <td>First screen of the game, includes terms & conditions, information about FrameNet, and gameplay instructions</td>
  </tr>
    <tr>
    <td>GameScript.cs</td>
    <td>C# Script</td>
    <td>SRLGame (see blue on flowchart)</td>
    <td>Actual gameplay of the game, gathers frames from the backend, allows users to annotate their own work</td>
  </tr>
  <tr>
    
    <tr>
    <td>ExampleController.cs</td>
    <td>C# Script</td>
    <td>n/a</td>
    <td>Allows users to see others' annotated sentences</td>
  </tr>
  <tr>
        <tr>
    <td>TextMeshPro/Examples & Extras/Scripts/CustomTextSelector.cs</td>
    <td>C# Script</td>
    <td>n/a</td>
    <td>Faciliates the interaction between the text elements and the user for text selecting, referenced by GameScript.cs and ExampleController.cs to create and display annotations.</td>
  </tr>
  
  <tr>
    <td>StoredInfo.cs</td>
    <td>C# Script</td>
    <td>n/a</td>
    <td>Holds all the classes and structures that model FrameNet data that the files draw from</td>
  </tr>
  
  <tr>
    <td>Backend/frameGameBackend.py</td>
    <td>Python Script</td>
    <td>n/a</td>
    <td>Sends and receives information from the game, is used to parse and communicate FrameNet data with the game.</td>
  </tr>
 </table>
 
### Other Assets
The other assets include fonts, spritesheets, and plugins used by the game.

## Tech Stack

- Unity (C# for scripting)
- Flask (Python) for the backend

## Setting Up

<ol>
  <li>Download or clone the repository</li>
  <li>Place the Backend/frameGameBackend.py folder in the same folder as the FrameNet data</li>
  <li>Create a new project in [Unity](https://unity.com/learn)</li>
  <li>Replace the contents of the Assets folder with the contents of this repository, fill in the necessary information (i.e. app id for Facebook SDK use)</li>
  <li>Replace the contents of the Assets folder with the contents of this repository</li>
  <li>[Run](https://flask.palletsprojects.com/en/2.1.x/quickstart/) the flask backend at the folder where you placed it in (2) </li>
    <li>Run the game in Unity</li>
</ol>

## License


Made by [Emily Amspoker](https://github.com/eamspoker) during the SUPERB REU, 2022.
