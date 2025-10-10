using UnityEditor;
using UnityEngine;

/// <summary>
/// General config class for GameBoard
/// </summary>
public class BoardConfig : MonoBehaviour
{
	public static BoardConfig INSTANCE;

	[SerializeField]
	private SettleKind _defaultSettleKind = SettleKind.Fall;
	private SettleKind _overrideSettleKind = SettleKind.Nil;
	public SettleKind SettleKind => _overrideSettleKind != SettleKind.Nil ? _overrideSettleKind : _defaultSettleKind;

	[SerializeField]
	private CharacterWeights _characterWeights;
	public CharacterWeights Weights => _characterWeights;

	[SerializeField]
	private BoardLayout _layout;
	public BoardLayout Layout { get => _layout; set => _layout = value; }

	[SerializeField]
	private Tile _defaultTilePrefab; // this will later be joined by any other tile types we choose to add
	public Tile DefaultTilePrefab => _defaultTilePrefab;

	[SerializeField]
	private Vector2 _tileSpacing;
	public Vector2 TileSpacing => _tileSpacing;
	[SerializeField]
	private Vector2 _spawnOffset;
	public Vector2 SpawnOffset => _spawnOffset;

	[SerializeField]
	[Min(0.1f)]
	private float _fallSpeed;
	public float FallSpeed => _fallSpeed;

	private void Awake()
	{
		// set up singleton

		if (INSTANCE != null && INSTANCE != this)
		{
			Destroy(gameObject);
			return;
		}

		INSTANCE = this;

#if DEBUG
		// this should not show up in the release build ever

		if (_layout == null)
		{
			ConstructTestBoard();
		}
#endif
	}

	// Start is called once before the first execution of Update after the MonoBehaviour is created
	void Start()
	{
	}

	// Update is called once per frame
	void Update()
	{
		
	}

#if DEBUG
	private void ConstructTestBoard()
	{
		_layout = ScriptableObject.CreateInstance<BoardLayout>();
		_layout._length = _layout._height = 7;
		_layout.SetGrid(new CellKind[_layout._length, _layout._height]);

		_layout[0, 0] = CellKind.Standard; _layout[1, 0] = CellKind.Standard; _layout[2, 0] = CellKind.Void;		_layout[3, 0] = CellKind.Void;		_layout[4, 0] = CellKind.Void;		_layout[5, 0] = CellKind.Standard; _layout[6, 0] = CellKind.Standard;
		_layout[0, 1] = CellKind.Standard; _layout[1, 1] = CellKind.Standard; _layout[2, 1] = CellKind.Standard; _layout[3, 1] = CellKind.Void;		_layout[4, 1] = CellKind.Standard; _layout[5, 1] = CellKind.Standard; _layout[6, 1] = CellKind.Standard;
		_layout[0, 2] = CellKind.Void;		_layout[1, 2] = CellKind.Standard; _layout[2, 2] = CellKind.Standard; _layout[3, 2] = CellKind.Standard; _layout[4, 2] = CellKind.Standard; _layout[5, 2] = CellKind.Standard; _layout[6, 2] = CellKind.Void;
		_layout[0, 3] = CellKind.Void;		_layout[1, 3] = CellKind.Void;		_layout[2, 3] = CellKind.Standard; _layout[3, 3] = CellKind.Standard; _layout[4, 3] = CellKind.Standard; _layout[5, 3] = CellKind.Void;		_layout[6, 3] = CellKind.Void;
		_layout[0, 4] = CellKind.Void;		_layout[1, 4] = CellKind.Standard; _layout[2, 4] = CellKind.Standard; _layout[3, 4] = CellKind.Standard; _layout[4, 4] = CellKind.Standard; _layout[5, 4] = CellKind.Standard; _layout[6, 4] = CellKind.Void;
		_layout[0, 5] = CellKind.Standard; _layout[1, 5] = CellKind.Standard; _layout[2, 5] = CellKind.Standard; _layout[3, 5] = CellKind.Void;		_layout[4, 5] = CellKind.Standard; _layout[5, 5] = CellKind.Standard; _layout[6, 5] = CellKind.Standard;
		_layout[0, 6] = CellKind.Standard; _layout[1, 6] = CellKind.Standard; _layout[2, 6] = CellKind.Void;		_layout[3, 6] = CellKind.Void;		_layout[4, 6] = CellKind.Void;		_layout[5, 6] = CellKind.Standard; _layout[6, 6] = CellKind.Standard;
	}
#endif

	public void SetOverrideSettlek(SettleKind overrideSettleKind)
	{
		_overrideSettleKind = overrideSettleKind;
	}
}
