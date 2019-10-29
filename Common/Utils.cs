using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;

namespace Common
{
    public class Utils
    {
        //public static string GetTimestampNow()
        //{
        //    return (DateTime.Now - DateTime.UnixEpoch).TotalMilliseconds.ToString().Split('.')[0];
        //}
        public static string GetDeviceName()
        {
            return Dns.GetHostName();
        }
        public static string GetLocalIPAddress()
        {
            //Console.WriteLine(Dns.GetHostName());
            IPAddress[] addressList = Dns.GetHostAddresses(Dns.GetHostName());
            string pattern = @"^((([1-9]\d?)|25[0-5]|2[0-4]\d|(1[013456789]\d)|(12[01234569]))\.)((25[0-5]|2[0-4]\d|1\d{2}|[1-9]?\d)\.){2}(25[0-5]|2[0-4]\d|((1\d{2})|([1-9]?\d)))";
            //string except = @"(^0.)|(^127.)";
            //var add = from i in addressList
            //          where Regex.IsMatch(i.ToString(), pattern)
            //          select i.ToString();
            List<string> add = new List<string>();
            foreach (var i in addressList)
            {
                string ip = i.ToString();
                if (Regex.IsMatch(ip, pattern))
                {
                    add.Add(ip);
                }
            }

            return add.Any() ? add.First() : GetLocalIPAddress2();
        }
        public static string GetLocalIPAddress2()
        {
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            string pattern = @"^((([1-9]\d?)|25[0-5]|2[0-4]\d|(1[013456789]\d)|(12[01234569]))\.)((25[0-5]|2[0-4]\d|1\d{2}|[1-9]?\d)\.){2}(25[0-5]|2[0-4]\d|((1\d{2})|([1-9]?\d)))";

            //foreach (var aInterface in networkInterfaces)
            //{
            //    foreach (var t in aInterface.GetIPProperties().UnicastAddresses)
            //    {
            //        if (Regex.IsMatch(t.Address.ToString(), pattern))
            //        {
            //            Console.WriteLine(t.Address.ToString());
            //        }
            //    }
            //}
            //return "";

            var adds = from aInterface in networkInterfaces
                       where aInterface.GetIPProperties().UnicastAddresses.Any()
                       select aInterface.GetIPProperties().UnicastAddresses.First().Address.ToString();
            var add = from i in adds
                      where Regex.IsMatch(i, pattern)
                      select i;

            return add.Any() ? add.First() : throw new NotSupportedException("Can't fetch your IP address.");
        }
        public static string GetLocalIPAddresses()
        {
            IPAddress[] addressList = Dns.GetHostAddresses(Dns.GetHostName());

            string r = "";
            foreach (var i in addressList)
                r += i.ToString() + '\n';
            return r;
        }
        public static int GeneratePin()
        {
            Random r = new Random();
            return r.Next(1, 999999);
        }
        public static string GenerateKey(int length = 128)
        {
            Random r = new Random();
            string key = "";
            for (int i = 0; i < length; i++)
            {
                key += (char)r.Next(33, 126);
            }
            return key;
        }
        public static string GenerateSemiKey(string key, out string guard)
        {
            Random r = new Random();
            int start = r.Next(0, key.Length - 41);
            guard = key.Substring(start + 20, 20);
            return key.Substring(start, 20);
        }
        public static string GenerateGuardKey(string key, string key1)
        {
            if (key.Contains(key1))
            {
                int i = key.IndexOf(key1);
                return key.Substring(i + 20, 20);
            }
            else return "";
        }

        internal static void PrintBytes(byte[] bs)
        {
            foreach (var b in bs)
            {
                Console.Write("{0:X2} ", b);
            }
        }
        public static string ShowBytes(byte[] bs)
        {
            string t = "";
            foreach (var b in bs)
            {
                t += $"{b:X2}";
            }
            return t;
        }
        public static byte[] GetBytes(string src) { return Encoding.Default.GetBytes(src); }
        public static byte[] GetBytes(long src, int size)
        {
            int len = size;
            byte[] bs = new byte[size];
            long t = src;
            for (int i = 0; i < len; i++)
            {
                bs[i] = (byte)(t & 0xff);
                t >>= 8;
            }
            return bs.Reverse().ToArray();
        }
        public static byte[] GetBytes(long src) { return GetBytes(src, 8); }
        public static byte[] GetBytes(int src) { return GetBytes(src, 4); }
        public static byte[] GetBytes(short src) { return GetBytes(src, 2); }
        //public static byte[] GetBytes()
        public static IPAddress BtoIP(byte[] src) { return IPAddress.Parse(BtoIPstr(src)); }
        public static string BtoIPstr(byte[] src) { return $"{src[0]}.{src[1]}.{src[2]}.{src[3]}"; }
        public static string BtoString(byte[] src) { return Encoding.Default.GetString(src); }
        public static long BtoNum(byte[] src, int size)
        {
            int len = size;
            long t = 0;
            for (int i = 0; i < len; i++)
            {
                t <<= 8;
                t += src[i];
            }
            return t;
        }
        public static long BtoLong(byte[] src) { return BtoNum(src, 8); }
        public static int BtoInt(byte[] src) { return (int)BtoNum(src, 4); }
        public static short BtoShort(byte[] src) { return (short)BtoNum(src, 2); }
        public static byte[] BytesSlice(byte[] src, int start, int length)
        {
            byte[] newb = new byte[length];
            Array.Copy(src, start, newb, 0, length);
            return newb;
        }
        public static byte[] BytesSlice(byte[] src, int start)
        {
            byte[] newb = new byte[src.Length - start];
            Array.Copy(src, start, newb, 0, src.Length - start);
            return newb;
        }
        //public static bool BytesEqual(byte[] src1, byte[] src2)
        //{
        //    if (src1.Length!=src2.Length)
        //        return false;
        //    //src1.
        //}
    }
}
