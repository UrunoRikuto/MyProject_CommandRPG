using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public class CS_CommandButtonInput : MonoBehaviour
{
    [SerializeField] private CS_SkillSelectWindow _skillSelectWindow;
    [SerializeField] private CS_TargetSelectWindow _targetSelectWindow;

    private CanvasGroup _canvasGroup;
    private IReadOnlyList<CSO_SkillData> _availableSkills;
    private IReadOnlyList<CS_CharacterState> _availableTargets;
    private IBattleCommand _pendingCommand;

    public event Action<IBattleCommand, CS_CharacterState> onCommandDecided;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _skillSelectWindow.onSkillSelected += HandleSkillSelected;
        _targetSelectWindow.onTargetSelected += HandleTargetSelected;
    }

    public void SetAvailableSkills(IReadOnlyList<CSO_SkillData> skills) => _availableSkills = skills;
    public void SetAvailableTargets(IReadOnlyList<CS_CharacterState> targets) => _availableTargets = targets;

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

    private void HandleSkillSelected(int skillIndex)
    {
        _pendingCommand = new CS_SkillCommand(skillIndex);
        _targetSelectWindow.Open(_availableTargets);
    }

    private void HandleTargetSelected(CS_CharacterState target)
    {
        onCommandDecided?.Invoke(_pendingCommand, target);
        _pendingCommand = null;
    }
}