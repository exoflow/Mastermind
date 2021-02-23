using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;
using UnityEngine.Analytics;

public class Playfield : MonoBehaviour
{

    // Gameobjects
    public GameObject topMenu;
    public GameObject playfield;
    public GameObject hintbox;
    public GameObject checkButton;
    public GameObject solution;
    public GameObject Sounds;
    public GameObject panels;

    List<GameObject> allSlots = new List<GameObject>();
    List<GameObject> allSolutionSlots = new List<GameObject>();
    List<GameObject> currentSlots;
    List<GameObject> allCheckButtons = new List<GameObject>();
    List<GameObject> allHintboxes = new List<GameObject>();

    GameObject currentCheckButton;
    GameObject currentHintbox;
    GameObject ChestScroll;
    GameObject EasyOpen;
    GameObject MasterOpen;

    GameObject PanelsWon;
    GameObject PanelsChangeToEasy;
    GameObject PanelsChangeToMaster;
    GameObject PanelsBuyMaster;
    GameObject PanelsNoChests;
    GameObject PanelsHint;
    GameObject PanelsMasterUnlocked;

    public AudioSource Soundtrack;
    AudioSource DiamondSound;
    AudioSource SoundsMasterUnlock;
    AudioSource SoundsSwipe;

    GameObject SolutionEasyChestPoint;
    GameObject SolutionMasterChestPoint;

    public List<GameObject> allHintPearls = new List<GameObject>();
    public List<GameObject> allPearls = new List<GameObject>();

    // Solution
    public GameObject closedChest;
    public GameObject openChest;
    public GameObject protector;
    public List<int> playerSolution;
    public List<int> realSolution;
    int fullMatches = 0;
    int halfMatches = 0;
    public Text chestText;

    // Sounds
    public GameObject soundOn;
    public GameObject soundOff;
    public AudioSource lockSound;
    public AudioSource gameOverSound;
    public AudioSource pearlSound; 
    public AudioSource checkButtonSound;

    // Current area and position
    public GameObject pointer;
    int currentPosition;
    int area;
    bool activeClick;
    game gameState;

    // Top menu
    public GameObject diamond;
    public GameObject restartButton;
    
    // Playerprefs
    int diamonds;
    int chests;
    bool isSoundOn;
    bool isMasterModeUnlocked;
    int currentMode;
    int tutorial;
    int achievementStep;
    int selectedMode;

    enum game
    {
        NotStarted,
        Started,
        Won,
        Over,
        NoChests,
        End,
    }

    void setGameState(game newState)
    {
        gameState = (chests <= 0) ? game.NoChests : newState;
    }

    void Start()
    {
        pointer.SetActive(false);
        syncPlayerPrefs();
        initializeDerivedGameobjects();
        setGameState(game.NotStarted);
        initializeGameMode();

        if (chests > 0)
        {
            generateSolution();
            area = 0;
            startArea();
        }
    }

    void Update()
    {
        switch (gameState)
        {
            case game.Won:
                StartCoroutine(gameWon());
                gameState = game.End;
                break;

            case game.Over:
                StartCoroutine(gameOver());
                gameState = game.End;
                break;

            case game.NoChests:
                waitForRefill();
                break;

            default:
                break;
        }

        if (gameState == game.Started)
        {
            setPointer();
            if (isAllSlotsFilled())
            {
                setCheckButton(true);
            }
        }

        if (gameState == game.NoChests && now() < chestRefillTime())
        {
            waitForRefill();
        }

        if (gameState == game.NoChests && now() >= chestRefillTime())
        {
            addChests(5);
            restart();
        }

        handleGameMode();
    }

    public void buyMasterMode()
    {
        if (diamonds >= 250)
        {
            PlayerPrefs.SetInt("currentMode", 5);
            PlayerPrefs.SetInt("isMasterModeUnlocked", 1);
            syncPlayerPrefs();
            addDiamonds(-250);
            PanelsMasterUnlocked.SetActive(true);
            addChests(5);
            playSound(SoundsMasterUnlock);
            AnalyticsEvent.AchievementUnlocked("MasterModeUnlocked");
            Debug.Log("AnalyticsEvent.AchievementUnlocked(MasterModeUnlocked);");
        }
    }

