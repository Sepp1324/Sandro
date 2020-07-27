﻿using System;

namespace OctoAwesome.Network
{
    public class Package
    {
        /// <summary>
        /// Bytesize of Header
        /// </summary>
        public const int HEAD_LENGTH = 11;
        public const int SUB_HEAD_LENGTH = HEAD_LENGTH + 10;
        public const int SUB_CONTENT_HEAD_LENGTH = 9;

        public static ulong NextUid => _nextUid++;

        private static ulong _nextUid;

        public PackageType Type { get; set; }

        public ushort Command { get; set; }

        public byte[] Payload { get; set; }

        public ulong Uid { get; set; }

        private byte[] _header;

        public Package(ushort command, int size, PackageType type = 0) : this()
        {
            Type = type;
            Command = command;
            Payload = new byte[size];
        }

        public Package()
        {

        }

        public Package(byte[] data) : this(0, data.Length)
        {

        }

        public void SerializePackage(OctoNetworkStream networkStream)
        {
            if (Payload.Length + _header.Length > networkStream.Length)
            {
                SerializeSubPackages(networkStream);
                return;
            }
            Type = PackageType.Normal;
            WriteHead(ref _header);
            networkStream.Write(_header, 0, _header.Length);
            networkStream.Write(Payload, 0, Payload.Length);
        }

        public void SerializeSubPackages(OctoNetworkStream networkStream)
        {
            var firstPackage = (int)networkStream.Length - SUB_HEAD_LENGTH;
            var contentPackage = (int)networkStream.Length - SUB_CONTENT_HEAD_LENGTH;
            var count = (int)Math.Round((double)((Payload.Length - firstPackage) / contentPackage), MidpointRounding.AwayFromZero);
            var offset = firstPackage;

            Type = PackageType.Subhead;
            WriteHead(ref _header);
            _header[8] = (byte)(count >> 8);
            _header[9] = (byte)(count & 0xFF);
            networkStream.Write(_header, 0, _header.Length);
            networkStream.Write(Payload, 0, firstPackage);
            Type = PackageType.Subcontent;

            for (int i = 0; i < count - 1; i++)
            {
                WriteHead(ref _header);
                networkStream.Write(_header, 0, _header.Length);
                networkStream.Write(Payload, offset, contentPackage);
                offset += contentPackage;
            }

            WriteHead(ref _header);
            networkStream.Write(_header, 0, _header.Length);
            networkStream.Write(Payload, offset, Payload.Length - offset);
        }

        public void DeserializePackage(OctoNetworkStream networkStream)
        {
            ReadHead(networkStream);

            if (Type == PackageType.Subhead)
            {
                DeserializeSubPackages(networkStream);
                return;
            }
            networkStream.Read(Payload, 0, Payload.Length);
        }

        public void DeserializeSubPackages(OctoNetworkStream networkStream)
        {
            var firstPackage = (int)networkStream.Length - SUB_HEAD_LENGTH;
            var contentPackage = (int)networkStream.Length - SUB_CONTENT_HEAD_LENGTH;

            networkStream.Read(Payload, 0, 2);

            var count = Payload[0] << 8 | Payload[1];
            var buffer = new byte[8];

            networkStream.Read(Payload, 0, firstPackage);

            Type = PackageType.Subcontent;

            var offset = firstPackage + contentPackage;

            for (int i = 0; i < count; i++)
            {
                ReadHead(networkStream);
                networkStream.Read(buffer, 0, 8);
                var uid = BitConverter.ToUInt64(buffer, 0);

                if (uid != Uid)
                    continue;

                networkStream.Read(Payload, offset, contentPackage);
                offset += contentPackage;
            }
            networkStream.Read(Payload, offset, Payload.Length - offset);
        }

        public void WriteHead(ref byte[] buffer, int offset = 0)
        {
            byte[] header, bytes;
            int index = offset;
            header = buffer;

            switch (Type)
            {
                case PackageType.Normal:
                    header = new byte[HEAD_LENGTH];
                    header[index] = (byte)Type;
                    header[index++] = (byte)(Command >> 8);
                    header[index++] = (byte)(Command & 0xFF);

                    bytes = BitConverter.GetBytes(Payload.LongLength);
                    Array.Copy(bytes, 0, header, index++, bytes.Length);
                    break;
                case PackageType.Subhead:
                    header = new byte[SUB_HEAD_LENGTH];
                    header[index] = (byte)Type;
                    header[index++] = (byte)(Command >> 8);
                    header[index++] = (byte)(Command & 0xFF);

                    bytes = BitConverter.GetBytes(Payload.LongLength);
                    Array.Copy(bytes, 0, header, index, bytes.Length);
                    index += bytes.Length;
                    bytes = BitConverter.GetBytes(NextUid);
                    Array.Copy(bytes, 0, header, index++, bytes.Length);
                    break;
                case PackageType.Subcontent:
                    header = new byte[SUB_CONTENT_HEAD_LENGTH];
                    header[index] = (byte)Type;

                    bytes = BitConverter.GetBytes(Uid);
                    Array.Copy(bytes, 0, header, index++, bytes.Length);
                    break;
                case PackageType.None:
                    break;
                default:
                    break;
            }
        }

        public void ReadHead(OctoNetworkStream networkStream)
        {
            var buffer = new byte[1];
            networkStream.Read(buffer, 0, 1);
            Type = (PackageType)buffer[0];

            switch (Type)
            {
                case PackageType.Normal:
                    buffer = new byte[HEAD_LENGTH - 1];

                    networkStream.Read(buffer, 0, buffer.Length);
                    Command = (ushort)(buffer[0] << 8 | buffer[1]);
                    Payload = new byte[BitConverter.ToUInt64(buffer, 2)];
                    break;
                case PackageType.Subhead:
                    buffer = new byte[SUB_HEAD_LENGTH - 1];

                    networkStream.Read(buffer, 0, buffer.Length);
                    Command = (ushort)(buffer[0] << 8 | buffer[1]);
                    Payload = new byte[BitConverter.ToUInt64(buffer, 2)];
                    Uid = BitConverter.ToUInt64(buffer, 10);
                    break;
                case PackageType.Subcontent:
                case PackageType.None:
                    break;
                default:
                    break;
            }
        }

        public enum PackageType : byte
        {
            None,
            Normal,
            Subhead,
            Subcontent
        }
    }

}