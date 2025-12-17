using System;
using UnityEngine;
using Random = UnityEngine.Random;

public class InventoryManager : MonoBehaviour
{
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
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public int RunPlayerDamageModRelics(int baseDamage, string word, FPART pOS)
    {
        int outDamage = baseDamage;
        if (_relicInventory.HasFlag(FRELICID.NOUNUP))
        {
            if (pOS.HasFlag(FPART.NOUN))
            {
                outDamage *= 2;
            }
        }

        if (_relicInventory.HasFlag(FRELICID.YUP))
        {
            if (word.Contains('Y'))
            {
                outDamage *= 2;
            }
        }

        return outDamage;
    }

    public int RunEnemyDamageModRelics(int baseDamage)
    {
        int outDamage = baseDamage;
        if (_relicInventory.HasFlag(FRELICID.RESISTUP))
        {
            outDamage = Mathf.CeilToInt(0.9f * baseDamage);
        }

        return outDamage;
    }
}
