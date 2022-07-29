from flask import Flask, jsonify, request
import requests
import json
from flask_cors import CORS
import xml.etree.ElementTree as ET
import os
import re
import string
from collections import OrderedDict
import random

app = Flask(__name__)
CORS(app)
agreements = {}
frameDictionary = {}
with open("fndata/frameDictionary.json", 'r') as f:
        frameDictionary = json.load(f)

"""
Params:

"""

# Gets the player object and sends it to the game
@app.route("/players/get/<id>")
def getInfo(id=""):
    players = {}
    directory = os.listdir('fndata')

    # read or create players.json file
    if("players.json" in directory):
        with open("fndata/players.json", "r") as f:
                players = json.loads(f.read())
        # create player entry or send back default player json (line 39)
        if(id not in players.keys()):
            players[id] = ["", 0, True]
            with open("fndata/players.json", "w") as f:
                json.dump(players,f)
            return {"id": id, "username": "", "coins": 0, "isFirstTime": True}
        else:
            entry = players[id]
            return {"id": id, "username": entry[0], "coins": entry[1], "isFirstTime": entry[2]}
    else:
        with open("fndata/players.json", "w") as f:
            json.dump({"id": id, "username": "", "coins": 0, "isFirstTime": True},f)
            return {"id": id, "username": "", "coins": 0, "isFirstTime": True}


# updates the list of players with a new player object  
@app.route("/players/update", methods=['POST'])
def updateInfo():
    players = {}
    directory = os.listdir('fndata')
    # create or read players.json file
    if("players.json" in directory):
        # update json file
        with open("fndata/players.json", "r") as f:
            players = json.loads(f.read())
        data = request.get_json()
        players[data["id"]] = [data["username"], data["coins"], data["isFirstTime"]]
        with open("fndata/players.json", "w") as f:
            json.dump(players,f)
    else:
        # create new players.json file
        data = request.get_json()
        players[data["id"]] = [data["username"], data["coins"], data["isFirstTime"]]
        with open("fndata/players.json", "w") as f:
            json.dump(players,f)

    return "Success"

# update the json showing who agreed to terms & conditions

@app.route("/players/agreement", methods=['POST'])
def updateAgreements():
    data = request.get_json()
    agreements[data["id"]] = data["time"]
    with open("agreements.json", "w") as f:
        json.dump(agreements,f)
    return "Success"

# return the json showing who agreed to terms & conditions
@app.route("/players/getAgreement")
def getAgreements():
    with open("agreements.json", "r") as f:
        return f.read()

# returns the frames that caused parsing errors
@app.route("/getErrors")
def getErrors():
    frames = {}
    directory = os.listdir('fndata/frame_errors')
    for filename in directory:
        # load each file, organize dictionary by frame
        with open('fndata/frame_errors/' + filename, 'r') as f:
            data = json.load(f)
            frames[data["frame"]] = data
    return {"frame errors": frames}

# updates the list of frames that caused errors 
@app.route("/frames/error", methods=['POST'])
def updateErrors():
    data = request.get_json()
    with open("fndata/frame_errors/"+ "errors" + data["frame"] + ".json", "w") as f:
        json.dump(data,f)
    return "Success"

# adds a new annotated sentence
@app.route("/frames/update/<frame_id>", methods=['POST'])
def updateFrames(frame_id):
    # parse data
    data = request.get_json()
    new_entry = {"author_id": data["author_id"], 
                        "fe_id": data["fe_id"], 
                        "startIndex": data["startIndex"],  
                        "length": data["length"]}
    text = data["text"].replace('\u200b', '')
    directory = os.listdir('fndata')
    if("annotatedSentences.json" in directory):
        annotatedSentences = {}
        with open("fndata/annotatedSentences.json", "r") as f:
            annotatedSentences = json.loads(f.read())
        # clear the file
        open('fndata/annotatedSentences.json', 'w').close()
        # check if frame ID in keys, add or update dict entry basedon that
        if(frame_id in annotatedSentences.keys()):
            if(text in annotatedSentences[frame_id].keys()):
                (annotatedSentences[frame_id][text]).append(new_entry)
            else:
                annotatedSentences[frame_id][text] = [new_entry]
        else:
            annotatedSentences[frame_id] = {data["text"]: [new_entry]}
        with open("fndata/annotatedSentences.json", "w") as f:
            json.dump(annotatedSentences,f)
    else:
        annotatedSentences = {}
        # clear the file
        open('fndata/annotatedSentences.json', 'w').close()
        annotatedSentences[frame_id] = {data["text"]: [new_entry]}
        with open("fndata/annotatedSentences.json", "w") as f:
            json.dump(annotatedSentences,f)
    return "Success"

