using System;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Analytics;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    public enum BattleState
    {
        Nil = -1,
        Load = 0,

		Pre_Player_Turn,	// Start of Round, used for ticking effects and updating forecasts
        Player_Turn,        // Player can take actions, use consumable items, etc
        Post_Player_Turn,   // Resolve attack and check if fight has been won or lost
        Settle_Board,       // Board settles into place
        Enemy_Turn,         // Enemy state machine runs to completion. Player death may occur during this and will have to be handled correctly
        Post_Enemy_Turn,    // Clear forecast, handle any cleanup tasks


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
	public BattleState CurrentState => _battleState;

    // sub-states

    private PostPlayerTurnState _pptState = PostPlayerTurnState.Nil;

    [SerializeField]
    private Enemy _enemyPrefab;

    [SerializeField]
    private GameObject _enemyDamagePopup;

    private Enemy _enemy;
    public Enemy CurrentEnemy => _enemy;

    public TMP_Text _forecastText;

    // Player Turn Data

    private Word _wordToSubmit;
    private Word _previousWord;

	// this is questionable, given how attack rules care about it, but it's fine for now.
	public Word MostRecentWord => _wordToSubmit;

	// currently we log word length, tile count, and triggered relics.
	//  other possible things to log are the number of spiny tiles used and the number of sandy tiles cleared.
	//  Once combo system is in we could log that too.
	//  We also don't store full-run word history but that can be added later if needed, and we can extract best/worst word stuff from that
	internal List<Word> _wordHistory = new List<Word>();

    public WordHistoryBox _wordHistoryBox;
    
    public Transform _tileDestination;
    List<Vector3> _directions = new List<Vector3>();
    private float _timeToDestination = 0.5f;
    private float _timeElapsed = 0;

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
        //SetBattleState(BattleState.Load);
    }

    // Update is called once per frame
    void Update()
    {
        UpdateBattleState();
    }

    private void UpdateBattleState()
    {
#if UNITY_EDITOR
		CheckDebugBattleCommands();
#endif

        while (true)
        {
            BattleState stateCur = _battleState;

            switch (stateCur)
            {
                case BattleState.Post_Player_Turn:
                    UpdatePPT();
					break;

				case BattleState.Settle_Board:
					if (GameBoard.INSTANCE.IsSettled())
					{
						SetBattleState(BattleState.Enemy_Turn);
					}
					break;

				case BattleState.Enemy_Turn:
					_enemy.UpdateTurn();

                    if (_enemy.IsTurnComplete)
                    {
                        SetBattleState(BattleState.Post_Enemy_Turn);
                    }
                    break;
            }

            if (stateCur == _battleState)
                break;
        }
    }

#if UNITY_EDITOR
	private void CheckDebugBattleCommands()
	{
		bool holdingShift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

		if (holdingShift && Input.GetKey(KeyCode.Space) && _enemy)
		{
			Destroy(_enemy.gameObject);
			SetBattleState(BattleState.Win);
		}

		if (holdingShift && Input.GetKey(KeyCode.H) && Player.INSTANCE)
		{
			Player.INSTANCE.Heal(Player.INSTANCE.MaxHealth);
		}

		// ideally we want a teleport to next fight option, maybe holding T? Would need to be in run manager
	}
