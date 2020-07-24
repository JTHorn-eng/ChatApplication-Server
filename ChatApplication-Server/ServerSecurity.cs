using ChatServer;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace ChatApplication_Server
{
    class ServerSecurity
    {


        private byte[] unencryptedSymmetricKey = null;
        private UnicodeEncoding ByteConverter = new UnicodeEncoding();
        private RSACryptoServiceProvider RSA = new RSACryptoServiceProvider();
        //Retrieve AES symmetric key from database
        public void decryptAESKey(ServerDatabase sd, string username) 
        {
            string info = sd.RetrieveAESSymmetricKey(username);
            Console.WriteLine("Retrieved database info: " + info);
            unencryptedSymmetricKey = RSADecryption(ByteConverter.GetBytes(info.Split(":")[2]), RSA.ExportParameters(false), false);
        }

        //AES Encrypt
        public byte[] EncryptStringToBytes_Aes(string plainText, byte[] key, byte[] IV)
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
        public string DecryptBytesToString_Aes(byte[] cipherText, byte[] key, byte[] IV)
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
        public byte[] RSAEncryption(byte[] Data, RSAParameters RSAKey, bool DoOAEPPadding)
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
        public byte[] RSADecryption(byte[] Data, RSAParameters RSAKey, bool DoOAEPPadding)
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

       
        public byte[] generateDigitalSignature(string plainText)
        {
            //hash message
            HashAlgorithm sha = new SHA1CryptoServiceProvider();
            return sha.ComputeHash(Encoding.UTF8.GetBytes(plainText));
        }










    }
}
