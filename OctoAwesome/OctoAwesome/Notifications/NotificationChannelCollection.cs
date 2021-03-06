﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace OctoAwesome.Notifications
{
    public class NotificationChannelCollection : IEnumerable<KeyValuePair<string, ObserverHashSet>>
    {
        public ObserverHashSet this[string channel] => _internalDictionary[channel];

        public ICollection<string> Channels => _internalDictionary.Keys;

        public int Count => _internalDictionary.Count;

        public Dictionary<string, ObserverHashSet>.ValueCollection Values => _internalDictionary.Values;

        private readonly Dictionary<string, ObserverHashSet> _internalDictionary;
        private readonly SemaphoreSlim _addSemaphore;

        public NotificationChannelCollection()
        {
            _internalDictionary = new Dictionary<string, ObserverHashSet>();
            _addSemaphore = new SemaphoreSlim(1,1);
        }

        public void Add(string channel, INotificationObserver value)
        {
            _addSemaphore.Wait();
            if (_internalDictionary.TryGetValue(channel, out ObserverHashSet hashset))
            {
                using (hashset.Wait())
                    hashset.Add(value);
            }
            else
            {
                _internalDictionary.Add(channel, new ObserverHashSet { value });
            }
            _addSemaphore.Release();
        }

        public void Clear() => _internalDictionary.Clear();

        public bool Contains(INotificationObserver item) => _internalDictionary.Values.Any(i => i == item);
        public bool Contains(string key) => _internalDictionary.ContainsKey(key);

        public Dictionary<string, ObserverHashSet>.Enumerator GetEnumerator() => _internalDictionary.GetEnumerator();

        public bool Remove(string key) => _internalDictionary.Remove(key);
      
        public bool Remove(INotificationObserver item)
        {
            var returnValue = false;

            foreach (ObserverHashSet hashSet in _internalDictionary.Values)
            {
                using (hashSet.Wait())
                    returnValue = returnValue ? returnValue : hashSet.Remove(item);
            }

            return returnValue;
        }

        public bool Remove(string key, INotificationObserver item)
        {
            var hashSet = _internalDictionary[key];
            bool returnValue;

            using (hashSet.Wait())
                returnValue = hashSet.Remove(item);

            return returnValue;
        }

        public bool TryGetValue(string key, out ObserverHashSet value) => _internalDictionary.TryGetValue(key, out value);

        IEnumerator IEnumerable.GetEnumerator() => _internalDictionary.GetEnumerator();
        IEnumerator<KeyValuePair<string, ObserverHashSet>> IEnumerable<KeyValuePair<string, ObserverHashSet>>.GetEnumerator() => _internalDictionary.GetEnumerator();
    }
}
