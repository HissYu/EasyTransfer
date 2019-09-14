using System;
using System.Net;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.InteropServices;

namespace Transfer
{
    #region Message Structs
    interface IMessageInfo
    {
        IPAddress IP { get; set; }
        int Pin { get; set; }
    }
    interface IMessageKey
    {
        string Key { get; set; }
    }
    interface IMessageConfirm
    {
        IPAddress IP { get; set; }
        string SemiKey { get; set; }
    }
    interface IMessageMeta
    {
        long Size { get; set; }
        int PackSize { get; set; }
        long PackCount { get; set; }
        string Hash { get; set; }
        string Filename { get; set; }
    }
    interface IMessageContinue
    {
        long PackID { get; set; }
    }
    interface IMessageFile
    {
        long PackID { get; set; }
        byte[] Data { get; set; }
    }
    interface IMessageText
    {
        string Text { get; set; }
    }
    #endregion
    public enum MsgType
    {
        Info, Key, Confirm, Meta, Continue, File, Text, Invalid
    }
    /* of no use
    //class Message2
    //{
    //    static public Message2<S> FromBytes<S>(byte[] bytes)
    //    {
    //        int len = bytes.Length;
    //        IntPtr ptr = Marshal.AllocHGlobal(len);
    //        Marshal.Copy(bytes, 0, ptr, len);
    //        Message2<S> msg = Marshal.PtrToStructure<Message2<S>>(ptr);
    //        Marshal.FreeHGlobal(ptr);
    //        return msg;
    //    }
    //    static public bool TryFromBytes<S>(byte[] bytes, out Message2<S> msg)
    //    {
    //        try
    //        {
    //            msg = FromBytes<S>(bytes);
    //            return true;
    //        }
    //        catch (Exception)
    //        {
    //            msg = new Message2<S>();
    //            return false;
    //        }
    //    }
    //}
    //class Message2<T> : Message2
    //{
    //    //readonly byte[] head = Encoding.Default.GetBytes("Tr@nsfer");
    //    public MsgType Type;
    //    public T MessageBody;
    //    public byte[] GetBytes()
    //    {
    //        int len = Marshal.SizeOf(this);
    //        byte[] bs = new byte[len];
    //        IntPtr ptr = Marshal.AllocHGlobal(len);
    //        Marshal.StructureToPtr(this, ptr, false);
    //        Marshal.Copy(ptr, bs, 0, len);
    //        Marshal.FreeHGlobal(ptr);
    //        //Span<byte> sb = bs;
    //        return bs;
    //    }
    //}
    */
    public class Message : IMessageConfirm, IMessageContinue, IMessageFile, IMessageInfo, IMessageKey, IMessageMeta, IMessageText
    {
        public string Text { get; set; }
        public long Size { get; set; }
        public int PackSize { get; set; }
        public long PackCount { get; set; }
        public string Hash { get; set; }
        public string Filename { get; set; }
        public string Key { get; set; }
        public IPAddress IP { get; set; }
        public int Pin { get; set; }
        public long PackID { get; set; }
        public byte[] Data { get; set; }
        public string SemiKey { get; set; }

        public MsgType Type
        {
            get =>
                IP != null ?
                    (SemiKey != null ? MsgType.Confirm : MsgType.Info) :
                Key != null ? MsgType.Key :
                PackID != 0 ?
                    (Data != null ? MsgType.File : MsgType.Continue) :
                Text != null ? MsgType.Text :
                (Filename != null && Size != 0 && Hash != null && PackSize != 0 && PackCount != 0) ? MsgType.Meta : MsgType.Invalid;
        }

