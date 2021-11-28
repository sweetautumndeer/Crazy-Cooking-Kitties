using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuButtons : MonoBehaviour {

    public GameObject MainPanel;
    public GameObject LevelSelectPanel;
    public GameObject OptionsPanel;
    public GameObject BingusPanel;

    public Scene Level1;

    // Start is called before the first frame update
    void Start() {
        MainPanel.SetActive(true);
        LevelSelectPanel.SetActive(false);
        OptionsPanel.SetActive(false);
        BingusPanel.SetActive(false);
    }

    #region Level Select
    public void ShowLevelSelect() {
        MainPanel.SetActive(false);
        LevelSelectPanel.SetActive(true);
    }

    public void HideLevelSelect() {
        LevelSelectPanel.SetActive(false);
        MainPanel.SetActive(true);
    }

    public void LoadLevel1() { SceneManager.LoadScene("Level1"); }
    #endregion

    public void ShowOptions() { OptionsPanel.SetActive(true); }
    public void HideOptions() { OptionsPanel.SetActive(false); }

    public void ToggleBingus() { 
        if (BingusPanel.activeSelf)
            BingusPanel.SetActive(false);
        else
            BingusPanel.SetActive(true);
    }

    public void QuitGame() {
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #else
            Application.Quit();
        #endif
    }
}
