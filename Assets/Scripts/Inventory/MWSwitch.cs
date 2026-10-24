using TMPro;
using UnityEngine;

public class MWSwitch : ActiveRelic
{
    private Item.UseState _nextState = UseState.Nil;

	public override void OnBattleStateChanged(BattleManager.BattleState oldBattleState, BattleManager.BattleState newBattleState)
	{
		bool isPlayerTurn = (newBattleState == BattleManager.BattleState.Player_Turn);

		if (isPlayerTurn)
		{
			// TODO check if new letter has been added by LetterTile mid-turn
			bool hasValidTarget = GameBoard.INSTANCE.CountTiles('M') > 0 || GameBoard.INSTANCE.CountTiles('W') > 0;
			State = (hasValidTarget) ? UseState.Can_Use : UseState.Unususable;
		}
		else
		{
			State = UseState.Unususable;
		}	
       
        _nextState = State;
	}

	public override void OnSelect()
	{
		Debug.Assert(State == UseState.Can_Use);

		State = UseState.In_Use;
        _nextState = UseState.Can_Use;
		TileSelector.INSTANCE.Mode = TileSelector.SelectionMode.Item_Use;
	}

	public override void OnTileClicked(Tile tile)
	{
		Debug.Assert(State == UseState.In_Use);
		Debug.Assert(TileSelector.INSTANCE.Mode == TileSelector.SelectionMode.Item_Use);

		bool isM = tile._letter == 'M';
		bool isW = tile._letter == 'W';

		bool isSelected = (tile.HighlightState & HighlightState.Selected) != 0;

		if (isSelected)
			return;

		// The EndUse() call is duplicated in case we decide that we want to get rid of the else case
		//  because this is the only situation where a misclick will auto-exit the relic

		if (isM || isW)
		{
			GameBoard.INSTANCE.ChangeTileLetter(tile, isM ? 'W' : 'M');
			_nextState = UseState.Unususable;
			EndUse();
		}
		else
		{
			_nextState = UseState.Can_Use;
			EndUse();
		}
	}

	public override void EndUse()
	{
		TileSelector.INSTANCE.Mode = TileSelector.SelectionMode.Letter_Selection;

        State = _nextState;
		base.EndUse();
	}
}
