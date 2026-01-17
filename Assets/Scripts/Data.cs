using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Word Data

[Flags]
public enum FPART : byte
{
	NONE			= 0b00000000,
	NOUN			= 0b00000001,
	VERB			= 0b00000010,
	ADJECTIVE		= 0b00000100,
	ADVERB			= 0b00001000,
	PREPOSITION		= 0b00010000, // include prep_phrase
	PRONOUN			= 0b00100000,
	CONJUNCTION		= 0b01000000,
	OTHER			= 0b10000000, // Not used in-game, only used when parsing to flag words for potential omission
}

public class Word
{
    private string _text;
    public string Text => _text;
    private FPART _pOS;
    public FPART PartsOfSpeech => _pOS;

    private int _baseDamage;
    public int BaseDamage => _baseDamage;
    private List<Tile> _tilesUsed;
    private float _modifiedDamage = 0;
    public int EffectiveDamage => _modifiedDamage != 0 ? (int) _modifiedDamage : _baseDamage;

    public Word(string text, FPART pOS, List<Tile> tilesUsed)
    {
        _text = text;
        _pOS = pOS;
        _tilesUsed = new List<Tile>(tilesUsed);

        _baseDamage = _text.Length * (1 + (_text.Length - 3) / 10);
    }

    internal void ModifyDamage(RelicEffect.Result result)
    {
        _modifiedDamage = _baseDamage * (1 + result._values.GetValueOrDefault(RelicEffect.ValueToModify.Damage_Percent_Increase));
        _modifiedDamage += result._values.GetValueOrDefault(RelicEffect.ValueToModify.Damage_Bonus);
    }
}

// Board Data

public enum CellKind : byte
{
	Standard,	// Can be filled
	Locked,		// Cannot be filled nor passed through
	Void,       // Cannot be filled, can be passed through
				// Covered? Let tile fall through, can't be selected in this spot?

	[InspectorName(null)]
	Max
}

public enum SettleKind
{
	[InspectorName(null)] // hides the NIL element in the inspector
	Nil = -1,   // Used for overrides
	[InspectorName("In Place")]
	In_Place,
	Fall,
	Rise,
	[InspectorName("From Left")]
	From_Left,
	[InspectorName("From Right")]
	From_Right,
}

public class BoardState // the layout _can_ change mid battle due to enemy disruptions.
{
	public readonly BoardLayout		_layout;
	private char[,]					_chars;

	public BoardState(BoardLayout layout)
	{
		_layout = layout;
		_chars = new char[layout._length, layout._height];

		// There is no built-in way to do this. See https://github.com/dotnet/runtime/issues/47213

		foreach (Vector2Int coord in new Vector2IntIterator(_layout.BottomRight()))
		{
			_chars[coord.x, coord.y] = ' ';
		}
	}

	public BoardState Clone()
	{
		BoardState clonedState = new BoardState(_layout);
		clonedState._chars = (char[,])_chars.Clone();
		return clonedState;
	}

	public char this[int col, int row]
	{
		get => _chars[col, row];
		set => _chars[col, row] = value;
	}

	public char this[Vector2Int coord]
	{
		get => _chars[coord.x, coord.y];
		set => _chars[coord.x, coord.y] = value;
	}


	public BoardState CloneSettled(SettleKind settlek, out BoardDelta delta)
	{
		BoardState clonedState = Clone();
		clonedState.Settle(settlek, out delta);

		return clonedState;
	}

	// Should only call from within CloneSettled(SETTLEK)

	private void Settle(SettleKind settlek, out BoardDelta delta)
	{
		switch (settlek)
		{
			case SettleKind.In_Place:
				SettleInPlace(out delta);
				return;

			case SettleKind.Fall:
			case SettleKind.Rise:
				SettleVertical(settlek, out delta);
				return;

			default:
				SettleHorizontal(settlek, out delta);
				return;
		}
	}

