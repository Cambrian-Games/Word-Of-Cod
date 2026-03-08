using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour
{
    public List<Relic> _passiveRelics;

    private Dictionary<RelicEffect.EventTiming, HashSet<Relic>> _sortedPassiveRelics;

    public List<int> _passiveRelicInventory = new List<int>();

    public GameObject _relicGrid;

    private int _prevNumRelics = 1;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Debug.Log(_passiveRelicInventory);

        if (_sortedPassiveRelics != null)
        {
            _sortedPassiveRelics.Clear();
        }

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
        _relicGrid.transform.GetChild(0).gameObject.GetComponent<Image>().sprite =
            _passiveRelics[_passiveRelicInventory[0]].Icon;

        GameObject.Find("Tooltip Text").GetComponent<TMP_Text>().text = "";
    }

    // Update is called once per frame
    void Update()
    {
        
        //if number of relics has changed:
        if (_passiveRelicInventory.Count > _prevNumRelics)
        {
            //for each new relic:
            for (int i = _prevNumRelics; i < _passiveRelicInventory.Count; i++)
            {
                //set its icon...
                _relicGrid.transform.GetChild(i).gameObject.GetComponent<Image>().sprite =
                    _passiveRelics[_passiveRelicInventory[i]].Icon;
                //and enable the element in the grid
                _relicGrid.transform.GetChild(i).gameObject.SetActive(true);
            }
            
        }

        _prevNumRelics = _passiveRelicInventory.Count;
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
}
