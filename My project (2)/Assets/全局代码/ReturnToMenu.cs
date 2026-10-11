using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToMenu : MonoBehaviour
{
    [Header("场景名（要和你 Build Settings 里的名字一致）")]
    public string MenuSceneName = "Menu";

    [Header("返回前要不要保存进度")]
    [Tooltip("勾上：先存档再回主菜单（战斗中途返回也不丢金币/牌组）")]
    public bool SaveBeforeReturn = true;

    /// <summary>点按钮时调用。</summary>
    public void GoToMenu()
    {
        // ★ 关键1：把时间恢复正常
        // 如果设置界面还开着（Time.timeScale = 0），切场景后新场景会一直卡住不动
        Time.timeScale = 1f;

        // ★ 关键2：先存档（可选）
        if (SaveBeforeReturn)
        {
            RunState run = FindObjectOfType<RunState>();
            if (run != null)
            {
                run.Save();
                Debug.Log("[返回主菜单] 已保存进度，金币 " + run.Gold);
            }
            else
            {
                Debug.LogWarning("[返回主菜单] 场景里没找到 RunState，跳过存档");
            }
        }

        Debug.Log("[返回主菜单] 加载场景：" + MenuSceneName);
        SceneManager.LoadScene(MenuSceneName);
    }
}
