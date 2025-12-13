using UnityEngine;

public class Player : Entity
{
	public static Player INSTANCE;

	protected override void Awake()
	{
		// set up singleton

		base.Awake();

		if (INSTANCE != null && INSTANCE != this)
		{
			Debug.LogError("Attempted to create second player!");
			Destroy(gameObject);
			return;
		}

		INSTANCE = this;
	}
}