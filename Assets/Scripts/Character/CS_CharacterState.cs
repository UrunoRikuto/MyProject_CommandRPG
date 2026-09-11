using System.Collections.Generic;
using UnityEngine;

public class CS_CharacterState
{
    public const int MAX_LEVEL = 100;

    // レベルアップに必要な経験値は「現在レベル × この値」(1レベルごとの必要量は固定幅で増える)
    private const int EXP_PER_LEVEL_STEP = 100;

    // キャラクターの基礎データ
    private CSO_CharacterData _characterData;

    // 装備中の装備品(敵は常に空リスト。味方のみCS_BattleStateMachineから渡される)
    private IReadOnlyList<CSO_EquipmentData> _equipment;

    // キャラクターが死亡しているかどうか
    public bool isDead => _currentHealth <= 0;

    // キャラクター名
    public string characterName => _characterData.characterName;

    // アイコン
    public Sprite characterIcon => _characterData.characterIcon;

    // 現在のレベル
    private int _level;
    public int level => _level;

    // 現在の経験値(次のレベルに達するとリセットされる)
    private int _currentExp;
    public int currentExp => _currentExp;
    // 次のレベルに必要な経験値(上限レベルに達している場合は0)
    public int expToNextLevel => _level >= MAX_LEVEL ? 0 : _level * EXP_PER_LEVEL_STEP;

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

    // 撃破された際に相手に与える経験値(レベルが高いほど多くなる。基礎値×現在レベル)
    public int expReward => _characterData.expReward * _level;

    // 撃破された際にドロップする可能性のあるアイテム/装備
    public IReadOnlyList<CSE_DropEntry> itemDrops => _characterData.itemDrops;
    public IReadOnlyList<CSE_DropEntry> equipmentDrops => _characterData.equipmentDrops;

    /// <summary>
    /// 指定した属性に対するこのキャラクターの耐性倍率(1=等倍)。装備の補正は乗算でスタックする
    /// </summary>
    public float GetElementMultiplier(CSE_ElementType element)
    {
        float multiplier = _characterData.GetElementMultiplier(element);
        foreach (var equipment in _equipment)
        {
            multiplier *= equipment.GetElementMultiplier(element);
        }
        return multiplier;
    }

    /// <summary>
    /// 「たたかう」で与える属性。武器のattackElementOverrideがNone以外ならそれを使う(毒武器など)
    /// </summary>
    public CSE_ElementType attackElement
    {
        get
        {
            foreach (var equipment in _equipment)
            {
                if (equipment.slotType == CSE_EquipmentSlot.Weapon && equipment.attackElementOverride != CSE_ElementType.None)
                {
                    return equipment.attackElementOverride;
                }
            }
            return CSE_ElementType.None;
        }
    }

    public CS_CharacterState(CSO_CharacterData data, int level = 1, IReadOnlyList<CSO_EquipmentData> equipment = null)
    {
        _characterData = data;
        _level = Mathf.Clamp(level, 1, MAX_LEVEL);
        _equipment = equipment ?? new List<CSO_EquipmentData>();

        RecalculateStats();
        _currentHealth = _maxHealth;
        _currentMP = _maxMP;

        // スキルリストの初期化
        _currentSkills = new List<CSO_SkillData>(_characterData.initialSkills);
    }

    /// <summary>
    /// レベルに応じた最大ステータスを算出する(基礎値+成長値×(レベル-1)、1レベルごとの上昇値は固定)。
    /// 装備中のボーナスもここで加算する
    /// </summary>
    private void RecalculateStats()
    {
        int levelBonus = _level - 1;

        _maxHealth = _characterData.baseHealth + _characterData.healthGrowth * levelBonus;
        _maxMP = _characterData.baseMP + _characterData.mpGrowth * levelBonus;
        _currentAttack = _characterData.baseAttack + _characterData.attackGrowth * levelBonus;
        _currentDefense = _characterData.baseDefense + _characterData.defenseGrowth * levelBonus;
        _currentSpeed = _characterData.baseSpeed + _characterData.speedGrowth * levelBonus;

        foreach (var equipment in _equipment)
        {
            _maxHealth += equipment.healthBonus;
            _maxMP += equipment.mpBonus;
            _currentAttack += equipment.attackBonus;
            _currentDefense += equipment.defenseBonus;
            _currentSpeed += equipment.speedBonus;
        }
    }

