using System;

/// <summary>
/// クエスト1件分のデータ。ランタイムでランダム生成され、セーブデータにそのまま持たせる
/// </summary>
[Serializable]
public class CS_QuestData
{
    public string questId;
    public string targetMonsterName;
    public int targetCount;
    public int currentCount;
    public CSE_QuestRewardType rewardType;
    public string rewardId;
    public int rewardCount;
    public bool isAccepted;
}
