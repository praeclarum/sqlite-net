using JetBrains.Annotations;

namespace SQLite.Net
{
    public interface ISerializable<T>
    {
        [PublicAPI]
        T Serialize();
    }
}