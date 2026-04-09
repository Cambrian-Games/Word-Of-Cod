using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AttackEffect))]
public class AttackEffectPropertyDrawer : PropertyDrawer
{
	static readonly float Y_OFFSET = EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		EditorGUI.BeginProperty(position, label, property);
		position.height = EditorGUIUtility.singleLineHeight;

		EditorGUI.LabelField(position, label, EditorStyles.boldLabel);

		position.y += Y_OFFSET;
		EditorGUI.PropertyField(position, property.FindPropertyRelative("name"));
		position.y += Y_OFFSET;
		EditorGUI.PropertyField(position, property.FindPropertyRelative("_endsTurn"));

		//AttackEffect.EffectKind ruleKind = (AttackEffect.EffectKind) rule.enumValueIndex;
		//
		//switch (ruleKind)
		//{
		//	case AttackEffect.EffectKind.Do_Nothing:
		//		break;
		//
		//	case AttackEffect.EffectKind.Standard_Attack:
		//		position.y += Y_OFFSET;
		//		EditorGUI.PropertyField(position, property.FindPropertyRelative("_damage"));
		//		break;
		//
		//	case AttackEffect.EffectKind.Transform_Tiles:
		//		float standardWidth = position.width;
		//		float standardX = position.x;
		//		position.y += Y_OFFSET;
		//		EditorGUI.PropertyField(position, property.FindPropertyRelative("_numTiles"), new GUIContent("Change"));
		//		position.y += Y_OFFSET;
		//		EditorGUI.MultiPropertyField(position, new GUIContent[] { new GUIContent("From"), new GUIContent("To") }, property.FindPropertyRelative("_from"));
		//		break;
		//
		//	case AttackEffect.EffectKind.Schooling_Attack:
		//		position.y += Y_OFFSET;
		//		EditorGUI.PropertyField(position, property.FindPropertyRelative("_minSchoolAttackHits"), new GUIContent("Minimum Hits"));
		//		position.y += Y_OFFSET;
		//		EditorGUI.PropertyField(position, property.FindPropertyRelative("_maxSchoolAttackHits"), new GUIContent("Maximum Hits"));
		//		position.y += Y_OFFSET;
		//		EditorGUI.PropertyField(position, property.FindPropertyRelative("_damage"), new GUIContent("Base Damage"));
		//		break;
		//
		//	case AttackEffect.EffectKind.Count_Variant_Tiles:
		//		position.y += Y_OFFSET;
		//		EditorGUI.PropertyField(position, property.FindPropertyRelative("_to"), new GUIContent("Kind"));
		//		break;
		//
		//	case AttackEffect.EffectKind.Variant_Tile_Attack:
		//		position.y += Y_OFFSET;
		//		EditorGUI.PropertyField(position, property.FindPropertyRelative("_to"), new GUIContent("Kind"));
		//		position.y += Y_OFFSET;
		//		EditorGUI.PropertyField(position, property.FindPropertyRelative("_damage"), new GUIContent("Base Damage"));
		//		break;
		//}
		//
		//position.y += Y_OFFSET;
		//GUI.enabled = ruleKind != AttackEffect.EffectKind.Do_Nothing;
		//EditorGUI.PropertyField(position, property.FindPropertyRelative("_afterEffectDelay"), new GUIContent("Delay After"));
		//position.y += Y_OFFSET;
		//EditorGUI.LabelField(position, "Delay is ignored if this is the last effect", EditorStyles.boldLabel);
		//position.y += Y_OFFSET;
		//EditorGUI.LabelField(position, "\tor if the effect kind is Do Nothing.", EditorStyles.boldLabel);
		//GUI.enabled = true;
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		//SerializedProperty rule = property.FindPropertyRelative("_effectKind");
		//AttackEffect.EffectKind ruleKind = (AttackEffect.EffectKind)rule.enumValueIndex;
		//
		//int lines = ruleKind switch
		//{
		//	AttackEffect.EffectKind.Do_Nothing => 0,
		//	AttackEffect.EffectKind.Standard_Attack => 1,
		//	AttackEffect.EffectKind.Transform_Tiles => 2,
		//	AttackEffect.EffectKind.Schooling_Attack => 3,
		//
		//	AttackEffect.EffectKind.Count_Variant_Tiles => 1,
		//	AttackEffect.EffectKind.Variant_Tile_Attack => 2,
		//	_ => 0,
		//};
		//
		//lines += 9; // element, effect kind, Delay After, Interrupt Checkpoint, Ends Turn, and warning (2 lines)
		//
		//return lines * Y_OFFSET + EditorGUIUtility.standardVerticalSpacing;

		return 3 * Y_OFFSET;
	}
}