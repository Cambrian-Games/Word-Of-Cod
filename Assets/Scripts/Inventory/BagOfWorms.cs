using UnityEngine;
using TMPro;

public class BagOfWorms : Item
{
    public TMP_Text _countText;
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
    }

    public override void OnSelect()
    {
        if (_currentCount > 0)
        {
            _currentCount--;
            _countText.text = _currentCount.ToString();
            Player.INSTANCE.CurrentHealth += 50;
        }
    }

    public override void OnTileClicked(Tile tile)
    {
    }
    
    
}
