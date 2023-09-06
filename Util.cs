using System;
using System.Collections.Generic;
using System.Text;

namespace SAFEditor
{
    class Util
    {
        public static Int32 GetBEInt32(Byte[] array, Int32 offset)
        {
            return (Int32)(array[offset] + array[offset + 1] * 0x100 + array[offset + 2] * 0x10000 + array[offset + 3] * 0x1000000);
        }
        public static Int16 GetBEInt16(Byte[] array, Int32 offset)
        {
            return (Int16)(array[offset] + array[offset + 1] * 0x100);
        }
        public static UInt16 GetBEUInt16(Byte[] array, Int32 offset)
        {
            return (UInt16)(array[offset] + array[offset + 1] * 0x100);
        }
        public static Int16 GetLEInt16(Byte[] array, Int32 offset)
        {
            return (Int16)(array[offset + 1] + array[offset] * 0x100);
        }
        public static void SetBEUInt16(Byte[] array, Int32 offset, UInt16 inData)
        {
            array[offset] = (Byte)(inData % 0x100);
            array[offset + 1] = (Byte)(inData / 0x100);
        }
        public static void SetBEUInt32(Byte[] array, Int32 offset, UInt32 inData)
        {
            array[offset] = (Byte)(inData % 0x100);
            array[offset + 1] = (Byte)(inData / 0x100 % 0x100);
            array[offset + 2] = (Byte)(inData / 0x10000 % 0x100);
            array[offset + 3] = (Byte)(inData / 0x1000000);
        }

        public static Byte[] ConvHexStringToBytes(String hexStr)
        {
            int i;
            byte[] ret = new byte[hexStr.Length / 2];

            for (i = 0; i < hexStr.Length / 2; i++)
            {
                ret[i] = Convert.ToByte(hexStr.Substring(i * 2, 2), 16);
            }

            return ret;
        }
        public static String ConvBytesToHexString(byte[] bytes, int offset, int len)
        {
            int i;
            String ret = "";

            for (i = 0; i < len; i++)
            {
                ret += Convert.ToString(bytes[i + offset], 16).PadLeft(2, '0') + " ";
            }

            return ret;
        }
    }
}
