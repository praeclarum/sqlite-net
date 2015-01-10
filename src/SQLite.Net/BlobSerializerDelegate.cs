using System;

namespace SQLite.Net
{
    public class BlobSerializerDelegate : IBlobSerializer
    {
        public delegate byte[] SerializeDelegate(object obj);

        public delegate bool CanSerializeDelegate(Type type);

        public delegate object DeserializeDelegate(byte[] data, Type type);

        private readonly SerializeDelegate _serializeDelegate;
        private readonly DeserializeDelegate _deserializeDelegate;
        private readonly CanSerializeDelegate _canDeserializeDelegate;

        public BlobSerializerDelegate(SerializeDelegate serializeDelegate,
            DeserializeDelegate deserializeDelegate,
            CanSerializeDelegate canDeserializeDelegate)
        {
            _serializeDelegate = serializeDelegate;
            _deserializeDelegate = deserializeDelegate;
            _canDeserializeDelegate = canDeserializeDelegate;
        }

        #region IBlobSerializer implementation

        public byte[] Serialize<T>(T obj)
        {
            return _serializeDelegate(obj);
        }

        public object Deserialize(byte[] data, Type type)
        {
            return _deserializeDelegate(data, type);
        }

        public bool CanDeserialize(Type type)
        {
            return _canDeserializeDelegate(type);
        }

        #endregion
    }
}