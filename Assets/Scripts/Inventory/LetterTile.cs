using NUnit.Framework;
using TMPro;
using UnityEngine;

public class LetterTile : Item
{
	private enum UseState
	{
		Unususable,
		Can_Use,
		In_Use
	}

	private Tile _selectedTile;
	private UseState _state;

	public TMP_Text _countText;



	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
	{
		_countText.text = _currentCount.ToString();
	}

	// Update is called once per frame
	void Update()
	{
		if (_state != UseState.In_Use)
		{
			_selectedTile = null;
			return;
		}

		// at this point, the item is in use

		if (Input.GetKeyDown(KeyCode.Escape))
		{
			EndUse();
			return;
		}

		if (!_selectedTile)
			return;

		// check for input this frame

		string input = Input.inputString;

		if (input == null || input.Length == 0)
			return;

		if (char.IsLetter(input[0]))
		{
			GameBoard.INSTANCE.ChangeTileLetter(_selectedTile, char.ToUpper(input[0]));

			_currentCount--;
			_countText.text = _currentCount.ToString();

			EndUse();
		}
	}

	public override void OnBattleStateChanged(BattleManager.BattleState oldState, BattleManager.BattleState newState)
	{
		_state = (newState == BattleManager.BattleState.Player_Turn && _currentCount > 0) ? UseState.Can_Use : UseState.Unususable;
	}

	public override void OnSelect()
	{
		if (_state == UseState.Can_Use)
		{
			_state = UseState.In_Use;
			TileSelector.INSTANCE.Mode = TileSelector.SelectionMode.Item_Use;
		}
	}

	public override void OnTileClicked(Tile tile)
	{
		if (_state != UseState.In_Use)
			return;

		Debug.Assert(_currentCount > 0);

		Debug.Assert(TileSelector.INSTANCE.Mode == TileSelector.SelectionMode.Item_Use);

		_selectedTile = tile;
	}

	public override void EndUse()
	{
		_state = UseState.Can_Use;
		TileSelector.INSTANCE.Mode = TileSelector.SelectionMode.Letter_Selection;
		base.EndUse();
	}
}
