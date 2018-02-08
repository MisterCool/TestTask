using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace FilterForImage
{
    public class Filter
    {
        public static void ApplyFilter(Bitmap bmp, string filter)
        {
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var bmpData = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);
            var ptr = bmpData.Scan0;

            int numBytes = bmpData.Width * bmp.Height * 4;
            byte[] rgbValues = new byte[numBytes];

            Marshal.Copy(ptr, rgbValues, 0, numBytes);
            switch (filter)
            {
                case "sepia":
                    GetRGBSepia(rgbValues);
                    break;
                case "grayscale":
                    GetRGBGrayScale(rgbValues);
                    break;
                default:
                    var arr = filter.Split('(', ')');
                    var x = int.Parse(arr[1]);
                    GetRGBThreshold(rgbValues, x);
                    break;
            }
            Marshal.Copy(rgbValues, 0, ptr, numBytes);
            bmp.UnlockBits(bmpData);
        }
        private static byte[] GetRGBSepia(byte[] array)
        {
            for (int i = 0; i < array.Length; i += 4)
            {
                int red = array[i + 2];
                int green = array[i + 1];
                int blue = array[i + 0];

                array[i + 2] = (byte)Math.Min((.393 * red) + (.769 * green) + (.189 * blue), 255.0);
                array[i + 1] = (byte)Math.Min((.349 * red) + (.686 * green) + (.168 * blue), 255.0);
                array[i + 0] = (byte)Math.Min((.272 * red) + (.534 * green) + (.131 * blue), 255.0);   
            }
            return array;
        }
        private static byte[] GetRGBThreshold(byte[] arrayRGB, int x)
        {
            for (int i = 0; i < arrayRGB.Length; i += 4)
            {
                int red = arrayRGB[i + 2];
                int green = arrayRGB[i + 1];
                int blue = arrayRGB[i + 0];
                int intensity = red + green + blue;
                if (intensity >= 255 * x / 100)
                {
                    arrayRGB[i + 2] = 255;
                    arrayRGB[i + 1] = 255;
                    arrayRGB[i + 0] = 255;
                }
                else
                {
                    arrayRGB[i + 2] = 0;
                    arrayRGB[i + 1] = 0;
                    arrayRGB[i + 0] = 0;
                }
            }
            return arrayRGB;
        }
        private static byte[] GetRGBGrayScale(byte[] arrayRGB)
        {
            for (int i = 0; i < arrayRGB.Length; i += 4)
            {
                int red = arrayRGB[i + 2];
                int green = arrayRGB[i + 1];
                int blue = arrayRGB[i + 0];
                int value = (red + green + blue) / 3;

                arrayRGB[i + 2] = (byte)value;
                arrayRGB[i + 1] = (byte)value;
                arrayRGB[i + 0] = (byte)value;
            }
            return arrayRGB;
        }
    }
}
