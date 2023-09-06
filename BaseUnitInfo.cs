using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;

namespace SAFEditor
{
    class BaseUnitInfo
    {
        public const Int32 BlockXLimit = 30;
        public const Int32 BlockYLimit = 24;
        protected const Byte SKIP = 0x40;
        protected const Byte FILL = 0x80;
        protected const Byte FILL_REPEAT = 0xC0;

        protected Byte[] CompressUnitData(Byte[] unitData)
        {
            const Byte MAX_COUNTER = 0x1E;
            Int32 i;
            String retStr = "";
            Byte[] ret = null;

            for (i = 0; i < unitData.Length; i += MAX_COUNTER * 2)
            {
                Byte[] blockData = GetSubArray(unitData, i, MAX_COUNTER * 2);
                String compressedBlockData = CompressBlockData(blockData);
                retStr += compressedBlockData;
            }

            ret = Util.ConvHexStringToBytes(retStr);

            return ret;
        }

        protected static void GetDrawCoordinate(Int32 offset, ref Int32 x, ref Int32 y, Int32 xLimit, Int32 yLimit)
        {
            Int32 BlockX = xLimit / BlockXLimit;
            Int32 BlockY = yLimit / BlockYLimit;
            Int32 pointCount = offset / 2;

            Int32 TotalLine = pointCount / BlockXLimit;
            Int32 TotalBlock = TotalLine / BlockYLimit;
            x = pointCount % BlockXLimit;
            y = TotalLine % BlockYLimit;

            x += (TotalBlock % BlockX) * BlockXLimit;
            y += (TotalBlock / BlockX) * BlockYLimit;
        }

        protected Byte[] GetDrawData(Byte[] unitData)
        {
            Int32 j;
            Int32 n;
            Int32 p = 0;
            Byte[] ret = new Byte[BlockXLimit * BlockYLimit * 2];


            for (j = 0; j < unitData.Length; j++)
            {
                n = (unitData[j] & 0x1F) + 1;
                if ((unitData[j] & 0x80) == 0x80)
                {
                    if ((unitData[j] & 0x40) == 0x40)
                    {
                        while (n-- > 0)
                        {
                            CopyBuffer(unitData, j + 1, ret, p, 2);
                            p += 2;
                        }

                        j += 2;
                    }
                    else
                    {
                        CopyBuffer(unitData, j + 1, ret, p, n * 2);
                        j += n * 2;
                        p += n * 2;
                    }
                }
                else
                {
                    if ((unitData[j] & 0x40) == 0x40)
                    {
                        p += n * 2;
                    }
                    else
                    {
                        //此情况暂未发现有图像使用
                        p += n * 2;
                    }
                }
            }

            return ret;
        }
        protected Bitmap MakeBitmap(Byte[] drawData, Int32 x, Int32 y)
        {
            return MakeBitmap(drawData, x, y, 0x00, 0x00, 0x00, 0x00);
        }

        protected Bitmap MakeBitmap(Byte[] drawData, Int32 x, Int32 y, Int32 alphaFlag, Int32 red, Int32 green, Int32 blue, Boolean isTransparent = false)
        {
            Int32 i = 0;
            Int32 j = 0;
            Int32 r;
            Int32 g;
            Int32 b;
            Int32 p = 0;
            Int16 color;
            Int32 alpha = 255;
            Color transparentColor = Color.Black;

            Bitmap retBitmap = new Bitmap(x, y);

            Graphics gp = Graphics.FromImage(retBitmap);

            while (p < drawData.Length)
            {
                color = Util.GetBEInt16(drawData, p);

                r = (color & 0x7C00) >> 7;// | ((color & 0x0300) >> 4);
                g = (color & 0x03E0) >> 2;// | ((color & 0x0180) >> 7);
                b = (color & 0x001F) << 3;// | ((color & 0x00C0) >> 2);

                SAFInfo.GetDrawCoordinate(p, ref i, ref j, x, y);

                if (alphaFlag == 0x00)
                {
                    alpha = 255;
                }
                else if (alphaFlag == 0x01)
                {
                    alpha = 128;
                }
                else if (alphaFlag == 0x02)
                {
                    alpha = 255;
                    transparentColor = Color.Black;
                }
                else if (alphaFlag == 0x07)
                {
                    alpha = 255;
                    transparentColor = Color.White;
                }

                SolidBrush sb = new SolidBrush(Color.FromArgb(alpha, r, g, b));
                gp.FillRectangle(sb, i, j, 1, 1);
                sb.Dispose();

                p += 2;
            }
            gp.Dispose();

            if (isTransparent)
            {
                retBitmap.MakeTransparent(transparentColor);
            }

            return retBitmap;
        }

        protected Byte[] GetSubArray(Byte[] inBuff, Int32 start, Int32 len)
        {
            Byte[] ret = new Byte[len];

            while (len > 0)
            {
                ret[len - 1] = inBuff[start + len - 1];
                len--;
            }

            return ret;
        }

