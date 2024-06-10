using System.Security.Cryptography;
using System.Text;

namespace TPLink
{
    internal class Encryption_v2
    {
        private System.Security.Cryptography.RSA _rsa;
        private CustomRSAPadding _crsa;
        private Aes _aes;


        internal int Seq { get; private set; }
        private string Hash { get; set; }
        internal string Key
        {
            get
            {
                return Encoding.UTF8.GetString(_aes.Key);
            }
        }
        internal string IV
        {
            get
            {
                return Encoding.UTF8.GetString(_aes.IV);
            }
        }
        internal string AesKeyString
        {
            get
            {
                return $"key={Key}&iv={IV}";
            }
        }

        public Encryption_v2()
        {
            _rsa = System.Security.Cryptography.RSA.Create();
            _rsa.KeySize = 512;

            _crsa = new CustomRSAPadding();
           

            _aes = Aes.Create();
            _aes.KeySize = 128;
            _aes.Mode = CipherMode.CBC;
        }

        public Encryption_v2 GenAESKey()
        {
            _aes.GenerateKey();
            _aes.GenerateIV();

            return this;
        }

        public Encryption_v2 SetSeq(string value)
        {
            Seq = int.Parse(value);
            return this;
        }

        public Encryption_v2 SetRSAKey(string n, string e)
        {
            RSAParameters rSAParameters = new RSAParameters()
            {
                Modulus = Encoding.UTF8.GetBytes(n),
                Exponent = Encoding.UTF8.GetBytes(e)
            };
            _rsa.ImportParameters(rSAParameters);

            return this;
        }

        public string GetSignature(string eq, bool isLogin)
        {
            string s = isLogin ? $"{AesKeyString}&" : "";
            s += $"{Hash}&s={Seq}" ?? Seq.ToString();
            byte[] dataToEncrypt = Encoding.UTF8.GetBytes(s);
            byte[] encryptedData = _rsa.Encrypt(dataToEncrypt, RSAEncryptionPadding.Pkcs1);           

            return Encoding.UTF8.GetString(encryptedData);
        }

        public (string data, string sign) AESEncrypt(string data, bool isLogin = false)
        {
            ICryptoTransform encryptor = _aes.CreateEncryptor(_aes.Key, _aes.IV);
            MemoryStream msEncrypt = new MemoryStream();
            CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write);
            StreamWriter swEncrypt = new StreamWriter(csEncrypt);
            swEncrypt.Write(data);
            byte[] encryptedData = msEncrypt.ToArray();
            string encrypted = Encoding.UTF8.GetString(encryptedData);

            string sign = GetSignature((Seq + encrypted.Length).ToString(), isLogin);
            return (encrypted, sign);
        }

        public string AESDecrypt(string data)
        {
            byte[] encryptedData = Encoding.UTF8.GetBytes(data);
            ICryptoTransform decryptor = _aes.CreateDecryptor(_aes.Key, _aes.IV);
            MemoryStream msDecrypt = new MemoryStream(encryptedData);
            CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read);
            StreamReader srDecrypt = new StreamReader(csDecrypt);
            string decrypted = srDecrypt.ReadToEnd();

            return decrypted;
        }
    }
}
