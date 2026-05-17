using UnityEngine;

public abstract class ActiveRelic : Item
{
	private void OnValidate()
	{
		_maxCount = 1;
	}

	protected override void OnUseStateChanged(UseState oldState, UseState newState)
	{
		Player.INSTANCE._inventory.SetIconColorFromUseState(this.ID, InventoryManager.InventorySection.Active_Relic, newState);
	}
}
