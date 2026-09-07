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

    private Image _characterImage;
    public Image characterImage => _characterImage;

    private CS_CharacterState _characterState;

    private void Awake()
    {
        _characterImage = GetComponent<Image>();
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
    }
}
