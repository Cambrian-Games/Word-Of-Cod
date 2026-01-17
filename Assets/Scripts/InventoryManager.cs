using NUnit.Framework;
using System;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

public class InventoryManager : MonoBehaviour
{
    public List<Relic> _passiveRelics;

    private Dictionary<RelicEffect.EventTiming, HashSet<Relic>> _sortedPassiveRelics;

    public FRELICID _relicInventory;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        int startingRelic = Random.Range(0, 3);
        switch (startingRelic)
        {
            case 0:
                _relicInventory = FRELICID.NOUNUP;
                break;
            case 1:
                _relicInventory = FRELICID.YUP;
                break;
            case 2:
                _relicInventory = FRELICID.RESISTUP;
                break;
        }
        
        Debug.Log(_relicInventory);

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
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnWordSubmit(Word word)
    {
        RelicEffect.Result result = new RelicEffect.Result();

        foreach (Relic relic in _sortedPassiveRelics[RelicEffect.EventTiming.On_Word_Submit])
        {
            // TODO check if we have the relic, by ID

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

    internal void OnEnemyAttack(int baseDamage, out int modifiedDamage)
    {
        RelicEffect.Result result = new RelicEffect.Result();

        foreach (Relic relic in _sortedPassiveRelics[RelicEffect.EventTiming.On_Enemy_Attack])
        {
            // TODO check if we have the relic, by ID

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

        float totalDamage = (baseDamage * (1 - totalResistPercent) - totalResistBonus);
        modifiedDamage = (int) totalDamage;

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
