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

        public override readonly string ToString()
        {
            return _section switch
            {
                InventorySection.Passive_Relic => Player.INSTANCE._inventory._passiveRelics[_id].DisplayName + " (Passive Relic)",
                InventorySection.Active_Relic => Player.INSTANCE._inventory._activeRelics[_id].DisplayName + " (Active Relic)",
                InventorySection.Consumable_Item => Player.INSTANCE._inventory._consumables[_id].DisplayName + " (Consumable Item)",
                _ => "",
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

        //for (int i = 0; i < _startingRelics; i++)
        //{
        //	GrantRelic();
        //}

        List<InventoryReference> relicRefs = GenerateItemReferences(_startingRelics);
        List<InventoryReference> test = GenerateItemReferences(_passiveRelics.Count(), new float[] { 100, 1 });

        foreach (InventoryReference relicRef in relicRefs)
        {
            GrantRelic(relicRef);
        }

		_tooltip.text = "";
	}

	private void InitPassiveRelics()
	{
		Debug.Log(_passiveRelicInventory);

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

		//_passiveRelicInventory.Add(Random.Range(0, _passiveRelics.Count));

		//sets the starting relic icon
		//_passiveRelicGrid.transform.GetChild(0).gameObject.GetComponent<Image>().sprite =
		//	_passiveRelics[_passiveRelicInventory[0]].Icon;
	}
	private void InitActiveRelics()
	{
		Debug.Log(_activeRelicInventory);

		for (int i = 0; i < _activeRelics.Count; i++)
		{
			_activeRelics[i].ID = i;
		}

		//_activeRelicInventory.Add(Random.Range(0, _activeRelics.Count));
		////_activeRelicInventory.Add(1);
//
		//_activeRelicGrid.transform.GetChild(0).gameObject.GetComponent<Image>().sprite =
		//	_activeRelics[_activeRelicInventory[0]].Icon;
//
		//_activeRelicGrid.transform.GetChild(0).gameObject.SetActive(true);
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

	public void GrantRelic()
	{
		int totalRelics = _activeRelics.Count + _passiveRelics.Count;
		bool granted = false;
		
		//if all relics owned
		if (_activeRelicInventory.Count + _passiveRelicInventory.Count == totalRelics)
		{
			return;
		}
		
		while (!granted)
		{
			//choose a random relic
			int relicToGrant = (int)Mathf.Floor(Random.Range(0.0f, 1.0f) * totalRelics);
			Debug.Log("Relic to Give:" + relicToGrant);
			
			//check to see if its within the passive list
			if (relicToGrant < _passiveRelics.Count)
			{
				//check to avoid duplicates
				if (_passiveRelicInventory.Contains(relicToGrant))
				{
					continue;
				}
				
				//add relic
				_passiveRelicInventory.Add(relicToGrant);
				Debug.Log("Passive Added");
				granted = true;
			}
			//otherwise it's in the active list
			else
			{
				//offset the number by the passive relic list count
				relicToGrant -= _passiveRelics.Count;
				
				//prevent duplicates
				if (_activeRelicInventory.Contains(relicToGrant))
				{
					continue;
				}
				
				//add relic
				_activeRelicInventory.Add(relicToGrant);
				granted = true;
			}
			
		}
	}

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

    public List<InventoryReference> GenerateItemReferences(int count)
    {
        return GenerateItemReferences(count, new float[] { 1, 1 });
    }

    public List<InventoryReference> GenerateItemReferences(int count, float[] sectionWeights, float[] passiveRelicWeights = null, List<InventoryReference> relicsToIgnore = null)
    {
        if (count <= 0)
            return new List<InventoryReference>();

        Debug.Assert(sectionWeights.Length == 2 && (sectionWeights[0] > 0 || sectionWeights[1] > 0));

        // set up relic candidates and selection weights

        List<int> activeIDs = _activeRelics.Select(relic => relic.ID).Where(id => !_activeRelicInventory.Contains(id)).ToList();
        List<int> passiveIDs = _passiveRelics.Select(relic => relic.ID).Where(id => !_passiveRelicInventory.Contains(id)).ToList();

        // Part of Speech, Letter, Other
        List<int>[] sublists = { new List<int>(passiveIDs), new List<int>(passiveIDs), new List<int>(passiveIDs) };

        if (relicsToIgnore != null)
        {
            foreach (InventoryReference ir in relicsToIgnore)
            {
                if (ir._section == InventorySection.Active_Relic)
                    activeIDs.Remove(ir._id);

                else if (ir._section == InventorySection.Passive_Relic)
                {
                    passiveIDs.Remove(ir._id);
                    sublists[0].Remove(ir._id);
                    sublists[1].Remove(ir._id);
                    sublists[2].Remove(ir._id);
                }
            }
        }

        float activeWeight = sectionWeights[0];
        float passiveWeight = sectionWeights[1];

        float[] subWeights = { -1, -1, -1 };

        if (passiveWeight >= 0 && passiveRelicWeights != null)
        {
            Debug.Assert(passiveRelicWeights.Length == 3);
            float totalSubWeight = passiveRelicWeights[0] + passiveRelicWeights[1] + passiveRelicWeights[2];
            float scaleFactor = passiveWeight / totalSubWeight;

            for (int i = 0; i < 3; i++)
            {
                subWeights[i] = passiveRelicWeights[i] * scaleFactor;
            }
        }

        // 

        List<InventoryReference> relicsSelected = new List<InventoryReference>();
        bool canFindMoreRelics = true;

        while (relicsSelected.Count < count && canFindMoreRelics)
        {
            CheckForEmptyRelicPools();

            if (activeWeight + passiveWeight == 0)
                canFindMoreRelics = false;

            float category = Random.Range(0.0f, 1.0f) * (activeWeight + passiveWeight);
            float selection = Random.Range(0.0f, 1.0f);

            if (category < activeWeight)
            {
                relicsSelected.Add(InventoryReferenceFromUValue(InventorySection.Active_Relic, selection));
            }
            else
            {
                if (passiveRelicWeights != null)
                {
                    category -= activeWeight;

                    for (int i = 0; i < subWeights.Length; i++)
                    {
                        if (category < subWeights[i])
                        {
                            relicsSelected.Add(InventoryReferenceFromUValue(InventorySection.Passive_Relic, selection, i));
                            break;
                        }
                        else
                        {
                            category -= subWeights[i];
                        }
                    }
                }
                else
                {
                    relicsSelected.Add(InventoryReferenceFromUValue(InventorySection.Passive_Relic, selection));
                }
            }
        }

        return relicsSelected;

        // Local helper functions

        void CheckForEmptyRelicPools()
        {
            if (activeIDs.Count == 0)
            {
                activeWeight = 0;
            }

            bool[] unclaimedPassives = sublists.Select(list => list.Count > 0).ToArray();

            if (!unclaimedPassives[0] || !unclaimedPassives[1] || !unclaimedPassives[2])
            {
                float totalSubWeight =
                    (unclaimedPassives[0] ? 0 : 1) * passiveRelicWeights[0] +
                    (unclaimedPassives[1] ? 0 : 1) * passiveRelicWeights[1] +
                    (unclaimedPassives[2] ? 0 : 1) * passiveRelicWeights[2];

                if (totalSubWeight == 0)
                {
                    passiveWeight = 0;
                    subWeights = new float[] { 0, 0, 0 };
                }
                else
                {
                    float scaleFactor = passiveWeight / totalSubWeight;
                    subWeights[0] = (unclaimedPassives[0] ? 0 : 1) * passiveRelicWeights[0] * scaleFactor;
                    subWeights[1] = (unclaimedPassives[1] ? 0 : 1) * passiveRelicWeights[1] * scaleFactor;
                    subWeights[2] = (unclaimedPassives[2] ? 0 : 1) * passiveRelicWeights[2] * scaleFactor;
                }
            }
        }

        InventoryReference InventoryReferenceFromUValue(InventorySection section, float uSelection, int subpoolIndex = -1)
        {
            List<int> pool;

            if (section == InventorySection.Active_Relic)
            {
                pool = activeIDs;
            }
            else if (subpoolIndex == -1)
            {
                pool = passiveIDs;
            }
            else
            {
                pool = sublists[subpoolIndex];
            }

            InventoryReference newRef = new InventoryReference
            {
                _section = section,
                _id = pool[(int)(uSelection * pool.Count)]
            };

            pool.Remove(newRef._id);

            if (section == InventorySection.Passive_Relic)
            {
                passiveIDs.Remove(newRef._id);
            }

            return newRef;
        }
    }

    public InventoryReference GenerateActiveRelicReference(List<InventoryReference> relicsToIgnore)
    {
        List<int> targets = _activeRelics.Select(relic => relic.ID).Where(relicID => !_activeRelicInventory.Contains(relicID)).ToList();

        foreach (InventoryReference ir in relicsToIgnore)
        {
            if (ir._section == InventorySection.Active_Relic)
                targets.Remove(ir._id);
        }

        return new InventoryReference
        {
            _section = InventorySection.Active_Relic,
            _id = (int)(Random.Range(0.0f, 1.0f) * targets.Count)
        };
    }
}
