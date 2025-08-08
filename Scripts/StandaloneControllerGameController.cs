using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StandaloneControllerGameController : MonoBehaviour
{
    public InputSO input;
    public Controller controller;

    void Start()
    {
        input.PushInput(controller.Inputs);
        // set camera target
        CameraController.controller.SetTarget(controller.transform);
    }
}


