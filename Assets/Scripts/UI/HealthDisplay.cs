using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public enum EntityType
{
    Player,
    Enemy,
}

public class HealthScript : MonoBehaviour
{
    
    public TMP_Text _text;
    public EntityType _type;
    private Enemy currEnemy;

    [SerializeField] private Slider _slider;
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
            currEnemy = BattleManager.INSTANCE.CurrentEnemy;
            if (currEnemy != null)
            {
                _text.text = $"{currEnemy.CurrentHealth} / {currEnemy.MaxHealth}";
                _slider.value = currEnemy.HealthPercent();
            } 
        }
        else
        {
            _text.text = $"{Player.INSTANCE.CurrentHealth} / {Player.INSTANCE.MaxHealth}";
            _slider.value = Player.INSTANCE.HealthPercent();
        }
    }
}
