using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class CS_CharacterUI : MonoBehaviour
{
    [SerializeField]
    private Image _hpFillImage;

    [SerializeField]
    private Image _mpFillImage;

    [SerializeField]
    private RectTransform _hpBarRect;

    [SerializeField]
    private RectTransform _mpBarRect;

    [SerializeField]
    private RectTransform _turnIndicatorRect;

    private Image _characterImage;
    public Image characterImage => _characterImage;

    private CS_CharacterState _characterState;
    private Vector2 _turnIndicatorBasePos;

    private void Awake()
    {
        _characterImage = GetComponent<Image>();
        if (_turnIndicatorRect != null)
        {
            _turnIndicatorBasePos = _turnIndicatorRect.anchoredPosition;
            _turnIndicatorRect.gameObject.SetActive(false);
        }
    }

    /// <summary>
    /// 行動選択中であることを示すマーカーの表示/非表示を切り替える
    /// </summary>
    public void SetTurnIndicator(bool active)
    {
        if (_turnIndicatorRect != null)
        {
            _turnIndicatorRect.gameObject.SetActive(active);
        }
    }

    public void SetCharacterState(CS_CharacterState characterState)
    {
        _characterState = characterState;
    }

    /// <summary>
    /// 敵側の場合はHP/MPバーをアイコンの下側(画面中央向き)に反転配置する。
    /// 単純に符号反転するとHP/MPの上下関係まで入れ替わってしまうため、
    /// Y座標をHP/MP間で入れ替えたうえで反転し、HPが常に上に来るようにする
    /// </summary>
    public void SetTeamSide(bool isEnemy)
    {
        if (!isEnemy)
        {
            return;
        }

        float hpY = _hpBarRect.anchoredPosition.y;
        float mpY = _mpBarRect.anchoredPosition.y;

        _hpBarRect.anchoredPosition = new Vector2(_hpBarRect.anchoredPosition.x, -mpY);
        _mpBarRect.anchoredPosition = new Vector2(_mpBarRect.anchoredPosition.x, -hpY);
    }

    private void Update()
    {
        if (_characterState == null)
        {
            return;
        }

        _hpFillImage.fillAmount = _characterState.maxHealth > 0
            ? (float)_characterState.currentHealth / _characterState.maxHealth
            : 0f;

        _mpFillImage.fillAmount = _characterState.maxMP > 0
            ? (float)_characterState.currentMP / _characterState.maxMP
            : 0f;

        if (_turnIndicatorRect != null && _turnIndicatorRect.gameObject.activeSelf)
        {
            float bob = Mathf.Sin(Time.time * 4f) * 6f;
            _turnIndicatorRect.anchoredPosition = _turnIndicatorBasePos + new Vector2(0, bob);
        }
    }
}
