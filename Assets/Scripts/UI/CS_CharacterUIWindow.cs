using System.Collections.Generic;
using UnityEngine;

public class CS_CharacterUIWindow : MonoBehaviour
{
    [SerializeField]
    [Header("アイコンの幅")]
    private float _iconWidth = 100f;

    [SerializeField]
    [Header("敵側UI")]
    private GameObject _enemyUIParent;

    [SerializeField]
    [Header("味方側UI")]
    private GameObject _allyUIParent;

    [SerializeField]
    [Header("キャラクターUIのプレハブ")]
    private GameObject _characterUIPrefab;

    private readonly Dictionary<CS_CharacterState, CS_CharacterUI> _characterUIByState = new Dictionary<CS_CharacterState, CS_CharacterUI>();

    public void CreateCharacterUI(IReadOnlyList<CS_CharacterState> allyParty, IReadOnlyList<CS_CharacterState> enemyParty)
    {
        _characterUIByState.Clear();
        CreateCharacterIcons(allyParty, _allyUIParent, isEnemy: false);
        CreateCharacterIcons(enemyParty, _enemyUIParent, isEnemy: true);
    }

    /// <summary>
    /// パーティ1つ分のキャラクターアイコンを生成し、中央揃えで横一列に並べる
    /// </summary>
    private void CreateCharacterIcons(IReadOnlyList<CS_CharacterState> party, GameObject parent, bool isEnemy)
    {
        for (int i = 0; i < party.Count; i++)
        {
            GameObject characterUI = Instantiate(_characterUIPrefab, parent.transform);
            CS_CharacterUI characterUIScript = characterUI.GetComponent<CS_CharacterUI>();
            characterUIScript.characterImage.sprite = party[i].characterIcon;
            characterUIScript.SetCharacterState(party[i]);
            characterUIScript.SetTeamSide(isEnemy);
            _characterUIByState[party[i]] = characterUIScript;

            // アイコン位置を設定(人数が偶数でも中央揃えになるようfloatで計算)
            RectTransform rectTransform = characterUI.GetComponent<RectTransform>();
            rectTransform.anchoredPosition = new Vector2((i - (party.Count - 1) / 2f) * _iconWidth, 0);
        }
    }

    /// <summary>
    /// 現在行動を選択しているキャラクターのアイコンにだけターンマーカーを表示する。nullを渡すと全て非表示にする
    /// </summary>
    public void SetActingCharacter(CS_CharacterState character)
    {
        foreach (var pair in _characterUIByState)
        {
            pair.Value.SetTurnIndicator(pair.Key == character);
        }
    }
}
