using NUnit.Framework.Constraints;
using System;
using UnityEngine;
using UnityEngine.InputSystem.Controls;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1)]
public class PauseManager : MonoBehaviour
{
    public enum PauseMenuMode
    {
        Playing,
        Paused,
        Confirm_Quit,
    };

    [SerializeField]
    private PauseMenuMode _pauseMode = PauseMenuMode.Playing;
#if UNITY_EDITOR
    private PauseMenuMode _prevPauseMode = PauseMenuMode.Playing;
#endif
    public PauseMenuMode PauseMode { get => _pauseMode; set => SetPauseState(value); }

    private GameObject _pauseCanvas;
    private GameObject _pauseRoot;
    private GameObject _quitRoot;

    public static PauseManager INSTANCE;



    void Awake()
    {
        if (INSTANCE != null && INSTANCE != this)
        {
            // this script lives on the shop canvas object, we don't want to destroy the whole gameobject
            Destroy(this);
            return;
        }

        INSTANCE = this;
    }

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        _pauseCanvas = transform.GetChild(0).gameObject;
        _pauseRoot = _pauseCanvas.transform.GetChild(0).GetChild(0).gameObject;
        _quitRoot = _pauseCanvas.transform.GetChild(0).GetChild(1).gameObject;
    }

    // Update is called once per frame
    void Update()
    {
        UpdatePauseState();
    }

    private void UpdatePauseState()
    {
#if UNITY_EDITOR
        if (_prevPauseMode != _pauseMode)
        {
            SetPauseState(_pauseMode, true);
        }
#endif
        bool receivedEscapeInput = Input.GetKeyDown(KeyCode.Escape);

        if (!receivedEscapeInput)
            return;

        switch (_pauseMode)
        {
            case PauseMenuMode.Playing:
                bool itemInUse = TileSelector.INSTANCE.Mode == TileSelector.SelectionMode.Item_Use;

                if (!itemInUse)
                {
                    SetPauseState(PauseMenuMode.Paused);
                }
                break;

            case PauseMenuMode.Paused:
                SetPauseState(PauseMenuMode.Playing);
                break;

            case PauseMenuMode.Confirm_Quit:
                SetPauseState(PauseMenuMode.Paused);
                break;
        }

#if UNITY_EDITOR
        _prevPauseMode = _pauseMode;
#endif
    }

    public void SetPauseState(PauseMenuMode pauseMode, bool force = false)
    {
        if (_pauseMode == pauseMode && !force)
            return;

        switch (_pauseMode)
        {
            case PauseMenuMode.Playing:
                Time.timeScale = 0.0f;
                _pauseCanvas.SetActive(true);
                break;

            case PauseMenuMode.Paused:
                _pauseRoot.SetActive(false);
                break;

            case PauseMenuMode.Confirm_Quit:
                _quitRoot.SetActive(false);
                break;
        }

        _pauseMode = pauseMode;

        switch (_pauseMode)
        {
            case PauseMenuMode.Playing:
                Time.timeScale = 1.0f;
                _pauseCanvas.SetActive(false);
                TileSelector.INSTANCE.ForceUpdateHighlightedTile();
                break;

            case PauseMenuMode.Paused:
                _pauseRoot.SetActive(true);
                break;

            case PauseMenuMode.Confirm_Quit:
                _quitRoot.SetActive(true);
                break;
        }
    }

    public void SetPauseStatePlaying() => SetPauseState(PauseMenuMode.Playing, force: true);
    public void SetPauseStatePaused() => SetPauseState(PauseMenuMode.Paused, force: true);
    public void SetPauseStateConfirmQuit() => SetPauseState(PauseMenuMode.Confirm_Quit, force: true);
    public void QuitToTitle() => SceneManager.LoadScene("TitleScene");
}
