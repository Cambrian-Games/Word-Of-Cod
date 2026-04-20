using UnityEditor;

[CustomEditor(typeof(Enemy))]
public class EnemyEditor : Editor
{
	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		// cache data for post-edit validation

		SerializedProperty ruleList = serializedObject.FindProperty("_rules");
		int ruleCount = ruleList.arraySize;

		SerializedProperty interruptList = serializedObject.FindProperty("_interruptRules");
		int interruptCount = interruptList.arraySize;

		base.OnInspectorGUI();

		// post edit validation

		while (ruleCount < ruleList.arraySize)
		{
			ruleList.GetArrayElementAtIndex(ruleCount).boxedValue = new AttackRule();
			ruleCount++;
		}

		while (interruptCount < interruptList.arraySize)
		{
			interruptList.GetArrayElementAtIndex(interruptCount).boxedValue = new AttackRule();
			interruptCount++;
		}

		serializedObject.ApplyModifiedProperties();
	}
}