        public static Message Parse(byte[] src)
        {
            Span<byte> bs = src;
            switch (src[0])
            {
                case 0:
                    return new Message { IP = Utils.BtoIP(bs.Slice(1, 4).ToArray()), Pin = (int)Utils.BtoNum(bs.Slice(5, 3).ToArray(), 3) };
                case 1:
                    return new Message { Key = Utils.BtoString(bs.Slice(1).ToArray()) };
                case 2:
                    return new Message { IP = Utils.BtoIP(bs.Slice(1, 4).ToArray()), SemiKey = Utils.BtoString(bs.Slice(5).ToArray()) };
                case 3:
                    return new Message { Size = Utils.BtoLong(bs.Slice(1, 8).ToArray()), PackSize = Utils.BtoInt(bs.Slice(9, 4).ToArray()), PackCount = Utils.BtoLong(bs.Slice(13, 8).ToArray()), Hash = Utils.BtoString(bs.Slice(21, 256).ToArray()), Filename = Utils.BtoString(bs.Slice(277).ToArray()) };
                case 4:
                    return new Message { PackID = Utils.BtoLong(bs.Slice(1, 8).ToArray()) };
                case 5:
                    return new Message { PackID = Utils.BtoLong(bs.Slice(1, 8).ToArray()), Data = bs.Slice(9).ToArray() };
                case 6:
                    return new Message { Text = Utils.BtoString(bs.Slice(1).ToArray()) };
                default:
                    throw new ArgumentException("Invalid protocol.");
            }
        }
        public static bool TryParse(byte[] src, out Message message)
        {
            try
            {
                message = Message.Parse(src);
                return true;
            }
            catch (ArgumentException)
            {
                message = new Message();
                return false;
            }
        }
        public byte[] ToBytes()
        {
            byte[] bs;
            switch (Type)
            {
                case MsgType.Info:
                    bs = new byte[1 + 4 + 3];
                    bs[0] = 0;
                    Array.Copy(IP.GetAddressBytes(), 0, bs, 1, 4);
                    Array.Copy(Utils.GetBytes(Pin, 3), 0, bs, 5, 3);
                    return bs;
                case MsgType.Key:
                    bs = new byte[Key.Length + 1];
                    bs[0] = 1;
                    Array.Copy(Utils.GetBytes(Key), 0, bs, 1, Key.Length);
                    return bs;
                case MsgType.Confirm:
                    bs = new byte[5 + SemiKey.Length];
                    bs[0] = 2;
                    Array.Copy(IP.GetAddressBytes(), 0, bs, 1, 4);
                    Array.Copy(Utils.GetBytes(SemiKey), 0, bs, 5, SemiKey.Length);
                    return bs;
                case MsgType.Meta:
                    bs = new byte[1 + 8 + 8 + 4 + 256 + Filename.Length];
                    bs[0] = 3;
                    Array.Copy(Utils.GetBytes(Size), 0, bs, 1, 8);
                    Array.Copy(Utils.GetBytes(PackSize), 0, bs, 9, 4);
                    Array.Copy(Utils.GetBytes(PackCount), 0, bs, 13, 8);
                    Array.Copy(Utils.GetBytes(Hash), 0, bs, 21, 256);
                    Array.Copy(Utils.GetBytes(Filename), 0, bs, 277, Filename.Length);
                    return bs;
                case MsgType.Continue:
                    bs = new byte[1 + 8];
                    bs[0] = 4;
                    Array.Copy(Utils.GetBytes(PackID), 0, bs, 1, 8);
                    return bs;
                case MsgType.File:
                    bs = new byte[1 + 8 + Data.Length];
                    bs[0] = 5;
                    Array.Copy(Utils.GetBytes(PackID), 0, bs, 1, 8);
                    Array.Copy(Data, 0, bs, 9, Data.Length);
                    return bs;
                case MsgType.Text:
                    bs = new byte[1 + Text.Length];
                    bs[0] = 6;
                    Array.Copy(Utils.GetBytes(Text), 0, bs, 1, Text.Length);
                    return bs;
                case MsgType.Invalid:
                    throw new ArgumentException("Message Invalid.");
                default:
                    throw new ArgumentException("Message Invalid.");
            }
        }
        public override string ToString()
        {
            switch (Type)
            {
                case MsgType.Info:
                    return $" (Obj) IP: {IP.ToString()}, Pin: {Pin}";
                case MsgType.Key:
                    return $" (Obj) Key: {Key}";
                case MsgType.Confirm:
                    return $" (Obj) IP: {IP.ToString()}, SemiKey: {SemiKey}";
                case MsgType.Meta:
                    return $" (Obj) Filename: {Filename}, Size: {Size} bytes, PackCount: {PackCount}, PackSize: {PackSize} bytes\n (Obj) Hash: {Hash}";
                case MsgType.Continue:
                    return $" (Obj) PackId: {PackID}";
                case MsgType.File:
                    return $" (Obj) PackID: {PackID}\n (Obj) Data: {Utils.ShowBytes(Data)}";
                case MsgType.Text:
                    return $" (Obj) Text: {Text}";
                case MsgType.Invalid:
                    return $" (Obj) InvalidMessage";
                default:
                    return $" (Obj) InvalidMessage";
            }
        }
    }
}
