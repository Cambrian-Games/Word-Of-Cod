using TMPro;
using UnityEngine;

public abstract class Item : MonoBehaviour
{
	[SerializeField]
	protected int _maxCount;
	public int _currentCount;

	[SerializeField]
	private Sprite _icon;
	public Sprite Icon => _icon;

	public TMP_Text _countText;

	private int _id = -1; // will be assigned by the inventory manager

	public int ID { get => _id; set => SetID(value); }

	private void SetID(int i)
	{
		if (_id != -1 && _id != i)
		{
			Debug.LogWarning($"Overwriting ID {_id} with {i}");
		}

		_id = i;
	}

	[SerializeField]
	private string _displayName;
	public string DisplayName => _displayName;

	[SerializeField]
	private string _description;
	public string Description => _description;

	public enum UseState
	{
		Nil = -1,

		// Consider adding Not_Player_Turn or some other way to modify the state that doesn't alter the color.
		//  An item or relic can be unusable due to not being the player's turn OR some other gameplay-related reason,
		//  and it would be nice to reflect that
		Unususable,
		Can_Use,
		In_Use
	}

	private UseState _state = UseState.Nil;
	public UseState State { get => _state; set => SetState(value); }

	private void SetState(UseState newState)
	{
		if (_state == newState)
			return;

		UseState oldState = _state;
		_state = newState;
		OnUseStateChanged(oldState, newState);
	}

	public virtual void OnSelect() { }
	public virtual void OnBattleStateChanged(BattleManager.BattleState oldBattleState, BattleManager.BattleState newBattleState) { }

	protected virtual void OnUseStateChanged(UseState oldUseState, UseState newUseState)
	{
		Player.INSTANCE._inventory.SetIconColorFromUseState(ID, InventoryManager.InventorySection.Consumable_Item, newUseState);
	}
	public virtual void OnEnterRunEvent() { }
	public virtual void OnTileClicked(Tile tile) { }

	public virtual void EndUse()
	{
		Player.INSTANCE._inventory.EndItemUse(this);
	}
}
