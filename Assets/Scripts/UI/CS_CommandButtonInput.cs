using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class CS_CommandButtonInput : MonoBehaviour
{
    [SerializeField] private CS_SkillSelectWindow _skillSelectWindow;
    [SerializeField] private CS_TargetSelectWindow _targetSelectWindow;
    [SerializeField] private CS_ItemSelectWindow _itemSelectWindow;
    [SerializeField] private Button _skillButton;
    [SerializeField] private Button _itemButton;

    private CanvasGroup _canvasGroup;
    private IReadOnlyList<CSO_SkillData> _availableSkills;
    private IReadOnlyList<CS_CharacterState> _availableTargets;
    private IReadOnlyList<CS_CharacterState> _availableAllyTargets;
    private IReadOnlyList<(CSO_ItemData item, int count)> _availableItems;
    private IBattleCommand _pendingCommand;

    public event Action<IBattleCommand, CS_CharacterState> onCommandDecided;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _skillSelectWindow.onSkillSelected += HandleSkillSelected;
        _targetSelectWindow.onTargetSelected += HandleTargetSelected;
        _itemSelectWindow.onItemSelected += HandleItemSelected;
        _skillSelectWindow.onCancelled += HandleSelectionCancelled;
        _targetSelectWindow.onCancelled += HandleSelectionCancelled;
        _itemSelectWindow.onCancelled += HandleSelectionCancelled;
    }

    public void SetAvailableSkills(IReadOnlyList<CSO_SkillData> skills)
    {
        _availableSkills = skills;
        // 使えるスキルが1つもない場合は選択できてしまわないようボタン自体を非活性にする
        if (_skillButton != null)
        {
            _skillButton.interactable = skills != null && skills.Count > 0;
        }
    }

    public void SetAvailableTargets(IReadOnlyList<CS_CharacterState> targets) => _availableTargets = targets;

    public void SetAvailableAllyTargets(IReadOnlyList<CS_CharacterState> targets) => _availableAllyTargets = targets;

    public void SetAvailableItems(IReadOnlyList<(CSO_ItemData item, int count)> items)
    {
        _availableItems = items;
        // 使えるどうぐが1つもない場合は選択できてしまわないようボタン自体を非活性にする
        if (_itemButton != null)
        {
            _itemButton.interactable = items != null && items.Count > 0;
        }
    }

    public void Show()
    {
        gameObject.SetActive(true);
        SetInteractable(true);
    }

    public void Hide() => gameObject.SetActive(false);

    private void SetInteractable(bool value)
    {
        _canvasGroup.interactable = value;
        _canvasGroup.blocksRaycasts = value;
    }

    public void OnAttackButtonClicked()
    {
        _pendingCommand = new CS_AttackCommand();
        SetInteractable(false);
        _targetSelectWindow.Open(_availableTargets);
    }

    public void OnSkillButtonClicked()
    {
        SetInteractable(false);
        _skillSelectWindow.Open(_availableSkills);
    }

    public void OnEscapeButtonClicked()
    {
        onCommandDecided?.Invoke(new CS_EscapeCommand(), null);
    }

    public void OnItemButtonClicked()
    {
        SetInteractable(false);
        _itemSelectWindow.Open(_availableItems);
    }

    private void HandleSkillSelected(int skillIndex)
    {
        _pendingCommand = new CS_SkillCommand(skillIndex);
        _targetSelectWindow.Open(_availableTargets);
    }

    private void HandleItemSelected(CSO_ItemData item)
    {
        _pendingCommand = new CS_ItemCommand(item);
        // 回復・バフ系は味方、ダメージ・デバフ系は敵を対象選択に表示する
        var candidates = item.targetSide == CSE_ItemTargetSide.Ally ? _availableAllyTargets : _availableTargets;
        _targetSelectWindow.Open(candidates);
    }

    private void HandleTargetSelected(CS_CharacterState target)
    {
        onCommandDecided?.Invoke(_pendingCommand, target);
        _pendingCommand = null;
    }

    /// <summary>
    /// スキル/ターゲット選択のどちらで「戻る」が押されても、保留中のコマンドを破棄してコマンド選択に戻す
    /// </summary>
    private void HandleSelectionCancelled()
    {
        _pendingCommand = null;
        SetInteractable(true);
    }
}
