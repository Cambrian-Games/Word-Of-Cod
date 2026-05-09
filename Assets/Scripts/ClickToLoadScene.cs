using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClickToLoadScene : MonoBehaviour
{
	public string _targetScene;

	public void LoadScene()
	{
		SceneManager.LoadScene(_targetScene);
	}
}
