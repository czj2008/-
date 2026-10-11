using UnityEngine;
using UnityEngine.SceneManagement;

public class StartMenu : MonoBehaviour
{
    [Header("场景名（必须和场景文件名完全一致）")]
    [Tooltip("战斗场景的名字")]
    public string BattleSceneName = "Battle";

    [Header("新游戏的初始设置")]
    public int StartingGold = 50;
    public int PlayerMaxHealth = 75;

    private void Start()
    {
        // 应用上次保存的分辨率
        SettingsPanel.ApplySavedResolution();

        Debug.Log("[开始界面] 就绪");
    }

    /// <summary>点「新游戏」：清空存档 → 进战斗场景。</summary>
    public void StartNewGame()
    {
        // 清掉旧的进度（就像从头开始玩）
        PlayerPrefs.DeleteKey("run.cleared");
        PlayerPrefs.DeleteKey("run.unlocked");
        PlayerPrefs.DeleteKey("run.gold");
        PlayerPrefs.DeleteKey("run.hp");
        PlayerPrefs.DeleteKey("run.maxHp");
        PlayerPrefs.DeleteKey("run.deck");
        PlayerPrefs.Save();

        Debug.Log("[开始界面] 新游戏，存档已清空");

        SceneManager.LoadScene(BattleSceneName);
    }

    /// <summary>点「继续游戏」：直接进战斗场景（GameFlow 会自动读取存档）。</summary>
    public void ContinueGame()
    {
        Debug.Log("[开始界面] 继续游戏");
        SceneManager.LoadScene(BattleSceneName);
    }

    /// <summary>点「退出游戏」。（编辑器里停止播放，打包后关闭程序）</summary>
    public void QuitGame()
    {
        Debug.Log("[开始界面] 退出游戏");
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
