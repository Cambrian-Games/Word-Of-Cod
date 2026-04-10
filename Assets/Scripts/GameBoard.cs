using System;
using System.Collections.Generic;
using System.Linq;
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
	}

	// Update is called once per frame
	void Update()
	{
        if (!_lockStateMachine)
        {
            UpdateResolveState();
        }
	}

	internal void Shuffle()
	{
		int numSpinyTiles = 0;
		int numSandyTiles = 0;

		foreach (Vector2Int coord in new Vector2IntIterator(_config.Layout.BottomRight()))
		{
			if (!_playableBoard[coord.x, coord.y])
				continue;

			switch (_playableBoard[coord.x, coord.y].Kind)
			{
				case Tile.TileKind.Spiny:
					numSpinyTiles++;
					break;

				case Tile.TileKind.Sandy:
					numSandyTiles++;
					break;
			}
		}

		DeleteBoard();

		// TODO tell Update to wait some amount of time before spawning new board, for animations

		GenerateBoard();

		TransformRandomTiles(Tile.TileKind.Normal, Tile.TileKind.Spiny, numSpinyTiles);
		TransformRandomTiles(Tile.TileKind.Normal, Tile.TileKind.Sandy, numSandyTiles);
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

		// create a delta that marks every tile for deletion

		_currDelta = new BoardDelta(_config.Layout);
		DeleteSelectedTiles();
		_resolveState = ResolveState.Nil;

		_currState = _nextState = null;

        foreach (Tile tile in _stagingBoard)
        {
            if (tile)
            {
                Destroy(tile.gameObject);
            }
        }

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
			tile.transform.localPosition = stagingTopLeft + (kvp.Key * tileSpacing * new Vector2(1, -1));
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

			if (immediate || Vector3.Dot(dest - tileToMove.transform.localPosition, fallDir) <= 0 || _config.SettleKind == SettleKind.In_Place)
			{
				tileToMove.transform.localPosition = dest;
			}
			else
			{
				tileToMove.transform.localPosition += _config.FallSpeed * dT * fallDir;
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

			if (immediate || Vector3.Dot(dest - tileToMove.transform.localPosition, fallDir) <= 0)
			{
				tileToMove.transform.localPosition = dest;
			}
			else
			{
				tileToMove.transform.localPosition += _config.FallSpeed * dT * fallDir;
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

		// move staged tiles into playable board

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

	internal void TransformRandomTiles(Tile.TileKind oldKind, Tile.TileKind newKind, int num)
	{
		int converted = 0;

		List<Tile> allTilesOfOldKind = _playableBoard.OfType<Tile>().Where(tile => tile.Kind == oldKind).ToList();

		if (num >= allTilesOfOldKind.Count || num == 0)
		{
			TransformAllTiles(oldKind, newKind);
			return;
		}

		while (converted < num)
		{
			int tileIndex = UnityEngine.Random.Range(0, allTilesOfOldKind.Count);

			TransformTile(allTilesOfOldKind[tileIndex], newKind);
			allTilesOfOldKind.RemoveAt(tileIndex);

			converted++;
		}
	}

	internal void TransformAllTiles(Tile.TileKind oldKind, Tile.TileKind newKind)
	{
		foreach (Tile tile in _playableBoard)
		{
			if (tile && tile.Kind == oldKind)
			{
				TransformTile(tile, newKind);
			}
		}
	}

	internal void TransformTile(Tile tile, Tile.TileKind newKind)
	{
		TransformTile(tile._coord, newKind);
	}

	internal void TransformTile(Vector2Int coord, Tile.TileKind newKind)
    {
        if (_playableBoard[coord.x, coord.y])
        {
            _playableBoard[coord.x, coord.y].Kind = newKind;
        }
    }

    internal void ClearSurroundingSandTiles(Vector2Int tileCoord)
    {
        Vector2Int dims = _config.Layout.Dims();
        Debug.Assert(tileCoord.x >= 0 && tileCoord.x < dims.x);
        Debug.Assert(tileCoord.y >= 0 && tileCoord.y < dims.y);

        if (tileCoord.x > 0)
        {
			Tile targetTile = _playableBoard[tileCoord.x - 1, tileCoord.y];

			if (targetTile && targetTile.Kind == Tile.TileKind.Sandy)
			{
				TransformTile(targetTile, Tile.TileKind.Normal);
			}
        }

        if (tileCoord.x < dims.x - 1)
        {
			Tile targetTile = _playableBoard[tileCoord.x + 1, tileCoord.y];

			if (targetTile && targetTile.Kind == Tile.TileKind.Sandy)
			{
				TransformTile(targetTile, Tile.TileKind.Normal);
			}
		}

        if (tileCoord.y > 0)
        {
			Tile targetTile = _playableBoard[tileCoord.x, tileCoord.y - 1];

			if (targetTile && targetTile.Kind == Tile.TileKind.Sandy)
			{
				TransformTile(targetTile, Tile.TileKind.Normal);
			}
		}

        if (tileCoord.y < dims.y - 1)
        {
			Tile targetTile = _playableBoard[tileCoord.x, tileCoord.y + 1];

			if (targetTile && targetTile.Kind == Tile.TileKind.Sandy)
			{
				TransformTile(targetTile, Tile.TileKind.Normal);
			}
		}
    }

	internal int CountTiles(char letter) => _playableBoard.OfType<Tile>().Count(tile => tile && tile._letter == letter);
	internal int CountTiles(Tile.TileKind kind) => _playableBoard.OfType<Tile>().Count(tile => tile && tile.Kind == kind);
	internal int TotalTiles() => _playableBoard.OfType<Tile>().Count(tile => tile);

	// Not the best implementation but the only option we have unless we significantly alter how the board is managed
	public void ChangeTileLetter(Tile tile, char newLetter)
	{
		Debug.Assert(IsSettled());
		Debug.Assert(tile._letter == _currState[tile._coord]);
		Debug.Assert('A' <= newLetter && newLetter <= 'Z');
		tile._letter = _currState[tile._coord] = newLetter;
	}
}
