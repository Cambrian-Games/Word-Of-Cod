using System.Linq;
using UnityEngine;

public class SchoolOfFish : Enemy
{
	private Transform _subSpriteHolder;
	private SpriteRenderer[] _spriteRenderers = new SpriteRenderer[5];

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
		_subSpriteHolder = transform.GetChild(0);
		for (int i = 0; i < 5; i++)
		{
			_spriteRenderers[i] = _subSpriteHolder.GetChild(i).GetComponent<SpriteRenderer>();
		}
    }

    // Update is called once per frame
    void Update()
    {
        
    }

	protected override void OnTakeDamage(int damageTaken)
	{
		base.OnTakeDamage(damageTaken);

		float currentHP = HealthPercent();

		for (int i = 0; i < _spriteRenderers.Length; i++)
		{
			// in the future we can have fish swim away or fade out, but disappearing is fine for now

			_spriteRenderers[i].enabled = currentHP >= i * .2f;
		}
	}
}
