using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DB_", menuName = "Scriptable Objects/DB_EquipmentData")]
public class CSO_EquipmentData : ScriptableObject
{
    [Header("セーブデータ・実行時解決用の一意キー")]
    [SerializeField] private string _equipmentId;
    public string equipmentId => _equipmentId;

    [Header("名前")]
    [SerializeField] private string _equipmentName;
    public string equipmentName => _equipmentName;

    [Header("アイコン")]
    [SerializeField] private Sprite _icon;
    public Sprite icon => _icon;

    [Header("スロット種別")]
    [SerializeField] private CSE_EquipmentSlot _slotType;
    public CSE_EquipmentSlot slotType => _slotType;

    [Header("体力ボーナス")]
    [SerializeField] private int _healthBonus;
    public int healthBonus => _healthBonus;

    [Header("MPボーナス")]
    [SerializeField] private int _mpBonus;
    public int mpBonus => _mpBonus;

    [Header("攻撃力ボーナス")]
    [SerializeField] private int _attackBonus;
    public int attackBonus => _attackBonus;

    [Header("防御力ボーナス")]
    [SerializeField] private int _defenseBonus;
    public int defenseBonus => _defenseBonus;

    [Header("速度ボーナス")]
    [SerializeField] private int _speedBonus;
    public int speedBonus => _speedBonus;

    [Header("属性耐性補正(キャラ本体の倍率に乗算でスタックする)")]
    [SerializeField] private List<CSE_ElementResistance> _elementResistances = new List<CSE_ElementResistance>();

    [Header("たたかうの属性を上書きする(None=上書きしない。毒武器などに使用)")]
    [SerializeField] private CSE_ElementType _attackElementOverride = CSE_ElementType.None;
    public CSE_ElementType attackElementOverride => _attackElementOverride;

    /// <summary>
    /// この装備自身が持つ属性補正倍率を返す。設定が無ければ等倍(1)
    /// </summary>
    public float GetElementMultiplier(CSE_ElementType element)
    {
        foreach (var resistance in _elementResistances)
        {
            if (resistance.element == element) return resistance.multiplier;
        }
        return 1f;
    }
}
