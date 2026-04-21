using UnityEngine;

public abstract class ActiveRelic : Item
{
	private void OnValidate()
	{
		_isConsumable = false;
		_maxCount = 1;
	}

	public override void EndUse()
	{
		// does not call Item::EndUse()

		Player.INSTANCE._inventory.EndActiveRelicUse(this);
	}
}
