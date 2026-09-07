using UnityEngine;

/// <summary>
/// フィールドでEscapeキーを押すと開閉するポーズメニュー。タイトルに戻る/ゲーム終了を提供する
/// </summary>
public class CS_PauseMenu : MonoBehaviour
{
    [SerializeField]
    private GameObject _menuPanel;

    private void Start()
    {
        _menuPanel.SetActive(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu();
        }
    }

    private void TogglePauseMenu()
    {
        _menuPanel.SetActive(!_menuPanel.activeSelf);
    }

    public void OnReturnToTitleButtonClicked()
    {
        CS_GameManager.Instance.SaveGame();
        CS_SceneManager.Instance.LoadScene("TitleScene");
    }

    public void OnQuitButtonClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
