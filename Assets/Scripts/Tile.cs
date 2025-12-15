using System;
using TMPro;
using UnityEngine;

public class Tile: MonoBehaviour
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
	private SpriteRenderer _spriteRenderer;

	[SerializeField]
	private Color _normalColor, _highlightedColor, _selectedColor, _selectedAndHighlightedColor;

	private HighlightState _highlightState = HighlightState.Normal;
	public HighlightState HighlightState { get => _highlightState; set => SetHighlightState(value); }

	void Update()
	{
		if (_tmpro)
		{
			string strLast = _tmpro.text;
			string strGoal = _tileKind == TileKind.Sandy ? "" : _letter.ToString();
			
			if (strLast != strGoal)
			{
				_tmpro.text = strGoal;
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

	private void SetHighlightState(HighlightState tileSelectState)
	{
		_highlightState = tileSelectState;

		if (_spriteRenderer)
		{
			_spriteRenderer.color = _highlightState switch
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

		if (_spriteRenderer)
		{
			_spriteRenderer.sprite = _tileKind switch
			{
				TileKind.Normal => _normalSprite,
				TileKind.Spiny => _spinySprite,
				TileKind.Sandy => _sandySprite,
				_ => throw new InvalidOperationException(),
			};
		}
	}
}