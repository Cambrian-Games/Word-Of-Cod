using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AttackEffect))]
public class AttackEffectPropertyDrawer : PropertyDrawer
{
	static readonly float YOFFSET = EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		EditorGUI.BeginProperty(position, label, property);
		position.height = EditorGUIUtility.singleLineHeight;

		EditorGUI.LabelField(position, label, EditorStyles.boldLabel);
		SerializedProperty rule = property.FindPropertyRelative("_effectKind");

		AttackEffect.EffectKind ruleKind = (AttackEffect.EffectKind)rule.enumValueIndex;
		position.y += YOFFSET;
		EditorGUI.PropertyField(position, rule);
		position.y += EditorGUIUtility.standardVerticalSpacing; // add a bit of space between the effect kind and parameters

		switch (ruleKind)
		{
			case AttackEffect.EffectKind.Do_Nothing:
				position.y += YOFFSET;
				EditorGUI.LabelField(position, "Only recommended when Attack Priority", EditorStyles.boldLabel);
				position.y += YOFFSET;
				EditorGUI.LabelField(position, "\tis set to Loop", EditorStyles.boldLabel);
				break;
			case AttackEffect.EffectKind.Standard_Attack:
				position.y += YOFFSET;
				EditorGUI.PropertyField(position, property.FindPropertyRelative("_damage"));
				break;

			case AttackEffect.EffectKind.Transform_Tiles:
				float standardWidth = position.width;
				float standardX = position.x;
				position.y += YOFFSET;
				EditorGUI.PropertyField(position, property.FindPropertyRelative("_numTiles"), new GUIContent("Change"));
				position.y += YOFFSET;
				EditorGUI.MultiPropertyField(position, new GUIContent[] { new GUIContent("From"), new GUIContent("To") }, property.FindPropertyRelative("_from"));
				break;
		}

		position.y += YOFFSET;
		GUI.enabled = ruleKind != AttackEffect.EffectKind.Do_Nothing;
		EditorGUI.PropertyField(position, property.FindPropertyRelative("_afterEffectDelay"), new GUIContent("Delay After"));
		position.y += YOFFSET;
		EditorGUI.LabelField(position, "Delay is ignored if this is the last effect", EditorStyles.boldLabel);
		position.y += YOFFSET;
		EditorGUI.LabelField(position, "\tor if the effect kind is Do Nothing.", EditorStyles.boldLabel);
		GUI.enabled = true;
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		SerializedProperty rule = property.FindPropertyRelative("_effectKind");
		AttackEffect.EffectKind ruleKind = (AttackEffect.EffectKind)rule.enumValueIndex;

		int lines = ruleKind switch
		{
			AttackEffect.EffectKind.Do_Nothing => 2,
			AttackEffect.EffectKind.Standard_Attack => 1,
			AttackEffect.EffectKind.Transform_Tiles => 2,
			_ => 0,
		};

		lines += 5; // element, effect kind, Delay After, and warning (2 lines)

		return lines * YOFFSET + EditorGUIUtility.standardVerticalSpacing;
	}
}