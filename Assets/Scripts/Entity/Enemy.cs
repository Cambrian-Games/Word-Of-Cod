public class Enemy : Entity
{
    // config 
    public int _attackDamage = 10;
    //public AttackRules obj;

    // attack state, controlled by EnemyTurnHandler
    internal bool _hasAttacked = false;
}