using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class TileSelector : MonoBehaviour
{
	public enum LetterSelectionKind
	{
		[InspectorName("Click and Drag")]
		Click_And_Drag,
		[InspectorName("Click to Start and Submit")]
		Click_And_Move,
		[InspectorName("Click Each Letter")]
		Click_Each_Letter,
		[InspectorName("Keyboard Movement")]
		Keyboard_Move,
		[InspectorName("Type")]
		Type
	}

	[SerializeField]
	private LetterSelectionKind _selectionKind = LetterSelectionKind.Click_And_Drag;

	[SerializeField]
	private LineRenderer _lineRenderer;

	private List<Tile> _selectedTiles = new List<Tile>();
	private Tile _currentHighlightedTile;
	private string _word = "";

	public TMP_Text _wordDisplay;
	private bool _isMouseSelecting = false;

	public enum SelectionMode
	{
		None,
		Letter_Selection,
		Item_Use
	}

	private SelectionMode _selectionMode = SelectionMode.None;

	public SelectionMode Mode
	{
		get => _selectionMode;
		set
		{
			if (value == SelectionMode.None)
			{
				DeselectAllTiles();
			}

			_selectionMode = value;
		}
	}

	public static TileSelector INSTANCE;

	private void Awake()
	{
		// set up singleton

		if (INSTANCE != null && INSTANCE != this)
		{
			Destroy(gameObject);
			return;
		}

		INSTANCE = this;
	}

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
		if (_selectionMode == SelectionMode.Letter_Selection)
		{
			UpdateTileSelection();
		}

		_wordDisplay.text = _word;
    }

	void UpdateTileSelection()
	{
		switch (_selectionKind)
		{
			case LetterSelectionKind.Click_And_Drag:
				UpdateMouseSelect(drag: true);
				return;
			case LetterSelectionKind.Click_And_Move:
				UpdateMouseSelect(drag: false);
				return;
			case LetterSelectionKind.Click_Each_Letter:
				UpdateClickLetter();
				return;
			case LetterSelectionKind.Keyboard_Move:
				UpdateKeyboardMove();
				return;
			case LetterSelectionKind.Type:
				UpdateType();
				return;
		}
	}

	private void UpdateMouseSelect(bool drag)
	{
		if (!_isMouseSelecting)
		{
			if (Input.GetMouseButtonDown((int)MouseButton.Left))
			{
				_isMouseSelecting = true;
				Debug.Log("Selecting Started");

				// should we disallow starting a selection if you're not already highlighting a tile?

				if (_currentHighlightedTile)
				{
					TrySelectTile(_currentHighlightedTile);
				}
			}
		}
		else
		{
			// the drag check makes one of these early exit

			bool stoppedDragSelecting = drag && !Input.GetMouseButton((int)MouseButton.Left);
			bool stoppedMoveSelecting = !drag && Input.GetMouseButtonDown((int)MouseButton.Left);

			if (stoppedDragSelecting || stoppedMoveSelecting)
			{
				_isMouseSelecting = false;
				Debug.Log("Selecting Ended\n");

				if (_word != "")
				{
                    SendSelectedWord();
				}
			}
		}
	}

	private void UpdateType()
	{
		throw new NotImplementedException();
	}

	private void UpdateKeyboardMove()
	{
		throw new NotImplementedException();
	}

	private void UpdateClickLetter()
	{
		if (Input.GetKeyDown(KeyCode.KeypadEnter) || Input.GetKeyDown(KeyCode.Return))
        {
            SendSelectedWord();
        }
	}

	/// <summary>
	/// There are multiple cases here that are a bit difficult to understand:
	/// 
	/// 1) Hovering over tile when not trying to select => highlight
	/// 2) Hovering over a tile when trying to select, and list is empty => select and highlight
	/// 3) Hovering over the second-to-last selected tile => deselect last tile, select + highlight current tile
	/// 4) Hovering over a different selected tile => no change
	/// 5) Hovering over an adjacent unselected tile => select and highlight
	/// 6) Hovering over a non-adjacent unselected tile => highlight
	/// </summary>
	/// <param name="tile"></param>
	internal void MouseOverTile(Tile tile)
	{
		_currentHighlightedTile = tile;

		bool hoverSelecting = (_selectionKind == LetterSelectionKind.Click_And_Drag || _selectionKind == LetterSelectionKind.Click_And_Move) && _isMouseSelecting;

		if (hoverSelecting)
		{
			if (_selectedTiles.Count == 0)
			{
				TrySelectTile(tile);
			}
			else
			{
				// if the tile is the second-to-last element in the list, remove the last element

				if (_selectedTiles.Count >= 2 && _selectedTiles[^2] == tile)
				{
					DeselectTile(_selectedTiles[^1]);
				}
				else
				{
					if (_selectedTiles.Contains(tile))
					{
						// do nothing
					}
					else
					{
						Vector2Int gridDist = _selectedTiles[^1]._coord - tile._coord;

						if (Mathf.Abs(gridDist.x) <= 1 && Mathf.Abs(gridDist.y) <= 1 && !_selectedTiles.Contains(tile))
						{
							TrySelectTile(tile);
						}
						else
						{
							tile.HighlightState = HighlightState.Highlighted;
						}
					}
				}
			}
		}
		else
		{
            if (_selectedTiles.Contains(tile))
            {
                tile.HighlightState = HighlightState.Selected_And_Highlighted;
            }
            else
            {
                tile.HighlightState = HighlightState.Highlighted;
            }
		}
	}

	internal void MouseLeaveTile(Tile tile)
	{
		if (tile.HighlightState == HighlightState.Highlighted)
			tile.HighlightState = HighlightState.Normal;
		else if (tile.HighlightState == HighlightState.Selected_And_Highlighted)
			tile.HighlightState = HighlightState.Selected;

		if (_currentHighlightedTile == tile)
		{
			_currentHighlightedTile = null;
		}

		// how should we handle if you have only one selected tile, you're in click + move, and you move the mouse out of the play grid?
	}

	internal void ClickTile(Tile tile)
	{
		switch (_selectionMode)
		{
			case SelectionMode.None:
				break;

			case SelectionMode.Letter_Selection:
				if (_selectionKind == LetterSelectionKind.Click_Each_Letter)
				{
					int tileIndex = _selectedTiles.IndexOf(tile);

					if (tileIndex == -1)
					{
						if (_selectedTiles.Count > 0)
						{
							Tile lastTile = _selectedTiles[^1];

							if (Math.Abs(lastTile._coord.x - tile._coord.x) <= 1 && Math.Abs(lastTile._coord.y - tile._coord.y) <= 1)
							{
								TrySelectTile(tile);
							}
						}
						else
						{
							TrySelectTile(tile);
						}

					}
					else if (tileIndex == _selectedTiles.Count - 1)
					{
						DeselectTile(tile);
					}

					// otherwise do nothing
				}
				// this could drive selection starting instead of UpdateMouseSelect
				break;

			case SelectionMode.Item_Use:
				Player.INSTANCE._inventory.OnTileClicked(tile);
				break;
		}
	}

	internal void TrySelectTile(Tile tile)
	{
		if (!tile.IsSelectable)
			return;

		_selectedTiles.Add(tile);

        if (tile.IsQuTile())
        {
            _word += "QU";
        }
        else
        {
            _word += tile._letter;
        }

		tile.HighlightState = HighlightState.Selected_And_Highlighted;

		_lineRenderer.positionCount++;
		_lineRenderer.SetPosition(_lineRenderer.positionCount - 1, tile.transform.position);
	}

	internal void DeselectTile(Tile tile)
	{
        Debug.Assert(_selectedTiles[^1] == tile);
		_selectedTiles.Remove(_selectedTiles[^1]);
        
        if (tile.IsQuTile())
        {
            _word = _word[..^2];
        }
        else
        {
            _word = _word[..^1];
        }

		tile.HighlightState = HighlightState.Normal;

		_lineRenderer.positionCount--;

        if (_selectedTiles.Count > 0 && (_selectionKind == LetterSelectionKind.Click_And_Drag || _selectionKind == LetterSelectionKind.Click_And_Move))
        {
            _selectedTiles[^1].HighlightState = HighlightState.Selected_And_Highlighted;
        }
	}

	internal void DeselectAllTiles()
	{
		// would be interesting to check the performance of this vs setting all to normal and THEN highlighting a tile

		foreach (Tile tile in _selectedTiles)
		{
			if (tile == _currentHighlightedTile)
			{
				tile.HighlightState = HighlightState.Highlighted;
			}
			else
			{
				tile.HighlightState = HighlightState.Normal;
			}
		}

		_selectedTiles.Clear();
		_word = "";
		_lineRenderer.positionCount = 0;
	}

    public void SendSelectedWord()
    {
        List<string> coordList = _selectedTiles.Select(tile => tile._coord).Select(coord => $"<{coord.x},{coord.y}>").ToList();

        if (BattleManager.INSTANCE.TrySubmitWord(_word, _selectedTiles))
        {
            Debug.Log("- Tiles Used: " + string.Join(", ", coordList));
            Debug.Log("");
        }
        else
        {
            Debug.Log($"{_word} is not a word.");
            Debug.Log("- Deselecting " + string.Join(", ", coordList));
            Debug.Log("");
        }

		DeselectAllTiles();
    }

    internal void RightClickTile(Tile tile)
    {
        if (_selectionMode != SelectionMode.Letter_Selection || !tile.IsSelectable)
            return;

        tile.TryToggleQu();
    }
}
