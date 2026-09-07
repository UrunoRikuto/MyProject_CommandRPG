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
}
