using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace eac_xtract
{
    internal class EACCrypto
    {
        public static byte[] DecryptBuffer(byte[] enc)
        {
            // create a buffer to store our decrypted output into
            byte[] outbuf = new byte[enc.Length];
            Buffer.BlockCopy(enc, 0, outbuf, 0, enc.Length);

            // sanity
            if (enc.Length < 2)
                return outbuf;

            // correct the last byte
            outbuf[enc.Length - 1] += (byte)(3 - 3 * (enc.Length));

            // in reverse, go through the module and adjust by the previous byte
            for (int i = enc.Length - 2; i > 0; i--)
                outbuf[i] += (byte)(-3 * i - outbuf[i + 1]);

            // adjustment on the first byte is different
            outbuf[0] -= outbuf[1];

            return outbuf;
        }
    }
}
