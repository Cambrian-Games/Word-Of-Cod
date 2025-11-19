using System.Collections.Generic;
using System.Linq;

// This will become more complex as needed

internal class EnemyTurnHandler
{
    private List<Enemy> _enemies;

    public EnemyTurnHandler(List<Enemy> enemies)
    {
        _enemies = new List<Enemy>(enemies);
    }

    public EnemyTurnHandler(Enemy enemy) : this(new List<Enemy>() { enemy })
    {
    }

    public bool IsTurnComplete()
    {
        return BattleManager.INSTANCE.CurrentPlayerHealth() <= 0 || _enemies.All(enemy => enemy.IsTurnComplete);
    }

	public void StartRound()
	{
		_enemies.ForEach(enemy => enemy.StartRound());
	}

    public void StartTurn()
    {
        _enemies.ForEach(enemy => enemy.StartTurn());
    }

	public void EndTurn()
	{
		_enemies.ForEach(enemy => enemy.EndTurn());
	}

    public void Update()
    {
        for (int i = 0; i < _enemies.Count; i++)
        {
            if (BattleManager.INSTANCE.CurrentPlayerHealth() <= 0)
                break;

			_enemies[i].UpdateTurn();
        }
    }
}