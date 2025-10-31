using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class TileSelector : MonoBehaviour
{
	public enum TileSelectionKind
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
	private TileSelectionKind _selectionKind = TileSelectionKind.Click_And_Drag;

	[SerializeField]
	private LineRenderer _lineRenderer;

	[SerializeField]
	private SpriteRenderer _debugWordConfirmer;

	private List<Tile> _selectedTiles = new List<Tile>();
	private Tile _currentHighlightedTile;
	private string _word = "";
	private FPART _pOS;

	private bool _isMouseSelecting = false;
	internal bool _isSelectingEnabled = true;

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
		if (_isSelectingEnabled)
		{
			UpdateTileSelection();
		}
    }

	void UpdateTileSelection()
	{
		switch (_selectionKind)
		{
			case TileSelectionKind.Click_And_Drag:
				UpdateMouseSelect(drag: true);
				return;
			case TileSelectionKind.Click_And_Move:
				UpdateMouseSelect(drag: false);
				return;
			case TileSelectionKind.Click_Each_Letter:
				UpdateClickLetter();
				return;
			case TileSelectionKind.Keyboard_Move:
				UpdateKeyboardMove();
				return;
			case TileSelectionKind.Type:
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

                    // would be interesting to check the performance of this vs setting all to normal and THEN highlighting a tile
                    
                    foreach (Tile tile in _selectedTiles)
                    {
                        if (tile == _currentHighlightedTile)
                        {
                            tile.HighlightState = HIGHLIGHTS.HIGHLIGHTED;
                        }
                        else
                        {
                            tile.HighlightState = HIGHLIGHTS.NORMAL;
                        }
                    }

					// Deselect all tiles
					_selectedTiles.Clear();
					_word = "";
					_lineRenderer.positionCount = 0;
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
		throw new NotImplementedException();
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

		bool hoverSelecting = (_selectionKind == TileSelectionKind.Click_And_Drag || _selectionKind == TileSelectionKind.Click_And_Move) && _isMouseSelecting;

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
							tile.HighlightState = HIGHLIGHTS.HIGHLIGHTED;
						}
					}
				}
			}
		}
		else
		{
			tile.HighlightState = HIGHLIGHTS.HIGHLIGHTED;
		}
	}

	internal void MouseLeaveTile(Tile tile)
	{
		if (tile.HighlightState == HIGHLIGHTS.HIGHLIGHTED)
			tile.HighlightState = HIGHLIGHTS.NORMAL;
		else if (tile.HighlightState == HIGHLIGHTS.SELECTED_AND_HIGHLIGHTED)
			tile.HighlightState = HIGHLIGHTS.SELECTED;

		if (_currentHighlightedTile == tile)
		{
			_currentHighlightedTile = null;
		}

		// how should we handle if you have only one selected tile, you're in click + move, and you move the mouse out of the play grid?
	}


	internal void ClickTile(Tile tile)
	{
		// this could drive selection starting instead of UpdateMouseSelect
	}

	internal void TrySelectTile(Tile tile)
	{
		if (!tile.IsSelectable)
			return;

		_selectedTiles.Add(tile);
		_word += tile._letter;
		tile.HighlightState = HIGHLIGHTS.SELECTED_AND_HIGHLIGHTED;

		_lineRenderer.positionCount++;
		_lineRenderer.SetPosition(_lineRenderer.positionCount - 1, tile.transform.position);
	}

	internal void DeselectTile(Tile tile)
	{
		_selectedTiles.Remove(_selectedTiles[^1]);
		_word = _word[..^1];
		tile.HighlightState = HIGHLIGHTS.NORMAL;

		_lineRenderer.positionCount--;

		_selectedTiles[^1].HighlightState = HIGHLIGHTS.SELECTED_AND_HIGHLIGHTED;
	}
}
