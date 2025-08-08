using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestUI : MonoBehaviour
{
    public InputSO input;
    public GameObject panel;
    protected Inputs uiInputs;

    void Start()
    {
        uiInputs = new();
        uiInputs.click2 = CloseUI;
    }

    public void Toggle()
    {
        if (panel.activeSelf) CloseUI();
        else OpenUI();
    }

    private void OpenUI()
    {
        input._inputTargets.StackTop._Value.cancel();
        input.PushInput(uiInputs);
        panel.SetActive(true);
    }

    private void CloseUI()
    {
        input.PopInput();
        panel.SetActive(false);
    }
}
