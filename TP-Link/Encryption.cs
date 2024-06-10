using System;
using System.Numerics;
using System.Security.Cryptography;
using System.Text;

namespace TPLink
{
    using System;
    using System.Numerics;
    using System.Security.Cryptography;
    using System.Text;

    internal class AES
    {
        private string key;
        private string iv;

        public void GenKey()
        {
            //using (var rng = new RNGCryptoServiceProvider())
            //{
            //    byte[] key = new byte[8];
            //    byte[] iv = new byte[8];
            //    rng.GetBytes(key);
            //    rng.GetBytes(iv);
            //    this.key = BitConverter.ToString(key).Replace("-", "").ToLower();
            //    this.iv = BitConverter.ToString(iv).Replace("-", "").ToLower();
            //}
            //key = "be50a9322231102d";
            //iv = "299c91800a685c10";
            //byte[] vKey = RandomNumberGenerator.GetBytes(8);
            //byte[] vIV = RandomNumberGenerator.GetBytes(8);

            //key = BitConverter.ToString(vKey).Replace("-", "").ToLower();
            //iv = BitConverter.ToString(vIV).Replace("-", "").ToLower();

            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] keyBytes = new byte[8];
                rng.GetBytes(keyBytes);
                key = BitConverter.ToString(keyBytes).Replace("-", "").ToLower();

                byte[] ivBytes = new byte[8];
                rng.GetBytes(ivBytes);
                iv = BitConverter.ToString(ivBytes).Replace("-", "").ToLower();
            }
        }

        public void SetKey(string key, string iv)
        {
            this.key = key;
            this.iv = iv;
        }

        public string GetKeyString()
        {
            return $"key={this.key}&iv={this.iv}";
        }

        public string Encrypt(string plainText)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(this.key);
                aes.IV = Encoding.UTF8.GetBytes(this.iv);
                //aes.KeySize = 128;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                var encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                using (var msEncrypt = new MemoryStream())
                {
                    using (var csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (var swEncrypt = new StreamWriter(csEncrypt))
                        {
                            swEncrypt.Write(plainText);
                        }
                        return Convert.ToBase64String(msEncrypt.ToArray());
                    }
                }
            }
        }

        public string Decrypt(string cipherText)
        {
            using (var aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(this.key);
                aes.IV = Encoding.UTF8.GetBytes(this.iv);
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
        public BigInteger N { get; private set; } 
        public int E { get; private set; } = 0;

        public BigInteger NoPadding(string message, int nLength)
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
                    //byteArray[j++] = (byte)((charCode & 63) | 128);
                    //byteArray[j++] = (byte)((charCode >> 6) | 192);
                    byteArray[j++] = (byte)((charCode & 0x3F) | 0x80);
                    byteArray[j++] = (byte)((charCode >> 6) | 0xC0);
                }
                else
                {
                    //byteArray[j++] = (byte)((charCode & 63) | 128);
                    //byteArray[j++] = (byte)(((charCode >> 6) & 63) | 128);
                    //byteArray[j++] = (byte)((charCode >> 12) | 224);
                    byteArray[j++] = (byte)((charCode & 0x3F) | 0x80);
                    byteArray[j++] = (byte)(((charCode >> 6) & 0x3F) | 0x80);
                    byteArray[j++] = (byte)((charCode >> 12) | 0xE0);
                }
            }
            while (j < nLength)
            {
                byteArray[j++] = 0;
            }
            return new BigInteger(byteArray);
        }

        public BigInteger DoPublic(BigInteger message)
        {
            return BigInteger.ModPow(message, E,(BigInteger)N);
        }

        public void SetPublic(string N, string E)
        {
            if (!string.IsNullOrEmpty(N) && !string.IsNullOrEmpty(E))
            {
                //this.n = BigInteger.Parse(N, System.Globalization.NumberStyles.AllowHexSpecifier);
                this.N = BigInteger.Parse(N, System.Globalization.NumberStyles.HexNumber);
                //this.e = int.Parse(E, System.Globalization.NumberStyles.AllowHexSpecifier);
                this.E = int.Parse(E, System.Globalization.NumberStyles.HexNumber);
            }
            else
            {
                throw new Exception("Invalid RSA public key");
            }
        }

        public string Encrypt(string text)
        {
            //var m = NoPadding(text, (int)Math.Ceiling(BigInteger.Log((BigInteger)n, 2) + 7) >> 3);
            //var absN = BigInteger.Abs(n.Value);
            //var m = NoPadding(text, (int)Math.Ceiling(BigInteger.Log(absN, 2) + 7) >> 3);            
            BigInteger message = NoPadding(text, ((int)N.GetBitLength() + 7) >> 3);
            if (message == null)
            {
                return null;
            }
            var cipher = DoPublic(message);
            if (cipher == null)
            {
                return null;
            }
            
            string hexString = cipher.ToString("X");
            //return (h.Length & 1) == 0 ? h : "0" + h;
            return hexString.Length % 2 == 0 ? hexString : $"0{hexString}";
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

        public string CalculateRsaChunk(RSAKey rsaKey, string val, int strEnlen)
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

        public string CalculateRsaChunk(string val, int strEnlen)
        {
            string result = rsaKey.Encrypt(val);
            if (result.Length != strEnlen)
            {
                result = string.Concat(Enumerable.Repeat("0", Math.Abs(strEnlen - result.Length))) + result;
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
                end = Math.Min(end, plainText.Length);
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
