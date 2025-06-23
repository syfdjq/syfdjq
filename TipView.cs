using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class TipView : BaseView
{
    public override void Open(params object[] args)
    {
        base.Open(args);
        Find<Text>("bg/txt").text = args[0].ToString();
        float duration = 0.75f;
        if(args[1] != null)
        {
            duration = (float)args[1];
        }
        DG.Tweening.Sequence seq = DOTween.Sequence();
        seq.Append(Find("bg").transform.DOScaleY(1, 0.15f)).SetEase(Ease.OutBack);
        seq.AppendInterval(duration);
        seq.Append(Find("bg").transform.DOScaleY(0, 0.15f)).SetEase(Ease.Linear);
        seq.AppendCallback(delegate ()
        {
            GameApp.ViewManager.Close(ViewId);
        });
    }

    public override void UpDateView(params object[] args)
    {
        base.UpDateView(args);
        Find<Text>("bg/txt").text = args[0].ToString();
        float duration = 0.75f;
        if (args[1] != null)
        {
            duration = (float)args[1];
        }
        DG.Tweening.Sequence seq = DOTween.Sequence();
        seq.Append(Find("bg").transform.DOScaleY(1, 0.15f)).SetEase(Ease.OutBack);
        seq.AppendInterval(duration);
        seq.Append(Find("bg").transform.DOScaleY(0, 0.15f)).SetEase(Ease.Linear);
        seq.AppendCallback(delegate ()
        {
            GameApp.ViewManager.Close(ViewId);
        });
    }
}
