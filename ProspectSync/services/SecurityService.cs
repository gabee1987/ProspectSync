using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ProspectSync.services
{
    public class SecurityService
    {
        public byte[] EncryptStringToBytes_Aes( string plainText, string key )
        {
            byte[] encrypted;

            using ( Aes aesAlg = Aes.Create() )
            {
                aesAlg.Key = Encoding.UTF8.GetBytes( key.PadRight( 32 ) ); // AES key size is 256 bits or 32 bytes
                aesAlg.IV = Encoding.UTF8.GetBytes( key.PadRight( 16 ) );  // AES block size is 128 bits or 16 bytes

                ICryptoTransform encryptor = aesAlg.CreateEncryptor( aesAlg.Key, aesAlg.IV );

                using ( MemoryStream msEncrypt = new MemoryStream() )
                {
                    using ( CryptoStream csEncrypt = new CryptoStream( msEncrypt, encryptor, CryptoStreamMode.Write ) )
                    using ( StreamWriter swEncrypt = new StreamWriter( csEncrypt ) )
                    {
                        swEncrypt.Write( plainText );
                    }

                    encrypted = msEncrypt.ToArray();
                }
            }

            return encrypted;
        }

        public string DecryptStringFromBytes_Aes( byte[] cipherText, string key )
        {
            string plaintext;

            try
            {
                using ( Aes aesAlg = Aes.Create() )
                {
                    aesAlg.Key = Encoding.UTF8.GetBytes( key.PadRight( 32 ) );
                    aesAlg.IV = Encoding.UTF8.GetBytes( key.PadRight( 16 ) );

                    ICryptoTransform decryptor = aesAlg.CreateDecryptor( aesAlg.Key, aesAlg.IV );

                    using ( MemoryStream msDecrypt = new MemoryStream( cipherText ) )
                    {
                        using ( CryptoStream csDecrypt = new CryptoStream( msDecrypt, decryptor, CryptoStreamMode.Read ) )
                        using ( StreamReader srDecrypt = new StreamReader( csDecrypt ) )
                        {
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
            catch ( CryptographicException )
            {
                throw new Exception( "Invalid password or corrupted data" );
            }
            catch ( Exception )
            {

                throw;
            }
            

            return plaintext;
        }
    }
}