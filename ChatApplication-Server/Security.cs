using System;
using System.Data.SQLite;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ChatServer
{
    public static class Security
    {

        private static byte[] unencryptedSymmetricKey = null;
        private static UnicodeEncoding ByteConverter = new UnicodeEncoding();
        private static RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
        

        //AES Encrypt
        public static byte[] EncryptStringToBytes_Aes(string plainText, byte[] key, byte[] IV)
        {
            if (plainText.Length <= 0 || key.Length <= 0 || IV.Length <= 0)
            {
                //create an AES object
                Aes aesAlg = Aes.Create();
                aesAlg.Key = key;
                aesAlg.IV = IV;

                //create an ecryptor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                //Create the streams used for encryption
                MemoryStream msEncrypt = new MemoryStream();
                CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
                StreamWriter swEncrypt = new StreamWriter(csEncrypt);
                swEncrypt.Write(plainText);

                return msEncrypt.ToArray();
                

            } else
            {
                Console.WriteLine("Could not encrypt plainText");
                return new byte[0];
            }
        }

        //AES Decrypt
        public static string DecryptBytesToString_Aes(byte[] cipherText, byte[] key, byte[] IV)
        {
            string plainText = null;
            if (cipherText.Length <= 0 || key.Length <= 0 || IV.Length <= 0)
            {
                Aes aesAlg = Aes.Create();
                aesAlg.Key = key;
                aesAlg.IV = IV;

                //create an ecryptor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                //Create the streams used for encryption
                MemoryStream msDecrypt = new MemoryStream(cipherText);
                CryptoStream cdDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
                StreamReader srDecrypt = new StreamReader(cdDecrypt);
                plainText = srDecrypt.ReadToEnd();

                return plainText;

            }
            else
            {
                Console.WriteLine("Could not decrypt cipherText");
                return "";
            }
        }



        //RSA Encrypt
        public static byte[] RSAEncryption(byte[] Data, RSAParameters RSAKey, bool DoOAEPPadding)
        {
            try
            {
                byte[] encryptedData;
                RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
                RSA.ImportParameters(RSAKey);
                encryptedData = RSA.Encrypt(Data, DoOAEPPadding);
                return encryptedData;
            }
            catch (CryptographicException e)
            {
                Console.WriteLine("Error encrypting message: " + e.Message);
            } return null;
        }



        //RSA Decrypt
        public static byte[] RSADecryption(byte[] Data, RSAParameters RSAKey, bool DoOAEPPadding)
        {
            try
            {
                byte[] decryptedData;
                RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
                RSA.ImportParameters(RSAKey);
                decryptedData = RSA.Decrypt(Data, DoOAEPPadding);
                return decryptedData;
            }
            catch (CryptographicException e)
            {
                Console.WriteLine("Error encrypting message: " + e.Message);
            }
            return null;
        }

        //RSA create public/private key pair
        //Save key pair in server key container
        public static string GenKey(string containerName)
        {

            //Create crypto service provider parameters
            CspParameters p = new CspParameters();
            p.KeyContainerName = containerName;

            //Create new key container.
            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(p);
            rsa.PersistKeyInCsp = false;

            rsa.Clear();
            CspParameters p2 = new CspParameters();
            p.KeyContainerName = containerName;

            RSACryptoServiceProvider rsa2 = new RSACryptoServiceProvider(p2);
            Console.WriteLine("Generated new RSA key pair, saved in " + containerName);
            return rsa2.ToXmlString(true);
        }
        public static string RetrieveKeyPair(string containerName)
        {
            CspParameters p = new CspParameters();
            p.KeyContainerName = containerName;

            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider(p);
            Console.WriteLine("Retrieved RSA key pair from " + containerName);
            return rsa.ToXmlString(true);
        }

        

        public static byte[] generateDigitalSignature(string plainText)
        {
            //hash message
            HashAlgorithm sha = new SHA1CryptoServiceProvider();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(plainText));
        }
    }
}
