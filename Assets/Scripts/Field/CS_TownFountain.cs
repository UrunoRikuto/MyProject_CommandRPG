using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 街の泉。プレイヤーが近くにいる間にFキーを押すとパーティ全員の体力・MPを全回復する
/// (CS_QuestBoard/CS_TownGateと同じ「近づいてFキー」方式)
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class CS_TownFountain : MonoBehaviour
{
    [SerializeField] private List<CSO_CharacterData> _playerPartyData;

    private bool _playerNearby;
    private CS_InteractionPrompt _prompt;

    private void Start()
    {
        _prompt = GameObject.FindAnyObjectByType<CS_InteractionPrompt>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.GetComponent<CS_PlayerMove>() != null)
        {
            _playerNearby = true;
            if (_prompt != null) _prompt.Show("Fキーで回復");
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.GetComponent<CS_PlayerMove>() != null)
        {
            _playerNearby = false;
            if (_prompt != null) _prompt.Hide();
        }
    }

    private void Update()
    {
        if (_playerNearby && Input.GetKeyDown(KeyCode.F))
        {
            CS_GameManager.Instance.HealPartyToFull(_playerPartyData);
            Debug.Log("泉に触れた。パーティが全回復した。");
        }
    }
}
