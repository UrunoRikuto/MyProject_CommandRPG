using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 装備の変更候補選択ウィンドウ。CS_SkillSelectWindowと同じ骨格。
/// 先頭に「外す」の選択肢を表示し、選ぶとnullを通知する
/// </summary>
public class CS_EquipmentPickWindow : MonoBehaviour
{
    [SerializeField] private Button _equipmentButtonPrefab;
    [SerializeField] private Transform _buttonParent;
    [SerializeField] private ScrollRect _scrollRect;

    public event Action<CSO_EquipmentData> onEquipmentSelected;
    public event Action onCancelled;

    public void Open(IReadOnlyList<CSO_EquipmentData> candidates)
    {
        ClearButtons();
        gameObject.SetActive(true);

        // 日本語グリフを持つTMP Font Assetが無いため、動的テキストは既存の慣例に合わせて英語表記にする
        Button unequipButton = Instantiate(_equipmentButtonPrefab, _buttonParent);
        unequipButton.GetComponentInChildren<TextMeshProUGUI>().text = "(Unequip)";
        unequipButton.onClick.AddListener(() => Select(null));

        foreach (var candidate in candidates)
        {
            CSO_EquipmentData captured = candidate;
            Button button = Instantiate(_equipmentButtonPrefab, _buttonParent);
            button.GetComponentInChildren<TextMeshProUGUI>().text = captured.equipmentName;
            button.onClick.AddListener(() => Select(captured));
        }

        // ここでレイアウトの再計算
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_buttonParent.GetComponent<RectTransform>());

        // 確定した高さを元にスクロール位置を設定する
        _scrollRect.verticalNormalizedPosition = 1f;
    }

    private void Select(CSO_EquipmentData equipment)
    {
        onEquipmentSelected?.Invoke(equipment);
        Close();
    }

    /// <summary>
    /// 「戻る」ボタン用。何も選ばずに装備画面へ戻る
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
