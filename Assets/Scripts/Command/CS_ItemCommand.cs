using UnityEngine;

/// <summary>
/// どうぐの使用。効果種別(回復/MP回復/ダメージ)に応じて対象に適用し、
/// デバッグモード以外では所持数を1消費する
/// </summary>
public class CS_ItemCommand : IBattleCommand
{
    private readonly CSO_ItemData _itemData;
    public CSO_ItemData itemData => _itemData;

    public string commandName => "どうぐ";

    public CS_ItemCommand(CSO_ItemData itemData)
    {
        _itemData = itemData;
    }

    public void Execute(CS_BattleContext context, CS_CharacterState user, CS_CharacterState target)
    {
        switch (_itemData.effectType)
        {
            case CSE_ItemEffectType.Heal:
                target.Heal(_itemData.power);
                Debug.Log($"{user.characterName}は{_itemData.itemName}を使った！ {target.characterName}のHPが{_itemData.power}回復した！");
                break;
            case CSE_ItemEffectType.RestoreMP:
                target.RestoreMP(_itemData.power);
                Debug.Log($"{user.characterName}は{_itemData.itemName}を使った！ {target.characterName}のMPが{_itemData.power}回復した！");
                break;
            case CSE_ItemEffectType.Damage:
                target.TakeDamage(_itemData.power, _itemData.element);
                Debug.Log($"{user.characterName}は{_itemData.itemName}を使った！ {target.characterName}に{_itemData.power}のダメージを与えた！");
                break;
        }

        if (!CS_GameManager.Instance.isDebugMode)
        {
            CS_GameManager.Instance.TryConsumeItem(_itemData.itemId);
        }
    }
}
