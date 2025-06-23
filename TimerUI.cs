using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TimerUI : MonoBehaviour
{
    [SerializeField] private Image timeFill;

    private void Start()
    {
        GameTimeManager.instance.OnPlayingTimeChanged += GameTimeManager_OnPlayingTimeChanged;
        Hide();
    }

    private void GameTimeManager_OnPlayingTimeChanged(object sender, System.EventArgs e)
    {
        if (GameTimeManager.instance.IsPlayingState())
        {
            Show();
            timeFill.fillAmount = GameTimeManager.instance.GetPlayingTimeNormalized();
        }
        else
        {
            Hide();
        }
    }

    private void Show()
    {
        gameObject.SetActive(true);
    }

    private void Hide()
    {
        gameObject.SetActive(false);
    }


}
