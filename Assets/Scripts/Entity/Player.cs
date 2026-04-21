using UnityEngine;

public class Player : Entity
{
	public static Player INSTANCE;

	public InventoryManager _inventory;



	protected override void Awake()
	{
		base.Awake();

		// set up singleton

		if (INSTANCE != null && INSTANCE != this)
		{
			Debug.LogError("Attempted to create second player!");
			Destroy(gameObject);
			return;
		}

		INSTANCE = this;
	}

	protected override void OnTakeDamage(int damageTaken)
	{
		_inventory.OnPlayerTakeDamage();
	}
}