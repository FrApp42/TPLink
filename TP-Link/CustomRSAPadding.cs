using System.Text;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Parameters;
using Org.BouncyCastle.Security;

namespace TPLink
{
    internal class CustomRSAPadding
    {
        public static byte[] CustomPad(string input, int keySize)
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(input);
            byte[] paddedInput = new byte[keySize / 8]; // keySize is in bits, convert to bytes

            Array.Copy(inputBytes, 0, paddedInput, 0, inputBytes.Length);

            // The rest of the array is already initialized to 0
            return paddedInput;
        }

        public static string EncryptWithCustomPadding(string input, string modulusHex, string exponentHex)
        {
            RsaKeyParameters publicKey = new RsaKeyParameters(false, new Org.BouncyCastle.Math.BigInteger(modulusHex, 16), new Org.BouncyCastle.Math.BigInteger(exponentHex, 16));
            IAsymmetricBlockCipher cipher = new Org.BouncyCastle.Crypto.Engines.RsaEngine();
            cipher.Init(true, publicKey);

            byte[] paddedInput = CustomPad(input, publicKey.Modulus.BitLength);
            byte[] cipherText = cipher.ProcessBlock(paddedInput, 0, paddedInput.Length);

            return BitConverter.ToString(cipherText).Replace("-", "");
        }
    }
}
