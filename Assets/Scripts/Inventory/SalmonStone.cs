using UnityEngine;

public class SalmonStone : ActiveRelic
{
    internal bool _used = false;
    public Sprite _brokenIcon;



    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _used = false;
    }
}
