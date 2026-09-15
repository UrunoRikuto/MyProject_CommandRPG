using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 町の石碑(クエスト板)を開くと表示するウィンドウ。CS_PauseMenu/CS_EquipmentMenuと同じ
/// _menuPanel開閉パターン。受注可能なクエストと、受注中のクエストを別々のリストで表示する
/// </summary>
public class CS_QuestBoardMenu : MonoBehaviour
{
    [SerializeField] private GameObject _menuPanel;

    // 受注可能なクエスト一覧(受注ボタン付き)
    [SerializeField] private GameObject _boardRowTemplate;
    [SerializeField] private Transform _boardListParent;
    [SerializeField] private ScrollRect _boardScrollRect;

    // 受注中のクエスト一覧(進捗表示のみ)
    [SerializeField] private GameObject _activeRowTemplate;
    [SerializeField] private Transform _activeListParent;
    [SerializeField] private ScrollRect _activeScrollRect;

    private CS_PlayerMove _playerMove;

    private void Start()
    {
        _menuPanel.SetActive(false);
        _playerMove = GameObject.FindAnyObjectByType<CS_PlayerMove>();
        _boardRowTemplate.SetActive(false);
        _activeRowTemplate.SetActive(false);
    }

    public void Open()
    {
        CS_GameManager.Instance.EnsureQuestBoardFilled();
        _menuPanel.SetActive(true);
        if (_playerMove != null)
        {
            _playerMove.enabled = false;
        }
        RefreshLists();
    }

    public void OnCloseButtonClicked()
    {
        _menuPanel.SetActive(false);
        if (_playerMove != null)
        {
            _playerMove.enabled = true;
        }
    }

    // 行の高さ・間隔。CS_EquipmentMenu.BuildMemberButtonsと同じく、LayoutGroupに頼らず
    // anchoredPositionを直接計算する(そちらで実績のある確実な方式に合わせた)
    private const float ROW_HEIGHT = 70f;
    private const float ROW_SPACING = 8f;

    private void RefreshLists()
    {
        ClearChildren(_boardListParent);
        var board = CS_GameManager.Instance.boardQuests;
        for (int i = 0; i < board.Count; i++)
        {
            BuildBoardRow(board[i], i);
        }
        ResizeContent(_boardListParent, board.Count);
        _boardScrollRect.verticalNormalizedPosition = 1f;

        ClearChildren(_activeListParent);
        var active = CS_GameManager.Instance.activeQuests;
        for (int i = 0; i < active.Count; i++)
        {
            BuildActiveRow(active[i], i);
        }
        ResizeContent(_activeListParent, active.Count);
        _activeScrollRect.verticalNormalizedPosition = 1f;
    }

    private void BuildBoardRow(CS_QuestData quest, int index)
    {
        GameObject row = Instantiate(_boardRowTemplate, _boardListParent);
        row.SetActive(true);
        PositionRow(row, index);
        SetRowTexts(row, quest);

        string capturedQuestId = quest.questId;
        Button button = row.GetComponent<Button>();
        button.onClick.AddListener(() =>
        {
            if (CS_GameManager.Instance.AcceptQuest(capturedQuestId))
            {
                RefreshLists();
            }
        });
    }

    private void BuildActiveRow(CS_QuestData quest, int index)
    {
        GameObject row = Instantiate(_activeRowTemplate, _activeListParent);
        row.SetActive(true);
        PositionRow(row, index);
        SetRowTexts(row, quest);
    }

    private void PositionRow(GameObject row, int index)
    {
        RectTransform rect = row.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = new Vector2(0f, ROW_HEIGHT);
        rect.anchoredPosition = new Vector2(0f, -index * (ROW_HEIGHT + ROW_SPACING));
    }

    /// <summary>
    /// Contentの縦幅を実際の行数に合わせて広げる(ScrollRectのスクロール範囲を正しく保つため)
    /// </summary>
    private void ResizeContent(Transform listParent, int rowCount)
    {
        RectTransform contentRect = listParent.GetComponent<RectTransform>();
        float height = rowCount > 0 ? rowCount * ROW_HEIGHT + (rowCount - 1) * ROW_SPACING : 0f;
        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, height);
    }

    private void SetRowTexts(GameObject row, CS_QuestData quest)
    {
        TextMeshProUGUI title = row.transform.Find("Title").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI reward = row.transform.Find("Reward").GetComponent<TextMeshProUGUI>();

        title.text = quest.isAccepted
            ? $"{quest.targetMonsterName} {quest.currentCount}/{quest.targetCount}討伐"
            : $"{quest.targetMonsterName}を{quest.targetCount}体討伐";
        reward.text = $"報酬: {ResolveRewardName(quest)} ×{quest.rewardCount}";
    }

    private string ResolveRewardName(CS_QuestData quest)
    {
        if (quest.rewardType == CSE_QuestRewardType.Item)
        {
            var item = CS_ItemDatabase.GetItem(quest.rewardId);
            return item != null ? item.itemName : quest.rewardId;
        }

        var equipment = CS_ItemDatabase.GetEquipment(quest.rewardId);
        return equipment != null ? equipment.equipmentName : quest.rewardId;
    }

    private void ClearChildren(Transform parent)
    {
        // Destroyはフレーム末まで実際には削除されないため、レイアウト計算に混ざらないよう先に非表示にしておく
        foreach (Transform child in parent)
        {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }
    }
}
