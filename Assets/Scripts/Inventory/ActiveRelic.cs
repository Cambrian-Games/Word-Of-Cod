using UnityEngine;

public abstract class ActiveRelic : Item
{
	private void OnValidate()
	{
		_maxCount = 1;
	}

    public override InventoryManager.InventoryReference AsInventoryReference()
    {
        return new InventoryManager.InventoryReference(InventoryManager.InventorySection.Active_Relic, this.ID);
    }

    protected override void OnUseStateChanged(UseState oldState, UseState newState)
	{
		Player.INSTANCE._inventory.SetIconColorFromUseState(this.ID, InventoryManager.InventorySection.Active_Relic, newState);
	}
}
