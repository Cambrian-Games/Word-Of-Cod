using UnityEngine;

[CreateAssetMenu(fileName = "EntityTemplate", menuName = "Scriptable Objects/Entity Template")]
public class EntityTemplate : ScriptableObject
{
	public string _displayName;
	public Sprite _sprite;
	public int _maxHealth;

	public EntityInstance CreateEntity() => new EntityInstance(this);
}

[CreateAssetMenu(fileName = "EnemyTemplate", menuName = "Scriptable Objects/Enemy Template")]
public class EnemyTemplate : EntityTemplate
{
	// provide state machine template object

	public new EnemyInstance CreateEntity() => new EnemyInstance(this);
}

[CreateAssetMenu(fileName = "PlayerTemplate", menuName = "Scriptable Objects/Player Template")]
public class PlayerTemplate : EntityTemplate
{
	public new PlayerInstance CreateEntity() => new PlayerInstance(this);
}


public class EntityInstance
{
	private readonly EntityTemplate _template;
	public int _currentHealth;

	public EntityInstance(EntityTemplate template)
	{
		_template = template;
		_currentHealth = _template._maxHealth;
	}
}

public class EnemyInstance : EntityInstance
{
	public EnemyInstance(EnemyTemplate template) : base(template)
	{
		// state machine instance object
	}

	public void Update()
	{

	}

	public void StartTurn()
	{

	}

	public bool HasEndedTurn()
	{
		return true;
	}
}

public class PlayerInstance : EntityInstance
{
	public PlayerInstance(PlayerTemplate template) : base(template)
	{
	}
}