    DateTime chestRefillTime()
    {
        return DateTime.FromBinary(Convert.ToInt64(PlayerPrefs.GetString("waitForRefill")));
    }

    DateTime now()
    {
        return System.DateTime.Now;
    }

    void waitForRefill()
    {
        deactiveScroll();
        deactivatePearls();
        string difference = now().Subtract(chestRefillTime()).ToString().Substring(4, 5);
        PanelsNoChests.SetActive(true);
        PanelsNoChests.transform.Find("MiddleText").GetComponent<Text>().text = "Your chests will refill in " + difference + " minutes";
    }

    void deactiveScroll()
    {
        SolutionEasyChestPoint.SetActive(false);
        SolutionMasterChestPoint.SetActive(false);
        //(ChestScroll.GetComponent("DirectionalScrollSnap") as MonoBehaviour).enabled = false;
    }

    void generateSolution()
    {
        realSolution = new List<int>(currentMode);

        for (int i = 0; i < currentMode; i++)
        {
            int pearlId = UnityEngine.Random.Range(0, allPearls.Count-1);

            realSolution.Add(pearlId);
            GameObject newPearl = Instantiate(allPearls[pearlId], allSolutionSlots[i].transform, false);
            newPearl.transform.position = newPearl.transform.parent.position;
            newPearl.GetComponent<Button>().enabled = false;
        }
    }


    void instantiatePearl(int pearlId)
    {
        // Include pearl to the player solution
        playerSolution[currentPosition] = pearlId;

        // Instantiate new pearls at current position
        GameObject newPearl = Instantiate(allPearls[pearlId], currentSlots[currentPosition].transform, false);
        newPearl.transform.position = currentSlots[currentPosition].transform.position;
        newPearl.GetComponent<RectTransform>().sizeDelta = new Vector2(100, 100);
        newPearl.GetComponent<Button>().enabled = false;

        // Increase current position
        setCurrentPosition(getCurrentPosition() + 1);
    }

