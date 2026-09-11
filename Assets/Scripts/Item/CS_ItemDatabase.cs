using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Resources配下のCSO_ItemData/CSO_EquipmentDataをID文字列から引けるようにする静的テーブル。
/// セーブデータはScriptableObject参照を直接持てない(JsonUtility)ため、IDで永続化しここで解決する
/// </summary>
public static class CS_ItemDatabase
{
    private static Dictionary<string, CSO_ItemData> _items;
    private static Dictionary<string, CSO_EquipmentData> _equipment;

    public static CSO_ItemData GetItem(string id)
    {
        EnsureLoaded();
        if (string.IsNullOrEmpty(id)) return null;
        return _items.TryGetValue(id, out var item) ? item : null;
    }

    public static CSO_EquipmentData GetEquipment(string id)
    {
        EnsureLoaded();
        if (string.IsNullOrEmpty(id)) return null;
        return _equipment.TryGetValue(id, out var equipment) ? equipment : null;
    }

    public static IReadOnlyList<CSO_ItemData> AllItems
    {
        get
        {
            EnsureLoaded();
            return new List<CSO_ItemData>(_items.Values);
        }
    }

    public static IReadOnlyList<CSO_EquipmentData> AllEquipment
    {
        get
        {
            EnsureLoaded();
            return new List<CSO_EquipmentData>(_equipment.Values);
        }
    }

    private static void EnsureLoaded()
    {
        if (_items != null && _equipment != null) return;

        _items = new Dictionary<string, CSO_ItemData>();
        foreach (var item in Resources.LoadAll<CSO_ItemData>("Item"))
        {
            if (string.IsNullOrEmpty(item.itemId))
            {
                Debug.LogWarning($"CSO_ItemData '{item.name}' の_itemIdが未設定です");
                continue;
            }
            _items[item.itemId] = item;
        }

        _equipment = new Dictionary<string, CSO_EquipmentData>();
        foreach (var equipment in Resources.LoadAll<CSO_EquipmentData>("Equipment"))
        {
            if (string.IsNullOrEmpty(equipment.equipmentId))
            {
                Debug.LogWarning($"CSO_EquipmentData '{equipment.name}' の_equipmentIdが未設定です");
                continue;
            }
            _equipment[equipment.equipmentId] = equipment;
        }
    }
}
