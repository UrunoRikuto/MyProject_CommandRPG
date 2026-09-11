using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 属性1つ分の耐性倍率。1=等倍、1より大きい=弱点、1より小さい=耐性、0=無効
/// </summary>
[System.Serializable]
public class CSE_ElementResistance
{
    public CSE_ElementType element;
    public float multiplier = 1f;
}

/// <summary>
/// ドロップ品1件分。itemIdはCS_ItemDatabaseで解決するアイテム/装備のID、dropChanceは0〜1
/// </summary>
[System.Serializable]
public class CSE_DropEntry
{
    public string itemId;
    [Range(0f, 1f)] public float dropChance = 1f;
}

[CreateAssetMenu(fileName = "DB_", menuName = "Scriptable Objects/DB_CharacterData")]
public class CSO_CharacterData : ScriptableObject
{
    [Header("名前")]
    [SerializeField]
    private string _characterName;
    public string characterName => _characterName;

    [Header("アイコン")]
    [SerializeField]
    private Sprite _characterIcon;
    public Sprite characterIcon => _characterIcon;

    [Header("基礎体力")]
    [SerializeField]
    private int _baseHealth;
    public int baseHealth => _baseHealth;

    [Header("基礎MP")]
    [SerializeField]
    private int _baseMP;
    public int baseMP => _baseMP;

    [Header("基礎攻撃力")]
    [SerializeField]
    private int _baseAttack;
    public int baseAttack => _baseAttack;

    [Header("基礎防御力")]
    [SerializeField]
    private int _baseDefense;
    public int baseDefense => _baseDefense;

    [Header("基礎速度")]
    [SerializeField]
    private int _baseSpeed;
    public int baseSpeed => _baseSpeed;

    [Header("レベルアップ時の体力上昇値")]
    [SerializeField]
    private int _healthGrowth;
    public int healthGrowth => _healthGrowth;

    [Header("レベルアップ時のMP上昇値")]
    [SerializeField]
    private int _mpGrowth;
    public int mpGrowth => _mpGrowth;

    [Header("レベルアップ時の攻撃力上昇値")]
    [SerializeField]
    private int _attackGrowth;
    public int attackGrowth => _attackGrowth;

    [Header("レベルアップ時の防御力上昇値")]
    [SerializeField]
    private int _defenseGrowth;
    public int defenseGrowth => _defenseGrowth;

    [Header("レベルアップ時の速度上昇値")]
    [SerializeField]
    private int _speedGrowth;
    public int speedGrowth => _speedGrowth;

    [Header("初期から所持しているスキルリスト")]
    [SerializeField]
    private List<CSO_SkillData> _initialSkills;
    public IReadOnlyList<CSO_SkillData> initialSkills => _initialSkills;

    [Header("AIが「たたかう」を選ぶ重み")]
    [SerializeField] private float _attackWeight = 1f;
    public float attackWeight => _attackWeight;
    [Header("AIが各スキルを選ぶ重み")]
    [SerializeField] private List<float> _skillWeights;
    public IReadOnlyList<float> skillWeights => _skillWeights;

    [Header("撃破時に味方が得られる経験値")]
    [SerializeField] private int _expReward;
    public int expReward => _expReward;

    [Header("1ターンごとに回復するMP量")]
    [SerializeField] private int _mpRegenPerTurn;
    public int mpRegenPerTurn => _mpRegenPerTurn;

    [Header("属性耐性(リストに無い属性は等倍)")]
    [SerializeField] private List<CSE_ElementResistance> _elementResistances = new List<CSE_ElementResistance>();

    [Header("撃破時にドロップする可能性のあるアイテム")]
    [SerializeField] private List<CSE_DropEntry> _itemDrops = new List<CSE_DropEntry>();
    public IReadOnlyList<CSE_DropEntry> itemDrops => _itemDrops;

    [Header("撃破時にドロップする可能性のある装備")]
    [SerializeField] private List<CSE_DropEntry> _equipmentDrops = new List<CSE_DropEntry>();
    public IReadOnlyList<CSE_DropEntry> equipmentDrops => _equipmentDrops;

    /// <summary>
    /// 指定した属性のダメージ倍率を返す。設定が無ければ等倍(1)
    /// </summary>
    public float GetElementMultiplier(CSE_ElementType element)
    {
        foreach (var resistance in _elementResistances)
        {
            if (resistance.element == element) return resistance.multiplier;
        }
        return 1f;
    }

    void OnValidate()
    {
        // スキルの数と重みの数が一致するように調整
        if (_skillWeights.Count != _initialSkills.Count)
        {
            int diff = _initialSkills.Count - _skillWeights.Count;
            if (diff > 0)
            {
                for (int i = 0; i < diff; i++)
                {
                    _skillWeights.Add(0.0f); // デフォルトの重みを追加
                }
            }
            else
            {
                _skillWeights.RemoveRange(_skillWeights.Count + diff, -diff); // 余分な重みを削除
            }
        }
    }
}
