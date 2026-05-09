using System;
using UnityEngine;

public class YouSubstitution : ActiveRelic
{
    private bool _inUse;
    private bool _canUse;

    public override void OnEnterRunEvent()
    {
        _canUse = true;
    }

    public override void OnSelect()
    {
        if (_canUse)
        {
            _canUse = false;
            Player.INSTANCE._substitute = true;
            int loss = Mathf.RoundToInt(Player.INSTANCE.MaxHealth * 0.2f);
            Player.INSTANCE.CurrentHealth = Mathf.Max(Player.INSTANCE.CurrentHealth - loss, 1);
        }
        EndUse();

    }
    
    public override void EndUse()
    {
        base.EndUse();
    }
    
    
}
