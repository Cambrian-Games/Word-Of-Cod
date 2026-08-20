using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
	public enum InventorySection
	{
		Passive_Relic,
		Active_Relic,
		Consumable_Item,
	}

    public struct InventoryReference
    {
        public InventorySection _section;
        public int _id;

        public InventoryReference(InventorySection section, int id)
        {
            _section = section;
            _id = id;
        }

        public readonly Relic PassiveRelic()
        {
            if (_section == InventorySection.Passive_Relic)
            {
                return Player.INSTANCE._inventory._passiveRelics[_id];
            }
            Debug.Assert(_section == InventorySection.Passive_Relic);
            return null;
        }

        public readonly ActiveRelic ActiveRelic()
        {
            if (_section == InventorySection.Active_Relic)
            {
                return (ActiveRelic) Player.INSTANCE._inventory._activeRelics[_id];
            }
            Debug.Assert(_section == InventorySection.Active_Relic);
            return null;
        }

        public readonly Item ConsumableItem()
        {
            if (_section == InventorySection.Consumable_Item)
            {
                return Player.INSTANCE._inventory._consumables[_id];
            }
            Debug.Assert(_section == InventorySection.Consumable_Item);
            return null;
        }

        public readonly IDisplayInfo DisplayInfo()
        {
            IDisplayInfo displayInfo = _section switch
            {
                InventorySection.Passive_Relic => Player.INSTANCE._inventory._passiveRelics[_id],
                InventorySection.Active_Relic => Player.INSTANCE._inventory._activeRelics[_id],
                InventorySection.Consumable_Item => Player.INSTANCE._inventory._consumables[_id],
                _ => throw new InvalidOperationException(),
            };

            return displayInfo;
        }

        public override readonly string ToString()
        {
            return _section switch
            {
                InventorySection.Passive_Relic => Player.INSTANCE._inventory._passiveRelics[_id].DisplayName + " (Passive Relic)",
                InventorySection.Active_Relic => Player.INSTANCE._inventory._activeRelics[_id].DisplayName + " (Active Relic)",
                InventorySection.Consumable_Item => Player.INSTANCE._inventory._consumables[_id].DisplayName + " (Consumable Item)",
                _ => throw new InvalidOperationException(),
            };
        }


    }

	public List<Relic> _passiveRelics;
	public List<Item> _activeRelics;
	public List<Item> _consumables;

    private Dictionary<RelicEffect.EventTiming, HashSet<Relic>> _sortedPassiveRelics;

    public List<int> _passiveRelicInventory = new List<int>();
	public List<int> _activeRelicInventory = new List<int>();

    public GameObject _passiveRelicGrid;
    public GameObject _activeRelicGrid;
    public GameObject _consumableGrid;

    private int _prevNumPassive = 0;
    private int _prevNumActive = 0;

	private Item _itemInUse;

	public int _startingRelics;

	private static Color USABLE_COLOR = Color.white;
	private static Color UNUSABLE_COLOR = Color.grey;
	private static Color IN_USE_COLOR = Color.orange;
	[SerializeField]
	private TMP_Text _tooltip;
    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
		InitPassiveRelics();
		InitActiveRelics();
		InitConsumables();

        List<InventoryReference> relicRefs = GenerateRelicItemReferences(_startingRelics);

        foreach (InventoryReference relicRef in relicRefs)
        {
            GrantRelic(relicRef);
        }

		_tooltip.text = "";
	}

	private void InitPassiveRelics()
	{
		_sortedPassiveRelics?.Clear();

		_sortedPassiveRelics = new Dictionary<RelicEffect.EventTiming, HashSet<Relic>>();

		for (int i = 0; i < _passiveRelics.Count; i++)
		{
			_passiveRelics[i].ID = i;

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
	}
	private void InitActiveRelics()
	{
		for (int i = 0; i < _activeRelics.Count; i++)
		{
			_activeRelics[i].ID = i;
		}
	}

	private void InitConsumables()
	{
		for (int i = 0; i < _consumables.Count; i++)
		{
			_consumables[i].ID = i;
		}
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

		if (TileSelector.INSTANCE.Mode == TileSelector.SelectionMode.Item_Use)
		{
			if (Input.GetKeyDown(KeyCode.Escape) || Input.GetMouseButtonDown((int) MouseButton.Right))
			{
				if (_itemInUse)
				{
					_itemInUse.EndUse();
				}
			}
		}
    }

    #region Inventory Events
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

            if (item.Key == RelicEffect.ValueToModify.Bubble)
            {
	            Player.INSTANCE._bubbleShield += item.Value;
				continue;
            }

            Debug.LogError($"Unsupported modification of {item.Key} during OnWordSubmit");
        }

		word.LogPassiveRelicsUsed(result);
    }

    internal void OnEnemyAttack(float baseDamage, out float modifiedDamage)
    {
	    RelicEffect.Result result = new RelicEffect.Result();

	    if (_sortedPassiveRelics.ContainsKey(RelicEffect.EventTiming.On_Enemy_Attack))
	    {
		    foreach (Relic relic in _sortedPassiveRelics[RelicEffect.EventTiming.On_Enemy_Attack])
		    {
			    if (!_passiveRelicInventory.Contains(relic.ID))
				    continue;

			    result += relic.OnEnemyAttack(baseDamage);
		    }
	    }

	    if (result._values.Count == 0 && Player.INSTANCE._bubbleShield == 0 && !Player.INSTANCE._substitute)
	    {
		    modifiedDamage = baseDamage;
		    return;
	    }
	    
        float totalResistPercent = result._values.GetValueOrDefault(RelicEffect.ValueToModify.Resist_Percent_Increase)
            + result._values.GetValueOrDefault(RelicEffect.ValueToModify.Enemy_Damage_Resist_Percent_Increase);

        float totalResistBonus = result._values.GetValueOrDefault(RelicEffect.ValueToModify.Resist_Bonus)
            + result._values.GetValueOrDefault(RelicEffect.ValueToModify.Enemy_Damage_Resist_Bonus) + Player.INSTANCE._bubbleShield;

        //if player you substition active deal 0
        if (Player.INSTANCE._substitute)
        {
	        modifiedDamage = 0;
	        Player.INSTANCE._substitute = false;
        }
        else
        {
	        modifiedDamage = (baseDamage * (1 - totalResistPercent) - totalResistBonus);
        }

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
            
            if (item.Key == RelicEffect.ValueToModify.Bubble)
				continue;

            Debug.LogError($"Unsupported modification of {item.Key} during OnEnemyAttack");
        }
    }

	internal void OnItemClicked(InventorySection section, int inventoryIndex)
	{
		if (!_itemInUse)
		{
			switch (section)
			{
				case InventorySection.Passive_Relic:
					break;

				case InventorySection.Active_Relic:
					Item targetRelic = _activeRelics[_activeRelicInventory[inventoryIndex]];

					if (targetRelic.State == Item.UseState.Can_Use)
					{
						_itemInUse = targetRelic;
						_itemInUse.OnSelect();
					}
					break;

				case InventorySection.Consumable_Item:
					Item targetConsumable = _consumables[inventoryIndex];

					if (targetConsumable.State == Item.UseState.Can_Use && targetConsumable._currentCount > 0)
					{
						_itemInUse = targetConsumable;
						_itemInUse.OnSelect();
					}
					break;
			}
		}
	}

	internal void OnBattleStateChanged(BattleManager.BattleState oldState, BattleManager.BattleState newState)
	{
		foreach (int activeRelicID in _activeRelicInventory)
		{
			_activeRelics[activeRelicID].OnBattleStateChanged(oldState, newState);
		}

		foreach (Item consumable in _consumables)
		{
			consumable.OnBattleStateChanged(oldState, newState);
		}
	}

	internal void OnEnterRunEvent()
	{
		foreach (int activeRelicID in _activeRelicInventory)
		{
			_activeRelics[activeRelicID].OnEnterRunEvent();
		}

		foreach (Item consumable in _consumables)
		{
			consumable.OnEnterRunEvent();
		}
	}
	
	internal void OnTileClicked(Tile tile)
	{
		_itemInUse.OnTileClicked(tile);
	}

	internal void OnPlayerTakeDamage()
	{
		Item salmonStone = _activeRelics.Where(relic => relic.GetComponent<SalmonStone>()).FirstOrDefault();

		bool hasSalmonStone = salmonStone && _activeRelicInventory.Contains(salmonStone.ID);
		bool canUseSalmonStone = salmonStone.State == Item.UseState.Can_Use && Player.INSTANCE.CurrentHealth <= 0;

		if (hasSalmonStone && canUseSalmonStone)
		{
			Player.INSTANCE.Heal(Mathf.FloorToInt(Player.INSTANCE.MaxHealth * 0.3f));
			salmonStone.State = Item.UseState.Unususable;
		}
	}

	internal void EndItemUse(Item item)
	{
		Debug.Assert(_itemInUse == item);
		_itemInUse = null;
	}
    #endregion

    public void GrantRelic(InventoryReference relicRef)
    {
        Debug.Assert(relicRef._section != InventorySection.Consumable_Item);

        switch (relicRef._section)
        {
            case InventorySection.Active_Relic:
                Debug.Assert(!_activeRelicInventory.Contains(relicRef._id));
                _activeRelicInventory.Add(relicRef._id);
                break;
            case InventorySection.Passive_Relic:
                Debug.Assert(!_passiveRelicInventory.Contains(relicRef._id));
                _passiveRelicInventory.Add(relicRef._id);
                break;
        }
    }

	internal void SetIconColorFromUseState(int id, InventorySection section, Item.UseState newState)
	{
		int index = section switch
		{
			InventorySection.Passive_Relic => _passiveRelicInventory.IndexOf(id),
			InventorySection.Active_Relic => _activeRelicInventory.IndexOf(id),
			InventorySection.Consumable_Item => id,
			_ => throw new System.NotImplementedException()
		};

		// This is an unobtained item, ignore it
		if (index == -1)
			return;

		GameObject grid = section switch
		{
			InventorySection.Passive_Relic => _passiveRelicGrid,
			InventorySection.Active_Relic => _activeRelicGrid,
			InventorySection.Consumable_Item => _consumableGrid,
			_ => throw new System.NotImplementedException(),
		};

		Image icon = grid.transform.GetChild(index).GetComponent<Image>();

		icon.color = newState switch
		{
			Item.UseState.Unususable => UNUSABLE_COLOR,
			Item.UseState.Can_Use => USABLE_COLOR,
			Item.UseState.In_Use => IN_USE_COLOR,
			_ => throw new System.NotImplementedException(),
		};
	}

	internal void SetIcon(int id, InventorySection section, Sprite newSprite)
	{
		int index = section switch
		{
			InventorySection.Passive_Relic => _passiveRelicInventory.IndexOf(id),
			InventorySection.Active_Relic => _activeRelicInventory.IndexOf(id),
			InventorySection.Consumable_Item => id,
			_ => throw new System.NotImplementedException()
		};

		// This is an unobtained item, ignore it
		if (index == -1)
			return;

		GameObject grid = section switch
		{
			InventorySection.Passive_Relic => _passiveRelicGrid,
			InventorySection.Active_Relic => _activeRelicGrid,
			InventorySection.Consumable_Item => _consumableGrid,
			_ => throw new System.NotImplementedException(),
		};

		Image icon = grid.transform.GetChild(index).GetComponent<Image>();
		icon.sprite = newSprite;
	}

    public List<InventoryReference> GenerateRelicItemReferences(int count)
    {
        return GenerateRelicItemReferences(count, new float[] { 1, 1 });
    }

    public List<InventoryReference> GenerateRelicItemReferences(int count, float[] sectionWeights, float[] passiveRelicWeights = null, List<InventoryReference> relicsToIgnore = null)
    {
        if (count <= 0)
            return new List<InventoryReference>();

        Debug.Assert(sectionWeights.Length == 2 && (sectionWeights[0] > 0 || sectionWeights[1] > 0));

        Relic.FPASSIVEPOOL[] poolEnums = (Relic.FPASSIVEPOOL[])Enum.GetValues(typeof(Relic.FPASSIVEPOOL));
        Debug.Assert(passiveRelicWeights == null || passiveRelicWeights.Length == poolEnums.Length);

        // set up relic pools

        List<InventoryReference> activeRefs = _activeRelics
            .Where(relic => !_activeRelicInventory.Contains(relic.ID))
            .Select(relic => relic.AsInventoryReference())
            .ToList();

        List<InventoryReference> passiveRefs = new List<InventoryReference>();

        Dictionary<Relic.FPASSIVEPOOL, List<InventoryReference>> pools = new Dictionary<Relic.FPASSIVEPOOL, List<InventoryReference>>
        {
            [Relic.FPASSIVEPOOL.OTHER] = new List<InventoryReference>(),
            [Relic.FPASSIVEPOOL.LETTER] = new List<InventoryReference>(),
            [Relic.FPASSIVEPOOL.PART_OF_SPEECH] = new List<InventoryReference>()
        };

        foreach (Relic relic in _passiveRelics)
        {
            if (_passiveRelicInventory.Contains(relic.ID))
                continue;

            passiveRefs.Add(relic.AsInventoryReference());

            if (relic.PassivePools == Relic.FPASSIVEPOOL.OTHER)
            {
                pools[Relic.FPASSIVEPOOL.OTHER].Add(relic.AsInventoryReference());
                continue;
            }
            else
            {
                // start at 1 to skip the Relic.FPASSIVEPOOL.OTHER case
                for (int poolEnumIndex = 1; poolEnumIndex < poolEnums.Length; poolEnumIndex++)
                {
                    if ((relic.PassivePools & poolEnums[poolEnumIndex]) != 0)
                    {
                        pools[poolEnums[poolEnumIndex]].Add(relic.AsInventoryReference());
                    }
                }
            }
        }

        // clear out ignored relics

        if (relicsToIgnore != null)
        {
            foreach (InventoryReference relicRef in relicsToIgnore)
            {
                if (relicRef._section == InventorySection.Active_Relic)
                    activeRefs.Remove(relicRef);

                else if (relicRef._section == InventorySection.Passive_Relic)
                {
                    passiveRefs.Remove(relicRef);

                    // O(n) if the relic isn't in the pool, but might be faster than looking up the relic and checking its ID?
                    pools[Relic.FPASSIVEPOOL.OTHER].Remove(relicRef);
                    pools[Relic.FPASSIVEPOOL.LETTER].Remove(relicRef);
                    pools[Relic.FPASSIVEPOOL.PART_OF_SPEECH].Remove(relicRef);
                }
            }
        }

        // generate initial weights

        float activeWeight, passiveWeight;
        float[] poolWeights = new float[poolEnums.Length];

        UpdatePoolWeights();

        if (activeWeight + passiveWeight == 0)
            return new List<InventoryReference>();

        List<InventoryReference> relicsSelected = new List<InventoryReference>();

        while (relicsSelected.Count < count)
        {
            float category = UnityEngine.Random.Range(0.0f, 1.0f) * (activeWeight + passiveWeight);
            float selection = UnityEngine.Random.Range(0.0f, 1.0f);

            if (category < activeWeight)
            {
                relicsSelected.Add(InventoryReferenceFromUValue(InventorySection.Active_Relic, selection));
            }
            else
            {
                if (passiveRelicWeights != null)
                {
                    category -= activeWeight;

                    for (int i = 0; i < poolEnums.Length; i++)
                    {
                        if (category < poolWeights[i])
                        {
                            relicsSelected.Add(InventoryReferenceFromUValue(InventorySection.Passive_Relic, selection, i));
                            break;
                        }
                        else
                        {
                            category -= poolWeights[i];
                        }
                    }
                }
                else
                {
                    relicsSelected.Add(InventoryReferenceFromUValue(InventorySection.Passive_Relic, selection));
                }
            }

            UpdatePoolWeights();

            if (activeWeight + passiveWeight == 0)
                break;
        }

        return relicsSelected;

        // Local helper functions

        void UpdatePoolWeights()
        {
            activeWeight = activeRefs.Count > 0 ? sectionWeights[(int)InventorySection.Active_Relic] : 0;
            passiveWeight = passiveRefs.Count > 0 ? sectionWeights[(int)InventorySection.Passive_Relic] : 0;

            // no work left to do
            if (activeWeight + passiveWeight == 0 || passiveRelicWeights == null)
                return;

            float totalSubWeight = 0;

            for (int i = 0; i < poolEnums.Length; i++)
            {
                totalSubWeight += (pools[poolEnums[i]].Count > 0 ? 1 : 0) * passiveRelicWeights[i];
            }

            if (totalSubWeight == 0)
            {
                Debug.LogError("No relics in sub-pools, but there are still unclaimed passive relic IDs");
                passiveWeight = 0;
                return;
            }

            float scaleFactor = passiveWeight / totalSubWeight;

            for (int i = 0; i < poolEnums.Length; i++)
            {
                poolWeights[0] = (pools[poolEnums[i]].Count > 0 ? 1 : 0) * passiveRelicWeights[i] * scaleFactor;
            }
        }

        InventoryReference InventoryReferenceFromUValue(InventorySection section, float uSelection, int subpoolIndex = -1)
        {
            List<InventoryReference> pool;

            if (section == InventorySection.Active_Relic)
            {
                pool = activeRefs;
            }
            else if (subpoolIndex == -1)
            {
                pool = passiveRefs;
            }
            else
            {
                pool = pools[(Relic.FPASSIVEPOOL)subpoolIndex];
            }

            InventoryReference newRef = pool[(int)uSelection * pool.Count];

            pool.Remove(newRef);

            if (section == InventorySection.Passive_Relic)
            {
                passiveRefs.Remove(newRef);
            }

            return newRef;
        }
    }
}
