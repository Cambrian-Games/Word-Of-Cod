using UnityEngine;

public class Player : Entity
{
	public static Player INSTANCE;

	private void Awake()
	{
		// set up singleton

		if (INSTANCE != null && INSTANCE != this)
		{
			Debug.LogError("Attempted to create second player!");
			Destroy(gameObject);
			return;
		}

		INSTANCE = this;

		Init();
	}
}