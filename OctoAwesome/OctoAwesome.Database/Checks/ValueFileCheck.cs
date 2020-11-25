﻿using System;
using System.IO;

namespace OctoAwesome.Database.Checks
{
    public sealed class ValueFileCheck<TTag> where TTag : ITag, new()
    {
        private readonly FileInfo fileInfo;

        public ValueFileCheck(FileInfo fileInfo) => this.fileInfo = fileInfo;

        public bool Check()
        {
            using (var fileStream = fileInfo.Open(FileMode.Open, FileAccess.Read, FileShare.None))
            {
                var keyBuffer = new byte[Key<TTag>.KEY_SIZE];

                do
                {
                    fileStream.Read(keyBuffer, 0, keyBuffer.Length);

                    var key = Key<TTag>.FromBytes(keyBuffer, 0);

                    if (!key.Validate())
                        throw new CheckFailedException($"Key is not valid", fileStream.Position);

                    if (key.Index != fileStream.Position - Key<TTag>.KEY_SIZE)
                        return false;

                    int length;

                    if (key.IsEmpty)
                    {
                        var intBuffer = new byte[sizeof(int)];

                        fileStream.Read(intBuffer, 0, sizeof(int));
                        length = BitConverter.ToInt32(intBuffer, 0) - sizeof(int);
                    }
                    else
                    {
                        length = key.ValueLength;
                    }

                    fileStream.Seek(length, SeekOrigin.Current);

                } while (fileStream.Position != fileStream.Length);
            }
            return true;
        }
    }
}
