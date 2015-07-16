using SQLite;
using System;
using System.Text;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;
using Windows.Storage.Streams;

namespace SQLite
{
    public class SQLiteWinRTCryptoProvider : IEncryptionProvider
    {
        CryptographicKey _CryptoKey;

        public SQLiteWinRTCryptoProvider(string key)
        {
            _CryptoKey = CreateKey(key);
        }
        public string EncryptString(string value)
        {
            if (value == String.Empty)
            {
                return value;
            }
            else
            {
                var valueBuffer = CryptographicBuffer.ConvertStringToBinary(value, BinaryStringEncoding.Utf8);
                var encryptedBuffer = CryptographicEngine.Encrypt(_CryptoKey, valueBuffer, null);
                return CryptographicBuffer.EncodeToBase64String(encryptedBuffer);
            }
        }

        public string DecryptString(string value)
        {
            if (value == String.Empty)
            {
                return value;
            }
            else
            {
                return CryptographicBuffer.ConvertBinaryToString(BinaryStringEncoding.Utf8,
                    CryptographicEngine.Decrypt(_CryptoKey,
                        CryptographicBuffer.DecodeFromBase64String(value), null));

            }
        }


        private CryptographicKey CreateKey(string key)
        {
            var keyBuffer = CryptographicBuffer.ConvertStringToBinary(key, BinaryStringEncoding.Utf8);
            var keyHash = KeyDerivationAlgorithmProvider
                .OpenAlgorithm(KeyDerivationAlgorithmNames.Pbkdf2Sha256)
                .CreateKey(keyBuffer);
            
            var keyMaterial = CryptographicEngine.DeriveKeyMaterial(keyHash, KeyDerivationParameters.BuildForPbkdf2(keyBuffer, 10), 16);

            return SymmetricKeyAlgorithmProvider.OpenAlgorithm(SymmetricAlgorithmNames.AesCbcPkcs7).CreateSymmetricKey(keyMaterial);
        }
    }
}