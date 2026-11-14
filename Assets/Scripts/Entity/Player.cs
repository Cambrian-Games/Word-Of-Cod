using UnityEngine;

public class Player : Entity
{
	public static Player INSTANCE;

	public InventoryManager _inventory;

	public float _bubbleShield =  0f;

	public bool _substitute = false;

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

	void Start()
	{
		MaxHealth = MaxHealth + SaveManager.INSTANCE._saveData._metaHealthPoints * 20;
		CurrentHealth = MaxHealth;
	}

	protected override void OnTakeDamage(int damageTaken)
	{
		_inventory.OnPlayerTakeDamage();
	}
}