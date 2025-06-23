using StarterAssets;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class PhoneUI : MonoBehaviour
{
    public static PhoneUI Instance {  get; private set; }
    private StarterAssetsInputs playerInputs;   //按键输入

    public event EventHandler OnPhone;

    [SerializeField] private Button number_1Button;
    [SerializeField] private Button number_2Button;
    [SerializeField] private Button number_3Button;
    [SerializeField] private Button number_4Button;
    [SerializeField] private Button number_5Button;
    [SerializeField] private Button number_6Button;
    [SerializeField] private Button number_7Button;
    [SerializeField] private Button number_8Button;
    [SerializeField] private Button number_9Button;
    [SerializeField] private Button callButton;

    [SerializeField] private Text numberText;

    private void Awake()
    {
        Instance = this;
        number_1Button.onClick.AddListener(() => { PressButton(1); });
        number_2Button.onClick.AddListener(() => { PressButton(2); });
        number_3Button.onClick.AddListener(() => { PressButton(3); });
        number_4Button.onClick.AddListener(() => { PressButton(4); });
        number_5Button.onClick.AddListener(() => { PressButton(5); });
        number_6Button.onClick.AddListener(() => { PressButton(6); });
        number_7Button.onClick.AddListener(() => { PressButton(7); });
        number_8Button.onClick.AddListener(() => { PressButton(8); });
        number_9Button.onClick.AddListener(() => { PressButton(9); });

        callButton.onClick.AddListener(OnCallPhone);



        numberText.text = "";
    }

    private void Start()
    {
        playerInputs = FindObjectOfType<StarterAssetsInputs>();
        Hide();
    }

    private void OnCallPhone()
    {
        if(numberText.text == "119")
        {
            GameApp.ViewManager.Open(ViewType.TipView, "呼救成功",0.75f);
            OnPhone?.Invoke(this, EventArgs.Empty);
            //(移除物品) 更新任务
            Destroy(GameApp.GameDataManager.itemInHand);
            GameApp.TaskManager.CompleteTask();
            Hide();
        }
        else
        {
            GameApp.ViewManager.Open(ViewType.TipView, "119都打不对?",0.75f);
        }
        numberText.text = "";
    }

    private void PressButton(int value)
    {
        numberText.text += value.ToString();
    }

    public void Show()
    {
        gameObject.SetActive(true);
        //关闭鼠标显示和视角锁定
        if (playerInputs != null)
        {
            playerInputs.cursorInputForLook = false;
            playerInputs.cursorLocked = false;
            playerInputs.SetMouse(playerInputs.cursorLocked);
        }
    }
    public void Hide()
    {
        gameObject.SetActive(false);
        if (playerInputs != null)
        {
            playerInputs.cursorInputForLook = true;
            playerInputs.cursorLocked = true;
            playerInputs.SetMouse(playerInputs.cursorLocked);
        }
    }
}
