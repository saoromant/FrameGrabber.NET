using Emgu.CV;
using Emgu.CV.Structure;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FrameGrabber
{
    class Program
    {
        private const string COMMAND = "*RDY*";
        private const int WIDTH =  640/2;
        private const int HEIGHT =  480/2;
        static void Main(string[] args)
        {
            int[][] rgb = Enumerable.Range(0, HEIGHT).Select(x => Enumerable.Range(0, WIDTH).Select(y => 0).ToArray()).ToArray();
            int[][] rgb2 = Enumerable.Range(0, WIDTH).Select(x => Enumerable.Range(0, HEIGHT).Select(y => 0).ToArray()).ToArray();
            using (SerialPort sp = new SerialPort("COM4", 1000000, Parity.None, 8, StopBits.One))
            {
                int counter = 0;
                sp.Open();
                while (sp.IsOpen)
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    Console.WriteLine("waiting for image...");
                    while (!IsImageStart(sp)) ;
                    Console.WriteLine($"Receiving image after {sw.ElapsedMilliseconds} ms");
                    sw.Restart();
                    for (int y = 0; y < HEIGHT; y++)
                    {
                        for (int x = 0; x < WIDTH; x++)
                        {
                            int temp = sp.ReadByte();
                            rgb[y][x] = ((temp & 0xFF) << 16) | ((temp & 0xFF) << 8) | (temp & 0xFF);
                        }
                    }

                    for (int y = 0; y < HEIGHT; y++)
                    {
                        for (int x = 0; x < WIDTH; x++)
                        {
                            rgb2[x][y] = rgb[y][x];
                        }
                    }

                    BMP bmp = new BMP(); 
                  var bytes =  bmp.saveBMP(rgb2);//"out" + (counter++) + ".bmp"
                     

                    Image<Rgb, byte> depthImage = new Image<Emgu.CV.Structure.Rgb, byte>(HEIGHT, WIDTH);

                    depthImage.Bytes = bytes;

                    CvInvoke.Imshow("frame",depthImage);
                    // CvInvoke.Imshow("frame", new Mat("out" + (counter - 1) + ".bmp"));
                    
                    CvInvoke.WaitKey(1);
                    Console.WriteLine($"image received in {sw.ElapsedMilliseconds} ms");
                }
            }
        }


        private static bool IsImageStart(SerialPort sp)
        {
            bool result = true;
            for (int i = 0; i < COMMAND.Length && result; i++)
            {
                int c = sp.ReadChar(); 
                result &=  c == COMMAND[i];
            }
            return result;
        }



    }

    public class BMP
    {
        byte[] bytes;

        public int[,] readBMP(String fileName)
        {
            byte[] buf = new byte[54];
            int[,] rgb = null;

            try
            {
                FileStream fos = new FileStream(fileName, FileMode.Open);
                fos.Read(buf, 0, buf.Length);

                int width = ((buf[21] & 0xFF) << 24) + ((buf[20] & 0xFF) << 16) + ((buf[19] & 0xFF) << 8) + (buf[18] & 0xFF);
                int height = ((buf[25] & 0xFF) << 24) + ((buf[24] & 0xFF) << 16) + ((buf[23] & 0xFF) << 8) + (buf[22] & 0xFF);

                rgb = new int[height, width];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        fos.Read(buf, 0, 3);
                        rgb[y, x] = ((buf[2] & 0xFF) << 16) + ((buf[1] & 0xFF) << 8) + (buf[0] & 0xFF);
                    }
                }
            }
            catch (IOException e)
            {
                Console.WriteLine(e.ToString());
            }

            return rgb;
        }

        public byte[] saveBMP(int[][] rgbValues, string filename = null)
        {
            try
            {

                bytes = new byte[/**/ + 3 * rgbValues.Length * rgbValues[0].Length];

                saveFileHeader();
                saveInfoHeader(rgbValues.Length, rgbValues[0].Length);
                saveBitmapData(rgbValues);

                if(!string.IsNullOrEmpty(filename))
                File.WriteAllBytes(filename, bytes);

                return bytes;
            }
            catch (IOException e)
            {
                Console.WriteLine(e.ToString());
                throw;
            }
        }

        private void saveFileHeader()
        {
            //bytes[0] = (byte)'B';
            //bytes[1] = (byte)'M';

            //bytes[5] = (byte)bytes.Length;
            //bytes[4] = (byte)(bytes.Length >> 8);
            //bytes[3] = (byte)(bytes.Length >> 16);
            //bytes[2] = (byte)(bytes.Length >> 24);

            ////data offset
            //bytes[10] = 0;//54;
        }

        private void saveInfoHeader(int height, int width)
        {
            bytes[14] = 40;

            bytes[18] = (byte)width;
            bytes[19] = (byte)(width >> 8);
            bytes[20] = (byte)(width >> 16);
            bytes[21] = (byte)(width >> 24);

            bytes[22] = (byte)height;
            bytes[23] = (byte)(height >> 8);
            bytes[24] = (byte)(height >> 16);
            bytes[25] = (byte)(height >> 24);

            bytes[26] = 1;

            bytes[28] = 24;
        }

        private void saveBitmapData(int[][] rgbValues)
        {
            for (int i = 0; i < rgbValues.Length; i++)
            {
                writeLine(i, rgbValues);
            }
        }

        private void writeLine(int row, int[][] rgbValues)
        {
            int offset = 0;//54;
            int rowLength = rgbValues[row].Length;
            for (int i = 0; i < rowLength; i++)
            {
                int rgb = rgbValues[row][i];
                int temp = offset + 3 * (i + rowLength * row);

                bytes[temp + 2] = (byte)(rgb >> 16);
                bytes[temp + 1] = (byte)(rgb >> 8);
                bytes[temp] = (byte)rgb;
            }
        }
    }
}