# returns 3 randomly frames or an arbitrary number frame based on words
@app.route("/lookup/<word>")
def getWord(word):
    directory = os.listdir('fndata/frame')
    json_directory = os.listdir('fndata/game_json')
    frames = []
    if(word == "!"):
        for i in range(3):
            filename = directory[random.randrange(len(directory))]
            parsed = {}
            if(('fndata/game_json/'+filename[:-3]+"json") in json_directory):
                with open('fndata/game_json/'+filename[:-3]+"json", 'r') as f:
                    parsed = json.load(f)
                frames.append(parsed)
            else:
                parsed = parseFrameFile(filename)
                if(parsed not in frames and parsed != None):
                    with open("fndata/game_json/" +filename[:-3]+"json", "w") as f:
                        json.dump(parsed,f)
                    frames.append(parsed)
                else:
                    i += 1
        return {"Frames": frames} 
    else:
        return getFrameFromWord(word)

# returns all frames that contains a word
def getFrameFromWord(word):
    tree1 = ET.parse('fndata/miscXML/lemma_to_wordformR1.7.xml')
    root1 = tree1.getroot()
    finds = []
    directory = os.listdir('fndata/frame')
        
    for lemma in (root1.findall('Lemma')):
        for wEntries in lemma.findall('WordFormEntries'):
            for wordFormEntry in wEntries:
                wordList = wordFormEntry.text.split(" ")
                newList = []
                for w in wordList:
                    pattern = re.compile('[\s]')
                    newStr = re.sub('[\s]','', w)
                    if newStr != '':
                        newList.append(newStr)
                if(' '.join(newList) == word):
                    finds.append(lemma.attrib["id"])
    newFinds = []
    for filename in directory:
        f = os.path.join('fndata/frame', filename)
        tree2 = ET.parse(f)
        root2 = tree2.getroot()
        for lu in root2.findall("{http://framenet.icsi.berkeley.edu}lexUnit"):
            if((lu.attrib["lemmaID"] in finds)):
                newFinds.append(parseFrameFile(filename))
    return {"Frames": newFinds}

# parses a frame file into a json readable by the game
def parseFrameFile(filename):
    f = os.path.join('fndata/frame', filename)
    tree2 = ET.parse(f)
    root2 = tree2.getroot()
    to_return = {}
    to_return["name"] = root2.attrib["name"]
    to_return["ID"] = root2.attrib["ID"]
    defn = root2[0].text.split('\n')[0]
    plaintext = re.sub('<([\s\S]*?)>', '', defn)
    to_return["def"] = plaintext
    to_return["examples"] = getFrameExamples(root2.attrib["name"])
    to_return["FEs"] = getFrameFEs(root2.attrib["name"])
    Lus = []
    LuIDs = []
    
    #exclude non-lexical frames
    for semT in root2.findall("{http://framenet.icsi.berkeley.edu}semType"):
        if(semT.attrib["name"] == "Non-Lexical Frame"):
            return None
    for lu in root2.findall("{http://framenet.icsi.berkeley.edu}lexUnit"):
        Lus.append(lu.attrib["name"].split('.')[0])
        LuIDs.append(lu.attrib["lemmaID"])
    to_return["LUs"] = Lus
    to_return["allLUs"] = getWordForms(LuIDs)
    for example in to_return["examples"]:
        newLabels = []
        for label in example["labels"]:
            hasReplaced = False
            for FE in to_return["FEs"]:
                if FE["abbreviation"] == label:
                    newLabels.append({"title": FE["name"],
                                       "start": example["labels"][label][0], 
                                        "length": example["labels"][label][1]})
                    hasReplaced = True
            if not hasReplaced:
                newLabels.append({"title": label,
                                       "start": example["labels"][label][0], 
                                        "length": example["labels"][label][1]})
        example["labels"] = newLabels
    return to_return


