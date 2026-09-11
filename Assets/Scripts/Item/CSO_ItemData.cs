using UnityEngine;

[CreateAssetMenu(fileName = "DB_", menuName = "Scriptable Objects/DB_ItemData")]
public class CSO_ItemData : ScriptableObject
{
    [Header("セーブデータ・実行時解決用の一意キー")]
    [SerializeField] private string _itemId;
    public string itemId => _itemId;

    [Header("名前")]
    [SerializeField] private string _itemName;
    public string itemName => _itemName;

    [Header("アイコン")]
    [SerializeField] private Sprite _icon;
    public Sprite icon => _icon;

    [Header("対象(味方/敵どちらを対象選択に表示するか)")]
    [SerializeField] private CSE_ItemTargetSide _targetSide;
    public CSE_ItemTargetSide targetSide => _targetSide;

    [Header("効果種別")]
    [SerializeField] private CSE_ItemEffectType _effectType;
    public CSE_ItemEffectType effectType => _effectType;

    [Header("効果量")]
    [SerializeField] private int _power;
    public int power => _power;

    [Header("属性(Damageのみ使用)")]
    [SerializeField] private CSE_ElementType _element = CSE_ElementType.None;
    public CSE_ElementType element => _element;
}
