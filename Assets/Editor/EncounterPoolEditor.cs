using UnityEditor;

[CustomEditor(typeof(EncounterPool))]
public class EncounterPoolEditor : Editor
{
	public override void OnInspectorGUI()
	{
		serializedObject.Update();

		// cache data for post-edit validation

		SerializedProperty entryList = serializedObject.FindProperty("_entries");
		int entryCount = entryList.arraySize;

		base.OnInspectorGUI();

		// post edit validation

		while (entryCount < entryList.arraySize)
		{
			entryList.GetArrayElementAtIndex(entryCount).boxedValue = new EncounterPool.PoolEntry();
			entryCount++;
		}

		serializedObject.ApplyModifiedProperties();
	}
}