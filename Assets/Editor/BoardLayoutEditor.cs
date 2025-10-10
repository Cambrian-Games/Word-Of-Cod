using System;
using UnityEditor;
using UnityEngine;

public class BoardLayoutEditorWindow : EditorWindow
{
	public BoardLayout _target;

	private bool _hadTargetPrev = false;

	private int _heightPending;
	private int _lengthPending;

	static Color[] COLORS = new Color[(int)CellKind.Max]
	{ 
		new Color32(0xFF, 0xFF, 0xFF, 0xFF),
		new Color32(0xFF, 0x49, 0x49, 0xFF),
		new Color32(0x64, 0x77, 0xFF, 0xFF)
	};

	[MenuItem("Tools/Board Layout Editor")]
    public static BoardLayoutEditorWindow ShowBoardLayoutEditor()
	{
		return GetWindow<BoardLayoutEditorWindow>();
	}

	private void OnGUI()
	{
		_target = (BoardLayout) EditorGUILayout.ObjectField("Layout Object", _target, typeof(BoardLayout), false);
		if (_target)
		{
			EditorGUILayout.LabelField("Dimensions");
			EditorGUI.indentLevel++;
			_lengthPending = Math.Max(1, EditorGUILayout.IntField("Length", !_hadTargetPrev ? _target._length : _lengthPending));
			_heightPending = Math.Max(1, EditorGUILayout.IntField("Height", !_hadTargetPrev ? _target._height : _heightPending));
			EditorGUI.indentLevel--;

			_hadTargetPrev = true;

			bool pendingSizeChange = _target._length != _lengthPending || _target._height != _heightPending;
			GUI.enabled = pendingSizeChange;
			if (GUILayout.Button("Apply Dimension Changes"))
			{
				UpdateArrayDims();
			}
			GUI.enabled = true;

			foreach (CellKind cellk in Enum.GetValues(typeof(CellKind)))
			{
				if (cellk == CellKind.Max)
					continue;

				COLORS[(int)cellk] = EditorGUILayout.ColorField(Enum.GetName(typeof(CellKind), cellk), COLORS[(int)cellk]);
			}

			GUI.enabled = !pendingSizeChange;

			Color defaultColor = GUI.color;
			Rect rectDefault = GUILayoutUtility.GetLastRect();
			rectDefault.y += EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;
			Rect rectNew = rectDefault;

			// WHY????
			// Relied on https://github.com/Unity-Technologies/UnityCsReference/blob/4b463aa72c78ec7490b7f03176bd012399881768/Editor/Mono/Inspector/SpriteRendererEditor.cs
			// for insights into why we needed to specify the width, but I do not fully understand why we need it

			rectNew.width = EditorGUIUtility.singleLineHeight;

			foreach (Vector2Int coord in new Vector2IntIterator(_target.BottomRight()))
			{
				rectNew.x = rectDefault.x + coord.x * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);
				rectNew.y = rectDefault.y + coord.y * (EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing);

				GUI.color = COLORS[(int)_target[coord]];
				if (!EditorGUI.Toggle(rectNew, true))
				{
					int cellkNext = (int)_target[coord] + 1;
					cellkNext %= (int)CellKind.Max;
					_target[coord] = (CellKind)cellkNext;
				}
			}

			GUI.color = defaultColor;
			GUI.enabled = true;

			EditorUtility.SetDirty(_target);
		}
	}

	private void UpdateArrayDims()
	{
		Debug.Assert(_target);

		CellKind[,] gridNew = new CellKind[_lengthPending, _heightPending];

		if (_target.Grid != null)
		{
			foreach (Vector2Int coord in new Vector2IntIterator(Vector2Int.Min(new Vector2Int(_lengthPending - 1, _heightPending - 1), _target.BottomRight())))
			{
				gridNew[coord.x, coord.y] = _target[coord];
			}
		}

		_target.SetGrid(gridNew);
	}
}

[CustomEditor(typeof(BoardLayout))]
public class BoardLayoutEditor : Editor
{
	public override void OnInspectorGUI()
	{
		if (GUILayout.Button("Open in Board Layout Editor"))
		{
			BoardLayoutEditorWindow.ShowBoardLayoutEditor()._target = target as BoardLayout;
		}
	}
}
