using System;
using System.Collections.Generic;
using UnityEngine;

public class GameBoard : MonoBehaviour
{
	enum ResolveState
	{
		Nil = -1,
		Spawn_New_Tiles,
		Tiles_Fall,
		Cleanup
	}

	private BoardState _currState, _nextState;
	private BoardDelta _currDelta;
	private Tile[,] _playableBoard, _stagingBoard; // staging board is for new tiles before they fall onto the screen

	private ResolveState _resolveState = ResolveState.Nil;

	private BoardConfig _config;
    private bool _lockStateMachine;

	public static GameBoard INSTANCE;

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
		_config = BoardConfig.INSTANCE;

		GenerateBoard();
	}

	// Update is called once per frame
	void Update()
	{
        if (!_lockStateMachine)
        {
            UpdateResolveState();
        }
	}

	private void UpdateResolveState()
	{
		switch (_resolveState)
		{
			case ResolveState.Nil:
				return;

			case ResolveState.Spawn_New_Tiles:
				SpawnNewTiles();
				return;

			case ResolveState.Tiles_Fall:
				FallTiles();
				return;

			case ResolveState.Cleanup:
				FinishResolve();
				return;
		}
	}

	[ContextMenu("Generate Board")]
	public void GenerateBoard()
	{
		if (_currState != null)
			return;

		_currState = new BoardState(_config.Layout);

		_playableBoard = new Tile[_config.Layout._length, _config.Layout._height];
		_stagingBoard = new Tile[_config.Layout._length, _config.Layout._height];

		_nextState = _currState.CloneSettled(_config.SettleKind, out _currDelta);

		SpawnNewTiles();
		FallTiles(immediate: true);
		FinishResolve();

		Debug.Log("Generated Board:");
		Debug.Log(_currState);
	}

	[ContextMenu("Delete Board")]
	public void DeleteBoard()
	{
		if (_currState == null)
			return;

		_currDelta = new BoardDelta(_config.Layout);
		DeleteSelectedTiles();
		_resolveState = ResolveState.Nil;

		_currState = _nextState = null;
		_playableBoard = _stagingBoard = null;
	}

	private void DeleteSelectedTiles()
	{
		foreach (Vector2Int coord in new Vector2IntIterator(_config.Layout.BottomRight(), Vector2Int.zero))
		{
			BoardDelta.TileDelta tileDelta = _currDelta[coord];

			if (tileDelta.IsTileDeletion() && _playableBoard[coord.x, coord.y])
			{
				Destroy(_playableBoard[coord.x, coord.y].gameObject);
			}
		}

		_resolveState = ResolveState.Spawn_New_Tiles;
	}

	private void SpawnNewTiles()
	{
        _nextState = _currState.CloneSettled(_config.SettleKind, out _currDelta);

        // Vector2s are pass-by-value so there's no point in creating and destroying a new one every iteration of the loop

        Vector2 spawnDir = (_config.SettleKind == SettleKind.In_Place) ? Vector2.up : -FallDirection();
		Vector2 layoutDims = _config.Layout.Dims();
		Vector2 tileSpacing = _config.TileSpacing;
		Vector2 spawnOffset = _config.SpawnOffset;
		Vector2 stagingTopLeft = spawnOffset + tileSpacing * spawnDir * layoutDims;

		// I'd like this to use SpawnTile but this allows for better data caching.

		foreach (var kvp in _currDelta._newTiles)
		{
			Tile tile = _stagingBoard[kvp.Key.x, kvp.Key.y] = Instantiate<Tile>(_config.DefaultTilePrefab, this.transform);
			tile.transform.position = stagingTopLeft + (kvp.Key * tileSpacing * new Vector2(1, -1));
			tile._letter = kvp.Value;
			tile._coord = kvp.Key;
		}

		// TODO mini settle+cleanup step

		_resolveState = ResolveState.Tiles_Fall;
	}

	private void FallTiles(bool immediate = false)
	{
		float dT = Time.deltaTime;

		// iterate through present tiles

		bool movedTile = false;

		Vector3 fallDir = FallDirection();
		Vector2 spawnOffset = _config.SpawnOffset;
		Vector2 tileSpacing = _config.TileSpacing;

		foreach (Vector2Int coord in new Vector2IntIterator(_config.Layout.BottomRight()))
		{
			BoardDelta.TileDelta tDelta = _currDelta[coord];

			// skip deleted tiles

			if (tDelta.IsTileDeletion())
				continue;

			Tile tileToMove = _playableBoard[coord.x, coord.y];

			if (!tileToMove)
				continue;

			Vector3 dest = spawnOffset + _currDelta[coord]._destCoord * tileSpacing * new Vector2(1, -1);

			// We are at or past our destination. It's a better check than before but still not ideal. Would be good to stress test this

			if (immediate || Vector3.Dot(dest - tileToMove.transform.position, fallDir) <= 0 || _config.SettleKind == SettleKind.In_Place)
			{
				tileToMove.transform.position = dest;
			}
			else
			{
				tileToMove.transform.position += _config.FallSpeed * dT * fallDir;
				movedTile = true;
			}
		}

		// iterate through staged tiles

		foreach (Vector2Int coord in new Vector2IntIterator(_config.Layout.BottomRight()))
		{
			Tile tileToMove = _stagingBoard[coord.x, coord.y];

			if (!tileToMove)
				continue;

			Vector3 dest = spawnOffset + coord * tileSpacing * new Vector2(1, -1);

			if (immediate || Vector3.Dot(dest - tileToMove.transform.position, fallDir) <= 0)
			{
				tileToMove.transform.position = dest;
			}
			else
			{
				tileToMove.transform.position += _config.FallSpeed * dT * fallDir;
				movedTile = true;
			}
		}

		// check if we're done

		if (!movedTile)
		{
			_resolveState = ResolveState.Cleanup;
		}
	}

	void FinishResolve()
	{
		Vector2IntIterator coordIterator;

		switch (_config.SettleKind)
		{
			case SettleKind.In_Place:
			case SettleKind.Fall:
			case SettleKind.From_Left:
				coordIterator = new Vector2IntIterator(_config.Layout.BottomRight(), Vector2Int.zero); // y first doesn't really matter here
				break;
			default:
				coordIterator = new Vector2IntIterator(Vector2Int.zero, _config.Layout.BottomRight());
				break;
		}

		foreach (Vector2Int startCoord in coordIterator)
		{
			BoardDelta.TileDelta tDelta = _currDelta[startCoord];

			if (tDelta.IsTileDeletion())
				continue;

			_playableBoard[tDelta._destCoord.x, tDelta._destCoord.y] = _playableBoard[startCoord.x, startCoord.y];
			_playableBoard[tDelta._destCoord.x, tDelta._destCoord.y]._coord = tDelta._destCoord;
		}

		// move staged tiles

		foreach (Vector2Int coord in new Vector2IntIterator(_config.Layout.BottomRight(), Vector2Int.zero))
		{
			if (_stagingBoard[coord.x, coord.y])
			{
				_playableBoard[coord.x, coord.y] = _stagingBoard[coord.x, coord.y];
				_playableBoard[coord.x, coord.y]._coord = coord;
				_stagingBoard[coord.x, coord.y] = null;
			}
		}

		_currDelta = null;
		_resolveState = ResolveState.Nil;
		_currState = _nextState;
		_nextState = null;

		Debug.Log("");
		Debug.Log("Current State:");
		Debug.Log(_currState);
		Debug.Log("");
	}

	private Vector2 FallDirection()
	{
		switch (_config.SettleKind)
		{
			case SettleKind.In_Place:
				return Vector2.zero;
			case SettleKind.Fall:
				return Vector2.down;
			case SettleKind.Rise:
				return Vector2.up;
			case SettleKind.From_Left:
				return Vector2.right;
			case SettleKind.From_Right:
				return Vector2.left;
			default:
#if UNITY_EDITOR
				Debug.LogError($"Unexpected SETTLEK {_config.SettleKind} encountered");
#endif
				return new Vector2(float.NaN, float.NaN);
		}
	}

    /// <summary>
    /// Remove the specified tiles from the board and transfer them to a different transform (expected to be the battle manager)
    /// </summary>
    /// <param name="selectedTiles"></param>
    /// <param name="lockStateMachine"></param>
	internal void DisconnectTiles(List<Tile> selectedTiles, Transform newParent)
	{
		foreach (Tile tile in selectedTiles)
		{
			_currState[tile._coord] = ' ';

            // the board no longer cares about this tile, but the Battle Manager does care about it

            _playableBoard[tile._coord.x, tile._coord.y] = null;

            tile.transform.parent = newParent;
		}

		_resolveState = ResolveState.Spawn_New_Tiles;
	}

    public void LockStateMachine(bool locked)
    {
        _lockStateMachine = locked;
    }

    internal bool IsSettled()
    {
        return _resolveState == ResolveState.Nil;
    }

	internal void TransformTiles(Tile.TileKind oldKind, Tile.TileKind newKind, int num)
	{
		int converted = 0;
		Vector2Int dims = _config.Layout.Dims();

		while (converted < num)
		{
			int randCol = UnityEngine.Random.Range(0, dims.x);
			int randRow = UnityEngine.Random.Range(0, dims.y);

			if (_playableBoard[randCol, randRow].Kind == oldKind)
			{
				_playableBoard[randCol, randRow].Kind = newKind;
				converted++;
			}
		}
	}
}
