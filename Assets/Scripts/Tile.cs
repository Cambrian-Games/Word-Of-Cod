using System;
using TMPro;
using UnityEngine;

public class Tile : MonoBehaviour
{
	public enum TileKind
	{
		Normal,
		Spiny,
		Sandy
	}

	private TileKind _tileKind = TileKind.Normal;
	internal TileKind Kind
	{
		get => _tileKind;
		set => SetTileKind(value);
	}

	public TextMeshPro _tmpro;
	public char _letter;
    private bool _isQu;

	// should not be editor-accessible, should be accessed by code
	internal Vector2Int _coord;

	[SerializeField]
	private Sprite _normalSprite;
	[SerializeField]
	private Sprite _spinySprite;
	[SerializeField]
	private Sprite _sandySprite;

	[Min(0)]
	public int _spinyDamage = 10;

	[SerializeField]
	private SpriteRenderer _bgSpriteRenderer;

    [SerializeField]
    private SpriteRenderer _letterSpriteRenderer;

	[SerializeField]
	private Color _normalColor, _highlightedColor, _selectedColor, _selectedAndHighlightedColor;

	private HighlightState _highlightState = HighlightState.Normal;
	public HighlightState HighlightState { get => _highlightState; set => SetHighlightState(value); }

    private static readonly Vector3 DEFAULT_LETTER_POSITION = new Vector3(0, 0, -1);

    private void Start()
    {
        if (_bgSpriteRenderer)
        {
            _bgSpriteRenderer.sprite = _tileKind switch
            {
                TileKind.Normal => _normalSprite,
                TileKind.Spiny => _spinySprite,
                TileKind.Sandy => _sandySprite,
                _ => throw new InvalidOperationException(),
            };
        }
    }

    void Update()
	{
        if (_letter != 'Q')
            _isQu = false;

		// Currently unused; tiles no longer use tmpro
		if (_tmpro)
		{
			string strLast = _tmpro.text;
			string strGoal = _tileKind == TileKind.Sandy ? "" : _letter.ToString();
			
			if (strLast != strGoal)
			{
				_tmpro.text = strGoal;
			}
		}

        if (_letterSpriteRenderer)
        {
            Sprite spriteLast = _letterSpriteRenderer.sprite;

            // TODO check if letter is Qu

            Sprite spriteGoal = null;

            if (_tileKind != TileKind.Sandy)
            {
                spriteGoal = IsQuTile() ? BoardConfig.INSTANCE.CharSet.GetQuSprite() : BoardConfig.INSTANCE.CharSet.GetSprite(_letter);
            }

            if (spriteLast != spriteGoal)
            {
                _letterSpriteRenderer.sprite = spriteGoal;

                Vector3 letterOffset = IsQuTile() ? BoardConfig.INSTANCE.CharSet.GetQuOffset() : BoardConfig.INSTANCE.CharSet.GetOffset(_letter);
                _letterSpriteRenderer.transform.localPosition = DEFAULT_LETTER_POSITION + letterOffset;
            }
        }
	}

	public bool IsSelectable => _tileKind != TileKind.Sandy;

	public void OnSubmit()
	{
		switch (_tileKind)
		{
			case TileKind.Spiny:
				// potential player death is checked immediately after OnSubmit
				Player.INSTANCE.CurrentHealth -= _spinyDamage;
				Player.INSTANCE._inventory.OnPlayerTakeDamage();
				break;
		}
	}

	private void OnMouseEnter()
	{
		TileSelector.INSTANCE.MouseOverTile(this);
	}

	private void OnMouseExit()
	{
		TileSelector.INSTANCE.MouseLeaveTile(this);
	}

	private void OnMouseDown()
	{
		TileSelector.INSTANCE.ClickTile(this);
	}

    private void OnMouseOver()
    {
        if (Input.GetMouseButtonDown(1))
        {
            TileSelector.INSTANCE.RightClickTile(this);
        }
    }

    private void SetHighlightState(HighlightState tileSelectState)
	{
		_highlightState = tileSelectState;

		if (_bgSpriteRenderer)
		{
			_bgSpriteRenderer.color = _highlightState switch
			{
				HighlightState.Normal => _normalColor,
				HighlightState.Highlighted => _highlightedColor,
				HighlightState.Selected => _selectedColor,
				HighlightState.Selected_And_Highlighted => _selectedAndHighlightedColor,
				_ => throw new InvalidOperationException(),
			};
		}
	}

	private void SetTileKind(TileKind tileKind)
	{
		if (_tileKind == tileKind)
			return;

		_tileKind = tileKind;

		if (_bgSpriteRenderer)
		{
			_bgSpriteRenderer.sprite = _tileKind switch
			{
				TileKind.Normal => _normalSprite,
				TileKind.Spiny => _spinySprite,
				TileKind.Sandy => _sandySprite,
				_ => throw new InvalidOperationException(),
			};
		}
	}

    public void TryToggleQu()
    {
        if (_letter == 'Q' && (_highlightState & HighlightState.Selected) == 0)
        {
            _isQu = !_isQu;
        }
    }

    public bool IsQuTile() => _letter == 'Q' && _isQu;
}