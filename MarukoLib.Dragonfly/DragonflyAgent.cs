using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using MarukoLib.Logging;

namespace MarukoLib.Dragonfly
{

    public class Message
    {

        public Message(global::Dragonfly.Message rawMessage) => RawMessage = rawMessage ?? throw new ArgumentNullException(nameof(rawMessage));

        public global::Dragonfly.Message RawMessage { get; }

        public int MessageType => RawMessage.msg_type;

        public T GetData<T>() where T : new() => global::Dragonfly.MessageExt.GetData<T>(RawMessage);

    }

    public class DragonflyAgent
    {

        private static readonly Logger Logger = Logger.GetLogger(typeof(DragonflyAgent));

        public interface IMessageHandler
        {

            IEnumerable<int> SupportedMessageTypes { get; }

            void Handle(Message message);

        }

        public abstract class MessageHandler : IMessageHandler
        {

            private class Basic : MessageHandler
            {

                private readonly Action<Message> _action;

                public Basic(Action<Message> action, params int[] messageTypes) : base(messageTypes) => _action = action;

                public override void Handle(Message message) => _action(message);

            }

            private readonly int[] _supportedMessageTypes;

            protected MessageHandler(params int[] messageTypes) => _supportedMessageTypes = (int[])messageTypes.Clone();

            public static MessageHandler Of(Action<Message> action, params int[] messageTypes) => new Basic(action, messageTypes);

            public IEnumerable<int> SupportedMessageTypes => _supportedMessageTypes;

            public abstract void Handle(Message message);

        }

        public abstract class TypedMessageHandler<T> : MessageHandler where T : new()
        {

            private class Basic : TypedMessageHandler<T>
            {

                private readonly Action<Message, T> _action;

                public Basic(Action<Message, T> action, params int[] messageTypes) : base(messageTypes) => _action = action;

                public override void Handle(Message message, T data) => _action(message, data);

            }

            protected TypedMessageHandler(params int[] messageTypes) : base(messageTypes) { }

            public static TypedMessageHandler<T> Of(Action<T> action, params int[] messageTypes) => Of((msg, val) => action(val), messageTypes);

            public static TypedMessageHandler<T> Of(Action<Message, T> action, params int[] messageTypes) => new Basic(action, messageTypes);

            public sealed override void Handle(Message message) => Handle(message, message.GetData<T>());

            public abstract void Handle(Message message, T data);

        }

        public event EventHandler Connected;

        public event EventHandler Disconnected;

        private readonly object _registrationLock = new object();

        private readonly object _moduleOperationLock = new object();

        private readonly global::Dragonfly.Module _module = new global::Dragonfly.Module();

        private readonly IDictionary<int, ICollection<IMessageHandler>> _handlers = new Dictionary<int, ICollection<IMessageHandler>>();

        private readonly short _moduleId;

        private readonly string _address;

        private volatile Thread _thread;

        public DragonflyAgent(short moduleId, string address)
        {
            _moduleId = moduleId;
            _address = address;
        }

        /// <summary>
        /// Add useLegacyV2RuntimeActivationPolicy="true" attribute for startup node to use the library.
        /// </summary>
        public static void UsingBuiltinLibrary()
        {
            AppDomain.CurrentDomain.AssemblyResolve += (sender, args) =>
            {
                if (!Equals(new AssemblyName(args.Name).Name, "Dragonfly.NET")) return null;
                var file = Path.GetTempFileName();
                using (var stream = new FileStream(file, FileMode.Create))
                {
                    var bytes = Properties.Resources.Dragonfly_NET;
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush();
                }
                return Assembly.LoadFile(file);
            };
        }

