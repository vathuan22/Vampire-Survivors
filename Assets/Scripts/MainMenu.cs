using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class MainMenu : MonoBehaviour
{
    public string firstLevelName; // The first level name. GK

    [Tooltip("Optional title text to display 'Class Survival'.")]
    public TMP_Text gameTitleText;

    void Start()
    {
        // Ensure the title always shows the correct game name.
        if (gameTitleText != null)
            gameTitleText.text = "Class Survival";
    }

    public void StartGame() // Start the game. GK
    {
        SceneManager.LoadScene(firstLevelName); // Load the first level. GK
    }

    public void QuitGame() // Quit the game. GK
    {
        Application.Quit(); // Quit the game. GK
        Debug.Log("Quitting Class Survival");
    }
}