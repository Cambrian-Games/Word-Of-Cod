using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class ShopManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    public Button _relicChoiceButton;

    public InventoryManager _inventoryManger;

    public RunManager _runManager;

    private int _relicChoice = -1;
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
        for (int i = 0; i < _inventoryManger._passiveRelics.Count; i++)
        {
            if (!_inventoryManger._passiveRelicInventory.Contains(i))
            {
                unownedRelics.Add(i);
            }
        }

        _relicChoice = unownedRelics[Random.Range(0, unownedRelics.Count)];
        _relicChoiceButton.GetComponent<Image>().sprite = _inventoryManger._passiveRelics[_relicChoice].Icon;
        
        //throw new NotImplementedException();
    }

    public void AddRelic()
    {
        if (_relicChoice != -1)
        {
            _inventoryManger._passiveRelicInventory.Add(_relicChoice);
        }
        
        _relicChoiceButton.gameObject.SetActive(false);
    }

    public void LeaveShop()
    {
        gameObject.SetActive(false);
        _runManager.SetRunState(RunManager.RunState.Traveling_To_Next_Event);
    }
}