# gets all frame elements in a frame
def getFrameFEs(name):
    tree = ET.parse('fndata/frame/'+name +'.xml')
    root = tree.getroot()
    fes = []
    # Finding FE info
    for fe in root.findall("{http://framenet.icsi.berkeley.edu}FE"):
        if(fe.attrib["coreType"] == "Core"):
            defn = fe[0].text.split('\n')[0]
            plaintext = re.sub('<ex>([\s\S]*?)<\/ex>', '', defn)
            plaintext = re.sub('<([\s\S]*?)>', '', plaintext)
            
            excludes = fe.findall('{http://framenet.icsi.berkeley.edu}excludesFE')
            requires = fe.findall('{http://framenet.icsi.berkeley.edu}requiresFE')
            exList = []
            for exc in excludes:
                exList.append(exc.attrib["name"])
            
            reqList = []
            for req in requires:
                reqList.append(req.attrib["name"])
            
            fes.append({"name": fe.attrib["name"],
                        "description": plaintext,
                        "excludes": exList,
                        "requires": reqList,
                        "abbreviation": fe.attrib["abbrev"],
                        "color": fe.attrib["bgColor"]})
    return fes

# parses examples and turns the html tags into labels compatible with the game
@app.route("/examples/<name>")
def getFrameExamples(name):
    tree = ET.parse('fndata/frame/'+name +'.xml')
    root = tree.getroot()
    example_text = []
    for defn in root.findall('{http://framenet.icsi.berkeley.edu}definition'):
        for ex1 in re.findall('<ex>([\s\S]*?)<\/ex>', defn.text):
            ex = re.sub('<ment>|<\/ment>', '', ex1)
            # uncomment the line below and the alternative return to see unparsed text
            # example_text.append(ex1)
            if ex != "":
                # find all tags
                elems = re.finditer('(<fex.*?>([\s\S]*?)<\/fex>)|(<t>([\s\S]*?)<\/t>)|(<m>([\s\S]*?)<\/m>)', ex)
                target = re.search('<t>([\s\S]*?)<\/t>', ex)
                # with tags removed
                plaintext = re.sub('<fex.*?>|<\/fex>|<t>|<\/t>|<m>|<\/m>', '', ex)
                i = 0
                startIndices = OrderedDict()
                # used for when there are multiple <t> </t> tags that need
                # to be consolidated
                isInTarget = False
                targetCount = 0
                feCounts = {}
                for item in elems:
                    # search for name (indication of FE tag)
                    nameS = re.search('(?<=name=\\\")(.*?)(?=\\\")', item[0])
                    if re.search('(?<=>)(.*\S.*)(?=<)', item[0]):
                        # the actual content of the FE tag
                        content = re.search('(?<=>)(.*\S.*)(?=<)', item[0])[0]
                    else:
                        content = ""
                    start = item.start()
                    targetS = re.search('<t>', item[0])
                    if nameS and targetS:
                        # check if target is on the inside of the fe tag
                        targetInside = re.search('<t>', content)
                        length = len(content)-(19 + len(nameS[0]))
                        label = nameS[0]
                        if label in feCounts.keys():
                            feCounts[label] = feCounts[label] + 1
                        else:
                            feCounts[label] = 1
                        if label in startIndices.keys():
                            startIndices[label] = (startIndices[label][0], 
                                                    item.end()
                                                    -startIndices[label][0]
                                                    -((19 + len(label))*feCounts[label]))
                        else:
                            startIndices[label] = (start, len(content))
                            feCounts[label] = 0
                        if targetInside:
                            length = len(content) - 7
                        startIndices[nameS[0]] = (start, length)
                        targetCount += 1
                        if isInTarget:
                            startIndices["Target"] = (startIndices["Target"][0], item.end()-startIndices["Target"][0]-(7*targetCount))
                        else:
                            isInTarget = True
                            startIndices["Target"] = (start, length)
                    elif nameS:
                        label = nameS[0]
                        if label in feCounts.keys():
                            feCounts[label] = feCounts[label] + 1
                        else:
                            feCounts[label] = 1
                        if label in startIndices.keys():
                            startIndices[label] = (startIndices[label][0], 
                                                    item.end()
                                                    -startIndices[label][0]
                                                    -((19 + len(label))*feCounts[label]))
                        else:
                            startIndices[label] = (start, len(content))
                    elif targetS:
                        targetCount +=1
                        if isInTarget:
                            startIndices["Target"] = (startIndices["Target"][0], item.end()-startIndices["Target"][0]-(7*targetCount))
                        else:
                            isInTarget = True
                            startIndices["Target"] = (start, len(content))
                    else:
                        isInTarget = False
                        if not nameS:
                            label = "Support"
                            startIndices[label] = (start, len(content))
                totalShift = 0
                substr = []
                # sort indices, then shift for every preceding element
                items = list(startIndices.items())
                overLappingShift = 0
                for label, ind in items:
                    idx = items.index((label, ind))
                    # check for overlapping labels
                    if idx != 0 and items[idx-1][1][0] >= ind[0]:
                        pass
                    else:
                        overLappingShift = totalShift
                    start = ind[0] - overLappingShift
                    length = ind[1]
                    if(label == "Target"):
                        totalShift += (7*targetCount)
                    elif(label == "Support"):
                        totalShift += 7
                    else:
                        totalShift += 19 + len(label)
                    substr.append(plaintext[start:start+length])
                    startIndices[label] = (start, length)
                example_text.append({"text": plaintext, "labels": startIndices})
    return example_text
    # return {"ex": example_text}

