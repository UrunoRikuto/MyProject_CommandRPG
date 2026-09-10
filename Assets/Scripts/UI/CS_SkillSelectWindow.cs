using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CS_SkillSelectWindow : MonoBehaviour
{
    [SerializeField] private Button _skillButtonPrefab;
    [SerializeField] private Transform _buttonParent;
    [SerializeField] private ScrollRect _scrollRect;

    public event Action<int> onSkillSelected;
    public event Action onCancelled;

    public void Open(IReadOnlyList<CSO_SkillData> skills)
    {
        ClearButtons();
        gameObject.SetActive(true);

        for (int i = 0; i < skills.Count; i++)
        {
            int capturedIndex = i;
            Button button = Instantiate(_skillButtonPrefab, _buttonParent);
            button.GetComponentInChildren<TextMeshProUGUI>().text = skills[i].skillName + $" (Cost: {skills[i].cost})";
            button.onClick.AddListener(() => Select(capturedIndex));
        }

        // ここでレイアウトの再計算
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_buttonParent.GetComponent<RectTransform>());

        // 確定した高さを元にスクロール位置を設定する
        _scrollRect.verticalNormalizedPosition = 1f;
    }

    private void Select(int index)
    {
        onSkillSelected?.Invoke(index);
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