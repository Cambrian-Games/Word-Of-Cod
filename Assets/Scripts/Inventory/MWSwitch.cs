using TMPro;
using UnityEngine;

public class MWSwitch : ActiveRelic
{
	private bool _inUse;
	private bool _canUse;

	public override void OnBattleStateChanged(BattleManager.BattleState oldState, BattleManager.BattleState newState)
	{
		_canUse = newState == BattleManager.BattleState.Player_Turn;
		_inUse = false;
	}

	public override void OnSelect()
	{
		if (_canUse)
		{
			_inUse = true;
			TileSelector.INSTANCE.Mode = TileSelector.SelectionMode.Item_Use;
		}
	}

	public override void OnTileClicked(Tile tile)
	{
		if (!_inUse || !_canUse)
			return;

		Debug.Assert(TileSelector.INSTANCE.Mode == TileSelector.SelectionMode.Item_Use);

		bool isM = tile._letter == 'M';
		bool isW = tile._letter == 'W';

		bool isSelected = (tile.HighlightState & HighlightState.Selected) != 0;

		if (isSelected)
			return;

		if (!isM && !isW)
		{
			_inUse = false;
			// this does not result in selecting the new tile, might want to fix?
			TileSelector.INSTANCE.Mode = TileSelector.SelectionMode.Letter_Selection;

			return;
		}

		GameBoard.INSTANCE.ChangeTileLetter(tile, isM ? 'W' : 'M');
		_canUse = false;
		TileSelector.INSTANCE.Mode = TileSelector.SelectionMode.Letter_Selection;
	}
}
