using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    public string playerName;

    public GameObject buttonPrefab;
    private string selectedLevel;

    void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else if (instance != this)
        {
            Destroy(gameObject);
        }
        DontDestroyOnLoad(gameObject);
    }

    void Start () {
        //hooks up OnSceneLoaded() to a Unity SceneManager event name sceneLoaded that fires when a scene loads
        SceneManager.sceneLoaded += OnSceneLoaded;
        DiscoverLevels();
	}

    public void RestartLevel(float delay)
    {
        StartCoroutine(RestartLevelDelay(delay));
    }

    private IEnumerator RestartLevelDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene("Game");
    }

    void Update () {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SceneManager.LoadScene("Menu");
        }
	}

    public List<PlayerTimeEntry> LoadPreviousTimes()
    {
        //attempt to load saved time entries for the player
        try
        {
            //construct the path to the file using a combination of the player's name and Application.persistentDataPath
            var levelName = Path.GetFileName(selectedLevel);
            var scoresFile = Application.persistentDataPath + "/" + playerName + "_" + levelName + "_times.dat";
            //opens a file stream to the file path to read any existing time entries using a binary formatter object
            using (var stream = File.Open(scoresFile, FileMode.Open))
            {
                var bin = new BinaryFormatter();
                //Deserialize = read (unpack) the data in the file by passing in the opened file stream
                var times = (List<PlayerTimeEntry>)bin.Deserialize(stream);
                //successful => return a list of player time entries
                return times;
            }
        }
        //unsuccessful => finds the errors
        catch (IOException ex)
        {
            Debug.LogWarning("Couldn't load previous times for: " + playerName + ". Exception: " + ex.Message);
            //return an empty list back
            return new List<PlayerTimeEntry>();
        }
    }

    public void SaveTime(decimal time)
    {
        //when saving a time, fetch existing times first
        var times = LoadPreviousTimes();
  
        var newTime = new PlayerTimeEntry();
        newTime.entryDate = DateTime.Now; //save the current date
        newTime.time = time; //save the runtime

        var bFormatter = new BinaryFormatter(); //will do the serialization
        var levelName = Path.GetFileName(selectedLevel);
        var filePath = Application.persistentDataPath + "/" + playerName + "_" + levelName + "_times.dat"; //creates the file path
        //opens the file (FileMode.Create = create a new file or overwrite existing files)
        using (var file = File.Open(filePath, FileMode.Create))
        {
            times.Add(newTime);
            bFormatter.Serialize(file, times);
        }
    }

    public void DisplayPreviousTimes()
    {
        var times = LoadPreviousTimes();
        var levelName = Path.GetFileName(selectedLevel);
        if(levelName != null)
        {
            levelName = levelName.Replace(".json", "");
        }

        var topThree = times.OrderBy(time => time.time).Take(3); //(LINQ query: sorts time from fastest to slowlest then takes the first three of each)

        var timesLabel = GameObject.Find("PreviousTimes").GetComponent<Text>();

        timesLabel.text = levelName + "\n";
        timesLabel.text = "BEST TIMES \n";
        foreach(var time in topThree)
        {
            timesLabel.text += time.entryDate.ToShortDateString() + ": " + time.time + "\n";
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
    {
        if (!string.IsNullOrEmpty(selectedLevel) && scene.name == "Game")
        {
            Debug.Log("Loading level content for: " + selectedLevel);
            LoadLevelContent();
            DisplayPreviousTimes();
        }
        if(scene.name == "Menu")
        {
            DiscoverLevels();
        }
    }

    private void SetLevelName(string levelFilePath)
    {
        selectedLevel = levelFilePath;
        SceneManager.LoadScene("Game");
    }

    private void DiscoverLevels()
    {
        var levelPanelRectTransform = GameObject.Find("LevelItemsPanel").GetComponent<RectTransform>();
        var levelFiles = Directory.GetFiles(Application.dataPath, "*.json");

        var yOffset = 0f;
        for(var i = 0; i < levelFiles.Length; i++)
        {
            if (i == 0)
            {
                yOffset = -30f;
            } else
            {
                yOffset -= 65f; 
            }
            var levelFile = levelFiles[i];
            var levelName = Path.GetFileName(levelFile);

            //instantiates a copy of the button prefab
            var levelButtonObj = (GameObject)Instantiate(buttonPrefab, Vector2.zero, Quaternion.identity);
            //gets its transform and makes it a child of LevelItemsPanel
            var levelButtonRectTransform = levelButtonObj.GetComponent<RectTransform>();
            levelButtonRectTransform.SetParent(levelPanelRectTransform, true);
            //positions it
            levelButtonRectTransform.anchoredPosition = new Vector2(212.5f, yOffset);
            //sets the button text to the level's name
            var levelButtonText = levelButtonObj.transform.GetChild(0).GetComponent<Text>();
            levelButtonText.text = levelName;

            var levelButton = levelButtonObj.GetComponent<Button>();
            //dynamically assign different calls to SetLevelName() to each instance of a button
            levelButton.onClick.AddListener(delegate { SetLevelName(levelFile); });
            //expands the vertical size of LevelItemsPanel to accommodate all possible buttons
            levelPanelRectTransform.sizeDelta = new Vector2(levelPanelRectTransform.sizeDelta.x, 60f * i);
        }

        //changes the panel's scroll position to ensure it's back at the top after all the level buttons are added
        levelPanelRectTransform.offsetMax = new Vector2(levelPanelRectTransform.offsetMax.x, 0f);
    }

    private void LoadLevelContent()
    {
        var existingLevelRoot = GameObject.Find("Level");
        Destroy(existingLevelRoot);
        var levelRoot = new GameObject("Level");

        //reads the JSON file content of the selected level
        var levelFileJsonContent = File.ReadAllText(selectedLevel);
        var levelData = JsonUtility.FromJson<LevelDataRepresentation>(levelFileJsonContent);

        //makes levelData.levelItems into a fully populated array of LevelItemRepresentation instances
        foreach(var li in levelData.levelItems)
        {
            //locates correct prefab and loads it
            var pieceResource = Resources.Load("Prefabs/" + li.prefabName);
            if(pieceResource == null)
            {
                Debug.LogError("Cannot find resource: " + li.prefabName);
            }

            //instantiates a clone of this prefab and configured it based on sprite data from JSON file
            var piece = (GameObject)Instantiate(pieceResource, li.position, Quaternion.identity);
            var pieceSprite = piece.GetComponent<SpriteRenderer>();
            if(pieceSprite != null)
            {
                pieceSprite.sortingOrder = li.spriteOrder;
                pieceSprite.sortingLayerName = li.spriteLayer;
                pieceSprite.color = li.spriteColor;
            }

            //makes the object a child of the Level GameObject and sets its transform
            piece.transform.parent = levelRoot.transform;
            piece.transform.position = li.position;
            piece.transform.rotation = Quaternion.Euler(li.rotation.x, li.rotation.y, li.rotation.z);
            piece.transform.localScale = li.scale;
        }

        var SoyBoy = GameObject.Find("SoyBoy");
        SoyBoy.transform.position = levelData.playerStartPosition;
        Camera.main.transform.position = new Vector3(SoyBoy.transform.position.x, SoyBoy.transform.position.y, Camera.main.transform.position.z);

        //locates the smooth follow script
        var camSettings = FindObjectOfType<CameraLerpToTransform>();

        //populates settings for speed, bounds and tracking target
        if(camSettings != null)
        {
            camSettings.cameraZDepth = levelData.cameraSettings.cameraZDepth;
            camSettings.camTarget = GameObject.Find(levelData.cameraSettings.cameraTrackTarget).transform;
            camSettings.maxX = levelData.cameraSettings.maxX;
            camSettings.maxY = levelData.cameraSettings.maxY;
            camSettings.minX = levelData.cameraSettings.minX;
            camSettings.minY = levelData.cameraSettings.minY;
            camSettings.trackingSpeed = levelData.cameraSettings.trackingSpeed;
        }
    }
}
