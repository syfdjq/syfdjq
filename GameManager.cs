using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance {  get; private set; }


    GameState state;

    public enum GameState
    {
        Idle,
        Enter,
        Playing,
        GameOver,
        GameWin
    }


    private void Awake()
    {
        Instance = this;
        state = GameState.Idle;
    }

    private void Update()
    {
        switch(state)
        {
            case GameState.Idle:
                break;
            case GameState.Enter:
                //这里在播放动画
                break;
            case GameState.Playing:
                GameTimeManager.instance.ChangeState(GameTimeManager.State.Playing);//改变时间状态
                GameApp.ViewManager.Open(ViewType.InfoView, GameApp.GameDataManager.taskDic);
                break;
            case GameState.GameOver:
                GameApp.ViewManager.Open(ViewType.GameOverView);
                break;
            case GameState.GameWin:
                GameApp.ViewManager.Open(ViewType.GameWinView);
                break;
        }    
    }

    public void ChangeState(GameState state)
    {
        this.state = state;
    }

    public bool IsPlaying()
    {
        return state == GameState.Playing;
    }


}
