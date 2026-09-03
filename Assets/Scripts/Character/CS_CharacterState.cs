using System.Collections.Generic;
using UnityEngine;

public class CS_CharacterState
{
    // キャラクターの基礎データ
    private CSO_CharacterData _characterData;

    // キャラクターが死亡しているかどうか
    public bool isDead => _currentHealth <= 0;

    // キャラクター名
    public string characterName => _characterData.characterName;

    // 最大体力
    private int _maxHealth;
    public int maxHealth => _maxHealth;
    // 現在の体力
    private int _currentHealth;
    public int currentHealth => _currentHealth;

    // 最大MP
    private int _maxMP;
    public int maxMP => _maxMP;
    // 現在のMP
    private int _currentMP;
    public int currentMP => _currentMP;

    // 現在の攻撃力
    private int _currentAttack;
    public int currentAttack => _currentAttack;

    // 現在の防御力
    private int _currentDefense;
    public int currentDefense => _currentDefense;

    // 現在の速度
    private int _currentSpeed;
    public int currentSpeed => _currentSpeed;

    // 現在のスキルリスト
    private List<CSO_SkillData> _currentSkills;
    public IReadOnlyList<CSO_SkillData> currentSkills => _currentSkills;

    public CS_CharacterState(CSO_CharacterData data)
    {
        _characterData = data;

        // 体力の初期化
        _maxHealth = _characterData.baseHealth;
        _currentHealth = _maxHealth;

        // MPの初期化
        _maxMP = _characterData.baseMP;
        _currentMP = _maxMP;

        // 攻撃力の初期化
        _currentAttack = _characterData.baseAttack;

        // 防御力の初期化
        _currentDefense = _characterData.baseDefense;

        // 速度の初期化
        _currentSpeed = _characterData.baseSpeed;

        // スキルリストの初期化
        _currentSkills = new List<CSO_SkillData>(_characterData.initialSkills);
    }

    /// <summary>
    /// ダメージを受ける処理
    /// </summary>
    /// <param name="damage">受けるダメージ量</param>
    public void TakeDamage(int damage)
    {
        // 防御力を考慮した実際のダメージ量を計算
        int effectiveDamage = Mathf.Max(damage - _currentDefense, 1);

        // 乱数を加えてダメージ量を変動させる（例: ±10%の範囲で変動）
        float randomFactor = Random.Range(0.9f, 1.1f);
        effectiveDamage = Mathf.RoundToInt(effectiveDamage * randomFactor);

        // 現在の体力を減少させる
        _currentHealth = Mathf.Max(_currentHealth - effectiveDamage, 0);
    }

    /// <summary>
    /// MPを消費する処理
    /// </summary>
    /// <param name="amount">消費するMP量</param>
    /// <returns>コストを消費できたかどうか</returns>
    public bool TryUseMP(int amount)
    {
        // MPが足りる場合のみ消費する
        if (_currentMP >= amount)
        {
            _currentMP -= amount;
            return true;
        }

        return false;
    }
}
