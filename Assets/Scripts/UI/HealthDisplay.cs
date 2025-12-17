using System;
using TMPro;
using UnityEngine;

public enum EntityType
{
    Player,
    Enemy,
}

public class HealthDisplay : MonoBehaviour
{
    
    public TMP_Text _text;
    public EntityType _type;
    private Enemy _currEnemy;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {

    }

    private void OnEnable()
    {
        //long term this is right
        //currEnemy = BattleManager.INSTANCE.CurrentEnemy;
    }

    // Update is called once per frame
    void Update()
    {
        if (_type == EntityType.Enemy)
        {
            //short term this is right
            _currEnemy = BattleManager.INSTANCE.CurrentEnemy;
            if (_currEnemy != null)
            {
                _text.text = $"{_currEnemy.CurrentHealth} / {_currEnemy.MaxHealth}";
            } 
        }
        else
        {
            _text.text = $"{Player.INSTANCE.CurrentHealth} / {Player.INSTANCE.MaxHealth}";
        }
    }
}
