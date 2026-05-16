using NUnit.Framework;
using TMPro;
using UnityEngine;

public class LetterTile : Item
{
	private Tile _selectedTile;

	public void Start()
	{
		_countText.text = _currentCount.ToString();
	}

	// Update is called once per frame
	void Update()
	{
		if (State != UseState.In_Use)
		{
			_selectedTile = null;
			return;
		}

		if (!_selectedTile)
			return;

		// check for input this frame

		string input = Input.inputString;

		if (input == null || input.Length == 0)
			return;

		if (char.IsLetter(input[0]) && _selectedTile._letter != input[0])
		{
			GameBoard.INSTANCE.ChangeTileLetter(_selectedTile, char.ToUpper(input[0]));

			_currentCount--;
			_countText.text = _currentCount.ToString();

			EndUse();
		}
	}

	public override void OnBattleStateChanged(BattleManager.BattleState oldState, BattleManager.BattleState newState)
	{
		State = (newState == BattleManager.BattleState.Player_Turn && _currentCount > 0) ? UseState.Can_Use : UseState.Unususable;
	}

	public override void OnSelect()
	{
		Debug.Assert(State == UseState.Can_Use);

		State = UseState.In_Use;
		TileSelector.INSTANCE.Mode = TileSelector.SelectionMode.Item_Use;
	}

	public override void OnTileClicked(Tile tile)
	{
		Debug.Assert(State == UseState.In_Use);
		Debug.Assert(_currentCount > 0);
		Debug.Assert(TileSelector.INSTANCE.Mode == TileSelector.SelectionMode.Item_Use);

		_selectedTile = tile;
	}

	public override void EndUse()
	{
		State = (_currentCount > 0) ? UseState.Can_Use : UseState.Unususable;
		TileSelector.INSTANCE.Mode = TileSelector.SelectionMode.Letter_Selection;
		base.EndUse();
	}
}
