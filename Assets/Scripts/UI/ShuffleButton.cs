using Unity.Services.Analytics;
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
		if (!GameBoard.INSTANCE.IsSettled() || BattleManager.INSTANCE.CurrentState != BattleManager.BattleState.Player_Turn)
			return;

		if (!_hasFirstClick)
		{
			_hasFirstClick = true;
			_timeSinceLastClick = 0;
		}
		else
		{
			TileSelector.INSTANCE.DeselectAllTiles();
			GameBoard.INSTANCE.Shuffle();
			//Shuffle Log analytics
			ShuffleEvent shuffleEvent = new ShuffleEvent() { _enemyIndex = RunManager.INSTANCE.CurrentEvent._eventIndex,
				_enemyName = BattleManager.INSTANCE.CurrentEnemy.name};
			AnalyticsService.Instance.RecordEvent(shuffleEvent);
			Debug.Log("ShuffleEventSent");

			_hasFirstClick = false;
			
			BattleManager.INSTANCE.SetBattleState(BattleManager.BattleState.Post_Player_Turn);
		}
	}
}
