using System;
using UnityEngine;

public class YouSubstitution : ActiveRelic
{
	private bool _used;
	public override void OnBattleStateChanged(BattleManager.BattleState oldBattleState, BattleManager.BattleState newBattleState)
	{
		if (newBattleState == BattleManager.BattleState.Load)
		{
			State = UseState.Can_Use;
			_used = false;
		}
		else if (!_used)
		{
			State = (newBattleState == BattleManager.BattleState.Player_Turn) ? UseState.Can_Use : UseState.Unususable;
		}
	}

	public override void OnSelect()
	{
		Debug.Assert(State == UseState.Can_Use);

		// Consider having this switch to In_Use and only drop to Unusable once the substitute has been broken
		State = UseState.Unususable;
		_used = true;
		Player.INSTANCE._substitute = true;
		int loss = Mathf.RoundToInt(Player.INSTANCE.MaxHealth * 0.2f);
		Player.INSTANCE.SetHealth(Mathf.Max(Player.INSTANCE.CurrentHealth - loss, 1));

		EndUse();
	}
}
