using System.Collections.Generic;
using System.Linq;
using Unity.Services.Analytics;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.UnityConsent;

public class RunManager : MonoBehaviour
{
	public static RunManager INSTANCE;

    [SerializeField]
    private RunFormat _format;
    public int SegmentsInFormat() => _format.Events.Sum(evt => evt.HasChoice ? 2 : 1);

	[Header("Do not modify this! This shows what has been selected so far")]
	[SerializeField]
	private List<RunFormat.SelectedEvent> _currentRun;
    public RunFormat.SelectedEvent CurrentEvent => _currentRun.Last();

	public float _distanceBetweenEvents = 7.5f;
	public float _travelTime = 3.0f;

    private bool _hasChoicePending;
    private Vector3 _lastEventPos;
    private Vector3 _nextEventPos;

	public string _loseScene;
	public string _winScene;

	private Vector3 _destination;

	public AnalyticsManager _analyticsManager;

	[SerializeField]
	private Canvas _mainCanvas;
	public Canvas MainCanvas => _mainCanvas;

    [SerializeField]
    private StatsHolder _statsHolder;
    
	private Enemy _overworldEnemy;

	public enum RunState
	{
		Nil = -1,
		Run_Start,
		Traveling_To_Next_Event,
		Enter_Event,
		In_Event,
		Post_Event, // rewards post-battle, usually

		Win,
		Lose
	}

