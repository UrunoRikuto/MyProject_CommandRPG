using System;
using System.Collections.Generic;
using UnityEngine;

public class CS_CommandButtonInput : MonoBehaviour
{
    [SerializeField] private CS_SkillSelectWindow _skillSelectWindow;

    private IReadOnlyList<CSO_SkillData> _availableSkills;

    public event Action<IBattleCommand> onCommandDecided;

    private void Awake()
    {
        _skillSelectWindow.onSkillSelected += HandleSkillSelected;
    }

    // CS_BattleStateCommandInput‚ÌEnter‚©‚çŒÄ‚ñ‚Å‚à‚ç‚¤‘z’è
    public void SetAvailableSkills(IReadOnlyList<CSO_SkillData> skills)
    {
        _availableSkills = skills;
    }

    public void OnAttackButtonClicked()
    {
        onCommandDecided?.Invoke(new CS_AttackCommand());
    }

    public void OnSkillButtonClicked()
    {
        _skillSelectWindow.Open(_availableSkills);
    }

    public void OnEscapeButtonClicked()
    {
        onCommandDecided?.Invoke(new CS_EscapeCommand());
    }

    private void HandleSkillSelected(int skillIndex)
    {
        onCommandDecided?.Invoke(new CS_SkillCommand(skillIndex));
    }
}