using System;
using System.Collections.Generic;

public class Observable<T>
{
    private T _value;
    private Action<T> _onValueChanged;

    public T Value
    {
        get => _value;
        set
        {
            if (!EqualityComparer<T>.Default.Equals(_value, value))
            {
                _value = value;
                _onValueChanged?.Invoke(_value);
            }
        }
    }

    public Observable(T initialValue = default)
    {
        _value = initialValue;
    }

    public void Subscribe(Action<T> callback, bool invokeImmediately = true)
    {
        _onValueChanged += callback;
        if (invokeImmediately)
        {
            callback?.Invoke(_value);
        }
    }

    public void Unsubscribe(Action<T> callback)
    {
        _onValueChanged -= callback;
    }
}