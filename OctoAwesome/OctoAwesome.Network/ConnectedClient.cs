﻿using OctoAwesome.Network.Pooling;
using OctoAwesome.Notifications;
using OctoAwesome.Serialization;
using System;
using System.Net.Sockets;

namespace OctoAwesome.Network
{
    public sealed class ConnectedClient : BaseClient, INotificationObserver
    {
        public IDisposable NetworkChannelSubscription { get; set; }

        public IDisposable ServerSubscription { get; set; }

        private readonly PackagePool _packagePool;

        public ConnectedClient(Socket socket) : base(socket) => _packagePool = TypeContainer.Get<PackagePool>();

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
        {
            Socket.Close();
            throw error;
        }

        public void OnNext(Notification value)
        {
            if (value.SenderId == Id)
                return;

            OfficialCommand command;
            byte[] payload;

            switch (value)
            {
                case EntityNotification entityNotification:
                    command = OfficialCommand.EntityNotification;
                    payload = Serializer.Serialize(entityNotification);
                    break;
                case BlockChangedNotification chunkNotification:
                    command = OfficialCommand.ChunkNotification;
                    payload = Serializer.Serialize(chunkNotification);
                    break;
                default:
                    return;
            }

            BuildAndSendPackage(payload, command);
        }

        private void BuildAndSendPackage(byte[] data, OfficialCommand officialCommand)
        {
            var package = _packagePool.Get();
            package.Payload = data;
            package.Command = (ushort)officialCommand;
            SendPackageAndRelase(package);
        }
    }
}