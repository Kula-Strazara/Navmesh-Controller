using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class InputHandler : MonoBehaviour
{
    public InputSO input;
    public void InputAxis(InputAction.CallbackContext context)
    {
        input.InputAxis(context.ReadValue<Vector2>());
    }

    public void Click1(InputAction.CallbackContext context)
    {
        if (context.performed)
            input.Click1();
    }

    public void Click2(InputAction.CallbackContext context)
    {
        if (context.performed)
            input.Click2();
    }


    public void Context1(InputAction.CallbackContext context)
    {
        if (context.performed)
            input.Context1();
    }


    public void Context1Released(InputAction.CallbackContext context)
    {

        if (context.performed)
            input.Context1Released();
    }
}
