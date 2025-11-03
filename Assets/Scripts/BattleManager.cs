using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleManager : MonoBehaviour
{
    public enum BattleState
    {
        Nil = -1,
        Load = 0,

        Settle_Board,       // Board settles into place
        Player_Turn,        // Player can take actions, use consumable items, etc
        Post_Player_Turn,   // Resolve attack and check if fight has been won or lost
        Enemy_Turn,         // Enemy state machine runs to completion. Player death may occur during this and will have to be handled correctly
        Post_Enemy_Turn,    // TBD

        Win,
        Lose
    }

    public enum PostPlayerTurnState
    {
        Nil = -1,

        Display_Word,
        Display_Combo,
        Attack_Enemy,
        Cleanup
    }

    private BattleState _battleState = BattleState.Nil;

    // sub-states

    private PostPlayerTurnState _pptState = PostPlayerTurnState.Nil;

    // player template might get moved yet again into something that persists across battles
    //  and enemy template will get wrapped into an encounter object eventually

    [SerializeField]
    private Enemy _enemyPrefab;
    [SerializeField]
    private Player _playerPrefab;

    private Enemy _enemy;
    private Player _player;

	public SceneAsset _loseScene;
	public SceneAsset _winScene;

    // may need to have a prefab and spawn multiple displays if we want multiple enemies on screen.
    public EntityDisplay _enemyDisplay;
    public EntityDisplay _playerDisplay;

    internal EnemyTurnHandler _enemyTurnHandler;

    // Player Turn Data

    FPART _pOS; // parts-of-speech for submitted word
    string _wordToSubmit;
    string _lastWord;
	internal string LastWord => _lastWord;
    List<Tile> _tilesInWord;

    public Transform _tileDestination;
    List<Vector3> _directions = new List<Vector3>();
    private float _timeToDestination = 0.5f;
    private float _timeElapsed = 0;

    private int _damageToDeal = 0;

    private int _totalWords; // used for logging

    public static BattleManager INSTANCE;

    private void Awake()
    {
        // set up singleton

        if (INSTANCE != null && INSTANCE != this)
        {
            Destroy(gameObject);
            return;
        }

        INSTANCE = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        SetBattleState(BattleState.Load);
    }

    // Update is called once per frame
    void Update()
    {
        UpdateBattleState();
    }

    private void UpdateBattleState()
    {
        while (true)
        {
            BattleState stateCur = _battleState;

            switch (stateCur)
            {
                case BattleState.Post_Player_Turn:
                    UpdatePPT();
                    break;

                case BattleState.Enemy_Turn:
                    _enemyTurnHandler.Update();

                    if (_enemyTurnHandler.IsTurnComplete())
                    {
                        SetBattleState(BattleState.Post_Enemy_Turn);
                    }
                    break;

                case BattleState.Settle_Board:
                    if (GameBoard.INSTANCE.IsSettled())
                    {
                        SetBattleState(BattleState.Player_Turn);
                    }
                    break;
            }

            if (stateCur == _battleState)
                break;
        }
    }

    void SetBattleState(BattleState newState)
    {
        if (_battleState == newState)
            return;

        switch (_battleState)
        {
            // if we leave this state for ANY reason, we want to turn off input.

            case BattleState.Player_Turn:
                TileSelector.INSTANCE._isSelectingEnabled = false;
                break;

			case BattleState.Enemy_Turn:
				_enemyTurnHandler.EndTurn();
				break;
        }

        _battleState = newState;

        switch (_battleState)
        {
            case BattleState.Load:

                // Long term, _player will be moved out of the battle manager and the enemy prefab will be defined by an encounter object

                _enemy = Instantiate<Enemy>(_enemyPrefab, this.transform);
                _player = Instantiate<Player>(_playerPrefab, this.transform);

                _enemy.Init();
                _player.Init();

                _enemyDisplay.Entity = _enemy;
                _playerDisplay.Entity = _player;

                _enemyTurnHandler = new EnemyTurnHandler(_enemy);

                SetBattleState(BattleState.Player_Turn);
                break;

            case BattleState.Player_Turn:
                TileSelector.INSTANCE._isSelectingEnabled = true;
                break;

            case BattleState.Post_Player_Turn:
                GameBoard.INSTANCE.DisconnectTiles(_tilesInWord, newParent: this.transform);
                GameBoard.INSTANCE.LockStateMachine(true);
                SetPostPlayerTurnState(PostPlayerTurnState.Display_Word);
                break;

            case BattleState.Enemy_Turn:
                _enemyTurnHandler.StartTurn();
                break;

            case BattleState.Post_Enemy_Turn:
                if (_player._currentHealth <= 0)
                {
                    SetBattleState(BattleState.Lose);
                }
                else
                {
                    SetBattleState(BattleState.Settle_Board);
                }
                break;

            case BattleState.Settle_Board:
                GameBoard.INSTANCE.LockStateMachine(false);
                break;

			case BattleState.Lose:
				SceneManager.LoadScene(_loseScene.name);
				break;

			case BattleState.Win:
				SceneManager.LoadScene(_winScene.name);
				break;
		}
    }

    private void UpdatePPT()
    {
        while (true)
        {
            PostPlayerTurnState stateCur = _pptState;

            switch (_pptState)
            {
                case PostPlayerTurnState.Display_Word:

                    float dTWord = Time.deltaTime;
                    for (int i = 0; i < _tilesInWord.Count; i++)
                    {
                        _tilesInWord[i].transform.position += _directions[i] * dTWord / _timeToDestination;
                    }

                    _timeElapsed += dTWord;

                    if (_timeElapsed > _timeToDestination)
                    {
                        if (_lastWord != null)
                        {
                            SetPostPlayerTurnState(PostPlayerTurnState.Display_Combo);
                        }
                        else
                        {
                            SetPostPlayerTurnState(PostPlayerTurnState.Attack_Enemy);
                        }
                    }

                    break;

                // stub, does nothing for now
                case PostPlayerTurnState.Display_Combo:
                    SetPostPlayerTurnState(PostPlayerTurnState.Attack_Enemy);
                    break;

                case PostPlayerTurnState.Attack_Enemy:

                    float dTAttack = Time.deltaTime;
                    for (int i = 0; i < _tilesInWord.Count; i++)
                    {
                        _tilesInWord[i].transform.position += _directions[i] * dTAttack / _timeToDestination;
                    }

                    _timeElapsed += dTAttack;

                    if (_timeElapsed > _timeToDestination)
                    {
                        Debug.Log($"{_enemy._currentHealth} - {_damageToDeal}");
                        _enemy._currentHealth = Mathf.Clamp(_enemy._currentHealth - _damageToDeal, 0, _enemy._maxHealth);
                        SetPostPlayerTurnState(PostPlayerTurnState.Cleanup);
                    }
                    break;
            }

            if (stateCur == _pptState)
                break;
        }
    }

    private void SetPostPlayerTurnState(PostPlayerTurnState newState)
    {
        if (_pptState == newState)
            return;

        switch (_pptState)
        {
            case PostPlayerTurnState.Display_Word:
                _directions.Clear();
				_tilesInWord.ForEach(tile => tile.OnSubmit());

				if (_player._currentHealth <= 0)
				{
					// interrupt state change to lose the game. Should probably be its own step instead.
					SetBattleState(BattleState.Lose);
				}
                break;

            case PostPlayerTurnState.Attack_Enemy:
                _directions.Clear();
                break;
        }

        _pptState = newState;

        switch (_pptState)
        {
            case PostPlayerTurnState.Display_Word:
                // TODO this hard-codes the width of the tile in the first Vector3.left

                Vector2 farLeft = _tileDestination.transform.position +
                    (_tilesInWord.Count / 2.0f) * Vector3.left +
                    ((_tilesInWord.Count - 1) / 2.0f) * (BoardConfig.INSTANCE.TileSpacing.x) * Vector3.left;

                for (int i = 0; i < _tilesInWord.Count; i++)
                {
                    // TODO this hardcodes the width of the tile with the 1 and 0.5f
                    Vector3 destPosition = farLeft + ((1 + BoardConfig.INSTANCE.TileSpacing.x) * i + 0.5f) * Vector2.right;
                    _directions.Add(destPosition - _tilesInWord[i].transform.position);
                }

                _timeElapsed = 0.0f;
                break;

            case PostPlayerTurnState.Display_Combo:
                // stub, does nothing
                break;

            case PostPlayerTurnState.Attack_Enemy:
                _damageToDeal = _tilesInWord.Count * (1 + (_tilesInWord.Count - 3) / 10); // first term here is a placeholder, will be changed to actual scoring calculation
                Debug.Log(_damageToDeal);

                for (int i = 0; i < _tilesInWord.Count; i++)
                {
                    _directions.Add(_enemyDisplay.transform.position - _tilesInWord[i].transform.position);
                }

                _timeElapsed = 0.0f;
                break;

            case PostPlayerTurnState.Cleanup:
                
                for (int i = 0; i < _tilesInWord.Count; i++)
                {
                    Destroy(_tilesInWord[i].gameObject);
                }

                _tilesInWord.Clear();

                if (_enemy._currentHealth <= 0)
                {
                    SetBattleState(BattleState.Win);
                }
                else
                {
                    SetBattleState(BattleState.Enemy_Turn);
                }

                break;
        }
    }

    public bool TrySubmitWord(string word, List<Tile> tilesUsed)
    {
        Debug.Assert(_battleState == BattleState.Player_Turn);

        if (WordChecker.INSTANCE.CheckWord(word, out _pOS))
        {
            Debug.Log($"Submitted {word} ({_pOS})");
            Debug.Log(++_totalWords + " Word(s) Submitted");

            _lastWord = _wordToSubmit;
            _wordToSubmit = word;
            _tilesInWord = new List<Tile>(tilesUsed);

            SetBattleState(BattleState.Post_Player_Turn);
            return true;
        }
        else
        {
            _pOS = FPART.NONE;
            return false;
        }

    }

    internal void DamagePlayer(int baseDamage)
    {
        _player._currentHealth = Mathf.Clamp(_player._currentHealth - baseDamage, 0, _player._maxHealth);
    }

    internal int CurrentPlayerHealth()
    {
        return _player._currentHealth;
    }

	internal int MaxPlayerHealth()
	{
		return _player._maxHealth;
	}
}
