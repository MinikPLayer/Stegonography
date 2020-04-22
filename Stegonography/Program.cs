using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Stegonography
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");

            string fileName = "";
            int bpp = 2;
            bool encrypt = false;

            string toEncrypt = "";
            for(int i = 0;i<1000;i++)
            {
                toEncrypt += i.ToString() + " ";//"Hello worldHello worldHello worldHello worldHello worldHello worldHello worldHello worldHello worldHello worldHello worldHello worldHello worldHello worldHello worldHello worldHello world";
            }
            if(args.Length > 0)
            {
                fileName = args[0];
                if(args.Length > 1)
                {
                    bpp = int.Parse(args[1]);
                }
            }
            else
            {
                Debug.Log("Enter fileName: ", ConsoleColor.White, false);
                fileName = Console.ReadLine();

                while (true)
                {
                    Debug.Log("Encrypt - e or decrypt - d?");
                    char c = Console.ReadKey().KeyChar;
                    Debug.Log("");

                    if(c == 'e' || c == 'E')
                    {
                        encrypt = true;
                        Debug.Log("What to encrypt?");
                        toEncrypt = Console.ReadLine();

                        break;
                    }
                    else if(c == 'd' || c == 'D')
                    {
                        break;
                    }
                    else
                    {
                        Debug.Log("You have to enter e or d");
                    }
                }

                Debug.Log("Bit per pixel (default - 2): ", ConsoleColor.White, false);
                bpp = int.Parse(Console.ReadLine());
            }

            Stegonography engine = new Stegonography(fileName);
            //engine.DisplayBytes(54, 90);


            if (encrypt)
            {
                engine.Encrypt(toEncrypt, bpp);
            }
            else
            {
                string value = engine.Decrypt(bpp);

                Debug.Log("Decrypted message: ");
                Debug.Log(value, ConsoleColor.Green);

                Console.ReadKey();
            }
        }


    }

    class Stegonography
    {
        string file;
        byte[] bytes;

        int sizeX, sizeY;
        int bpp;

        int startOffset;

        bool ParseHeader()
        {
            if(bytes.Length < 14)
            {
                Debug.LogError("Bad or corrupted header");
                return false;
            }

            if(bytes[0] != 'B' || bytes[1] != 'M')
            {
                Debug.LogError("Bad magic number");
                return false;
            }



            //long size = BytesToNumber(new byte[] { bytes[5], bytes[4], bytes[3], bytes[2] });
            long size = BytesToNumber(5, 2);
            Debug.Log("Size: " + size + ", bytes size: " + bytes.Length);

            if(size != bytes.Length)
            {
                Debug.LogError("File size mismatch with actual file size");
                return false;
            }

            startOffset = (int)BytesToNumber(13, 10);
            Debug.Log("StartOffset: " + startOffset);

            sizeX = (int)BytesToNumber(21, 18, true);
            sizeY = (int)BytesToNumber(25, 22, true);
            bpp = (int)BytesToNumber(29, 28);

            Debug.Log("Image size: " + sizeX + "x" + sizeY);
            Debug.Log("Bits Per Pixel: " + bpp);

            return true;
        }

        static void DisplayBinary(bool[] data)
        {
            for (int j = 0; j < data.Length; j++)
            {
                if (data[j])
                {
                    Debug.Log("1", ConsoleColor.White, false);
                }
                else
                {
                    Debug.Log("0", ConsoleColor.White, false);
                }
            }
            Debug.Log("");
        }

        public static bool[] CompleteTo(bool[] data, int size)
        {
            if (data.Length > size) return data;

            bool[] d = new bool[size];

            for(int i = 0;i<size;i++)
            { 
                if(i < size - data.Length)
                {
                    d[i] = false;
                }
                else
                {
                    d[i] = data[i - (size - data.Length)];
                }
            }

            return d;
        }

        public static List<bool> CompleteTo(List<bool> data, int size)
        {
            if (data.Count > size) return data;

            int count = data.Count;
            for(int i = 0;i<size - count; i++)
            {
                data.Insert(0, false);
            }

            return data;
        }

        public static long Pow(long a, int n)
        {
            if (n == 0) return 1;
            if (n == 1) return a;
            long pom = a;
            for(int i = 1;i<n;i++)
            {
                a *= pom;
            }

            return a;
        }

        public long BytesToNumber(int startByte, int endByte, bool u2 = false)
        {
            byte[] bs = new byte[Math.Abs(startByte - endByte) + 1];
            int mult = 1;
            if (endByte < startByte) mult = -1;
            for(int i = 0;i< bs.Length;i++)
            {
                bs[i] = bytes[i * mult + startByte];
            }

            return BytesToNumber(bs, u2);
        }

        public static long BytesToNumber(byte[] data, bool u2 = false)
        {
            if(data.Length > 8)
            {
                Debug.LogError("Too big number");
                return long.MinValue;
            }

            List<bool> values = new List<bool>();

            for(int i = 0;i< data.Length;i++)
            {
                bool[] v = CompleteTo(ByteToBinary(data[i]).ToArray(), 8);
                

                values.AddRange(v);
            }

            //DisplayBinary(values.ToArray());
            long value = 0;
            for(int i = 0;i<values.Count;i++)
            {

                if(values[i])
                {
                    long powV = Pow(2, values.Count - 1 - i);
                    if(u2 && i == 0)
                    {
                        powV *= -1;
                    }

                    value += powV;
                }
            }

            return value;
        }

        public static List<bool> LongToBinary(long number, int completeTo = -1)
        {
            List<bool> val = new List<bool>();

            while (number != 0)
            {
                val.Insert(0, Convert.ToBoolean(number % 2));
                number /= 2;
            }

            if (completeTo != -1)
            {
                val = CompleteTo(val, completeTo);
            }

            return val;
        }

        public static List<bool> ByteToBinary(byte number, int completeTo = -1)
        {
            return LongToBinary(number, completeTo);
        }

        public static byte BinToByte(List<bool> bin)
        {
            if (bin.Count > 8)
            {
                Debug.LogError("Cannot convert more than 8 bits to byte");
                return 0;
            }
            return (byte)BinToLong(bin);
        }

        public static byte BinToByte(bool[] bin)
        {
            if(bin.Length > 8)
            {
                Debug.LogError("Cannot convert more than 8 bits to byte");
                return 0;
            }
            return (byte)BinToLong(bin);
        }

        public static long BinToLong(bool[] bin)
        {
            long value = 0;

            for (int i = 0; i < bin.Length; i++)
            {
                if (bin[i])
                {
                    value += Pow(2, bin.Length - 1 - i);
                }
            }

            return value;
        }

        public static long BinToLong(List<bool> bin)
        {
            long value = 0;

            for (int i = 0; i < bin.Count; i++)
            {
                if (bin[i])
                {
                    value += Pow(2, bin.Count - 1 - i);
                }
            }

            return value;
        }

        public void DisplayBytes(int offset, int size)
        {
            for(int i = offset;i<offset + size;i++)
            {
                if (i >= bytes.Length)
                {
                    return;
                }

                bool[] v = ByteToBinary(bytes[i]).ToArray();
                Debug.Log(i + " = " + bytes[i] + ") ", ConsoleColor.White, false);

                DisplayBinary(v);
            }
        }

        public void Encrypt(string toEncrypt, int bitsPerPixel = 2)
        {
            if(bitsPerPixel > 8)
            {
                Debug.LogError("Max supported bits per pixel is 8");
                return;
            }

            byte[] enBytes = Encoding.UTF8.GetBytes(toEncrypt);
            List<bool> binary = new List<bool>();

            Debug.Log("Encrypting message with size: " + enBytes.Length);
            binary.AddRange(LongToBinary(enBytes.Length, 32)); // Add size

            for(int i = 0;i<enBytes.Length;i++)
            {
                binary.AddRange(ByteToBinary(enBytes[i], 8));
            }

            int it = 0;
            for(int i = 0;i<binary.Count;i += bitsPerPixel)
            {
                if(startOffset + it >= bytes.Length)
                {
                    Debug.LogError("Not enough space in picture to encrypt this message");
                    return;
                }
                byte b = bytes[startOffset + it];
                bool[] bin = ByteToBinary(b, 8).ToArray();
                for(int j = 0;j<bitsPerPixel && i + j < binary.Count;j++)
                {
                    bin[7 - j] = binary[i + j];
                }
                bytes[startOffset + it] = BinToByte(bin);

                it++;
            }

            File.WriteAllBytes(file + "_st.bmp", bytes);
        }

        public string Decrypt(int bitsPerPixel = 2)
        {
            if (bitsPerPixel > 8)
            {
                Debug.LogError("Max supported bits per pixel is 8");
                return "";
            }


            //long size = BytesToNumber(startOffset, startOffset + 3);
            //Debug.Log("Decrypt size: " + size);
            bool[] sizeBin = new bool[32];
            for(int i = 0;i<4 * 8 / bitsPerPixel;i++)
            {
                List<bool> bin = ByteToBinary(bytes[startOffset + i], 8);
                for(int j = 0;j<bitsPerPixel;j++)
                {
                    sizeBin[(i * bitsPerPixel) + j] = bin[bin.Count - 1 - j];
                }
            }

            int offset = startOffset + (4 * 8 / bitsPerPixel);

            long size = BinToLong(sizeBin);

            if(offset + (size * 8 / bitsPerPixel) >= bytes.Length)
            {
                Debug.LogError("Something went wrong, bad encoded message size - probably configuration error or image does not have any encoded informations");
                return "";
            }
            Debug.Log("Decrypt size: " + size);

            bool[] binary = new bool[size * 8];
            for(int i = 0;i<size * 8 / bitsPerPixel;i++)
            {
                if(offset + i >= bytes.Length)
                {
                    Debug.LogError("Too big size, not enough image data to read from, probably corrupted image");
                    return "";
                }

                List<bool> bin = ByteToBinary(bytes[offset + i], 8);
                for (int j = 0; j < bitsPerPixel; j++)
                {
                    binary[(i * bitsPerPixel) + j] = bin[bin.Count - 1 - j];
                }
            }

            byte[] message = new byte[size];
            for(int i = 0;i<binary.Length;i += 8)
            {
                bool[] b = new bool[8];
                for(int j = 0;j<8;j++)
                {
                    b[j] = binary[i + j];
                }

                message[i / 8] = BinToByte(b);
            }

            return Encoding.UTF8.GetString(message);
        }

        //Encrypt
        public Stegonography(string filePath)
        {
            file = filePath;

            bytes = File.ReadAllBytes(file);

            //Debug.Log("Header: ");
            //DisplayBytes(0, 54);


            if(!ParseHeader())
            {
                Debug.LogError("Cannot parse header");
                return;
            }

            //Debug.Log("First 100 pixels: ");
            //DisplayBytes(startOffset, 100 * bpp / 8);
        }
    }
}
