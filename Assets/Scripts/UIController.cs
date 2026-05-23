using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UIController : MonoBehaviour
{
    public static UIController instance;

    public void Awake()
    {
        instance = this;
    }

    public Slider explvlSlider;
    public TMP_Text explvlText;

    public LevelUpSellectionButton[] levelUpButtons;

    public GameObject levelUpPanel;
    [Tooltip("Optional: Title text shown inside the level-up panel (e.g. 'Choose Your Path').")]
    public TMP_Text levelUpPanelTitleText;

    [Header("Promotion Panel")]
    public GameObject promotionPanel;
    public LevelUpSellectionButton[] promotionButtons;
    [Tooltip("Optional: Title text shown inside the promotion panel.")]
    public TMP_Text promotionPanelTitleText;

    public TMP_Text coinText;
    public GameObject levelEndScreen;
    public TMP_Text endTimeText;

    public PlayerStatUpgradeDisplay moveSpeedUpgradeDisplay,
        healthUpgradeDisplay,
        pickupRangeUpgradeDisplay,
        maxWeaponsUpgradeDisplay;

    public TMP_Text timeText;
    public string mainMenuName;
    public GameObject pauseScreen;

    [Tooltip("Optional HUD label displaying the player's active class.")]
    public TMP_Text activeClassText;

    void Start()
    {
        UpdateActiveClassDisplay();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            PauseUnpause();
        }
    }

    public void UpdateExperience(int currentExp, int levelExp, int currentLvl)
    {
        if (explvlSlider != null)
        {
            explvlSlider.maxValue = levelExp;
            explvlSlider.value = currentExp;
        }

        if (explvlText != null)
            explvlText.text = "Level: " + currentLvl;
    }

    public void SetLevelUpPanelTitle(string title)
    {
        if (levelUpPanelTitleText != null)
            levelUpPanelTitleText.text = title;
    }

    public void SetPromotionPanelTitle(string title)
    {
        if (promotionPanelTitleText != null)
            promotionPanelTitleText.text = title;
    }

    public void UpdateActiveClassDisplay()
    {
        if (activeClassText == null) return;
        if (ClassManager.instance == null) return;

        var active = ClassManager.instance.ActiveClass;
        activeClassText.text = active != null ? "Class: " + active.className : "Class: None";
    }

    public void SkipLevelUp()
    {
        if (levelUpPanel != null)
            levelUpPanel.SetActive(false);

        Time.timeScale = 1f;
    }

    public void UpdateCoins()
    {
        if (coinText != null && CoinController.instance != null)
            coinText.text = "Coins: " + CoinController.instance.currentCoins;
    }

    public void PurchaseMoveSpeed()
    {
        PlayerStatController.instance.PurchaseMoveSpeed();
        SkipLevelUp();
    }

    public void PurchaseHealth()
    {
        PlayerStatController.instance.PurchaseHealth();
        SkipLevelUp();
    }

    public void PurchasePickupRange()
    {
        PlayerStatController.instance.PurchasePickupRange();
        SkipLevelUp();
    }

    public void PurchaseMaxWeapons()
    {
        PlayerStatController.instance.PurchaseMaxWeapons();
        SkipLevelUp();
    }

    public void UpdateTimer(float time)
    {
        float minutes = Mathf.FloorToInt(time / 60);
        float seconds = Mathf.FloorToInt(time % 60);
        if (timeText != null)
            timeText.text = "Time: " + minutes + ":" + seconds.ToString("00");
    }

    public void GoToMainMenu()
    {
        SceneManager.LoadScene(mainMenuName);
        Time.timeScale = 1f;
    }

    public void Restart()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        Time.timeScale = 1f;
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void PauseUnpause()
    {
        if (pauseScreen == null) return;

        if (pauseScreen.activeSelf == false)
        {
            pauseScreen.SetActive(true);
            Time.timeScale = 0f;
        }
        else
        {
            pauseScreen.SetActive(false);
            if (levelUpPanel == null || levelUpPanel.activeSelf == false)
            {
                Time.timeScale = 1f;
            }
        }
    }
}