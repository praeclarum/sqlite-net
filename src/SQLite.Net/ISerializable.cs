using System;

namespace SQLite.Net
{
    public interface ISerializable<T>
    {
        T Serialize();
    }
}

