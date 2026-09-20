using UnityEngine;

public class ExitButton : MonoBehaviour
{
	public void Exit()
	{
		SaveManager.INSTANCE.WriteSaveData();
		Application.Quit();
	}
}
