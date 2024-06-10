namespace TPLink
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;

    public class AES
    {
        private byte[] key;
        private byte[] iv;

        public void GenKey()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                key = new byte[16]; // AES-128 uses a 16-byte key
                iv = new byte[16]; // AES-CBC uses a 16-byte IV
                rng.GetBytes(key);
                rng.GetBytes(iv);
            }

            Console.WriteLine($"Key {BitConverter.ToString(key)}");
            Console.WriteLine("Key: " + BitConverter.ToString(key).Replace("-", "").ToLower());
            Console.WriteLine("IV: " + BitConverter.ToString(iv).Replace("-", "").ToLower());
        }

        public void SetKey(string keyHex, string ivHex)
        {
            key = HexStringToByteArray(keyHex);
            iv = HexStringToByteArray(ivHex);
        }

        public string GetKeyString()
        {
            return $"key={BitConverter.ToString(key).Replace("-", "").ToLower()}&iv={BitConverter.ToString(iv).Replace("-", "").ToLower()}";
        }

        public string Encrypt(string plainText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.KeySize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (var ms = new MemoryStream())
                {
                    using (var cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                    {
                        using (var sw = new StreamWriter(cs))
                        {
                            sw.Write(plainText);
                        }
                        return Convert.ToBase64String(ms.ToArray());
                    }
                }
            }
        }

        public string Decrypt(string encrypted)
        {
            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (var ms = new MemoryStream(Convert.FromBase64String(encrypted)))
                {
                    using (var cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (var sr = new StreamReader(cs))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
            }
        }

        private byte[] HexStringToByteArray(string hex)
        {
            int length = hex.Length;
            byte[] bytes = new byte[length / 2];
            for (int i = 0; i < length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }
    }

    public class RSAKey
    {
        private RSAParameters rsaKeyInfo;

        public void SetPublic(string N, string E)
        {
            rsaKeyInfo = new RSAParameters
            {
                Modulus = HexStringToByteArray(N),
                Exponent = HexStringToByteArray(E)
            };
        }

        public byte[] Encrypt(string text)
        {
            byte[] bytesToEncrypt = Encoding.UTF8.GetBytes(text);
            int maxBlockSize = (rsaKeyInfo.Modulus.Length * 8 / 8) - 11; // PKCS#1 v1.5 padding

            using (var rsa = new RSACryptoServiceProvider())
            {
                rsa.ImportParameters(rsaKeyInfo);
                using (var ms = new MemoryStream())
                {
                    for (int i = 0; i < bytesToEncrypt.Length; i += maxBlockSize)
                    {
                        byte[] block = new byte[Math.Min(maxBlockSize, bytesToEncrypt.Length - i)];
                        Array.Copy(bytesToEncrypt, i, block, 0, block.Length);
                        byte[] encryptedBlock = rsa.Encrypt(block, false);
                        ms.Write(encryptedBlock, 0, encryptedBlock.Length);
                    }
                    return ms.ToArray();
                }
            }
        }

        private byte[] HexStringToByteArray(string hex)
        {
            int length = hex.Length;
            byte[] bytes = new byte[length / 2];
            for (int i = 0; i < length; i += 2)
            {
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            }
            return bytes;
        }
    }

    public class RSA
    {
        private RSAKey rsaKey;

        public void SetKey(string n, string e)
        {
            rsaKey = new RSAKey();
            rsaKey.SetPublic(n, e);
        }

        public string Encrypt(string plainText)
        {
            byte[] encryptedBytes = rsaKey.Encrypt(plainText);
        return BitConverter.ToString(encryptedBytes).Replace("-", "");
        }        
    }

    public class Encryption
    {
        private AES aes;
        private RSA rsa;
        private int seq;
        private string aesKeyString;
        private string hash;

        public Encryption()
        {
            aes = new AES();
            rsa = new RSA();
        }

        public Encryption SetSeq(int seq)
        {
            this.seq = seq;
            return this;
        }

        public Encryption GenAESKey()
        {
            aes.GenKey();
            aesKeyString = aes.GetKeyString();

            return this;
        }

        public string GetAESKeyString()
        {
            return aes.GetKeyString();
        }

        public void SetAESKey(string key, string iv)
        {
            aes.SetKey(key.ToLower(), iv.ToLower());
            aesKeyString = aes.GetKeyString();
        }

        public Encryption SetRSAKey(string n, string e)
        {
            rsa.SetKey(n, e);
            return this;
        }

        public string GetSignature(int seq, bool isLogin)
        {
            var s = isLogin ? aesKeyString + '&' : string.Empty;
            s += $"h={hash}&s={seq}";
            return rsa.Encrypt(s);
        }

        public (string data, string sign) AESEncrypt(string data, bool isLogin = false)
        {
            string encrypted = aes.Encrypt(data);
            return (encrypted, GetSignature(seq + encrypted.Length, isLogin));
        }

        public string AESDecrypt(string data)
        {
            return aes.Decrypt(data);
        }
    }

}