# gets the word form (for target verification) from an LU ID
# @app.route("/find")
def getWordForms(LuIDs):
    tree1 = ET.parse('fndata/miscXML/lemma_to_wordformR1.7.xml')
    root1 = tree1.getroot()
    wordForms = []
    directory = os.listdir('fndata/frame')
    for lemma in (root1.findall('Lemma')):
        if lemma.attrib["id"] in LuIDs:
            for wEntries in lemma.findall('WordFormEntries'):
                for wordFormEntry in wEntries:
                    # removing whitespace from wordForm entry
                    wordList = wordFormEntry.text.split(" ")
                    newList = []
                    for w in wordList:
                        pattern = re.compile('[\s]')
                        newStr = re.sub('[\s]','', w)
                        if newStr != '':
                            newList.append(newStr)
                    wordForms.append(' '.join(newList).lower())
    # print(wordForms)   
    return wordForms   

# parses files in advance and saves the .json file
@app.route("/cache")
def cache():
    directory = os.listdir('fndata/frame')
    json_directory = os.listdir('fndata/game_json')
    for i in range(30):
        filename = directory[random.randrange(len(directory))]
        parsed = {}
        if(filename[:-3]+"json" not in json_directory):
            parsed = parseFrameFile(filename)
            if(parsed != None):
                with open("fndata/game_json/" +filename[:-3]+"json", "w") as f:
                    json.dump(parsed,f)
        else:
            i += 1
    return "Success"

# caches frames specifically from the test folder
@app.route("/cacheTest")
def cacheTest():
    directory = os.listdir('fndata/frame')
    json_directory = os.listdir('fndata/game_json')
    for filename in directory:
        if(filename[0] != "."):
            parsed = {}
            if(filename[:-3]+"json" not in json_directory):
                parsed = parseFrameFile(filename)
                if(parsed != None):
                    with open("fndata/game_json/" +filename[:-3]+"json", "w") as f:
                        json.dump(parsed,f)
    return "Success"

# Gets sentences for display in "view others' work" mode
@app.route("/getSentences")
def getSentences():
    sentenceList = []
    directory = os.listdir('fndata')
    annotatedSentences = {}
    if("annotatedSentences.json" in directory):
        with open("fndata/annotatedSentences.json", "r") as f:
            annotatedSentences = json.loads(f.read())
    for frame in annotatedSentences.keys():
        sentenceInfo = {}
        for text in annotatedSentences[frame].keys():
            sentenceInfo["text"] = text
            sentenceInfo["frame_name"] = frameDictionary[frame]
            labelList = annotatedSentences[frame][text]
            for label in labelList:
                with open("fndata/players.json", "r") as f:
                    players = json.loads(f.read())
                    name = players[label["author_id"]][0]
                    label["author_id"] = name
            sentenceInfo["labels"] = labelList
        sentenceList.append(sentenceInfo)
    return {"list": sentenceList}

# creates a dictionary of frame id to frame name for sentence display
@app.route("/createFrameDictionary")
def createFrameDict():
    directory = os.listdir('fndata/frame')
    for filename in directory:
        if(filename[-3:] == "xml"):
            f = os.path.join('fndata/frame', filename)
            tree2 = ET.parse(f)
            root2 = tree2.getroot()
            frameDictionary[root2.attrib["ID"]] = root2.attrib["name"]
    with open("fndata/frameDictionary.json", "w") as f:
            json.dump(frameDictionary,f)
    return "Success"

    

if __name__ == "__main__":
    app.run()