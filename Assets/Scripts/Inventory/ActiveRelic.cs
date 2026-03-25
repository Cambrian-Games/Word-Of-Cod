using UnityEngine;

public abstract class ActiveRelic : Item
{
	private void OnValidate()
	{
		_isConsumable = false;
		_maxCount = 1;
	}
}
