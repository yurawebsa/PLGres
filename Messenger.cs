using System;
using System.Collections.Generic;

namespace CounterPlg {
    public class SelectFileMessage { }

    public class LogActionMessage {
        public string Title { get; }
        public string Icon { get; }

        public LogActionMessage(string title, string icon)
        {
            Title = title;
            Icon = icon;
        }
    }

    public class Messenger {
        private static readonly Messenger _instance = new Messenger();
        public static Messenger Instance => _instance;

        private readonly Dictionary<Type, List<object>> _handlers = new Dictionary<Type, List<object>>();

        private Messenger() { }

        public void Register<T>(object recipient, Action<T> action)
        {
            var messageType = typeof(T);
            if (!_handlers.ContainsKey(messageType))
            {
                _handlers[messageType] = new List<object>();
            }
            _handlers[messageType].Add(new WeakReference(recipient, action));
        }

        public void Send<T>(T message)
        {
            if (_handlers.TryGetValue(typeof(T), out var handlerList))
            {
                var handlersToRemove = new List<object>();
                foreach (var handlerRef in handlerList)
                {
                    if (handlerRef is WeakReference weakRef && weakRef.IsAlive)
                    {
                        if (weakRef.Target is Action<T> action)
                        {
                            action(message);
                        }
                    }
                    else
                    {
                        handlersToRemove.Add(handlerRef);
                    }
                }

                foreach (var handler in handlersToRemove)
                {
                    handlerList.Remove(handler);
                }
            }
        }

        private class WeakReference {
            private readonly System.WeakReference _recipient;
            private readonly Delegate _action;

            public WeakReference(object recipient, Delegate action)
            {
                _recipient = new System.WeakReference(recipient);
                _action = action;
            }

            public bool IsAlive => _recipient.IsAlive;

            public object Target => _action;
        }
    }
}