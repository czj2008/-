using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class Menu : MonoBehaviour
{
    public void GameStart()
    {
        SceneManager.LoadScene("Level Select");
    }

    public void LevelSelect()
    {
        SceneManager.LoadScene("SampleScene");
    }

    public void LevelSelect2()
    {
        SceneManager.LoadScene("Level2");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}

public class LevelSelectManager : MonoBehaviour
{

    public GameObject LevelSelectPanel;
    Button[] LevelSelectButtons;
    int unlockedLevelIndex;
    
    void Start()
    {
        LevelSelectButtons = new Button[LevelSelectPanel.transform.childCount];
        unlockedLevelIndex = PlayerPrefs.GetInt("unlockedLevelIndex");

        for (int i = 0; i < LevelSelectPanel.transform.childCount; i++)
        {
            LevelSelectButtons[i] = LevelSelectPanel.transform.GetChild(i).GetComponent<Button>();
        }
    
        for (int i = 0; i < LevelSelectButtons.Length; i++)
        {           
            LevelSelectButtons[i].interactable = false;            
        }

        for (int i = 0; i < unlockedLevelIndex + 1; i++)
        {
            LevelSelectButtons[i].interactable = true;
        }


    }                       

}
