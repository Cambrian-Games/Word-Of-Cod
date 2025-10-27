using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(AttackRule))]
public class AttackRulePropertyDrawer : PropertyDrawer
{

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		EditorGUI.BeginProperty(position, label, property);
		position.height = EditorGUIUtility.singleLineHeight;

		EditorGUI.LabelField(position, label, EditorStyles.boldLabel);
		SerializedProperty rule = property.FindPropertyRelative("_ruleKind");

		AttackRule.RuleKind ruleKind = (AttackRule.RuleKind)rule.enumValueIndex;
		position.y += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
		EditorGUI.PropertyField(position, rule);

		switch (ruleKind)
		{
			case AttackRule.RuleKind.Wait_Turns:
				position.y += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
				EditorGUI.PropertyField(position, property.FindPropertyRelative("_turnsToWait"));
				break;
			case AttackRule.RuleKind.Standard_Attack:
				position.y += EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;
				EditorGUI.PropertyField(position, property.FindPropertyRelative("_damage"));
				break;
		}
	}

	public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
		SerializedProperty rule = property.FindPropertyRelative("_ruleKind");
		AttackRule.RuleKind ruleKind = (AttackRule.RuleKind)rule.enumValueIndex;
		Debug.Log(ruleKind);
		int lines = 0;
		switch (ruleKind)
		{
			case AttackRule.RuleKind.Wait_Turns:
			case AttackRule.RuleKind.Standard_Attack:
				lines = 2;
				break;
			default:
				lines = 0;
				break;
		}

		return (lines + 1) * EditorGUIUtility.singleLineHeight + (lines + 1) * EditorGUIUtility.standardVerticalSpacing;
	}
}