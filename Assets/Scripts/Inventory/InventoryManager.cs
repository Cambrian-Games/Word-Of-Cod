using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;
using Random = UnityEngine.Random;

public class InventoryManager : MonoBehaviour
{
    public List<Relic> _passiveRelics;
	public List<Item> _activeRelics;

    private Dictionary<RelicEffect.EventTiming, HashSet<Relic>> _sortedPassiveRelics;

    public List<int> _passiveRelicInventory = new List<int>();
	public List<int> _activeRelicInventory = new List<int>();

    public GameObject _passiveRelicGrid;
    public GameObject _activeRelicGrid;

    private int _prevNumPassive = 1;
    private int _prevNumActive = 1;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
		InitPassiveRelics();
		InitActiveRelics();

		GameObject.Find("Tooltip Text").GetComponent<TMP_Text>().text = "";
	}

	private void InitPassiveRelics()
	{
		Debug.Log(_passiveRelicInventory);

		_sortedPassiveRelics?.Clear();

		_sortedPassiveRelics = new Dictionary<RelicEffect.EventTiming, HashSet<Relic>>();

		for (int i = 0; i < _passiveRelics.Count; i++)
		{
			_passiveRelics[i].SetID(i);

			List<RelicEffect> effects = _passiveRelics[i].Effects;

			foreach (RelicEffect eff in effects)
			{
				if (!_sortedPassiveRelics.ContainsKey(eff.Event))
				{
					_sortedPassiveRelics.Add(eff.Event, new HashSet<Relic>());
				}

				_sortedPassiveRelics[eff.Event].Add(_passiveRelics[i]);
			}
		}

		_passiveRelicInventory.Add(Random.Range(0, _passiveRelics.Count));

		//sets the starting relic icon
		_passiveRelicGrid.transform.GetChild(0).gameObject.GetComponent<Image>().sprite =
			_passiveRelics[_passiveRelicInventory[0]].Icon;
	}
	private void InitActiveRelics()
	{
		Debug.Log(_activeRelicInventory);

		for (int i = 0; i < _activeRelics.Count; i++)
		{
			_activeRelics[i].SetID(i);
		}

		_activeRelicInventory.Add(Random.Range(0, _activeRelics.Count));
		//_activeRelicInventory.Add(1);

		_activeRelicGrid.transform.GetChild(0).gameObject.GetComponent<Image>().sprite =
			_activeRelics[_activeRelicInventory[0]].Icon;

		_activeRelicGrid.transform.GetChild(0).gameObject.SetActive(true);
	}

	// Update is called once per frame
	void Update()
    {
        //if number of relics has changed:
        if (_passiveRelicInventory.Count > _prevNumPassive)
        {
            //for each new relic:
            for (int i = _prevNumPassive; i < _passiveRelicInventory.Count; i++)
            {
                //set its icon...
                _passiveRelicGrid.transform.GetChild(i).gameObject.GetComponent<Image>().sprite =
                    _passiveRelics[_passiveRelicInventory[i]].Icon;
                //and enable the element in the grid
                _passiveRelicGrid.transform.GetChild(i).gameObject.SetActive(true);
            }
            
        }
        //todo set up active relic change
        if (_activeRelicInventory.Count > _prevNumActive)
        {
	        //for each new relic:
	        for (int i = _prevNumActive; i < _activeRelicInventory.Count; i++)
	        {
		        //set its icon...
		        _activeRelicGrid.transform.GetChild(i).gameObject.GetComponent<Image>().sprite =
			        _activeRelics[_activeRelicInventory[i]].Icon;
		        //and enable the element in the grid
		        _activeRelicGrid.transform.GetChild(i).gameObject.SetActive(true);
	        }
            
        }
        _prevNumPassive = _passiveRelicInventory.Count;
        _prevNumActive = _activeRelicInventory.Count;
    }

    public void OnWordSubmit(Word word)
    {
        RelicEffect.Result result = new RelicEffect.Result();

        if (!_sortedPassiveRelics.ContainsKey(RelicEffect.EventTiming.On_Word_Submit))
            return;

        foreach (Relic relic in _sortedPassiveRelics[RelicEffect.EventTiming.On_Word_Submit])
        {
            if (!_passiveRelicInventory.Contains(relic.ID))
                continue;

            result += relic.OnWordSubmit(word);
        }

        if (result._values.Count == 0)
            return;

        word.ModifyDamage(result);

        foreach (var item in result._values)
        {
            if (item.Key == RelicEffect.ValueToModify.Damage_Percent_Increase)
                continue;

            if (item.Key == RelicEffect.ValueToModify.Damage_Bonus)
                continue;

            Debug.LogError($"Unsupported modification of {item.Key} during OnWordSubmit");
        }

		word.LogPassiveRelicsUsed(result);
    }

    internal void OnEnemyAttack(float baseDamage, out float modifiedDamage)
    {
        RelicEffect.Result result = new RelicEffect.Result();

        if (!_sortedPassiveRelics.ContainsKey(RelicEffect.EventTiming.On_Enemy_Attack))
        {
            modifiedDamage = baseDamage;
            return;
        }

        foreach (Relic relic in _sortedPassiveRelics[RelicEffect.EventTiming.On_Enemy_Attack])
        {
            if (!_passiveRelicInventory.Contains(relic.ID))
                continue;

            result += relic.OnEnemyAttack(baseDamage);
        }

        if (result._values.Count == 0)
        {
            modifiedDamage = baseDamage;
            return;
        }

        float totalResistPercent = result._values.GetValueOrDefault(RelicEffect.ValueToModify.Resist_Percent_Increase)
            + result._values.GetValueOrDefault(RelicEffect.ValueToModify.Enemy_Damage_Resist_Percent_Increase);

        float totalResistBonus = result._values.GetValueOrDefault(RelicEffect.ValueToModify.Resist_Bonus)
            + result._values.GetValueOrDefault(RelicEffect.ValueToModify.Enemy_Damage_Resist_Bonus);

        modifiedDamage = (baseDamage * (1 - totalResistPercent) - totalResistBonus);

        foreach (var item in result._values)
        {
            if (item.Key == RelicEffect.ValueToModify.Resist_Percent_Increase)
                continue;

            if (item.Key == RelicEffect.ValueToModify.Enemy_Damage_Resist_Percent_Increase)
                continue;

            if (item.Key == RelicEffect.ValueToModify.Resist_Bonus)
                continue;

            if (item.Key == RelicEffect.ValueToModify.Enemy_Damage_Resist_Bonus)
                continue;

            Debug.LogError($"Unsupported modification of {item.Key} during OnEnemyAttack");
        }
    }

	internal void OnActiveRelicClicked(int inventoryIndex)
	{
		_activeRelics[_activeRelicInventory[inventoryIndex]].OnSelect();
	}

	internal void OnBattleStateChanged(BattleManager.BattleState oldState, BattleManager.BattleState newState)
	{
		foreach (int activeRelicID in _activeRelicInventory)
		{
			_activeRelics[activeRelicID].OnBattleStateChanged(oldState, newState);
		}
	}

	internal void OnTileClicked(Tile tile)
	{
		foreach (int activeRelicID in _activeRelicInventory)
		{
			_activeRelics[activeRelicID].OnTileClicked(tile);
		}
	}

	internal void OnPlayerTakeDamage()
	{
		if (_activeRelicInventory.Contains(1))
		{
			SalmonStone stone = _activeRelics[1].gameObject.GetComponent<SalmonStone>();
			if (!stone.GetUsed())
			{
				if (Player.INSTANCE.CurrentHealth <= 0)
				{
					Player.INSTANCE.Heal(Mathf.FloorToInt(Player.INSTANCE.MaxHealth * 0.3f));
					stone.SetUsed(true);
					int index = _activeRelicInventory.IndexOf(1);
					_activeRelicGrid.transform.GetChild(index).gameObject.GetComponent<Image>().sprite =
						stone._brokenIcon;
				}
			}
		}
	}
}
