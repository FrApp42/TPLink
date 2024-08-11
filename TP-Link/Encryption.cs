using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace FrApp42.TPLink
{

    internal class AES
    {
        private string _key;
        private string _iv;

        public void GenKey()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] keyBytes = new byte[8];
                rng.GetBytes(keyBytes);

                byte[] ivBytes = new byte[8];
                rng.GetBytes(ivBytes);

                _key = BitConverter.ToString(keyBytes).Replace("-", "").ToLower();
                _iv = BitConverter.ToString(ivBytes).Replace("-", "").ToLower();
            }
        }

        public void SetKey(string key, string iv)
        {
            _key = key;
            _iv = iv;
        }

        public string GetKeyString()
        {
            return $"key={_key}&iv={_iv}";
        }

        public string Encrypt(string plainText)
        {
            using (AesCryptoServiceProvider aes = new AesCryptoServiceProvider())
            {
                aes.Key = Encoding.UTF8.GetBytes(_key);
                aes.IV = Encoding.UTF8.GetBytes(_iv);
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                byte[] plainBytes = Encoding.UTF8.GetBytes(plainText);
                byte[] encryptedBytes = encryptor.TransformFinalBlock(plainBytes, 0, plainBytes.Length);

                string result = Convert.ToBase64String(encryptedBytes);
                return Convert.ToBase64String(encryptedBytes);
            }
        }

        public string Decrypt(string cipherText)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(_key);
                aes.IV = Encoding.UTF8.GetBytes(_iv);
                //aes.KeySize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                var decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                using (var msDecrypt = new MemoryStream(Convert.FromBase64String(cipherText)))
                {
                    using (var csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (var srDecrypt = new StreamReader(csDecrypt))
                        {
                            return srDecrypt.ReadToEnd();
                        }
                    }
                }
            }
        }
    }

    internal class RSAKey
    {
        private BigInteger _n;
        private int _e = 0;

        private BigInteger NoPadding(string message, int nLength)
        {
            if (nLength < message.Length)
            {
                throw new Exception("Message too long for RSA");
            }
            var byteArray = new byte[nLength];
            int i = 0, j = 0;

            while (i < message.Length && j < nLength)
            {
                int charCode = (int)message[i++];
                if (charCode < 128)
                {
                    byteArray[j++] = (byte)charCode;
                }
                else if (charCode > 127 && charCode < 2048)
                {
                    byteArray[j++] = (byte)((charCode & 63) | 128);
                    byteArray[j++] = (byte)((charCode >> 6) | 192);
                    //byteArray[j++] = (byte)((charCode & 0x3F) | 0x80);
                    //byteArray[j++] = (byte)((charCode >> 6) | 0xC0);
                }
                else
                {
                    byteArray[j++] = (byte)((charCode & 63) | 128);
                    byteArray[j++] = (byte)(((charCode >> 6) & 63) | 128);
                    byteArray[j++] = (byte)((charCode >> 12) | 224);
                    //byteArray[j++] = (byte)((charCode & 0x3F) | 0x80);
                    //byteArray[j++] = (byte)(((charCode >> 6) & 0x3F) | 0x80);
                    //byteArray[j++] = (byte)((charCode >> 12) | 0xE0);
                }
            }
            while (j < nLength)
            {
                byteArray[j++] = 0;
            }
            BigInteger bg = new BigInteger(byteArray, isUnsigned: true, isBigEndian: true);
            string bgStr = bg.ToString();

            return new BigInteger(byteArray, isUnsigned: true, isBigEndian: true);
        }

        private byte[] ConvertIntArrayToByteArray(int[] intArray)
        {
            // Calculer la taille nécessaire pour le tableau de bytes
            int byteCount = intArray.Length * sizeof(int);
            byte[] byteArray = new byte[byteCount];

            // Copier les valeurs des entiers dans le tableau de bytes
            Buffer.BlockCopy(intArray, 0, byteArray, 0, byteCount);

            return byteArray;
        }

        private BigInteger DoPublic(BigInteger message)
        {
            return BigInteger.ModPow(message, _e, _n);
        }

        public void SetPublic(string N, string E)
        {
            if (!string.IsNullOrEmpty(N) && !string.IsNullOrEmpty(E))
            {
                // Unsigned BigInteger
                _n = BigInteger.Parse("00" + N, System.Globalization.NumberStyles.HexNumber);
                _e = int.Parse(E, System.Globalization.NumberStyles.HexNumber);
            }
            else
            {
                throw new Exception("Invalid RSA public key");
            }
        }

        public string Encrypt(string text)
        {
            BigInteger message = NoPadding(text, ((int)_n.GetBitLength() + 7) >> 3);
            if (message == null)
            {
                return null;
            }
            BigInteger cipher = DoPublic(message);
            if (cipher == null)
            {
                return null;
            }

            string hexString = cipher.ToString("X");
            if (hexString.Substring(0, 1) == "0")
            {
                hexString = hexString.Substring(1);
            }
            hexString = hexString.ToLower();
            return (hexString.Length & 1) == 0 ? hexString : "0" + hexString;
        }
    }

    internal class RSA
    {
        private RSAKey rsaKey;

        public void SetKey(string n, string e)
        {
            rsaKey = new RSAKey();
            rsaKey.SetPublic(n, e);
        }

        private string CalculateRsaChunk(RSAKey rsaKey, string val, int strEnlen)
        {
            string result = rsaKey.Encrypt(val);
            if (result.Length != strEnlen)
            {
                int l = Math.Abs(strEnlen - result.Length);
                for (int i = 0; i < l; i++)
                {
                    result = '0' + result;
                }
            }
            return result;
        }

        private string CalculateRsaChunk(string val, int strEnlen)
        {
            string result = rsaKey.Encrypt(val);
            if (result.Length != strEnlen)
            {
                decimal l = Math.Abs(strEnlen - result.Length);
                for (int i = 0; i < l; i++)
                {
                    result = "0" + result;
                }
            }
            return result;
        }

        public string Encrypt(string plainText)
        {
            const int RSA_BIT = 512;
            const int STR_EN_LEN = RSA_BIT / 4;
            const int STR_DE_LEN = RSA_BIT / 8;
            int step = STR_DE_LEN;

            int start = 0;
            int end = step;
            string buffer = string.Empty;

            while (start < plainText.Length)
            {
                end = end < plainText.Length ? end : plainText.Length;
                buffer += CalculateRsaChunk(plainText.Substring(start, end - start), STR_EN_LEN);
                start += step;
                end += step;
            }

            return buffer;
        }
    }

    internal class Encryption
    {
        private AES aes;
        private RSA rsa;

        private int seq;
        private string aesKeyString;
        private string hash;

        public Encryption()
        {
            this.aes = new AES();
            this.rsa = new RSA();
        }

        public Encryption SetSeq(string seq)
        {
            this.seq = int.Parse(seq);
            return this;
        }

        public Encryption GenAESKey()
        {
            this.aes.GenKey();
            this.aesKeyString = this.aes.GetKeyString();
            return this;
        }

        public string GetAESKeyString()
        {
            return this.aes.GetKeyString();
        }

        public Encryption SetAESKey(string key, string iv)
        {
            this.aes.SetKey(key, iv);
            this.aesKeyString = this.aes.GetKeyString();
            return this;
        }

        public Encryption SetRSAKey(string n, string e)
        {
            this.rsa.SetKey(n, e);
            return this;
        }

        public string GetSignature(string seq, bool isLogin)
        {
            string s = isLogin ? this.aesKeyString + "&" : "";
            if (string.IsNullOrEmpty(this.hash))
            {
                this.hash = "undefined";
            }
            s += "h=" + this.hash + "&s=" + seq ?? this.seq.ToString();
            return this.rsa.Encrypt(s);
        }

        public (string data, string sign) AESEncrypt(string data, bool isLogin = false)
        {
            string encrypted = this.aes.Encrypt(data);
            return (encrypted, GetSignature((this.seq + encrypted.Length).ToString(), isLogin));
        }

        public string AESDecrypt(string data)
        {
            return this.aes.Decrypt(data);
        }
    }
}
