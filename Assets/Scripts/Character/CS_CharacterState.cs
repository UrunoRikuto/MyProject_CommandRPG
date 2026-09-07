using System.Collections.Generic;
using UnityEngine;

public class CS_CharacterState
{
    // �L�����N�^�[�̊�b�f�[�^
    private CSO_CharacterData _characterData;

    // �L�����N�^�[�����S���Ă��邩�ǂ���
    public bool isDead => _currentHealth <= 0;

    // �L�����N�^�[��
    public string characterName => _characterData.characterName;

    // �A�C�R��
    public Sprite characterIcon => _characterData.characterIcon;

    // �ő�̗�
    private int _maxHealth;
    public int maxHealth => _maxHealth;
    // ���݂̗̑�
    private int _currentHealth;
    public int currentHealth => _currentHealth;

    // �ő�MP
    private int _maxMP;
    public int maxMP => _maxMP;
    // ���݂�MP
    private int _currentMP;
    public int currentMP => _currentMP;

    // ���݂̍U����
    private int _currentAttack;
    public int currentAttack => _currentAttack;

    // ���݂̖h���
    private int _currentDefense;
    public int currentDefense => _currentDefense;

    // ���݂̑��x
    private int _currentSpeed;
    public int currentSpeed => _currentSpeed;

    // ���݂̃X�L�����X�g
    private List<CSO_SkillData> _currentSkills;
    public IReadOnlyList<CSO_SkillData> currentSkills => _currentSkills;

    // CS_CharacterState.cs �ɒǉ�
    public float attackWeight => _characterData.attackWeight;
    public IReadOnlyList<float> skillWeights => _characterData.skillWeights;

    public CS_CharacterState(CSO_CharacterData data)
    {
        _characterData = data;

        // �̗͂̏�����
        _maxHealth = _characterData.baseHealth;
        _currentHealth = _maxHealth;

        // MP�̏�����
        _maxMP = _characterData.baseMP;
        _currentMP = _maxMP;

        // �U���͂̏�����
        _currentAttack = _characterData.baseAttack;

        // �h��͂̏�����
        _currentDefense = _characterData.baseDefense;

        // ���x�̏�����
        _currentSpeed = _characterData.baseSpeed;

        // �X�L�����X�g�̏�����
        _currentSkills = new List<CSO_SkillData>(_characterData.initialSkills);
    }

    /// <summary>
    /// �_���[�W���󂯂鏈��
    /// </summary>
    /// <param name="damage">�󂯂�_���[�W��</param>
    public void TakeDamage(int damage)
    {
        // �h��͂��l���������ۂ̃_���[�W�ʂ��v�Z
        int effectiveDamage = Mathf.Max(damage - _currentDefense, 1);

        // �����������ă_���[�W�ʂ�ϓ�������i��: �}10%�͈̔͂ŕϓ��j
        float randomFactor = Random.Range(0.9f, 1.1f);
        effectiveDamage = Mathf.RoundToInt(effectiveDamage * randomFactor);

        // ���݂̗̑͂�����������
        _currentHealth = Mathf.Max(_currentHealth - effectiveDamage, 0);
    }

    /// <summary>
    /// MP������鏈��
    /// </summary>
    /// <param name="amount">�����MP��</param>
    /// <returns>�R�X�g������ł������ǂ���</returns>
    public bool TryUseMP(int amount)
    {
        // MP�������ꍇ�̂ݏ����
        if (_currentMP >= amount)
        {
            _currentMP -= amount;
            return true;
        }

        return false;
    }
}
