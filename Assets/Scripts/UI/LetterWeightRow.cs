using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LetterWeightRow : MonoBehaviour
{
    #region UI Elements
    [SerializeField]
    private Image _letterIcon;
    public Image LetterIcon => _letterIcon;

    [SerializeField]
    private TMP_Text _baseWeightText;
    public TMP_Text BaseWeightText => _baseWeightText;

    [SerializeField]
    private Button _minusButton, _plusButton;
    public Button MinusButton => _minusButton;
    public Button PlusButton => _plusButton;

    [SerializeField]
    private TMP_Text _playerTweakText;
    public TMP_Text PlayerTweakText => _playerTweakText;

    [SerializeField]
    private TMP_Text _finalWeightText;
    public TMP_Text FinalWeightText => _finalWeightText;
    #endregion

    [SerializeField, HideInInspector]
    private char _letter;
    public char Letter
    {
        get => _letter;
#if UNITY_EDITOR
        set => _letter = value;
#endif
    }

    [SerializeField, HideInInspector]
    private LetterWeightMenu _parent;
    public LetterWeightMenu Parent
    {
        get => _parent;
#if UNITY_EDITOR
        set => _parent = value;
#endif
    }

    public void ClickPlus()
    {
        _parent.OnClickPlus(this);
    }

    public void ClickMinus()
    {
        _parent.OnClickMinus(this);
    }
}