#endif

	internal void SetBattleState(BattleState newState)
    {
        if (_battleState == newState)
            return;

		// leave old state

        switch (_battleState)
        {
            // if we leave this state for ANY reason, we want to turn off input.

            case BattleState.Player_Turn:
				TileSelector.INSTANCE.Mode = TileSelector.SelectionMode.None;
                break;

			case BattleState.Post_Player_Turn:
				_pptState = PostPlayerTurnState.Nil;
				break;

            case BattleState.Enemy_Turn:
				_forecastText.text = "";
                break;

			case BattleState.Nil:
				if (_wordHistory == null)
				{
					_wordHistory = new List<Word>();
				}
				_wordHistory.Clear();
                _wordHistoryBox.ClearHistoryBox();

				break;
        }

		BattleState oldState = _battleState;
        _battleState = newState;
		Player.INSTANCE._inventory.OnBattleStateChanged(oldState, newState);

		switch (_battleState)
        {
            case BattleState.Nil:
                _enemy = null;

                GameBoard.INSTANCE.DeleteBoard();

				_wordHistory.Clear();
                _wordHistoryBox.ClearHistoryBox();
                break;

            case BattleState.Load:

                // expensive, just here for testing
                CameraTracker tracker = FindAnyObjectByType<CameraTracker>();
                _enemy = Instantiate<Enemy>(_enemyPrefab, this.transform);
				Vector3 enemyLocalPos = Player.INSTANCE.transform.position - tracker.transform.position;
				enemyLocalPos.x *= -1;
				_enemy.transform.localPosition = enemyLocalPos;

                GameBoard.INSTANCE.GenerateBoard();

                SetBattleState(BattleState.Pre_Player_Turn);
                break;

			case BattleState.Pre_Player_Turn:
				_enemy.StartRound();

				Debug.Log("Forecast: " + _enemy.FormattedForecast());
				//change forecast text
				_forecastText.text = _enemy.FormattedForecast();

				SetBattleState(BattleState.Player_Turn);
				break;

			case BattleState.Player_Turn:
				TileSelector.INSTANCE.Mode = TileSelector.SelectionMode.Letter_Selection;
                
				_previousWord = _wordToSubmit;
				_wordToSubmit = null;

                break;

            case BattleState.Post_Player_Turn:
				if (_wordToSubmit != null)
				{
					// TODO this needs to change due to durable vowels and the changing attack animation
					GameBoard.INSTANCE.DisconnectTiles(_wordToSubmit.Tiles, newParent: this.transform);
					GameBoard.INSTANCE.LockStateMachine(true);
					SetPostPlayerTurnState(PostPlayerTurnState.Display_Word);
				}
				else
				{
					SetPostPlayerTurnState(PostPlayerTurnState.Cleanup);
				}
				break;

			case BattleState.Settle_Board:
				GameBoard.INSTANCE.LockStateMachine(false);
				break;

			case BattleState.Enemy_Turn:
				_enemy.StartTurn();

				if (_forecastText.text != _enemy.FormattedForecast())
				{
					Debug.Log("Forecast Change! " + _enemy.FormattedForecast());
					//change forecast text
					_forecastText.text = _enemy.FormattedForecast();
				}
				break;

            case BattleState.Post_Enemy_Turn:
                if (Player.INSTANCE.CurrentHealth <= 0)
                {
                    SetBattleState(BattleState.Lose);
                }
                else
                {
                    SetBattleState(BattleState.Pre_Player_Turn);
                }
                break;

            case BattleState.Lose:
                RunManager.INSTANCE.SetRunState(RunManager.RunState.Lose);
                break;

            case BattleState.Win:
                RunManager.INSTANCE.SetRunState(RunManager.RunState.Post_Event);
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
                    for (int i = 0; i < _wordToSubmit.Tiles.Count; i++)
                    {
						_wordToSubmit.Tiles[i].transform.position += _directions[i] * dTWord / _timeToDestination;
                    }

                    _timeElapsed += dTWord;

                    if (_timeElapsed > _timeToDestination)
                    {
                        if (_previousWord != null)
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
                    for (int i = 0; i < _wordToSubmit.Tiles.Count; i++)
                    {
						_wordToSubmit.Tiles[i].transform.position += _directions[i] * dTAttack / _timeToDestination;
                    }

                    _timeElapsed += dTAttack;

                    if (_timeElapsed > _timeToDestination)
                    {
                        Debug.Log($"{_enemy.CurrentHealth} - {_wordToSubmit.EffectiveDamage}");
						_enemyDamagePopup.GetComponent<DamagePopupScript>().Popup(_wordToSubmit.EffectiveDamage);
                        _enemy.Damage(_wordToSubmit.EffectiveDamage);
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
                //int healthBefore = Player.INSTANCE.CurrentHealth;
				_wordToSubmit.Tiles.ForEach(tile => tile.OnSubmit());
                
                if (Player.INSTANCE.CurrentHealth <= 0)
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

				int tileCount = _wordToSubmit.Tiles.Count;

				Vector2 farLeft = _tileDestination.transform.position +
                    (tileCount / 2.0f) * Vector3.left +
                    ((tileCount - 1) / 2.0f) * (BoardConfig.INSTANCE.TileSpacing.x) * Vector3.left;

                for (int i = 0; i < tileCount; i++)
                {
                    // TODO this hardcodes the width of the tile with the 1 and 0.5f
                    Vector3 destPosition = farLeft + ((1 + BoardConfig.INSTANCE.TileSpacing.x) * i + 0.5f) * Vector2.right;
                    _directions.Add(destPosition - _wordToSubmit.Tiles[i].transform.position);
                }

                // TODO this will probably need to move somewhere else eventually, but this is safe for now

                GameBoard board = GameBoard.INSTANCE;
				_wordToSubmit.Tiles.ForEach(tile => board.ClearSurroundingSandTiles(tile._coord));

                _timeElapsed = 0.0f;
                break;

            case PostPlayerTurnState.Display_Combo:
                // stub, does nothing
                break;

            case PostPlayerTurnState.Attack_Enemy:

                for (int i = 0; i < _wordToSubmit.Tiles.Count; i++)
                {
                    _directions.Add(_enemy.transform.position - _wordToSubmit.Tiles[i].transform.position);
                }

                _timeElapsed = 0.0f;
                break;

            case PostPlayerTurnState.Cleanup:

				if (_wordToSubmit != null)
				{
					for (int i = 0; i < _wordToSubmit.Tiles.Count; i++)
					{
						Destroy(_wordToSubmit.Tiles[i].gameObject);
					}

					_wordToSubmit.Tiles.Clear();
				}

                if (_enemy.CurrentHealth <= 0)
                {
                    Destroy(_enemy.gameObject); // long term there will probably be an animation here so we may wait before going to Win State
                    SetBattleState(BattleState.Win);
                }
                else
                {
                    SetBattleState(BattleState.Settle_Board);
                }

                break;
        }
    }

    public bool TrySubmitWord(string text, List<Tile> tilesUsed)
    {
        Debug.Assert(_battleState == BattleState.Player_Turn);

        if (WordChecker.INSTANCE.TryGetWord(text, tilesUsed, out Word word))
        {
            _wordToSubmit = word;

            // This may need to move elsewhere if we have visual feedback

            Player.INSTANCE._inventory.OnWordSubmit(_wordToSubmit);
			_wordHistory.Add(word);
            _wordHistoryBox.AddWordToHistory(word);
            RunManager.INSTANCE.AddWordToStats(_wordToSubmit);
            
            SetBattleState(BattleState.Post_Player_Turn);
            return true;
        }
        else
        {
            //Analytics for Failed Word Here
			if (text.Length > 0)
			{
				WordFailedEvent wordFailedEvent = new WordFailedEvent { _failedWord = text };
				AnalyticsService.Instance.RecordEvent(wordFailedEvent);
				Debug.Log("FailedWordEventSent");
			}

            return false;
        }
    }

    internal void SetEnemy(Enemy prefab)
    {
        _enemyPrefab = prefab;
    }

    internal void Unload()
    {
        SetBattleState(BattleState.Nil);
    }

    internal void Load()
    {
        SetBattleState(BattleState.Load);
    }
}