	private void SettleInPlace(out BoardDelta delta)
	{
		delta = new BoardDelta(_layout);
		BoardConfig config = BoardConfig.INSTANCE;

		List<Vector2Int> newCharCoords = new List<Vector2Int>();

		foreach (Vector2Int coord in new Vector2IntIterator(_layout.BottomRight()))
		{
			if (_layout[coord] == CellKind.Standard)
			{
				if (this[coord] == ' ')
				{
					// new tile delta is added below, this tile gets deleted by default
					newCharCoords.Add(coord);
				}

			}
			else
			{
				// no-op delta
				delta[coord] = new BoardDelta.TileDelta(this[coord], coord);
			}
		}

		char[] newChars = config.Weights.RandomChars(newCharCoords.Count, state: this);

		for (int charIndex = 0; charIndex < newCharCoords.Count; charIndex++)
		{
			this[newCharCoords[charIndex]] = newChars[charIndex];
			delta.AddTile(newCharCoords[charIndex], newChars[charIndex]);
		}
	}

	private void SettleVertical(SettleKind settlek, out BoardDelta delta)
	{
		Debug.Assert(settlek == SettleKind.Fall || settlek == SettleKind.Rise);
		delta = new BoardDelta(_layout);
		BoardConfig config = BoardConfig.INSTANCE;

		List<Vector2Int> newCharCoords = new List<Vector2Int>();

		foreach (int col in new IntIterator(0, _layout._length - 1))
		{
			// check for empty cells

			IntIterator rowIterFall = settlek == SettleKind.Fall ?
				new IntIterator(_layout._height - 1, 0, -1) :	// bottom to top
				new IntIterator(0, _layout._height - 1, 1);		// top to bottom

			foreach (int row in rowIterFall)
			{
				if (_layout[col, row] != CellKind.Standard)
					continue;

				if (this[col, row] != ' ')
				{
					// no-op delta
					delta[col, row] = new BoardDelta.TileDelta(this[col, row], new Vector2Int(col, row));
					continue;
				}

				if (row == ((settlek == SettleKind.Fall) ? 0 : _layout._height - 1))
					continue;

				// check for non-empty cells that can fill the empty cell

				IntIterator rowIterScan = settlek == SettleKind.Fall ?
					new IntIterator(row - 1, 0, -1) :					// all cells above the current
					new IntIterator(row + 1, _layout._height - 1, 1);	// all cells below the current

				foreach (int rowScan in rowIterScan)
				{
					// if the cell is locked, stop searching

					if (_layout[col, rowScan] == CellKind.Locked)
						break;

					// if the cell is void, skip this cell and continue searching

					if (_layout[col, rowScan] == CellKind.Void)
						continue;

					// if the cell is standard and non-empty, move that to this cell and set that cell to empty

					if (this[col, rowScan] != ' ')
					{
						// store a delta that <col, rowScan> is moving to <col, row>
						delta[col, rowScan] = new BoardDelta.TileDelta(this[col, rowScan], new Vector2Int(col, row));

						this[col, row] = this[col, rowScan];
						this[col, rowScan] = ' ';

						break;
					}
				}
			}

			IntIterator rowIterPopulate = settlek == SettleKind.Fall ?
				new IntIterator(0, _layout._height - 1, 1) :	// top to bottom
				new IntIterator(_layout._height - 1, 0, -1);    // bottom to top

			foreach (int row in rowIterPopulate)
			{
				// if the cell is locked, then neither this cell nor any after it can have new characters generate

				if (_layout[col, row] == CellKind.Locked)
					break;

				// if the cell is void, skip this cell and continue generating new characters

				if (_layout[col, row] == CellKind.Void)
					continue;

				if (this[col, row] == ' ')
				{
					newCharCoords.Add(new Vector2Int(col, row));
				}
			}
		}

		char[] newChars = config.Weights.RandomChars(newCharCoords.Count, state: this);

		for (int charIndex = 0; charIndex < newCharCoords.Count; charIndex++)
		{
			this[newCharCoords[charIndex]] = newChars[charIndex];
			delta.AddTile(newCharCoords[charIndex], newChars[charIndex]);
		}
	}

