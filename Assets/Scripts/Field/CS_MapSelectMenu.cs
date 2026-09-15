using UnityEngine;

/// <summary>
/// 町の出入口(CS_TownGate)から開くマップ選択メニュー。CS_PauseMenuと同じ開閉パターン
/// </summary>
public class CS_MapSelectMenu : MonoBehaviour
{
    [SerializeField]
    private GameObject _menuPanel;

    private CS_PlayerMove _playerMove;

    private void Start()
    {
        _menuPanel.SetActive(false);
        _playerMove = GameObject.FindAnyObjectByType<CS_PlayerMove>();
    }

    public void Open()
    {
        _menuPanel.SetActive(true);
        if (_playerMove != null)
        {
            _playerMove.enabled = false;
        }
    }

    public void Close()
    {
        _menuPanel.SetActive(false);
        if (_playerMove != null)
        {
            _playerMove.enabled = true;
        }
    }

    private void GoToField(string sceneName)
    {
        CS_GameManager.Instance.SaveGame();
        CS_SceneManager.Instance.LoadScene(sceneName);
    }

    public void OnSelectGrasslandClicked() => GoToField("FieldScene_Grassland");
    public void OnSelectDesertClicked() => GoToField("FieldScene_Desert");
    public void OnSelectWetlandsClicked() => GoToField("FieldScene_Wetlands");
    public void OnSelectSnowClicked() => GoToField("FieldScene_Snow");

    public void OnCancelClicked()
    {
        Close();
    }
}
