using UnityEngine;
using TMPro;

public class BagOfWorms : Item
{  
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _countText.text = _currentCount.ToString();
    }

	public override void OnBattleStateChanged(BattleManager.BattleState oldBattleState, BattleManager.BattleState newBattleState)
	{
		bool isPlayerTurn = newBattleState == BattleManager.BattleState.Player_Turn;
		bool isPlayerAtFullHealth = Player.INSTANCE.HealthPercent() >= 1.0f;
		bool hasItem = _currentCount > 0;
		State = (hasItem && isPlayerTurn && !isPlayerAtFullHealth) ? UseState.Can_Use : UseState.Unususable;
	}

	public override void OnSelect()
    {
		Debug.Assert(State == UseState.Can_Use);
        _currentCount--;
        _countText.text = _currentCount.ToString();
		Player.INSTANCE.Heal(50);

		if (Player.INSTANCE.HealthPercent() >= 1.0f || _currentCount == 0)
		{
			State = UseState.Unususable;
		}

		EndUse();
    }  
}
