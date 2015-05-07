//
// Copyright (c) 2012 Krueger Systems, Inc.
// Copyright (c) 2013 Øystein Krog (oystein.krog@gmail.com)
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.
//

using JetBrains.Annotations;
using SQLite.Net.Interop;

namespace SQLite.Net
{
    /// <summary>
    ///     Represents a parsed connection string.
    /// </summary>
    public class SQLiteConnectionString
    {
        [PublicAPI]
        public SQLiteConnectionString(string databasePath, bool storeDateTimeAsTicks,
            IBlobSerializer serializer = null,
            IContractResolver resolver = null,
            SQLiteOpenFlags? openFlags = null)
        {
            ConnectionString = databasePath;
            StoreDateTimeAsTicks = storeDateTimeAsTicks;

            DatabasePath = databasePath;
            Serializer = serializer;
            Resolver = resolver ?? ContractResolver.Current;
            OpenFlags = openFlags ?? SQLiteOpenFlags.ReadWrite | SQLiteOpenFlags.Create;
        }

        [PublicAPI]
        public string ConnectionString { get; private set; }

        [PublicAPI]
        public string DatabasePath { get; private set; }

        [PublicAPI]
        public bool StoreDateTimeAsTicks { get; private set; }

        [PublicAPI]
        public IBlobSerializer Serializer { get; private set; }

        [PublicAPI]
        public IContractResolver Resolver { get; private set; }

        [PublicAPI]
        public SQLiteOpenFlags OpenFlags {get; private set; } 
    }
}