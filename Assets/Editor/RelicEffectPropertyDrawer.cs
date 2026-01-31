using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(RelicEffect))]
public class RelicEffectPropertyDrawer : PropertyDrawer
{
	static readonly float Y_OFFSET = EditorGUIUtility.standardVerticalSpacing + EditorGUIUtility.singleLineHeight;

	public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
	{
		EditorGUI.BeginProperty(position, label, property);
		position.height = EditorGUIUtility.singleLineHeight;

		EditorGUI.LabelField(position, label, EditorStyles.boldLabel);

		position.y += Y_OFFSET;
		EditorGUI.PropertyField(position, property.FindPropertyRelative("_event"));

		position.y += Y_OFFSET;
		SerializedProperty condition = property.FindPropertyRelative("_condition");
		EditorGUI.PropertyField(position, condition);
		position.y += EditorGUIUtility.standardVerticalSpacing; // add a bit of space between the effect kind and parameters

        RelicEffect.RelicCondition conditionKind = (RelicEffect.RelicCondition) condition.enumValueIndex;

        switch (conditionKind)
        {
            case RelicEffect.RelicCondition.Always_Active:
                break;

            case RelicEffect.RelicCondition.Contains_All_Letters:
            case RelicEffect.RelicCondition.Contains_Any_Letters:
            case RelicEffect.RelicCondition.Contains_Unique_Letters:
            case RelicEffect.RelicCondition.Contains_Sequence:
            case RelicEffect.RelicCondition.Does_Not_Contain_Letter:
            case RelicEffect.RelicCondition.Middle_Letter:
                position.y += Y_OFFSET;
                EditorGUI.PropertyField(position, property.FindPropertyRelative("_filterString"));
                break;

            case RelicEffect.RelicCondition.Contains_All_POS:
            case RelicEffect.RelicCondition.Contains_Any_POS:
            case RelicEffect.RelicCondition.Contains_No_POS:
                position.y += Y_OFFSET;
                EditorGUI.PropertyField(position, property.FindPropertyRelative("_filterPOS"));
                break;

            case RelicEffect.RelicCondition.Double_Letter:
            case RelicEffect.RelicCondition.Palindrome:
            case RelicEffect.RelicCondition.Alphabetical_Chain:
            case RelicEffect.RelicCondition.Fully_Alphabetized_Word:
            case RelicEffect.RelicCondition.Rev_Alphabetical_Chain:
            case RelicEffect.RelicCondition.Fully_Rev_Alphabetized_Word:
                break;
        }

        position.y += Y_OFFSET;
        EditorGUI.LabelField(position, "How many times this relic's effect should be applied", EditorStyles.boldLabel);
        position.y += Y_OFFSET;
        EditorGUI.LabelField(position, "if the condition is met more than once. 0 = No Limit", EditorStyles.boldLabel);
        position.y += Y_OFFSET;
        EditorGUI.PropertyField(position, property.FindPropertyRelative("_numTimesToApply"));

        position.y += Y_OFFSET;
        EditorGUI.LabelField(position, "If condition is met, chance to trigger, per time the condition is met.", EditorStyles.boldLabel);
        position.y += Y_OFFSET;
        EditorGUI.PropertyField(position, property.FindPropertyRelative("_chanceToTrigger"));

        position.y += Y_OFFSET;
        EditorGUI.PropertyField(position, property.FindPropertyRelative("_valueToModify"));

        position.y += Y_OFFSET;
        EditorGUI.PropertyField(position, property.FindPropertyRelative("_value"));
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
	{
        SerializedProperty condition = property.FindPropertyRelative("_condition");
        RelicEffect.RelicCondition conditionKind = (RelicEffect.RelicCondition) condition.enumValueIndex;

        int lines = 0;

        switch (conditionKind)
        {
            case RelicEffect.RelicCondition.Always_Active:
                lines = 1;
                break;

            case RelicEffect.RelicCondition.Contains_All_Letters:
            case RelicEffect.RelicCondition.Contains_Any_Letters:
            case RelicEffect.RelicCondition.Contains_Unique_Letters:
            case RelicEffect.RelicCondition.Contains_Sequence:
            case RelicEffect.RelicCondition.Does_Not_Contain_Letter:
            case RelicEffect.RelicCondition.Middle_Letter:
                lines = 2;
                break;

            case RelicEffect.RelicCondition.Contains_All_POS:
            case RelicEffect.RelicCondition.Contains_Any_POS:
            case RelicEffect.RelicCondition.Contains_No_POS:
                lines = 2;
                break;

            case RelicEffect.RelicCondition.Double_Letter:
            case RelicEffect.RelicCondition.Palindrome:
            case RelicEffect.RelicCondition.Alphabetical_Chain:
            case RelicEffect.RelicCondition.Fully_Alphabetized_Word:
            case RelicEffect.RelicCondition.Rev_Alphabetical_Chain:
                lines = 1;
                break;

            case RelicEffect.RelicCondition.Fully_Rev_Alphabetized_Word:

                lines = 1;
                break;
        }

		lines += 9; // boilerplate

		return lines * Y_OFFSET + EditorGUIUtility.standardVerticalSpacing;
	}
}