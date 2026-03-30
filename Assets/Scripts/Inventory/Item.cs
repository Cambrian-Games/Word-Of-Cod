using UnityEngine;

public abstract class Item : MonoBehaviour
{
	[SerializeField]
	protected int _maxCount;
	public int _currentCount;

	[SerializeField]
	protected bool _isConsumable = true;

	[SerializeField]
	private Sprite _icon;
	public Sprite Icon => _icon;

	private int _id = -1; // will be assigned by the inventory manager

	public int ID => _id;

	public void SetID(int i)
	{
		if (_id != -1 && _id != i)
		{
			Debug.LogWarning($"Overwriting ID {_id} with {i}");
		}

		_id = i;
	}

	public void SetIcon(Sprite newIcon)
	{
		_icon = newIcon;
	}
	[SerializeField]
	private string _displayName;
	public string DisplayName => _displayName;

	[SerializeField]
	private string _description;
	public string Description => _description;

	public abstract void OnSelect();
	public abstract void OnBattleStateChanged(BattleManager.BattleState oldState, BattleManager.BattleState newState);
	public abstract void OnTileClicked(Tile tile);
}
