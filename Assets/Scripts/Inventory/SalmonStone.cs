using UnityEngine;

public class SalmonStone : ActiveRelic
{
    private bool _used = false;
    public Sprite _brokenIcon;
    
    public bool getUsed() {return _used;}

    public void setUsed(bool used)
    {
        _used = used;
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _used = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    
    
    
    //unused here
    public override void OnBattleStateChanged(BattleManager.BattleState oldState, BattleManager.BattleState newState)
    {
        
    }
    
    public override void OnSelect()
    {
        
    }

    public override void OnTileClicked(Tile tile)
    {

    }

}
