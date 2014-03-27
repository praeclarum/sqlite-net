using System;

namespace SQLite.Net
{
    public class BlobSerializerDelegate : IBlobSerializer
    {
        public delegate byte[] SerializeDelegate(object obj);
        public delegate bool CanSerializeDelegate(Type type);
        public delegate object DeserializeDelegate(byte[] data, Type type);

        private readonly SerializeDelegate serializeDelegate;
        private readonly DeserializeDelegate deserializeDelegate;
        private readonly CanSerializeDelegate canDeserializeDelegate;

        public BlobSerializerDelegate (SerializeDelegate serializeDelegate, 
            DeserializeDelegate deserializeDelegate,
            CanSerializeDelegate canDeserializeDelegate)
        {
            this.serializeDelegate = serializeDelegate;
            this.deserializeDelegate = deserializeDelegate;
            this.canDeserializeDelegate = canDeserializeDelegate;
        }

        #region IBlobSerializer implementation

        public byte[] Serialize<T>(T obj)
        {
            return this.serializeDelegate (obj);
        }

        public object Deserialize(byte[] data, Type type)
        {
            return this.deserializeDelegate (data, type);
        }

        public bool CanDeserialize(Type type)
        {
            return this.canDeserializeDelegate (type);
        }

        #endregion
    }
}

