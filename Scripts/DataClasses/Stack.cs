using UnityEngine;
public class Stack<T>
{
    private StackItem<T> stackTop;
    private int stackSize;

    public Stack()
    {
        stackTop = null;
        stackSize = 0;
    }


    public virtual void Push(T _stackElement)
    {
        stackSize++;
        StackItem<T> newItem = new StackItem<T>(stackTop, _stackElement);
        stackTop = newItem;
    }

    public virtual StackItem<T> Pop()
    {
        if (stackTop == null)
            return null;
        StackItem<T> poppedItem = StackTop;
        stackTop = StackTop._Previous;
        stackSize--;
        return poppedItem;
    }

    public void Clear()
    {
        while (stackSize > 0)
            Pop();
    }

    public int StackSize => stackSize;
    public StackItem<T> StackTop { get => stackTop; }

}

public class StackItem<T>
{
    private StackItem<T> previous;
    private T _value;

    public StackItem()
    {
        previous = default(StackItem<T>);
        _value = default(T);
    }

    public StackItem(StackItem<T> _previous, T __value)
    {
        previous = _previous;
        _value = __value;
    }

    public StackItem<T> _Previous
    {
        get { return previous; }
        set { previous = value; }
    }

    public T _Value
    {
        set { this._value = value; }
        get { return _value; }
    }
}