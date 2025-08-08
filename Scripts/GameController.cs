using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public InputSO input;
    public static GameController controller { set; get; }
    public int currentController = 0;
    public List<Controller> controllers;

    public bool singleChar = true;
    public bool blockTestUI = true;

    void Awake()
    {
        if (controller == null)
            controller = this;
        else
            Destroy(this.gameObject);
    }


    private bool mainCharacter;

    void Start()
    {
        input.PushInput(controllers[0].Inputs);
        mainCharacter = true;

        // set camera target
        CameraController.controller.SetTarget(controllers[0].transform);
    }

    public void SwitchChars()
    {
        if (singleChar)
            return;
        mainCharacter = !mainCharacter;

        // trigger cancel so that the controller input is stopped
        // otherwise they will simply continue having the last input
        input._inputTargets.StackTop._Value.cancel();

        if (mainCharacter) input.PopInput();
        else input.PushInput(controllers[1].Inputs);

        //switch camera target
        CameraController.controller.SetTarget(mainCharacter ? controllers[0].transform : controllers[1].transform);
    }


    public TestUI ui;
    public void ToggleUI()
    {
        if (blockTestUI)
            return;
        ui.Toggle();
    }
}


