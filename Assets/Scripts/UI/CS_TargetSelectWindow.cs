using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CS_TargetSelectWindow : MonoBehaviour
{
    [SerializeField] private Button _targetButtonPrefab;
    [SerializeField] private Transform _buttonParent;
    [SerializeField] private ScrollRect _scrollRect;

    public event Action<CS_CharacterState> onTargetSelected;

    public void Open(IReadOnlyList<CS_CharacterState> candidates)
    {
        ClearButtons();
        gameObject.SetActive(true);

        foreach (var candidate in candidates)
        {
            if (candidate.isDead) continue; // ê∂ë∂ÇµÇƒÇ¢ÇÈìGÇæÇØï\é¶

            CS_CharacterState capturedTarget = candidate;
            Button button = Instantiate(_targetButtonPrefab, _buttonParent);
            button.GetComponentInChildren<TextMeshProUGUI>().text = candidate.characterName;
            button.onClick.AddListener(() => Select(capturedTarget));
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(_buttonParent.GetComponent<RectTransform>());
        _scrollRect.verticalNormalizedPosition = 1f;
    }

    private void Select(CS_CharacterState target)
    {
        onTargetSelected?.Invoke(target);
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