    /// <summary>
    /// ダメージを受ける処理。属性を指定すると自身の属性耐性が乗算される(Noneなら常に等倍)
    /// </summary>
    /// <param name="damage">受けるダメージ量</param>
    /// <param name="element">ダメージの属性</param>
    public void TakeDamage(int damage, CSE_ElementType element = CSE_ElementType.None)
    {
        // 防御力を考慮した実際のダメージ量を計算(最低1)
        int baseDamage = Mathf.Max(damage - _currentDefense, 1);

        // 属性耐性を乗算する(0倍なら完全無効化できる。装備の補正込み)
        float elementMultiplier = element == CSE_ElementType.None ? 1f : GetElementMultiplier(element);

        // ランダム性を加えてダメージ量を変動させる(例: ±10%の範囲で変動)
        float randomFactor = Random.Range(0.9f, 1.1f);

        int effectiveDamage = Mathf.Max(Mathf.RoundToInt(baseDamage * elementMultiplier * randomFactor), 0);

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
    /// 1ターン分のMP自然回復(キャラクターごとに設定した固定値、最大値まで)
    /// </summary>
    public void RegenMP()
    {
        _currentMP = Mathf.Min(_maxMP, _currentMP + _characterData.mpRegenPerTurn);
    }

    /// <summary>
    /// 経験値を獲得し、必要量を満たしていれば(上限レベルまで)連続してレベルアップする。
    /// 敵はこのメソッドを呼ばないため、レベルが上がるのは味方のみ
    /// </summary>
    public void GainExp(int amount)
    {
        if (_level >= MAX_LEVEL) return;

        _currentExp += amount;
        while (_level < MAX_LEVEL && _currentExp >= expToNextLevel)
        {
            _currentExp -= expToNextLevel;
            LevelUp();
        }
    }

    /// <summary>
    /// レベルを1上げてステータスを再計算する。最大値の増加分だけ現在値も引き上げる
    /// (レベルアップで相対的に弱くならないようにするため)
    /// </summary>
    private void LevelUp()
    {
        int previousMaxHealth = _maxHealth;
        int previousMaxMP = _maxMP;

        _level++;
        RecalculateStats();

        _currentHealth += _maxHealth - previousMaxHealth;
        _currentMP += _maxMP - previousMaxMP;
    }

    /// <summary>
    /// 保存されていた現在HP/MPを反映する(セーブデータ復元用)
    /// </summary>
    public void SetCurrentStats(int health, int mp)
    {
        _currentHealth = Mathf.Clamp(health, 0, _maxHealth);
        _currentMP = Mathf.Clamp(mp, 0, _maxMP);
    }

    /// <summary>
    /// 保存されていた経験値を反映する(セーブデータ復元用)
    /// </summary>
    public void SetExp(int exp)
    {
        _currentExp = Mathf.Max(0, exp);
    }

    /// <summary>
    /// 体力・MPを最大値まで回復する(敗北時の全回復など)
    /// </summary>
    public void FullHeal()
    {
        _currentHealth = _maxHealth;
        _currentMP = _maxMP;
    }

    /// <summary>
    /// 体力を指定量回復する(最大値でクランプ。回復アイテム用)
    /// </summary>
    public void Heal(int amount)
    {
        _currentHealth = Mathf.Min(_maxHealth, _currentHealth + amount);
    }

    /// <summary>
    /// MPを指定量回復する(最大値でクランプ。MP回復アイテム用)
    /// </summary>
    public void RestoreMP(int amount)
    {
        _currentMP = Mathf.Min(_maxMP, _currentMP + amount);
    }
}