        protected void CopyBuffer(Byte[] inBuff, Int32 inOffset, Byte[] outBuff, Int32 outOffset, Int32 length)
        {
            while (length-- > 0)
            {
                outBuff[outOffset++] = inBuff[inOffset++];
            }
        }
        protected String MakeSkipPattern(Byte[] unitData, Int32 pixelOffset, Int32 pixelLen)
        {
            String ret = "";
            Byte head = (Byte)(SKIP | pixelLen - 1);

            if (pixelLen == 0)
            {
                return ret;
            }

            ret = Convert.ToString(head, 16).PadLeft(2, '0');

            return ret;
        }

        protected String MakeFillRepeatPattern(Byte[] unitData, Int32 pixelOffset, Int32 pixelLen)
        {
            String ret = "";
            Byte head = (Byte)(FILL_REPEAT | pixelLen - 1);
            UInt16 color = 0;

            if (pixelLen == 0)
            {
                return ret;
            }

            color = (UInt16)Util.GetLEInt16(unitData, pixelOffset * 2);
            ret = Convert.ToString(head, 16).PadLeft(2, '0') + Convert.ToString(color, 16).PadLeft(4, '0');

            return ret;
        }

        protected String MakeFillPattern(Byte[] unitData, Int32 pixelOffset, Int32 pixelLen)
        {
            String ret = "";
            Byte head = (Byte)(FILL | pixelLen - 1);
            UInt16 color = 0;

            if (pixelLen == 0)
            {
                return ret;
            }

            ret = Convert.ToString(head, 16).PadLeft(2, '0');

            while (pixelLen-- > 0)
            {
                color = (UInt16)Util.GetLEInt16(unitData, (pixelOffset++) * 2);
                ret += Convert.ToString(color, 16).PadLeft(4, '0');
            }

            return ret;
        }
        protected String CompressBlockData(Byte[] blockData)
        {
            Byte[] ret = null;
            const Byte MAX_COUNTER = 0x1E;
            const Byte SAME = 0x00;
            const Byte UNSAME = 0x01;
            const Byte NOUSE = 0xFF;
            UInt16 color2 = 0;
            UInt16 color1 = 0;
            Int32 pixelOffset;
            Int32 compareFlag;
            Int32 pixelCounter;
            Int32 i;
            String tempData = "";
            String tempData1 = "";

            pixelOffset = 0;
            pixelCounter = 0;
            compareFlag = NOUSE;
            for (i = 0; i < blockData.Length; i += 2)
            {
                color1 = (UInt16)Util.GetLEInt16(blockData, i);
                if (i + 2 >= blockData.Length)
                {
                    color2 = 0;
                }
                else
                {
                    color2 = (UInt16)Util.GetLEInt16(blockData, i + 2);
                }

                if ((color1 != color2) || (i + 2 > blockData.Length))
                {
                    if ((color1 == 0) && ((compareFlag == SAME) || (compareFlag == NOUSE)))
                    {
                        tempData1 = MakeSkipPattern(blockData, pixelOffset, (i / 2) - pixelOffset + 1);
                        tempData += tempData1;
                        pixelCounter += (i / 2) - pixelOffset + 1;
                        pixelOffset = i / 2 + 1;
                        compareFlag = NOUSE;
                    }
                    else if ((color2 == 0) && ((compareFlag == UNSAME) || (compareFlag == NOUSE)))
                    {
                        tempData1 = MakeFillPattern(blockData, pixelOffset, (i / 2) - pixelOffset + 1);
                        tempData += tempData1;
                        pixelCounter += (i / 2) - pixelOffset + 1;
                        pixelOffset = i / 2 + 1;
                        compareFlag = NOUSE;
                    }
                    else
                    {
                        if (compareFlag == NOUSE)
                        {
                            compareFlag = UNSAME;
                        }
                        else if (compareFlag == SAME)
                        {
                            tempData1 = MakeFillRepeatPattern(blockData, pixelOffset, (i / 2) - pixelOffset + 1);
                            tempData += tempData1;
                            pixelCounter += (i / 2) - pixelOffset + 1;
                            pixelOffset = i / 2 + 1;
                            compareFlag = NOUSE;
                        }
                    }
                }
                else
                {
                    if (compareFlag == NOUSE)
                    {
                        compareFlag = SAME;
                    }
                    else if (compareFlag == UNSAME)
                    {
                        tempData1 = MakeFillPattern(blockData, pixelOffset, (i / 2) - pixelOffset);
                        tempData += tempData1;
                        pixelCounter += (i / 2) - pixelOffset;
                        pixelOffset = i / 2;
                        i -= 2;
                        compareFlag = NOUSE;
                    }
                }
            }

            if (pixelCounter < MAX_COUNTER)
            {
                if ((color1 == 0) && (compareFlag == SAME))
                {
                    tempData1 = MakeSkipPattern(blockData, pixelOffset, (i / 2) - pixelOffset);
                    tempData += tempData1;
                }
                else
                {
                    tempData += "";
                }
            }

            ret = Util.ConvHexStringToBytes(tempData);

            return tempData.ToUpper();
        }
    }
}