    public void restart()
    {
        if (gameState == game.NotStarted || gameState == game.NoChests)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            addChests(-1);
        }
    }

    void startArea()
    {
        if (area < allHintboxes.Count)
        {
            initializeArea();
            InitializeCurrentPosition();
            initializePlayerSolution();
            setSlotButtons(true);
        }

        if (gameState!=game.Won)
        {
            gameState = game.Over;
        }
       
    }


    void finishArea()
    {
        setCheckButton(false);
        setSlotButtons(false);
        setActiveClick(false);
        area++;
    }


    public void slotButtonClick(int slotId)
    {
        setCurrentPosition(slotId);
        setActiveClick(true);
    }

    public void pearlButtonClick(int pearlId)
    {
        instantiatePearl(pearlId);
        gameState = game.Started;
        playSound(pearlSound);

    }

    public void ClosePanelsHint()
    {
        PanelsHint.SetActive(false);
        PlayerPrefs.SetInt("tutorial", 0);
        syncPlayerPrefs();
        AnalyticsEvent.TutorialComplete();
        Debug.Log("AnalyticsEvent.TutorialComplete();");
    }

    public void checkButtonClick()
    {
        playSound(checkButtonSound);
        if (tutorial == 1) { PanelsHint.SetActive(true); }
        finishArea();
        checkSolution();
        showHints();
        startArea();
    }

    void playSound(AudioSource sound)
    {
        if (isSoundOn)
        {
            sound.Play();
        }
    }


    void initializeGameMode()
    {
        // Hintbox and CheckButton
        for (int i = hintbox.transform.childCount - 1; i >= 0; i--)
        {
            allHintboxes.Add(hintbox.transform.GetChild(i).gameObject);
        }

        for (int i = checkButton.transform.childCount - 1; i >= 0; i--)
        {
            allCheckButtons.Add(checkButton.transform.GetChild(i).gameObject);
        }

        if (currentMode == 5)
        {
            // Playfield slots
            for (int i = playfield.transform.childCount - 1; i >= 0; i--)
            {
                foreach (Transform slot in playfield.transform.GetChild(i))
                {
                    allSlots.Add(slot.gameObject);
                }
            }

            // Solution slots
            foreach (Transform slot in solution.transform.Find("Spacer"))
            {
                allSolutionSlots.Add(slot.gameObject);
            }

            solution.transform.Find("EasyChestPoint").gameObject.SetActive(false);
            solution.transform.Find("MasterChestPoint").gameObject.SetActive(true);
        }

        if (currentMode == 4)
        {
            // Playfield slots
            for (int i = playfield.transform.childCount - 1; i >= 0; i--)
            {
                int slotId = 0;
                foreach (Transform slot in playfield.transform.GetChild(i))
                {
                    if (slotId == 0 || slotId % 4 != 0)
                    {
                        allSlots.Add(slot.gameObject);
                    }
                    else
                    {
                        slot.gameObject.SetActive(false);
                    }
                    slotId++;
                }
            }

            // Playfield slots spacing
            foreach (Transform spacer in playfield.transform)
            {
                spacer.GetComponent<GridLayoutGroup>().spacing = new Vector2(35, 0);
            }

            // Solution slots
            solution.transform.Find("Spacer").GetChild(4).gameObject.SetActive(false);

            foreach (Transform slot in solution.transform.Find("Spacer"))
            {
                if (slot.gameObject.activeSelf)
                {
                    allSolutionSlots.Add(slot.gameObject);
                }
            }

            // Solution slots spacing
            solution.transform.Find("Spacer").GetComponent<GridLayoutGroup>().spacing = new Vector2(35, 0);

            // Protector active
            protector.transform.GetChild(4).gameObject.SetActive(false);

            // Protector spacing
            protector.transform.GetComponent<GridLayoutGroup>().spacing = new Vector2(35, 0);

            solution.transform.Find("EasyChestPoint").gameObject.SetActive(true);
            solution.transform.Find("MasterChestPoint").gameObject.SetActive(false);
        }
    }

    void checkSolution()
    {
        fullMatches = 0;
        halfMatches = 0;

        List<int> backup = new List<int>();

        // Backup solution
        for (int i = 0; i < currentMode; i++)
        {
            backup.Add(realSolution[i]);
        }

        // Check full Matches
        for (int i = 0; i < currentMode; i++)
        {
            if (realSolution[i] == playerSolution[i])
            {
                fullMatches++;
                realSolution[i] = -1;
                playerSolution[i] = -2;
            }
        }

        if (fullMatches == currentMode)
        {
            gameState = game.Won;
        }

        // Check half Matches
        for (int i = 0; i < currentMode; i++)
        {
            for (int j = 0; j < currentMode; j++)
            {
                if (realSolution[i] == playerSolution[j])
                {
                    halfMatches++;
                    realSolution[i] = -1;
                    playerSolution[j] = -2;
                }
            }
        }

        // Restore solution
        for (int i = 0; i < currentMode; i++)
        {
            realSolution[i] = backup[i];
        }
    }

    public void ToggleSelectedMode()
    {
        StartCoroutine(CR_ToggleSelectedMode());
    }

    IEnumerator CR_ToggleSelectedMode()
    {
        playSound(SoundsSwipe);
        yield return new WaitForSeconds(0.1F);
        var newMode = (selectedMode == 4) ? 5 : 4;
        selectedMode = newMode;
        var x = (newMode == 4) ? true : false;
        SolutionEasyChestPoint.SetActive(x);
        SolutionMasterChestPoint.SetActive(!x);
    }

    public int GetSelectedMode()
    {
        return selectedMode;
    }

    public void ToggleCurrentMode()
    {
        var newMode = (currentMode == 4) ? 5 : 4;
        PlayerPrefs.SetInt("currentMode", newMode);
        syncPlayerPrefs();
        restart();
    }

    void showHints()
    {
        int hintId = 1;

        for (int i = 0; i < fullMatches; i++)
        {
            GameObject newPearl = Instantiate(allHintPearls[0], currentHintbox.transform, false);
            newPearl.transform.localPosition = hintPosition(hintId);
            var x = currentHintbox.GetComponent<RectTransform>().sizeDelta.x / 135;
            newPearl.transform.localScale = new Vector3(x, x, 0);

            hintId++;
        }

        for (int i = 0; i < halfMatches; i++)
        {
            GameObject newPearl = Instantiate(allHintPearls[1], currentHintbox.transform, false);
            newPearl.transform.localPosition = hintPosition(hintId);
            var x = currentHintbox.GetComponent<RectTransform>().sizeDelta.x / 135;
            newPearl.transform.localScale = new Vector3(x, x, 0);
            hintId++;
        }
    }

    Vector3 hintPosition(int hintId)
    {
        var x = currentHintbox.GetComponent<RectTransform>().sizeDelta.x / 135;

        if (currentMode == 5)
        {
            switch (hintId)
            {
                case 1:
                    return new Vector3(-35*x, 35*x, 0);
                case 2:
                    return new Vector3(35*x, 35*x, 0);
                case 3:
                    return new Vector3(0, 0, 0);
                case 4:
                    return new Vector3(-35*x, -35*x, 0);
                case 5:
                    return new Vector3(35*x, -35*x, 0);
                default:
                    return new Vector3(0, 0, 0);
            }
        }
        else
        {
            switch (hintId)
            {
                case 1:
                    return new Vector3(-30*x, 30*x, 0);
                case 2:
                    return new Vector3(30*x, 30*x, 0);
                case 3:
                    return new Vector3(-30*x, -30*x, 0);
                case 4:
                    return new Vector3(30*x, -30*x, 0);
                default:
                    return new Vector3(0, 0, 0);
            }
        }
    }

    void initializeDerivedGameobjects()
    {
        // Sounds
        SoundsMasterUnlock = Sounds.transform.Find("MasterUnlock").GetComponent<AudioSource>();
        DiamondSound = Sounds.transform.Find("DiamondSound").GetComponent<AudioSource>();
        Soundtrack = Sounds.transform.Find("Soundtrack").GetComponent<AudioSource>();
        SoundsSwipe = Sounds.transform.Find("Swipe").GetComponent<AudioSource>();

        // Panels
        PanelsWon = panels.transform.Find("Won").gameObject;
        PanelsChangeToEasy = panels.transform.Find("ChangeToEasy").gameObject;
        PanelsChangeToMaster = panels.transform.Find("ChangeToMaster").gameObject;
        PanelsNoChests = panels.transform.Find("NoChests").gameObject;
        PanelsBuyMaster = panels.transform.Find("BuyMaster").gameObject;
        PanelsHint = panels.transform.Find("Hint").gameObject;
        PanelsMasterUnlocked = panels.transform.Find("MasterUnlocked").gameObject;

        // Solution
        ChestScroll = solution.transform.Find("ChestScroll").gameObject;
        EasyOpen = solution.transform.Find("OpenChests").Find("EasyOpen").gameObject;
        MasterOpen = solution.transform.Find("OpenChests").Find("MasterOpen").gameObject;
        SolutionEasyChestPoint = solution.transform.Find("EasyChestPoint").gameObject;
        SolutionMasterChestPoint = solution.transform.Find("MasterChestPoint").gameObject;

        // Mode
        selectedMode = currentMode;
    }

    // game over and win
    IEnumerator gameOver()
    {
        deactivatePearls();

        StartCoroutine(showSolution());
        var x = currentMode == 4 ? 1.45F : 1.8F;
        yield return new WaitForSeconds(x);
        deactiveScroll();
        playSound(gameOverSound); 
        panels.transform.Find("GameOver").gameObject.SetActive(true);

    }

    IEnumerator gameWon()
    {
        deactivatePearls();
        StartCoroutine(showSolution());
        var x = currentMode == 4 ? 1.45F : 1.8F;
        yield return new WaitForSeconds(x);

        // Diamonds
        var randomDiamonds = (currentMode == 5) ? UnityEngine.Random.Range(20, 31) : UnityEngine.Random.Range(1, 6);
        PanelsWon.transform.Find("Diamond").GetComponentInChildren<Text>().text = "+" + randomDiamonds.ToString();
        addDiamonds(randomDiamonds);

        // Chests
        deactiveScroll();
        var RightChest = currentMode == 4 ? EasyOpen : MasterOpen;
        RightChest.SetActive(true);

        // Panel
        PanelsWon.SetActive(true);
    }

    IEnumerator showSolution()
    {
        foreach (var childImage in protector.GetComponentsInChildren<Image>())
        {
            var protectorColor = childImage.color;

            float delta = 0.05F;
            while (protectorColor.a > 0)
            {
                protectorColor.a -= Mathf.Max(delta, 0);
                childImage.color = protectorColor;
                delta *= 2;
                yield return new WaitForSeconds(0.05F);
            }
            playSound(lockSound);
        }
        yield return new WaitForSeconds(0.2F);

    }

    
    void initializeArea()
    {
        // Current slots 
        currentSlots = new List<GameObject>(currentMode);
        for (int i = 0; i < currentMode; i++)
        {
            currentSlots.Add(allSlots[area * currentMode + i]);
        }
        
        for (int i = 0; i < currentSlots.Count; i++)
        {
            currentSlots[i].GetComponent<Button>().enabled = true;
        }

        // Initialize first button from allButtons
        currentCheckButton = allCheckButtons[area];

        // Initialize first hintBox from all hint boxes
        currentHintbox = allHintboxes[area];
    }

    void initializePlayerSolution()
    {
        playerSolution = new List<int>(currentMode);
        for (int i = 0; i < currentMode; i++)
        {
            playerSolution.Add(-1);
        }
    }

    void InitializeCurrentPosition()
    {
        currentPosition = 0;
    }

    bool getActiveClick()
    {
        return activeClick;
    }

    bool isAllSlotsFilled()
    {
        int filled = 0;

        for (int i = 0; i < playerSolution.Count; i++)
        {
            filled += playerSolution[i] > -1 ? 1 : 0;
        }

        if (filled == playerSolution.Count)
        {
            return true;
        }
        else
        {
            return false;
        }
    }


    int getCurrentPosition()
    {
        return currentPosition;
    }

    //int getDiamonds()
    //{
    //   return PlayerPrefs.GetInt("diamonds", 0);
    //}

    //int getChests()
    //{
    //    return PlayerPrefs.GetInt("chests", 5);
    //}

    void setSlotButtons(bool status)
    {
        for (int i = 0; i < currentMode; i++)
        {
            currentSlots[i].GetComponent<Button>().enabled = status;
        }
    }

    void setActiveClick(bool status)
    {
        activeClick = status;
    }


    void setPointer()
    {
        pointer.SetActive((!isAllSlotsFilled() || getActiveClick()) ? true : false);
        pointer.transform.position = currentSlots[currentPosition].transform.position;
    }

    void setCurrentPosition(int position)
    {
        if (position >= 0 && position < currentMode)
        {
            currentPosition = position;
        }
    }

    void setCheckButton(bool status)
    {
        currentCheckButton.GetComponent<Button>().enabled = status;
        currentCheckButton.GetComponent<Image>().enabled = status;
        currentCheckButton.GetComponentInChildren<Text>().enabled = status;
    }

    void addDiamonds(int number)
    {
        var newDiamonds = diamonds + number;
        PlayerPrefs.SetInt("diamonds", newDiamonds);
        syncPlayerPrefs();
        playSound(DiamondSound);

        // Analytics
        if (achievementStep == 1 && diamonds >= 10)
        {
            AnalyticsEvent.AchievementStep(1, "diamonds", new Dictionary<string, object>
            {{ "diamonds", 10}});
            Debug.Log("AnalyticsEvent.AchievementStep(1, diamonds, { diamonds, 10} ;");
            PlayerPrefs.SetInt("achievementStep", 2);
            syncPlayerPrefs();

        }

        if (achievementStep == 2 && diamonds >= 50)
        {
            AnalyticsEvent.AchievementStep(2, "diamonds", new Dictionary<string, object>
            {{ "diamonds", 50}});
            Debug.Log("AnalyticsEvent.AchievementStep(2, diamonds, { diamonds, 50} ;");
            PlayerPrefs.SetInt("achievementStep", 3);
            syncPlayerPrefs();
        }

        if (achievementStep == 3 && diamonds >= 100)
        {
            AnalyticsEvent.AchievementStep(3, "diamonds", new Dictionary<string, object>
            {{ "diamonds", 100}});
            Debug.Log("AnalyticsEvent.AchievementStep(3, diamonds, { diamonds, 100} ;");
            PlayerPrefs.SetInt("achievementStep", 4);
            syncPlayerPrefs();
        }

        if (achievementStep == 4 && diamonds >= 250)
        {
            AnalyticsEvent.AchievementStep(4, "diamonds", new Dictionary<string, object>
            {{ "diamonds", 250}});
            Debug.Log("AnalyticsEvent.AchievementStep(4, diamonds, { diamonds, 250} ;");
            PlayerPrefs.SetInt("achievementStep", 5);
            syncPlayerPrefs();
        }

        if (achievementStep == 5 && diamonds >= 1000)
        {
            AnalyticsEvent.AchievementUnlocked("1kDiamonds");
            Debug.Log("AnalyticsEvent.AchievementUnlocked(1kDiamonds)");
            PlayerPrefs.SetInt("achievementStep", 6);
            syncPlayerPrefs();
        }
    }

    public void addChests(int number)
    {
        var newChests = chests + number;

        if (newChests <= 0)
        {
            gameState = game.NoChests;
            PlayerPrefs.SetString("waitForRefill", System.DateTime.Now.AddMinutes(20).ToBinary().ToString());
            syncPlayerPrefs();
            AnalyticsEvent.AdOffer(true, AdvertisingNetwork.UnityAds);
            Debug.Log("AnalyticsEvent.AdOffer(true, AdvertisingNetwork.UnityAds);");
        }

        PlayerPrefs.SetInt("chests", newChests);
        syncPlayerPrefs();
    }

    void syncPlayerPrefs()
    {
        // Chests
        chests = PlayerPrefs.GetInt("chests", 5);
        solution.transform.Find("RemainingChests").GetComponentInChildren<Text>().text = "Chests: " + chests.ToString();

        // Diamonds
        diamonds = PlayerPrefs.GetInt("diamonds", 0);
        topMenu.transform.Find("Diamond").GetComponentInChildren<Text>().text = diamonds.ToString();

        // Game mode
        currentMode = PlayerPrefs.GetInt("currentMode", 4);

        // Master mode
        isMasterModeUnlocked = (PlayerPrefs.GetInt("isMasterModeUnlocked", 0) == 1) ? true : false;

        // Soundtrack
        isSoundOn = (PlayerPrefs.GetInt("sound", 1) == 1) ? true : false;
        soundOff.SetActive(!isSoundOn);
        soundOn.SetActive(isSoundOn);

        // Tutorial
        tutorial = PlayerPrefs.GetInt("tutorial", 1);

        // Analytics
        achievementStep = PlayerPrefs.GetInt("achievementStep", 1);
    }


    public void ToggleSound()
    {
        var x = (isSoundOn == true) ? 0 : 1;
        PlayerPrefs.SetInt("sound", x);
        syncPlayerPrefs();
        //if (isSoundOn) { Soundtrack.Pause(); } else { Soundtrack.Play(); }
    }

    void deactivatePearls()
    {
        for (int i = 0; i < allPearls.Count; i++)
        {
            allPearls[i].gameObject.GetComponent<Button>().enabled = false;
        }
    }

#if DEVELOPMENT_BUILD || UNITY_EDITOR
    public void test()
    {
        StartCoroutine(gameWon());
        addDiamonds(20);
        addChests(1);
    }
#endif

}
