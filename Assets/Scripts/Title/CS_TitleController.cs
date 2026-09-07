using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// タイトル画面のボタン操作。はじめから/つづきからでFieldSceneへ遷移する
/// </summary>
public class CS_TitleController : MonoBehaviour
{
    [SerializeField]
    private Button _continueButton;

    private void Start()
    {
        _continueButton.interactable = CS_GameManager.Instance.HasSaveData();
    }

    public void OnNewGameButtonClicked()
    {
        CS_GameManager.Instance.StartNewGame();
        CS_SceneManager.Instance.LoadScene("FieldScene");
    }

    public void OnContinueButtonClicked()
    {
        CS_SceneManager.Instance.LoadScene("FieldScene");
    }
}
