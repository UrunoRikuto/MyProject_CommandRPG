using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// どうぐ選択ウィンドウ。CS_SkillSelectWindowと同じ骨格。
/// countが負の値の場合は無制限(デバッグモード)として「∞」を表示する
/// </summary>
public class CS_ItemSelectWindow : MonoBehaviour
{
    [SerializeField] private Button _itemButtonPrefab;
    [SerializeField] private Transform _buttonParent;
    [SerializeField] private ScrollRect _scrollRect;

    public event Action<CSO_ItemData> onItemSelected;
    public event Action onCancelled;

    public void Open(IReadOnlyList<(CSO_ItemData item, int count)> items)
    {
        ClearButtons();
        gameObject.SetActive(true);

        foreach (var entry in items)
        {
            CSO_ItemData capturedItem = entry.item;
            string countLabel = entry.count < 0 ? "∞" : entry.count.ToString();
            Button button = Instantiate(_itemButtonPrefab, _buttonParent);
            button.GetComponentInChildren<TextMeshProUGUI>().text = capturedItem.itemName + $" (x{countLabel})";
            button.onClick.AddListener(() => Select(capturedItem));
        }

        // ここでレイアウトの再計算
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_buttonParent.GetComponent<RectTransform>());

        // 確定した高さを元にスクロール位置を設定する
        _scrollRect.verticalNormalizedPosition = 1f;
    }

    private void Select(CSO_ItemData item)
    {
        onItemSelected?.Invoke(item);
        Close();
    }

    /// <summary>
    /// 「戻る」ボタン用。何も選ばずにコマンド選択へ戻る
    /// </summary>
    public void OnCancelClicked()
    {
        onCancelled?.Invoke();
        Close();
    }

    private void Close()
    {
        ClearButtons();
        gameObject.SetActive(false);
    }

    private void ClearButtons()
    {
        // Destroyはフレーム末まで実際には削除されないため、レイアウト計算に混ざらないよう先に非表示にしておく
        foreach (Transform child in _buttonParent)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }
}
