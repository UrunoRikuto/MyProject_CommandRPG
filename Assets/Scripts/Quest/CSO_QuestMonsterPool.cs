using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// クエストの討伐対象にできる敵の一覧。キャラクターデータ(Assets/Data/Character配下)は
/// Resourcesフォルダの外にあり実行時にResources.LoadAllで列挙できないため、
/// CS_ItemDatabaseと同じ考え方でResources配下のこのアセット経由で参照を解決する
/// </summary>
[CreateAssetMenu(fileName = "DB_QuestMonsterPool", menuName = "Scriptable Objects/DB_QuestMonsterPool")]
public class CSO_QuestMonsterPool : ScriptableObject
{
    [SerializeField]
    private List<CSO_CharacterData> _monsters;
    public IReadOnlyList<CSO_CharacterData> monsters => _monsters;
}
