using TMPro;
using UnityEngine;

/// <summary>
/// 「Fキーで〜」のような入力待ち状態を示す画面下部のプロンプト表示。
/// 泉・クエスト板・出入口など、近づいてFキーで起動する各オブジェクトから共通で呼び出す
/// </summary>
public class CS_InteractionPrompt : MonoBehaviour
{
    [SerializeField] private GameObject _promptPanel;
    [SerializeField] private TextMeshProUGUI _promptText;

    private void Start()
    {
        _promptPanel.SetActive(false);
    }

    public void Show(string message)
    {
        _promptText.text = message;
        _promptPanel.SetActive(true);
    }

    public void Hide()
    {
        _promptPanel.SetActive(false);
    }
}