	private RunState _state = RunState.Nil;

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
		SetRunState(RunState.Run_Start);
		TryInitializeAnalytics();
    }

	// Update is called once per frame
	void Update()
	{
		UpdateRunState();
	}

    private void UpdateRunState()
    {
        while (true)
        {
            RunState currentState = _state;

            switch (_state)
            {
                case RunState.Traveling_To_Next_Event:
                    if (Player.INSTANCE.transform.position.x >= _destination.x) // not great check here
                    {
                        Player.INSTANCE.transform.position = _destination;

                        // we may be able to handle choosing inside this state. Just listen for a choice here,
                        //  and once one has been made we update the destination

                        if (_hasChoicePending)
                        {
                            // peek next event
                            int nextRunEventIndex = _currentRun.Count;
                            RunFormat.RunEvent nextEvent = _format.Event(nextRunEventIndex);

                            int choiceIndex = CheckForChoice(nextEvent);

                            if (choiceIndex != -1)
                            {
                                _destination = _nextEventPos;
                                _hasChoicePending = false;
                                SelectNextEvent(nextEvent, choiceIndex);
                            }
                        }
                        else 
                        {
                            SetRunState(RunState.Enter_Event);
                        }
                    }
                    else
                    {
                        Player.INSTANCE.transform.position += Vector3.right * _distanceBetweenEvents / _travelTime * Time.deltaTime;
                    }
                    break;

                case RunState.Enter_Event:
                    // Complete any transitions set up in SetRunState(RunState.Enter_Event)
                    SetRunState(RunState.In_Event);
                    break;

                case RunState.In_Event:
                    // If the current event isn't a shop, the battle manager kicks us into post event
                    
                    if (CurrentEvent._isShop && !ShopManager.INSTANCE.IsShopOpen())
                    {
                        SetRunState(RunState.Post_Event);
                    }
                    break;

                case RunState.Post_Event:
                    // if this was a boss fight, we wait for items/relics to be purchased
                    if (ShopManager.INSTANCE.IsShopOpen())
                        break;

                    if (!CurrentEvent._isShop)
                    {
                        BattleManager.INSTANCE.Unload();
                    }

                    // if this was the last event, win
                    if (_currentRun.Count == _format.Events.Count)
                    {
                        SetRunState(RunState.Win);
                        break;
                    }
                    
                    SetRunState(RunState.Traveling_To_Next_Event);
                    break;

                case RunState.Win:
                    break;

                case RunState.Lose:
                    break;
            }

            if (currentState == _state)
                break;
        }
    }

    internal void SetRunState(RunState newState)
	{
		if (newState == _state)
			return;

		_state = newState;

		switch (_state)
		{
			case RunState.Run_Start:
				SetRunState(RunState.Traveling_To_Next_Event);
				break;
			case RunState.Traveling_To_Next_Event:
                // peek next event, determine whether a choice must be made
                int nextRunEventIndex = _currentRun.Count;
                RunFormat.RunEvent nextEvent = _format.Event(nextRunEventIndex);

                _hasChoicePending = nextEvent.HasChoice;
                _lastEventPos = Player.INSTANCE.transform.position;
                _nextEventPos = _lastEventPos + (_hasChoicePending ? 2 : 1) * _distanceBetweenEvents * Vector3.right;
                // equals nextEventPos if there isn't a choice, halfway if there is
                _destination = _lastEventPos + _distanceBetweenEvents * Vector3.right;

                if (!_hasChoicePending)
                {
                    SelectNextEvent(nextEvent, 0);
                }
                else
                {
                    // display crossroads here
                }
                break;

            case RunState.Enter_Event:
                // queue screen transitions here if this is a battle
                //reset once per battle items/relics
                Player.INSTANCE._inventory.OnEnterRunEvent();
                break;

            case RunState.In_Event:
                // will modify this field if we ever have more than two event kinds
                if (CurrentEvent._isShop)
                {
                    ShopManager.INSTANCE.OpenEventShop();
                }
                else
                {
                    BattleManager.INSTANCE.SetEnemy(CurrentEvent.EncounterPrefab);
                    // This is starting to become a problem, will likely have to be changed later
                    BattleManager.INSTANCE.transform.position = (Vector2)Camera.main.transform.position; // the cast sets the z coord to zero
                    BattleManager.INSTANCE.Load();

                    // this transition's actually pretty seamless

                    if (_overworldEnemy)
                    {
                        Destroy(_overworldEnemy.gameObject);
                    }
                }
                break;

            case RunState.Post_Event:
                Enemy defeatedEnemyPrefab = CurrentEvent.EncounterPrefab;

                if (defeatedEnemyPrefab)
                {
                    Enemy.FENEMYTYPE enemyTypes = defeatedEnemyPrefab.EnemyTypes;
                    if (enemyTypes.HasFlag(Enemy.FENEMYTYPE.MINIBOSS) ||
                        enemyTypes.HasFlag(Enemy.FENEMYTYPE.BOSS))
                    {
                        ShopManager.INSTANCE.OpenPostBossShop();
                    }
                }
                break;

			case RunState.Win:
				//TODO Add Analytics for End Game
				SendWinEvent();
				SceneManager.LoadScene(_winScene);
				break;
			case RunState.Lose:
				//TODO Add analytics for lost run
				//    same as End Game, but with added "what you lost to" event
				SendLoseEvent();
				SceneManager.LoadScene(_loseScene);
				break;
		}
	}

    private int CheckForChoice(RunFormat.RunEvent runEvent)
    {
        Debug.Assert(runEvent.HasChoice);

        // allow mouse selecting if there are exactly two options
        if (runEvent.OptionCount == 2)
        {
            if (Input.GetMouseButtonDown((int)MouseButton.Left))
            {
                return 0;
            }
            else if (Input.GetMouseButtonDown((int)MouseButton.Right))
            {
                return 1;
            }
        }

        string input = Input.inputString;

        if (input == null || input.Length != 1 || !char.IsNumber(input[0]))
            return -1;

        return int.Parse(input) - 1;
    }

    private void SelectNextEvent(RunFormat.RunEvent runEvent, int option = 0)
    {
        RunFormat.SelectedEvent selectedEvent = runEvent.Select(option);

        if (!selectedEvent._isShop)
        {
            // If we haven't used this pool before, create a SpawnHistory for it
            if (!EncounterPool.SPAWN_HISTORIES.TryGetValue(selectedEvent._pool, out EncounterPool.SpawnHistory history))
            {
                history = selectedEvent._pool.CreateSpawnHistory();
                EncounterPool.SPAWN_HISTORIES.Add(selectedEvent._pool, history);
            }

            // these two lines could be more tightly coupled, or GetNextPrefab could auto-add to history
            selectedEvent.EncounterPrefab = selectedEvent._pool.GetNextPrefab(history);
            history.TryAddEntry(selectedEvent._pool, selectedEvent.EncounterPrefab);

            Debug.Assert(selectedEvent.EncounterPrefab != null);

            SpawnOverworldEnemy(selectedEvent.EncounterPrefab);
        }

        _currentRun.Add(selectedEvent);
    }

    private void SpawnOverworldEnemy(Enemy encounterPrefab)
    {
        Debug.Assert(encounterPrefab != null);
        _overworldEnemy = Instantiate(encounterPrefab);
        Vector3 offset = FindAnyObjectByType<CameraTracker>()._targetOffset;
        Vector3 targetPos = _nextEventPos - new Vector3(2 * offset.x, 0, 0);
        _overworldEnemy.transform.position = targetPos;
    }

    public void AddWordToStats(Word word)
	{
		//if first word, simply set all
		if (_statsHolder._sortedWordDamages.Count == 0)
		{
			_statsHolder._sortedWordDamages.Add(word.EffectiveDamage);
			_statsHolder._sortedWordLengths.Add(word.NumTilesUsed);
			_statsHolder._longestWord = word.Text;
			_statsHolder._mostDamagingWord = word.Text;
		}
		//if not first word
		else
		{
			//check for new longest
			if (word.Text.Length > _statsHolder._sortedWordLengths.Last())
			{
				_statsHolder._longestWord = word.Text;
			}
			//check for new highest damage
			if (word.EffectiveDamage > _statsHolder._sortedWordDamages.Last())
			{
				_statsHolder._mostDamagingWord = word.Text;
			}
			//insert sorted to appropriate list
			int index = _statsHolder._sortedWordLengths.BinarySearch(word.NumTilesUsed);
			if (index < 0) index = ~index;
			_statsHolder._sortedWordLengths.Insert(index, word.NumTilesUsed);
			
			index = _statsHolder._sortedWordDamages.BinarySearch(word.EffectiveDamage);
			if (index < 0) index = ~index;
			_statsHolder._sortedWordDamages.Insert(index, word.EffectiveDamage);
		}
	}

