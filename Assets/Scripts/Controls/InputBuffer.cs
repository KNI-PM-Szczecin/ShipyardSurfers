public sealed class InputBuffer
{
    private float _pressTime = float.NegativeInfinity;

    public void Press(float time) => _pressTime = time;

    public void Clear() => _pressTime = float.NegativeInfinity;

    public bool TryConsume(float now, float window)
    {
        if (now - _pressTime > window) return false;

        Clear();
        return true;
    }
}
