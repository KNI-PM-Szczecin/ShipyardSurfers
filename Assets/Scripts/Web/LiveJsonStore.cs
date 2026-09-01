public class LiveJsonStore
{
    private readonly object _lock = new object();
    private string _json = "{}";
    private int _version;

    public void Set(string json)
    {
        lock (_lock)
        {
            if (json == _json) return;
            _json = json;
            _version++;
        }
    }

    public string Get()
    {
        lock (_lock) return _json;
    }

    public bool TryGetNewer(ref int knownVersion, out string json)
    {
        lock (_lock)
        {
            if (_version == knownVersion)
            {
                json = null;
                return false;
            }
            knownVersion = _version;
            json = _json;
            return true;
        }
    }
}
