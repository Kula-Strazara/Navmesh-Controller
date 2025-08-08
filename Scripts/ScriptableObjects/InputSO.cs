using UnityEngine;
using System;

[CreateAssetMenu(fileName = "Input", menuName = "CustomSO/Input/InputHandling", order = 1)]
public class InputSO : ScriptableObject
{
    // stack top -> current target
    public Stack<Inputs> _inputTargets = new Stack<Inputs>();

    // push and pop the input stack
    public void PushInput(Inputs newInput) => _inputTargets.Push(newInput);
    public void PopInput() => _inputTargets.Pop();

    // clear stack
    public void Clear() => _inputTargets.Clear();

    // propagate input to current input target(stack top)
    public void InputAxis(Vector2 axis)
    {
        if (_inputTargets.StackTop != null)
            _inputTargets.StackTop._Value.inputAxis(axis);
    }

    public void Click1()
    {
        // if (_inputTargets.StackTop != null)
        //     _inputTargets.StackTop._Value.click1();

        // stop character switches while UI is open
        return;
        if (!GameController.controller.ui.panel.activeSelf)
            GameController.controller.SwitchChars();
    }

    public void Click2()
    {
        // if (_inputTargets.StackTop != null)
        //     _inputTargets.StackTop._Value.click2();

        return;
        GameController.controller.ToggleUI();
    }

    public void Context1()
    {
        if (_inputTargets.StackTop != null)
            _inputTargets.StackTop._Value.context1();
    }

    public void Context1Released()
    {
        _inputTargets.StackTop._Value.context1Released();
    }
}

public class Inputs
{
    public Action<Vector2> inputAxis = delegate { };
    public Action cancel = delegate { };
    public Action click1 = delegate { };
    public Action click2 = delegate { };

    public Action context1 = delegate { };
    public Action context1Released = delegate { };

    // empty constructor
    public Inputs() { }

    // for copy and clone
    protected Inputs(Inputs set)
    {
        this.inputAxis = set.inputAxis;
        this.cancel = set.cancel;
        this.click1 = set.click1;
        this.click2 = set.click2;
        this.context1 = set.context1;
        this.context1Released = set.context1Released;
    }

    public Inputs Clone() => new Inputs(this);
    public static Inputs Copy(Inputs setToCopy) => new Inputs(setToCopy);
}

