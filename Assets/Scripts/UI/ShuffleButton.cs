using UnityEngine;
using UnityEngine.EventSystems;

public class ShuffleButton : MonoBehaviour, IPointerDownHandler
{
	public float _doubleClickThreshold = 0.5f;
	private float _timeSinceLastClick = 0.0f;

	private bool _hasFirstClick = false;

	private void Update()
	{
		if (_hasFirstClick)
		{
			if (_timeSinceLastClick > _doubleClickThreshold)
			{
				_hasFirstClick = false;
			}
			_timeSinceLastClick += Time.deltaTime;
		}
	}
	public void OnPointerDown(PointerEventData eventData)
	{
		if (!GameBoard.INSTANCE.IsSettled())
			return;

		if (!_hasFirstClick)
		{
			_hasFirstClick = true;
			_timeSinceLastClick = 0;
		}
		else
		{
			GameBoard.INSTANCE.Shuffle();
			_hasFirstClick = false;
			
			// break combo if it exists

			BattleManager.INSTANCE.BreakCombo();
			BattleManager.INSTANCE.SetBattleState(BattleManager.BattleState.Post_Player_Turn);
		}
	}
}
