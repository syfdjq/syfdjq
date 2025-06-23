using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Phone : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        GameApp.ViewManager.Open(ViewType.TipView, "发生火灾时保持冷静，迅速拨打119，向接警员报告火灾位置和火灾类型。提供详细的地址信息，如楼层、火灾发生的具体位置以及是否有被困人员等信息。\n", 5f);
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E))
        {
            PhoneUI.Instance.Show();
        }
    }
}
