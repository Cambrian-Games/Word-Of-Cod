using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClickToLoadScene : MonoBehaviour
{
	public SceneAsset _targetScene;

	public void LoadScene()
	{
		SceneManager.LoadScene(_targetScene.name);
	}
}