#region Analytics
	private void CalculateAverages(out float meanDamage, out float medianDamage, out float meanLength,
		out float medianLength)
	{
		if (_statsHolder._sortedWordDamages.Count > 0)
		{
			meanDamage = (float)_statsHolder._sortedWordDamages.Average();
			meanLength = (float)_statsHolder._sortedWordLengths.Average();
			if (_statsHolder._sortedWordDamages.Count % 2 != 0)
			{
				medianLength = _statsHolder._sortedWordLengths.ElementAt(_statsHolder._sortedWordLengths.Count / 2);
				medianDamage = _statsHolder._sortedWordDamages.ElementAt(_statsHolder._sortedWordDamages.Count / 2);
			}
			else
			{
				medianLength = (_statsHolder._sortedWordLengths.ElementAt(_statsHolder._sortedWordLengths.Count / 2) + _statsHolder._sortedWordLengths.ElementAt((_statsHolder._sortedWordLengths.Count / 2) - 1)) / 2.0f;
				medianDamage = (_statsHolder._sortedWordDamages.ElementAt(_statsHolder._sortedWordDamages.Count / 2) + _statsHolder._sortedWordDamages.ElementAt((_statsHolder._sortedWordDamages.Count / 2) - 1)) / 2.0f;
			}
		}
		else
		{
			meanDamage = 0;
			meanLength = 0;
			medianDamage = 0;
			medianLength = 0;
		}
	}

	private void SendWinEvent()
	{
		CalculateAverages(out float meanDamage, out float medianDamage, out float meanLength, out float medianLength);
		_statsHolder._meanWordDamage = meanDamage;
		_statsHolder._medianWordDamage = medianDamage;
		_statsHolder._meanWordLength = meanLength;
		_statsHolder._medianWordLength = medianLength;
		WinEvent winEvent = new WinEvent()
		{
			_longestWord = _statsHolder._longestWord,
			_mostDamagingWord = _statsHolder._mostDamagingWord,
			_relicList = string.Join(", ", Player.INSTANCE._inventory._passiveRelicInventory)
				+ "; " + string.Join(", ", Player.INSTANCE._inventory._activeRelicInventory),
			_highestDamage = _statsHolder._sortedWordDamages.Count > 0 ? _statsHolder._sortedWordDamages.Last() : 0,
			_meanDamage = meanDamage,
			_meanLength = meanLength,
			_medianDamage = medianDamage,
			_medianLength = medianLength,
			_numWords = _statsHolder._sortedWordLengths.Count()
		};
		AnalyticsService.Instance.RecordEvent(winEvent);
		//need to flush to force upload before user quits
		AnalyticsService.Instance.Flush();
		Debug.Log("WinEventSent");
	}
	
	private void SendLoseEvent()
	{
		CalculateAverages(out float meanDamage, out float medianDamage, out float meanLength, out float medianLength);
		_statsHolder._meanWordDamage = meanDamage;
		_statsHolder._medianWordDamage = medianDamage;
		_statsHolder._meanWordLength = meanLength;
		_statsHolder._medianWordLength = medianLength;
		LoseEvent loseEvent = new LoseEvent()
		{
			_longestWord = _statsHolder._longestWord,
			_mostDamagingWord = _statsHolder._mostDamagingWord,
			_relicList = string.Join(", ", Player.INSTANCE._inventory._passiveRelicInventory)
				+ "; " + string.Join(", ", Player.INSTANCE._inventory._activeRelicInventory),
			_highestDamage = _statsHolder._sortedWordDamages.Count > 0 ? _statsHolder._sortedWordDamages.Last() : 0,
			_meanDamage = meanDamage,
			_meanLength = meanLength,
			_medianDamage = medianDamage,
			_medianLength = medianLength,
			_numWords = _statsHolder._sortedWordLengths.Count(),
			_enemyIndex = CurrentEvent._eventIndex, 
			_enemyName = BattleManager.INSTANCE.CurrentEnemy.name
		};
		AnalyticsService.Instance.RecordEvent(loseEvent);
		//need to flush to force upload before user quits
		AnalyticsService.Instance.Flush();
		Debug.Log("LoseEventSent");
	}

	private void TryInitializeAnalytics()
	{
		GameObject analyticsGameObject = GameObject.Find("Analytics Manager");
		if (analyticsGameObject)
		{
			_analyticsManager = analyticsGameObject.GetComponent<AnalyticsManager>();
			if (_analyticsManager._analyticsEnabled)
			{
				EndUserConsent.SetConsentState(new ConsentState
				{
					AnalyticsIntent = ConsentStatus.Granted
				});
				Debug.Log("start Data Collection");
			}
			else
			{
				EndUserConsent.SetConsentState(new ConsentState
				{
					AnalyticsIntent = ConsentStatus.Denied
				});
			}
		}
	}
    #endregion
}