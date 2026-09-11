using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// セーブデータ1ファイル分。プレイヤーのフィールド上の位置とパーティの現在HP/MPを持つ
/// </summary>
[Serializable]
public class CS_SaveData
{
    public Vector3 playerPosition;
    public List<CS_PartyMemberState> partyState;

    // 所持アイテム/装備(未装備で手持ちにあるもの)、開封済みの宝箱ID
    public List<CS_ItemStack> ownedItems = new List<CS_ItemStack>();
    public List<CS_ItemStack> ownedEquipment = new List<CS_ItemStack>();
    public List<string> openedChestIds = new List<string>();
}
