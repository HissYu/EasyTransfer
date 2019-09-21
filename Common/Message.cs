using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace Common
{
    #region Message Structs
    interface IMessageInfo
    {
        int Pin { get; set; }
    }
    interface IMessageKey
    {
        string Key { get; set; }
    }
    interface IMessageConfirm
    {
        string SemiKey { get; set; }
    }
    interface IMessageMeta
    {
        long Size { get; set; }
        int PackSize { get; set; }
        long PackCount { get; set; }
        byte[] Hash { get; set; }
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

    public class Message : IMessageConfirm, IMessageContinue, IMessageFile, IMessageInfo, IMessageKey, IMessageMeta, IMessageText
    {
        public string Text { get; set; }
        public long Size { get; set; }
        public int PackSize { get; set; }
        public long PackCount { get; set; }
        public byte[] Hash { get; set; }
        public string Filename { get; set; }
        public string Key { get; set; }
        public int Pin { get; set; }
        public long PackID { get; set; }
        public byte[] Data { get; set; }
        public string SemiKey { get; set; }

        public MsgType Type
        {
            get =>
                SemiKey != null ? MsgType.Confirm :
                Pin != 0 ? MsgType.Info :
                Key != null ? MsgType.Key :
                PackID != 0 ?
                    (Data != null ? MsgType.File : MsgType.Continue) :
                Text != null ? MsgType.Text :
                (Filename != null && Size != 0 && Hash != null && PackSize != 0 && PackCount != 0) ? MsgType.Meta : MsgType.Invalid;
        }

        public static Message Parse(byte[] src)
        {
            //Span<byte> bs = src;
            // Codes fucking ugly

            switch (src[0])
            {
                case 0:
                    //return new Message { Pin = (int)Utils.BtoNum(bs.Slice(1, 3).ToArray(), 3) };
                    return new Message { Pin = (int)Utils.BtoNum(Utils.BytesSlice(src, 1, 3), 3) };
                case 1:
                    //return new Message { Key = Utils.BtoString(bs.Slice(1).ToArray()) };
                    return new Message { Key = Utils.BtoString(Utils.BytesSlice(src, 1)) };
                case 2:
                    //return new Message { SemiKey = Utils.BtoString(bs.Slice(1).ToArray()) };
                    return new Message { SemiKey = Utils.BtoString(Utils.BytesSlice(src, 1)) };
                case 3:
                    //return new Message { Size = Utils.BtoLong(bs.Slice(1, 8).ToArray()), PackSize = Utils.BtoInt(bs.Slice(9, 4).ToArray()), PackCount = Utils.BtoLong(bs.Slice(13, 8).ToArray()), Hash = bs.Slice(21, 256).ToArray(), Filename = Utils.BtoString(bs.Slice(277).ToArray()) };
                    return new Message { Size = Utils.BtoLong(Utils.BytesSlice(src, 1, 8)), PackSize = Utils.BtoInt(Utils.BytesSlice(src, 9, 4)), PackCount = Utils.BtoLong(Utils.BytesSlice(src, 13, 8)), Hash = Utils.BytesSlice(src, 21, 256), Filename = Utils.BtoString(Utils.BytesSlice(src, 277)) };
                case 4:
                    //return new Message { PackID = Utils.BtoLong(bs.Slice(1, 8).ToArray()) };
                    return new Message { PackID = Utils.BtoLong(Utils.BytesSlice(src, 1, 8)) };
                case 5:
                    //return new Message { PackID = Utils.BtoLong(bs.Slice(1, 8).ToArray()), Data = bs.Slice(9).ToArray() };
                    return new Message { PackID = Utils.BtoLong(Utils.BytesSlice(src, 1, 8)), Data = Utils.BytesSlice(src, 9) };
                case 6:
                    //return new Message { Text = Utils.BtoString(bs.Slice(1).ToArray()) };
                    return new Message { Text = Utils.BtoString(Utils.BytesSlice(src, 1)) };
                default:
                    throw new ArgumentException("Invalid protocol.");
            }
        }
        public static Message CreateMeta(string filename, int packSize)
        {
            //data = File.ReadAllBytes(filename);
            byte[] hash = new byte[256];
            byte[] t;
            using (SHA256 s = SHA256.Create())
            using (FileStream fs = new FileStream(filename, FileMode.Open, FileAccess.Read))
            {
                t = s.ComputeHash(fs);
                Array.Copy(t, 0, hash, 0, t.Length);
                return new Message { Filename = filename, Size = fs.Length, PackSize = packSize, PackCount = (long)Math.Ceiling((double)fs.Length / packSize), Hash = hash };
            }
        }
        public static Message CreateTextMeta(int textLength) => new Message { Filename = "", Size = textLength, PackSize = -1, Hash = new byte[256], PackCount = -1 };
        public static bool IsText(Message message) => message.Filename == "" && message.PackSize == -1 && message.PackCount == -1 && message.Hash[0] == 0;
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
                    bs = new byte[1 + 3];
                    bs[0] = 0;
                    Array.Copy(Utils.GetBytes(Pin, 3), 0, bs, 1, 3);
                    return bs;
                case MsgType.Key:
                    bs = new byte[Key.Length + 1];
                    bs[0] = 1;
                    Array.Copy(Utils.GetBytes(Key), 0, bs, 1, Key.Length);
                    return bs;
                case MsgType.Confirm:
                    bs = new byte[1 + SemiKey.Length];
                    bs[0] = 2;
                    Array.Copy(Utils.GetBytes(SemiKey), 0, bs, 1, SemiKey.Length);
                    return bs;
                case MsgType.Meta:
                    bs = new byte[1 + 8 + 8 + 4 + 256 + Filename.Length];
                    bs[0] = 3;
                    Array.Copy(Utils.GetBytes(Size), 0, bs, 1, 8);
                    Array.Copy(Utils.GetBytes(PackSize), 0, bs, 9, 4);
                    Array.Copy(Utils.GetBytes(PackCount), 0, bs, 13, 8);
                    Array.Copy(Hash, 0, bs, 21, 256);
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
                    return $" (Obj) Pin: {Pin}";
                case MsgType.Key:
                    return $" (Obj) Key: {Key}";
                case MsgType.Confirm:
                    return $" (Obj) SemiKey: {SemiKey}";
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
