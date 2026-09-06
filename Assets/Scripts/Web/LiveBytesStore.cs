public class LiveBytesStore
{
    private readonly object _lock = new object();
    private byte[] _data;
    private int _version;

    public void Set(byte[] data)
    {
        lock (_lock)
        {
            _data = data;
            _version++;
        }
    }

    public bool TryGetNewer(ref int knownVersion, out byte[] data)
    {
        lock (_lock)
        {
            if (_version == knownVersion || _data == null)
            {
                data = null;
                return false;
            }

            knownVersion = _version;
            data = _data;
            return true;
        }
    }
}