	private void SettleHorizontal(SettleKind settlek, out BoardDelta delta)
	{
		Debug.Assert(settlek == SettleKind.From_Left || settlek == SettleKind.From_Right);

		delta = new BoardDelta(_layout);
		BoardConfig config = BoardConfig.INSTANCE;

		List<Vector2Int> newCharCoords = new List<Vector2Int>();

		foreach (int row in new IntIterator(0, _layout._height - 1))
		{
			IntIterator colIteratorFall = settlek == SettleKind.From_Left ?
				new IntIterator(_layout._length - 1, 0, -1) :	// right to left
				new IntIterator(0, _layout._length - 1, 1);	// left to right

			foreach (int col in colIteratorFall)
			{
				if (_layout[col, row] != CellKind.Standard)
					continue;

				if (this[col, row] != ' ')
				{
					// no-op delta
					delta[col, row] = new BoardDelta.TileDelta(this[col, row], new Vector2Int(col, row));
					continue;
				}

				if (col == ((settlek == SettleKind.From_Left) ? 0 : _layout._length - 1))
					continue;

				IntIterator colIteratorScan = settlek == SettleKind.From_Left ?
					new IntIterator(col - 1, 0, -1) :					// all cells left of the current
					new IntIterator(col + 1, _layout._length - 1, 1);	// all cells right of the current

				foreach (int colScan in colIteratorScan)
				{
					// if the cell is locked, stop searching

					if (_layout[colScan, row] == CellKind.Locked)
						break;

					// if the cell is void, skip this cell and continue searching

					if (_layout[colScan, row] == CellKind.Void)
						continue;

					// if the cell is standard and non-empty, move that to this cell and set that cell to empty

					if (this[colScan, row] != ' ')
					{
						// store a delta that <colScan, row> is moving to <col, row>
						delta[colScan, row] = new BoardDelta.TileDelta(this[colScan, row], new Vector2Int(col, row));

						this[col, row] = this[colScan, row];
						this[colScan, row] = ' ';
						break;
					}
				}
			}

			IntIterator colIteratorPopulate = settlek == SettleKind.From_Left ?
				new IntIterator(0, _layout._length - 1, 1) :	// left to right
				new IntIterator(_layout._length - 1, 0, -1);    // right to left

			foreach (int col in colIteratorPopulate)
			{
				// if the cell is locked, then neither this cell nor any after it can have new characters generate

				if (_layout[col, row] == CellKind.Locked)
					break;

				// if the cell is void, skip this cell and continue generating new characters

				if (_layout[col, row] == CellKind.Void)
					continue;

				if (this[col, row] == ' ')
				{
					newCharCoords.Add(new Vector2Int(col, row));
				}
			}
		}

		char[] newChars = config.Weights.RandomChars(newCharCoords.Count, state: this);

		for (int charIndex = 0; charIndex < newCharCoords.Count; charIndex++)
		{
			this[newCharCoords[charIndex]] = newChars[charIndex];
			delta.AddTile(newCharCoords[charIndex], newChars[charIndex]);
		}
	}

	public override string ToString()
	{
		string result = "";

		foreach (int row in new IntIterator(0, _layout._height - 1))
		{
			foreach (int col in new IntIterator(0, _layout._length - 1))
			{
				result += _chars[col, row];
			}
			if (row != _layout._length - 1)
			{
				result += '\n';
			}
		}

		return result;
	}
}

/// <summary>
/// Represents the steps required to get from one board state to the next. Used to tell tiles where to go.
/// If _vec2iEnd == <-1, -1> it means the tile in that spot was removed.
/// 
/// Still need to check the board state as that 
/// 
/// Storing _c may be unnecessary, we'll see.
/// </summary>
public class BoardDelta
{
	public struct TileDelta
	{
		public readonly char _char;
		public Vector2Int _destCoord;

		public TileDelta(char c, Vector2Int destCoord)
		{
			_char = c;
			_destCoord = destCoord;
		}

		public bool IsTileDeletion() => _destCoord == new Vector2Int(-1, -1);
	}

	public readonly TileDelta[,] _deltas;

	// We want to avoid duplicates here so a list would be inefficient

	public Dictionary<Vector2Int, char> _newTiles;

	public BoardDelta(BoardLayout layout)
	{
		_deltas = new TileDelta[layout._length, layout._height];

		foreach (Vector2Int startCoord in new Vector2IntIterator(layout.BottomRight()))
		{
			_deltas[startCoord.x, startCoord.y]._destCoord = new Vector2Int(-1, -1);
		}

		_newTiles = new Dictionary<Vector2Int, char>();
	}

	public TileDelta this[int col, int row]
	{
		get => _deltas[col, row];
		set => _deltas[col, row] = value;
	}

	public TileDelta this[Vector2Int coord]
	{
		get => _deltas[coord.x, coord.y];
		set => _deltas[coord.x, coord.y] = value;
	}

