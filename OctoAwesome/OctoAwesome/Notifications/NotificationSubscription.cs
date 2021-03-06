﻿using System;

namespace OctoAwesome.Notifications
{
    public sealed class NotificationSubscription : IDisposable
    {
        private INotificationObservable _observable;
        private INotificationObserver _observer;
        private readonly string _channel;

        public NotificationSubscription(INotificationObservable observable, INotificationObserver observer, string channel)
        {
            _observer = observer;
            _observable = observable;
            _channel = channel;
        }

        public void Dispose()
        {
            _observer.OnCompleted();
            _observable.Unsubscribe(_observer, _channel);
            _observable = null;
            _observer = null;
        }
    }
}
