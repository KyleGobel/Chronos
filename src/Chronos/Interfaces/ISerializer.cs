namespace Chronos.Interfaces
{
    public interface ISerializer
    {
        T Deserialize<T>(string s);
        string Serialize<T>(T obj);
        string ParseAsString(object obj);
    }
}