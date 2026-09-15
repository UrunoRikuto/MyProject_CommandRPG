using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// パーティの先頭(操作キャラクター)に後続メンバーが一列に連なってついてくる、ドラクエ方式の移動演出。
/// 先頭が通った軌跡を記録し、後続メンバーはその軌跡上を一定間隔離れて追いかけるので、
/// 曲がり角でもショートカットせず先頭とまったく同じ経路を辿る
/// </summary>
public class CS_PartyTrail : MonoBehaviour
{
    [Header("パーティ編成(先頭から順に。0番目は操作キャラクター自身の見た目にも使う)")]
    [SerializeField] private List<CSO_CharacterData> _partyRoster;

    [Header("軌跡上でメンバー同士を何ユニット分離すか")]
    [SerializeField] private float _followSpacing = 0.6f;

    [Header("軌跡を記録する間隔(この距離動くたびに1点記録する)")]
    [SerializeField] private float _recordInterval = 0.05f;

    [Header("後続メンバーの追従スピード")]
    [SerializeField] private float _followSpeed = 4f;

    private readonly List<Vector3> _trail = new List<Vector3>();
    private Vector3 _lastRecordedPosition;
    private readonly List<Transform> _followers = new List<Transform>();
    private int _stepsPerMember = 1;

    private void Start()
    {
        _lastRecordedPosition = transform.position;
        _trail.Add(transform.position);
        _stepsPerMember = Mathf.Max(1, Mathf.RoundToInt(_followSpacing / _recordInterval));

        ApplyLeaderIcon();
        SpawnFollowers();
    }

    private void ApplyLeaderIcon()
    {
        if (_partyRoster == null || _partyRoster.Count == 0) return;

        Sprite icon = _partyRoster[0].characterIcon;
        if (icon == null) return;

        SpriteRenderer renderer = GetComponentInChildren<SpriteRenderer>();
        if (renderer != null) renderer.sprite = icon;
    }

    private void SpawnFollowers()
    {
        if (_partyRoster == null) return;

        for (int i = 1; i < _partyRoster.Count; i++)
        {
            CSO_CharacterData member = _partyRoster[i];
            if (member == null) continue;

            GameObject go = new GameObject($"Follower_{member.characterName}");
            go.transform.SetParent(transform);
            go.transform.position = transform.position;

            SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = member.characterIcon;
            renderer.sortingOrder = -1;

            _followers.Add(go.transform);
        }
    }

    private void Update()
    {
        RecordTrail();
        MoveFollowers();
    }

    /// <summary>
    /// 先頭が一定距離動くたびに現在位置を軌跡の先頭に追加する。
    /// 必要な分(メンバー数分の間隔)より古い記録は捨てる
    /// </summary>
    private void RecordTrail()
    {
        if (Vector3.Distance(transform.position, _lastRecordedPosition) < _recordInterval) return;

        _trail.Insert(0, transform.position);
        _lastRecordedPosition = transform.position;

        int maxLength = _stepsPerMember * (_followers.Count + 1) + 10;
        if (_trail.Count > maxLength)
        {
            _trail.RemoveRange(maxLength, _trail.Count - maxLength);
        }
    }

    private void MoveFollowers()
    {
        for (int i = 0; i < _followers.Count; i++)
        {
            int targetIndex = _stepsPerMember * (i + 1);
            Vector3 targetPosition = targetIndex < _trail.Count ? _trail[targetIndex] : _trail[_trail.Count - 1];
            _followers[i].position = Vector3.MoveTowards(_followers[i].position, targetPosition, _followSpeed * Time.deltaTime);
        }
    }
}
