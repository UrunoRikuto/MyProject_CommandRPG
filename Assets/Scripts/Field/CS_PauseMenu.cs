using UnityEngine;

/// <summary>
/// フィールドでEscapeキーを押すと開閉するポーズメニュー。タイトルに戻る/ゲーム終了を提供する
/// </summary>
public class CS_PauseMenu : MonoBehaviour
{
    [SerializeField]
    private GameObject _menuPanel;

    private CS_PlayerMove _playerMove;

    private void Start()
    {
        _menuPanel.SetActive(false);
        _playerMove = GameObject.FindAnyObjectByType<CS_PlayerMove>();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePauseMenu();
        }
    }

    /// <summary>
    /// メニューの開閉に合わせてプレイヤーの移動も止める(開いている間に歩いてエンカウントしてしまうのを防ぐ)
    /// </summary>
    private void TogglePauseMenu()
    {
        bool isOpening = !_menuPanel.activeSelf;
        _menuPanel.SetActive(isOpening);

        if (_playerMove != null)
        {
            _playerMove.enabled = !isOpening;
        }
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
