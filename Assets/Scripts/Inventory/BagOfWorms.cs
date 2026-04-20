using UnityEngine;
using TMPro;

public class BagOfWorms : Item
{
    public TMP_Text _countText;

	private bool _canUse = false;


    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _countText.text = _currentCount.ToString();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	public override void OnBattleStateChanged(BattleManager.BattleState oldState, BattleManager.BattleState newState)
	{
		_canUse = newState == BattleManager.BattleState.Player_Turn;
	}

	public override void OnSelect()
    {
        if (_currentCount > 0 && _canUse)
        {
            _currentCount--;
            _countText.text = _currentCount.ToString();
            Player.INSTANCE.Heal(50);
        }

		EndUse();
    }  
}
