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

    public void Open(IReadOnlyList<CSO_SkillData> skills)
    {
        ClearButtons();
        gameObject.SetActive(true);

        for (int i = 0; i < skills.Count; i++)
        {
            int capturedIndex = i;
            Button button = Instantiate(_skillButtonPrefab, _buttonParent);
            button.GetComponentInChildren<TextMeshProUGUI>().text = skills[i].skillName;
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

    private void Close()
    {
        ClearButtons();
        gameObject.SetActive(false);
    }

    private void ClearButtons()
    {
        foreach (Transform child in _buttonParent) Destroy(child.gameObject);
    }
}