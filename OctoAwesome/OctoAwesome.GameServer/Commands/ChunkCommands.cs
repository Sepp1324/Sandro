﻿using System;
using System.IO;
using CommandManagementSystem.Attributes;

namespace OctoAwesome.GameServer.Commands
{
    public class ChunkCommands
    {
        [Command((ushort) OfficialCommands.LoadColumn)]
        public static byte[] LoadColumn(byte[] data)
        {
            Guid guid;
            int planetId;
            Index2 index2;

            using (var memoryStream = new MemoryStream(data))
            using (var reader = new BinaryReader(memoryStream))
            {
                guid = new Guid(reader.ReadBytes(16));
                planetId = reader.ReadInt32();
                index2 = new Index2(reader.ReadInt32(), reader.ReadInt32());
            }

            var column = Program.ServerHandler.SimulationManager.LoadColumn(guid, planetId, index2);

            using (var memoryStream = new MemoryStream())
            using (var writer = new BinaryWriter(memoryStream))
            {
                column.Serialize(writer, Program.ServerHandler.SimulationManager.DefinitionManager);
                return memoryStream.ToArray();
            }
        }
    }
}