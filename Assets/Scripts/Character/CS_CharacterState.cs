using System.Collections.Generic;
using UnityEngine;

public class CS_CharacterState
{
    public const int MAX_LEVEL = 100;

    // キャラクターの基礎データ
    private CSO_CharacterData _characterData;

    // キャラクターが死亡しているかどうか
    public bool isDead => _currentHealth <= 0;

    // キャラクター名
    public string characterName => _characterData.characterName;

    // アイコン
    public Sprite characterIcon => _characterData.characterIcon;

    // 現在のレベル
    private int _level;
    public int level => _level;

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

    public float attackWeight => _characterData.attackWeight;
    public IReadOnlyList<float> skillWeights => _characterData.skillWeights;

    public CS_CharacterState(CSO_CharacterData data, int level = 1)
    {
        _characterData = data;
        _level = Mathf.Clamp(level, 1, MAX_LEVEL);

        // レベル1を基準に、レベルごとの上昇値を(レベル-1)回分加算する(1レベルごとの上昇値は固定)
        int levelBonus = _level - 1;

        // 体力の初期化
        _maxHealth = _characterData.baseHealth + _characterData.healthGrowth * levelBonus;
        _currentHealth = _maxHealth;

        // MPの初期化
        _maxMP = _characterData.baseMP + _characterData.mpGrowth * levelBonus;
        _currentMP = _maxMP;

        // 攻撃力の初期化
        _currentAttack = _characterData.baseAttack + _characterData.attackGrowth * levelBonus;

        // 防御力の初期化
        _currentDefense = _characterData.baseDefense + _characterData.defenseGrowth * levelBonus;

        // 速度の初期化
        _currentSpeed = _characterData.baseSpeed + _characterData.speedGrowth * levelBonus;

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

        // ランダム性を加えてダメージ量を変動させる(例: ±10%の範囲で変動)
        float randomFactor = Random.Range(0.9f, 1.1f);
        effectiveDamage = Mathf.RoundToInt(effectiveDamage * randomFactor);

        // 現在の体力を減少させる
        _currentHealth = Mathf.Max(_currentHealth - effectiveDamage, 0);
    }

    /// <summary>
    /// MPを消費する処理
    /// </summary>
    /// <param name="amount">消費するMP量</param>
    /// <returns>コストを払えたかどうか</returns>
    public bool TryUseMP(int amount)
    {
        // MPが足りる場合のみ消費
        if (_currentMP >= amount)
        {
            _currentMP -= amount;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 保存されていた現在HP/MPを反映する(セーブデータ復元用)
    /// </summary>
    public void SetCurrentStats(int health, int mp)
    {
        _currentHealth = Mathf.Clamp(health, 0, _maxHealth);
        _currentMP = Mathf.Clamp(mp, 0, _maxMP);
    }
}