        public void AddMessageHandler(IMessageHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            lock (_registrationLock)
                foreach (var messageType in handler.SupportedMessageTypes)
                    if (!_handlers.ContainsKey(messageType))
                    {
                        var collection = new LinkedList<IMessageHandler>();
                        collection.AddLast(handler);
                        _handlers[messageType] = collection;
                        lock (_moduleOperationLock)
                            if (_module.IsConnected)
                                _module.Subscribe(messageType);
                    }
                    else
                    {
                        var collection = _handlers[messageType];
                        if (!collection.Contains(handler))
                            collection.Add(handler);
                    }
        }

        public void RemoveMessageHandler(IMessageHandler handler)
        {
            if (handler == null)
                throw new ArgumentNullException(nameof(handler));
            lock (_registrationLock)
                foreach (var messageType in handler.SupportedMessageTypes)
                    if (_handlers.ContainsKey(messageType))
                    {
                        var collection = _handlers[messageType];
                        if (collection.Remove(handler) && collection.Count <= 0)
                        {
                            _handlers.Remove(messageType);
                            lock (_moduleOperationLock)
                                if (_module.IsConnected)
                                    _module.Unsubscribe(messageType);
                        }
                    }
        }

        public void Connect()
        {
            lock (_moduleOperationLock)
                if (!_module.IsConnected)
                {
                    try
                    {
                        _module.ConnectToMMM(_moduleId, _address);
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Failed to connect to server: {_address}", e);
                    }
                    foreach (var type in _handlers.Keys)
                        _module.Subscribe(type);
                }
            (_thread = new Thread(AcquiringLoop)
            {
                IsBackground = true,
                Priority = ThreadPriority.AboveNormal
            }).Start();
            Connected?.Invoke(this, EventArgs.Empty);
        }

        public void Disconnect()
        {
            if (_thread.IsAlive)
                _thread.Interrupt();
            _thread = null;
            lock (_moduleOperationLock)
                if (_module.IsConnected)
                    _module.DisconnectFromMMM();
                else
                    return;
            Disconnected?.Invoke(this, EventArgs.Empty);
        }

        public bool Send(int type)
        {
            lock (_moduleOperationLock)
            {
                if (!_module.IsConnected)
                    return false;
                _module.SendSignal(type);
            }
            return true;
        }

        public bool Send(int type, object data)
        {
            lock (_moduleOperationLock)
            {
                if (!_module.IsConnected)
                    return false;
                _module.SendMessage(type, data);
            }
            return true;
        }

        private void AcquiringLoop()
        {
            var currentThread = Thread.CurrentThread;
            while (_thread == currentThread)
            {
                try
                {
                    global::Dragonfly.Message rawMessage;
                    lock (_moduleOperationLock)
                        rawMessage = _module.ReadMessage(0.5);
                    if (rawMessage == null || rawMessage.msg_type == -1)
                        continue;
                    var messageType = rawMessage.msg_type;
                    var message = new Message(rawMessage);
                    lock (_registrationLock)
                        if (_handlers.ContainsKey(messageType))
                            foreach (var handler in _handlers[messageType])
                                handler.Handle(message);
                        else
                            Console.WriteLine($"Message type without handler: #{rawMessage.msg_type}");
                }
                catch (ThreadInterruptedException) { break; }
                catch (Exception e)
                {
                    Logger.Warn("AcquiringLoop - connection lost", e, "moduleId", _moduleId, "address", _address);
                    lock (_moduleOperationLock)
                        _module.DisconnectFromMMM();
                    Disconnected?.Invoke(this, EventArgs.Empty);
                    if (!Reconnect(60, 2000))
                        Logger.Warn("AcquiringLoop - failed to reconnect", e, "moduleId", _moduleId, "address", _address);
                    return;
                }
            }
        }

        private bool Reconnect(int retry, int sleepWaitMillis)
        {
            for (; retry >= 0; retry--)
                try
                {
                    Connect();
                    Thread.Sleep(sleepWaitMillis);
                    return true;
                }
                catch (Exception e)
                {
                    Logger.Warn("Reconnect - on error", e, "moduleId", _moduleId, "address", _address);
                }
            return false;
        }

    }
}
