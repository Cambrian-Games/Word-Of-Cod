using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class BattleRewardManager : MonoBehaviour
{
    public Button _relicChoiceButton;

    [HideInInspector]
    public int _relicChoice = -1;
    
    private void OnEnable()
    {
        _relicChoiceButton.gameObject.SetActive(true);

        //choose new unowned relic and display
        List<int> unownedRelics = new List<int>();
        
        for (int i = 0; i < Player.INSTANCE._inventory._passiveRelics.Count; i++)
        {
            if (!Player.INSTANCE._inventory._passiveRelicInventory.Contains(i))
            {
                unownedRelics.Add(i);
            }
        }

        for (int i = 0; i < Player.INSTANCE._inventory._activeRelics.Count; i++)
        {
            if (!Player.INSTANCE._inventory._activeRelicInventory.Contains(i))
            {
                //offset active relics by passive count
                unownedRelics.Add(i + Player.INSTANCE._inventory._passiveRelics.Count);
            }
        }

        _relicChoice = unownedRelics[Random.Range(0, unownedRelics.Count)];
        if (_relicChoice < Player.INSTANCE._inventory._passiveRelics.Count)
        {
            _relicChoiceButton.GetComponent<Image>().sprite = Player.INSTANCE._inventory._passiveRelics[_relicChoice].Icon;
        }
        else
        {
            _relicChoiceButton.GetComponent<Image>().sprite = Player.INSTANCE._inventory._activeRelics[_relicChoice - Player.INSTANCE._inventory._passiveRelics.Count].Icon;
        }
        
        //throw new NotImplementedException();
    }

    public void AddRelic()
    {
        if (_relicChoice != -1)
        {
            if (_relicChoice < Player.INSTANCE._inventory._passiveRelics.Count)
            {
                Player.INSTANCE._inventory._passiveRelicInventory.Add(_relicChoice);
            }
            else
            {
                Player.INSTANCE._inventory._activeRelicInventory.Add(_relicChoice - Player.INSTANCE._inventory._passiveRelics.Count);
            }
        }
        
        _relicChoiceButton.gameObject.SetActive(false);
    }

    public void LeaveShop()
    {
        gameObject.SetActive(false);
        //RunManager.INSTANCE.SetRunState(RunManager.RunState.Traveling_To_Next_Event);
        BattleManager.INSTANCE.Unload();
    }
}
