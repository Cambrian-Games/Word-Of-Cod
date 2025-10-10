using odin.serialize.OdinSerializer;
using UnityEngine;

[CreateAssetMenu(fileName = "BoardLayout", menuName = "Scriptable Objects/Board Layout")]
public class BoardLayout : SerializedScriptableObject
{
	[Min(1)]
	public int _length = 1;

	[Min(1)]
	public int _height = 1;

	[OdinSerialize]
	private CellKind[,] _grid;

	public CellKind[,] Grid => _grid;

	public CellKind this[int col, int row]
	{
		get => _grid[col, row];
#if UNITY_EDITOR
		set => _grid[col, row] = value;
#endif
	}

	public CellKind this[Vector2Int coord]
	{
		get => _grid[coord.x, coord.y];
#if UNITY_EDITOR
		set => _grid[coord.x, coord.y] = value;
#endif
	}

#if UNITY_EDITOR
	// Does not compile in release builds

	public void SetGrid(CellKind[,] gridNew)
	{
		_grid = gridNew;
		_length = _grid.GetLength(0);
		_height = _grid.GetLength(1);
	}
#endif

	public Vector2Int Dims() => new Vector2Int(_length, _height);
	public Vector2Int BottomRight() => Dims() - Vector2Int.one;

	// should Layout have TopRow, BottomRow, LeftCol, and RightCol properties?
}
