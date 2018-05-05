﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Prime.Core;
using Prime.KeysManager.Messages;
using Prime.KeysManager.Utils;

namespace Prime.KeysManager.Transport
{
    public class TcpServer : ITcpServer
    {
        private TcpListener _listener;
        private readonly ConcurrentBag<TcpClient> _connectedClients = new ConcurrentBag<TcpClient>();
        private readonly ConcurrentDictionary<Type, Action<object, TcpClient>> _subscriptions = new ConcurrentDictionary<Type, Action<object, TcpClient>>();

        private readonly IDataProvider _dataProvider;

        public ILogger Logger { get; set; }

        public TcpServer(IDataProvider dataProvider, ILogger logger)
        {
            _dataProvider = dataProvider;

            Logger = logger;
        }

        private void Log(string text)
        {
            Logger.Log($"({typeof(TcpServer).Name}) : {text}");
        }
        
        public void StartServer(IPAddress address, short port)
        {
            _connectedClients.Clear();
            _listener = new TcpListener(address, port);
            _listener.Start();

            WaitForClient();
        }

        private void HandleResponse(object response, TcpClient clientSender)
        {
            var baseMessage = _dataProvider.Deserialize<BaseMessage>(response);

            var subscribedTypes = _subscriptions.Where(x =>
                x.Key.Name.Equals(baseMessage.Type, StringComparison.OrdinalIgnoreCase)).ToList();
            if (!subscribedTypes.Any())
                throw new NullReferenceException($"There is no handler registered for '{baseMessage.Type}' type.");

            var subscribedType = subscribedTypes.First();
            if (subscribedType.Value != null)
            {
                var parameter = _dataProvider.Deserialize(response, subscribedType.Key);
                subscribedType.Value(parameter, clientSender);
            }
            else
            {
                throw new NullReferenceException($"Subscribed handler for '{baseMessage.Type}' type is null.");
            }
        }

        private void WaitForClient()
        {
            _listener.BeginAcceptTcpClient(ClientAcceptedCallback, null);
        }

        private void ClientAcceptedCallback(IAsyncResult ar)
        {
            Task.Run(() =>
            {
                try
                {
                    using (var connectedClient = _listener.EndAcceptTcpClient(ar))
                    {
                        _connectedClients.Add(connectedClient);

                        using (var stream = connectedClient.GetStream())
                        {
                            while (connectedClient.Connected)
                            {
                                var data = _dataProvider.ReceiveData(stream);
                                
                                HandleResponse(data, connectedClient);
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    ExceptionOccurred?.Invoke(this, e);
                }
            });

            Log("Client connected");
            WaitForClient();
        }

        public void ShutdownServer()
        {
            _listener.Stop();
            Log("Disposing clients...");

            foreach (var connectedClient in _connectedClients)
            {
                connectedClient.Dispose();
            }

            Log("Server closed");
        }

        public void Subscribe<T>(Action<T, TcpClient> handler)
        {
            var t = typeof(T);
            if (_subscriptions.ContainsKey(t))
                throw new InvalidOperationException("Handler for specified type has already been added.");

            _subscriptions.TryAdd(t, (o, client) => handler((T)o, client));
        }

        public void Unsubscribe<T>()
        {
            var t = typeof(T);
            if (_subscriptions.ContainsKey(t))
                _subscriptions.TryRemove(t, out var action);
            else
                throw new InvalidOperationException("Handler for specified type does not exist.");
        }

        public void Send<T>(TcpClient client, T data)
        {
            _dataProvider.SendData(client, data);
        }

        public event EventHandler<Exception> ExceptionOccurred;
    }
}