	public void AddTile(Vector2Int destCoord, char c)
	{
		Debug.Assert(!_newTiles.ContainsKey(destCoord));

		_newTiles[destCoord] = c;
	}
}

// Tile Data

/// <summary>
/// While these behave similarly to flags (0b01 = highlighted, 0b10 = selected), they are distinct states.
/// See Tile::SetHighlightState() for an example use case.
/// </summary>
public enum HighlightState
{
	Normal,
	Highlighted,
	Selected,
	Selected_And_Highlighted
}

//we have up to 64 different relics (does not include charging relics)
//meant to be binary flags
//[Flags]
public enum FRELICID : long
{
	INVALID		= 0x0000000000000000,
	NOUNUP		= 0x0000000000000001, //increase damage of noun-tagged words
	YUP			= 0x0000000000000002, //increase damage of words with Y
	RESISTUP	= 0x0000000000000004, //reduce incoming damage
	NI1			= 0x0000000000000008, //not implemented
	NI2			= 0x0000000000000010, //not implemented
	NI3			= 0x0000000000000020, //not implemented
	NI4			= 0x0000000000000040, //not implemented
	NI5			= 0x0000000000000080, //not implemented
	NI6			= 0x0000000000000100, //not implemented
	NI7			= 0x0000000000000200, //not implemented
	NI8			= 0x0000000000000400, //not implemented
	NI9			= 0x0000000000000800, //not implemented
	NI10		= 0x0000000000001000, //not implemented
	NI11		= 0x0000000000002000, //not implemented
	NI12		= 0x0000000000004000, //not implemented
	NI13		= 0x0000000000008000, //not implemented
	NI14		= 0x0000000000010000, //not implemented
	NI15		= 0x0000000000020000, //not implemented
	NI16		= 0x0000000000040000, //not implemented
	NI17		= 0x0000000000080000, //not implemented
	NI18		= 0x0000000000100000, //not implemented
	NI19		= 0x0000000000200008, //not implemented
	NI20		= 0x0000000000400008, //not implemented
	NI21		= 0x0000000000800000, //not implemented
	NI22		= 0x0000000001000000, //not implemented
	NI23		= 0x0000000002000000, //not implemented
	NI24		= 0x0000000004000000, //not implemented
	NI25		= 0x0000000008000000, //not implemented
	NI26		= 0x0000000010000000, //not implemented
	NI27		= 0x0000000020000000, //not implemented
	NI28		= 0x0000000040000000, //not implemented
	NI29		= 0x0000000080000000, //not implemented
	NI30		= 0x0000000100000000, //not implemented
	NI31		= 0x0000000200000000, //not implemented
	NI32		= 0x0000000400000000, //not implemented
	NI33		= 0x0000000800000000, //not implemented
	NI34		= 0x0000001000000000, //not implemented
	NI35		= 0x0000002000000000, //not implemented
	NI36		= 0x0000004000000000, //not implemented
	NI37		= 0x0000008000000000, //not implemented
	NI38		= 0x0000010000000000, //not implemented
	NI39		= 0x0000020000000000, //not implemented
	NI40		= 0x0000040000000000, //not implemented
	NI41		= 0x0000080000000000, //not implemented
	NI42		= 0x0000100000000000, //not implemented
	NI43		= 0x0000200000000000, //not implemented
	NI44		= 0x0000400000000000, //not implemented
	NI45		= 0x0000800000000000, //not implemented
	NI46		= 0x0001000000000000, //not implemented
	NI47		= 0x0002000000000000, //not implemented
	NI48		= 0x0004000000000000, //not implemented
	NI49		= 0x0008000000000000, //not implemented
	NI50		= 0x0010000000000000, //not implemented
	NI51		= 0x0020000000000000, //not implemented
	NI52		= 0x0040000000000000, //not implemented
	NI53		= 0x0080000000000000, //not implemented
	NI54		= 0x0100000000000000, //not implemented
	NI55		= 0x0200000000000000, //not implemented
	NI56		= 0x0400000000000000, //not implemented
	NI57		= 0x0800000000000000, //not implemented
	NI58		= 0x1000000000000000, //not implemented
	NI59		= 0x2000000000000000, //not implemented
	NI60		= 0x4000000000000000, //not implemented
	//OTHER		= 0xFFFFFFFFFFFFFFFF, //placeholder for end rn
}