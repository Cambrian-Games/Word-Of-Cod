using System;
using UnityEngine;

public class SalmonStone : ActiveRelic
{
    public Sprite _brokenIcon;

	// This is the single sketchiest 
    public void Start()
    {
		State = UseState.Can_Use;
    }

	protected override void OnUseStateChanged(UseState oldState, UseState newState)
	{
		base.OnUseStateChanged(oldState, newState);
		Player.INSTANCE._inventory.SetIcon(ID, InventoryManager.InventorySection.Active_Relic, _brokenIcon);
	}
}
