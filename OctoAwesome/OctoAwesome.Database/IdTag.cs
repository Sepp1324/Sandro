﻿using System;

namespace OctoAwesome.Database
{
    public struct IdTag : ITag
    {
        public int Tag { get; private set; }

        public int Length => sizeof(int);

        public IdTag(int id) => Tag = id;

        public byte[] GetBytes() => BitConverter.GetBytes(Tag);

        public void FromBytes(byte[] array, int startIndex) => Tag = BitConverter.ToInt32(array, startIndex);
    }
}