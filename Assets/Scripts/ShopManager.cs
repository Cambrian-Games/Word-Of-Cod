using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class ShopManager : MonoBehaviour
{
    public Button _relicChoiceButton;

    private int _relicChoice = -1;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

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

        _relicChoice = unownedRelics[Random.Range(0, unownedRelics.Count)];
        _relicChoiceButton.GetComponent<Image>().sprite = Player.INSTANCE._inventory._passiveRelics[_relicChoice].Icon;
        
        //throw new NotImplementedException();
    }

    public void AddRelic()
    {
        if (_relicChoice != -1)
        {
            Player.INSTANCE._inventory._passiveRelicInventory.Add(_relicChoice);
        }
        
        _relicChoiceButton.gameObject.SetActive(false);
    }

    public void LeaveShop()
    {
        gameObject.SetActive(false);
        RunManager.INSTANCE.SetRunState(RunManager.RunState.Traveling_To_Next_Event);
    }
}
