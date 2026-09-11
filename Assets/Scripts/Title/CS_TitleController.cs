using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// タイトル画面のボタン操作。はじめから/つづきからでFieldSceneへ遷移する
/// </summary>
public class CS_TitleController : MonoBehaviour
{
    // このキーを押しながら「はじめから」を押すとデバッグモードで開始する(全装備/全アイテムが無制限に使える)
    private const KeyCode DEBUG_MODE_KEY = KeyCode.LeftShift;

    [SerializeField]
    private Button _continueButton;

    private void Start()
    {
        _continueButton.interactable = CS_GameManager.Instance.HasSaveData();
    }

    public void OnNewGameButtonClicked()
    {
        CS_GameManager.Instance.StartNewGame();
        CS_GameManager.Instance.SetDebugMode(Input.GetKey(DEBUG_MODE_KEY));
        CS_SceneManager.Instance.LoadScene("FieldScene");
    }

    public void OnContinueButtonClicked()
    {
        CS_SceneManager.Instance.LoadScene("FieldScene");
    }
}
