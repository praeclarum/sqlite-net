//
//  Copyright 2013, Sami M. Kallio
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.
//

using System;
using JetBrains.Annotations;

namespace SQLite.Net
{
    /// <summary>
    ///     The TextSerializer interface.
    /// </summary>
    public interface IBlobSerializer
    {
        /// <summary>
        ///     Serializes object to a byte buffer
        /// </summary>
        /// <param name="obj">Object to serialize</param>
        /// <returns>Serialized blob of the object</returns>
        [PublicAPI]
        byte[] Serialize<T>(T obj);

        /// <summary>
        ///     Deserializes byte buffer into an object
        /// </summary>
        /// <param name="data">Serialized object</param>
        /// <param name="type">Type of object</param>
        /// <returns>Deserialized object</returns>
        [PublicAPI]
        object Deserialize(byte[] data, Type type);

        [PublicAPI]
        bool CanDeserialize(Type type);
    }
}