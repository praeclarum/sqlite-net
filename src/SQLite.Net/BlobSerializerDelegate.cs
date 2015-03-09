using System;
using JetBrains.Annotations;

namespace SQLite.Net
{
    public class BlobSerializerDelegate : IBlobSerializer
    {
        public delegate bool CanSerializeDelegate(Type type);

        public delegate object DeserializeDelegate(byte[] data, Type type);

        public delegate byte[] SerializeDelegate(object obj);

        private readonly CanSerializeDelegate _canDeserializeDelegate;
        private readonly DeserializeDelegate _deserializeDelegate;
        private readonly SerializeDelegate _serializeDelegate;

        [PublicAPI]
        public BlobSerializerDelegate(SerializeDelegate serializeDelegate,
            DeserializeDelegate deserializeDelegate,
            CanSerializeDelegate canDeserializeDelegate)
        {
            _serializeDelegate = serializeDelegate;
            _deserializeDelegate = deserializeDelegate;
            _canDeserializeDelegate = canDeserializeDelegate;
        }

        #region IBlobSerializer implementation

        [PublicAPI]
        public byte[] Serialize<T>(T obj)
        {
            return _serializeDelegate(obj);
        }

        [PublicAPI]
        public object Deserialize(byte[] data, Type type)
        {
            return _deserializeDelegate(data, type);
        }

        [PublicAPI]
        public bool CanDeserialize(Type type)
        {
            return _canDeserializeDelegate(type);
        }

        #endregion
    }
}