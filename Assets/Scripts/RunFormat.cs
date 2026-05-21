using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CreateAssetMenu(fileName = "New Run Format", menuName = "Scriptable Objects/Run Format")]
public class RunFormat : ScriptableObject
{
    [SerializeField]
    private List<RunEvent> _events;
    public List<RunEvent> Events => _events;

    [Serializable]
    public class RunEvent
    {
        [SerializeField, HideInInspector]
        private int _index = -1;
        public int Index { get => _index;
#if UNITY_EDITOR
            set => _index = value;
#endif
        }

        [SerializeField]
        private bool _canBeShop;
        [SerializeField, Tooltip("What option # the shop is")]
        private int _shopOptionIndex;
        [SerializeField]
        private List<EncounterPool> _encounterPools;
        
        // TODO Add "Ends Act" field



        public bool HasChoice => OptionCount >= 2;
        public int OptionCount => _encounterPools.Count + (_canBeShop ? 1 : 0);

        public SelectedEvent Select(int selectionIndex)
        {
            Debug.Assert(selectionIndex >= 0 && selectionIndex < _encounterPools.Count + (_canBeShop ? 1 : 0));

            EncounterPool pool = null;

            if (!_canBeShop || selectionIndex < _shopOptionIndex)
            {
                pool = _encounterPools[selectionIndex];
            }
            else if (selectionIndex > _shopOptionIndex)
            {
                pool = _encounterPools[selectionIndex - 1];
            }

            return new SelectedEvent(_index, selectionIndex, isShop: selectionIndex == _shopOptionIndex, pool);
        }
    }

    #region Run Event Property Drawer
#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(RunEvent))]
    public class RunEventPropertyDrawer : PropertyDrawer
    {
        protected static readonly float Y_OFFSET = EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;



        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);
            position.height = EditorGUIUtility.singleLineHeight;

            EditorGUI.LabelField(position, label);

            position.y += Y_OFFSET;

            EditorGUI.indentLevel++;
            SerializedProperty canBeShop = property.FindPropertyRelative("_canBeShop");
            EditorGUI.PropertyField(position, canBeShop);

            SerializedProperty poolList = property.FindPropertyRelative("_encounterPools");
            int poolCount = poolList.arraySize;

            if (canBeShop.boolValue)
            {
                position.y += Y_OFFSET;
                SerializedProperty shopOptionIndex = property.FindPropertyRelative("_shopOptionIndex");
                EditorGUI.PropertyField(position, shopOptionIndex);
                shopOptionIndex.intValue = Mathf.Clamp(shopOptionIndex.intValue, 0, poolCount);
            }

            position.y += Y_OFFSET;
            EditorGUI.PropertyField(position, poolList);

            while (poolCount < poolList.arraySize)
            {
                poolList.GetArrayElementAtIndex(poolCount).boxedValue = null;
                poolCount++;
            }

            EditorGUI.indentLevel--;
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float baseHeight = base.GetPropertyHeight(property, label);
            float canBeShopHeight = Y_OFFSET + (property.FindPropertyRelative("_canBeShop").boolValue ? Y_OFFSET : 0);
            float poolsHeight = EditorGUI.GetPropertyHeight(property.FindPropertyRelative("_encounterPools"));

            return baseHeight + canBeShopHeight + EditorGUIUtility.standardVerticalSpacing + poolsHeight;
        }
    }
#endif
    #endregion

    [Serializable]
    public class SelectedEvent
    {
        public readonly int _eventIndex;
        public readonly int _selectionIndex;
        public readonly bool _isShop;
        public readonly EncounterPool _pool;
        private Enemy _encounterPrefab;

        public SelectedEvent(int eventIndex, int selectionIndex, bool isShop, EncounterPool pool)
        {
            _eventIndex = eventIndex;
            _selectionIndex = selectionIndex;
            _isShop = isShop;
            _pool = pool;
        }

        public Enemy EncounterPrefab
        {
            get => _encounterPrefab;
            set
            {
                if (_encounterPrefab != null)
                {
                    throw new InvalidOperationException();
                }
                else
                {
                    _encounterPrefab = value;
                }
            }
        }
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        for (int i = 0; i < _events.Count; i++)
        {
            _events[i].Index = i;
        }
    }
#endif

    public RunEvent Event(int index) => _events[index];
}
