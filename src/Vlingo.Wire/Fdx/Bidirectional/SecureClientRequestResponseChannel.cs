// Copyright © 2012-2020 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using Vlingo.Actors;
using Vlingo.Common;
using Vlingo.Common.Pool;
using Vlingo.Wire.Channel;
using Vlingo.Wire.Message;
using Vlingo.Wire.Node;

namespace Vlingo.Wire.Fdx.Bidirectional
{
    public class SecureClientRequestResponseChannel : IClientRequestResponseChannel, IDisposable
    {
        private readonly Address _address;
        private Socket? _channel;
        private SslStream? _sslStream;
        private TcpClient? _tcpClient;
        private bool _closed;
        private readonly IResponseChannelConsumer _consumer;
        private readonly ILogger _logger;
        private int _previousPrepareFailures;
        private readonly ConsumerByteBufferPool _readBufferPool;
        private bool _disposed;
        private bool _canStartProbing;
        private readonly ManualResetEvent _connectDone;
        private readonly ManualResetEvent _authenticateDone;
        private readonly AutoResetEvent _sendDone;
        private readonly AutoResetEvent _receiveDone;

        public SecureClientRequestResponseChannel(
            Address address,
            IResponseChannelConsumer consumer,
            int maxBufferPoolSize,
            int maxMessageSize,
            ILogger logger)
        {
            _address = address;
            _consumer = consumer;
            _logger = logger;
            _closed = false;
            _channel = null;
            _previousPrepareFailures = 0;
            _readBufferPool = new ConsumerByteBufferPool(ElasticResourcePool<IConsumerByteBuffer, string>.Config.Of(maxBufferPoolSize), maxMessageSize);
            _connectDone = new ManualResetEvent(false);
            _authenticateDone = new ManualResetEvent(false);
            _sendDone = new AutoResetEvent(false);
            _receiveDone = new AutoResetEvent(false);
        }
        
        //=========================================
        // RequestSenderChannel
        //=========================================
        
        public void Close()
        {
            if (_closed)
            {
                return;
            }

            _closed = true;

            CloseChannel();
            Dispose(true);
        }

        public void RequestWith(byte[] buffer)
        {
            while (_sslStream == null && _previousPrepareFailures < 10)
            {
                _sslStream = PreparedChannel();
            }

            if (_sslStream != null)
            {
                try
                {
                    _sslStream.BeginWrite(buffer, 0, buffer.Length, SendCallback, _sslStream);
                    _sendDone.WaitOne();
                    _canStartProbing = true;
                }
                catch (Exception e)
                {
                    _logger.Error($"Write to socket failed because: {e.Message}", e);
                    CloseChannel();
                }
            }
        }

        //=========================================
        // ResponseListenerChannel
        //=========================================

        public void ProbeChannel()
        {
            if (_closed || !_canStartProbing)
            {
                return;
            }

            try
            {
                while (_sslStream == null && _previousPrepareFailures < 10)
                {
                    _sslStream = PreparedChannel();
                }
                if (_sslStream != null)
                {
                    ReadConsume(_sslStream);
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Failed to read channel selector for {_address} because: {e.Message}", e);
            }
        }
        
        //=========================================
        // Dispose
        //=========================================
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);  
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (_disposed)
            {
                return;
            }
      
            if (disposing) 
            {
                Close();
            }
      
            _disposed = true;
            _connectDone.Dispose();
            _authenticateDone.Dispose();
            _receiveDone.Dispose();
            _sendDone.Dispose();
            _sslStream?.Dispose();
        }

        //=========================================
        // internal implementation
        //=========================================

        private void CloseChannel()
        {
            if (_channel != null)
            {
                try
                {
                    _channel.Close();
                }
                catch (Exception e)
                {
                    _logger.Error($"Failed to close channel to {_address} because: {e.Message}", e);
                }
            }

            _channel = null;
        }

        private SslStream? PreparedChannel()
        {
            try
            {
                if (_channel != null)
                {
                    // if (_channel.IsSocketConnected())
                    // {
                        _previousPrepareFailures = 0;
                        return _sslStream;
                    // }
                    //
                    // CloseChannel();
                }
                else
                {
                    _tcpClient = new TcpClient();
                    _tcpClient.BeginConnect(_address.HostName, _address.Port, ConnectCallback, _tcpClient);
                    _connectDone.WaitOne();
                    _sslStream = new SslStream(_tcpClient.GetStream(), false, (sender, certificate, chain, errors) => true);
                    _sslStream.BeginAuthenticateAsClient(_address.HostName, AuthenticateCallback, _sslStream);
                    _authenticateDone.WaitOne();
                    _previousPrepareFailures = 0;
                    return _sslStream;
                }
            }
            catch (Exception e)
            {
                CloseChannel();
                var message = $"{GetType().Name}: Cannot prepare/open channel because: {e.Message}";
                if (_previousPrepareFailures == 0)
                {
                    _logger.Error(message, e);
                }
                else if (_previousPrepareFailures % 20 == 0)
                {
                    _logger.Info($"AGAIN: {message}");
                }
            }
            ++_previousPrepareFailures;
            return null;
        }

        private void ReadConsume(SslStream sslStream)
        {
            var pooledBuffer = _readBufferPool.Acquire();
            var readBuffer = pooledBuffer.ToArray();
            try
            {
                // Create the state object.  
                var state = new StateObject(sslStream, readBuffer, pooledBuffer);
                sslStream.BeginRead(readBuffer, 0, readBuffer.Length, ReceiveCallback, state);
                _receiveDone.WaitOne();
            }
            catch (Exception e)
            {
                _logger.Error("Cannot begin receiving on the channel", e);
                throw;
            }
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                var client = (TcpClient)ar.AsyncState;

                // Complete the connection.
                client.EndConnect(ar);

                _logger.Info($"Socket connected to {client.Client.RemoteEndPoint}");

                // Signal that the connection has been made.
                _connectDone.Set();
            }
            catch (Exception e)
            {
                _logger.Error("Cannot connect", e);
            }
        }
        
        private void AuthenticateCallback(IAsyncResult ar)
        {
            try
            {
                var sslStream = (SslStream)ar.AsyncState;

                sslStream.EndAuthenticateAsClient(ar);

                _logger.Info($"Authenticate succeeded");
                DisplaySecurityLevel(sslStream);
                DisplaySecurityServices(sslStream);
                DisplayCertificateInformation(sslStream);
                DisplayStreamProperties(sslStream);
                DisplayUsage();

                _authenticateDone.Set();
            }
            catch (Exception e)
            {
                _logger.Error("Cannot authenticate", e);
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                var sslStream = (SslStream)ar.AsyncState;

                sslStream.EndWrite(ar);

                _sendDone.Set();
            }
            catch (Exception e)
            {
                _logger.Error("Error while sending bytes", e);
            }
        }

        private void ReceiveCallback(IAsyncResult ar)
        {
            // Retrieve the state object and the client socket   
            // from the asynchronous state object.  
            var state = (StateObject)ar.AsyncState;
            var sslStream = state.SslStream;
            var pooledBuffer = state.PooledByteBuffer;
            var readBuffer = state.Buffer;

            try
            {
                // Read data from the remote device.  
                int bytesRead = sslStream.EndRead(ar);

                if (bytesRead > 0)
                {
                    // There might be more data, so store the data received so far.  
                    pooledBuffer.Put(readBuffer, 0, bytesRead);
                }
                
                if (_tcpClient!.Available > 0)
                {
                    // Get the rest of the data.  
                    sslStream.BeginRead(readBuffer,0,readBuffer.Length, ReceiveCallback, state);
                }
                else
                {
                    // All the data has arrived; put it in response.  
                    if (pooledBuffer.Limit() >= 1)
                    {
                        _consumer.Consume(pooledBuffer.Flip());
                    }
                    else
                    {
                        pooledBuffer.Release();
                    }

                    // Signal that all bytes have been received.  
                    _receiveDone.Set();
                }
            }
            catch (Exception e)
            {
                pooledBuffer.Release();
                _logger.Error("Error while receiving bytes", e);
                throw;
            }
        }
        
        private void DisplaySecurityLevel(SslStream stream)
        {
            _logger.Debug($"Cipher: {stream.CipherAlgorithm} strength {stream.CipherStrength}");
            _logger.Debug($"Hash: {stream.HashAlgorithm} strength {stream.HashStrength}");
            _logger.Debug($"Key exchange: {stream.KeyExchangeAlgorithm} strength {stream.KeyExchangeStrength}");
            _logger.Debug($"Protocol: {stream.SslProtocol}");
        }
        
        private void DisplaySecurityServices(SslStream stream)
        {
            _logger.Debug($"Is authenticated: {stream.IsAuthenticated} as server? {stream.IsServer}");
            _logger.Debug($"IsSigned: {stream.IsSigned}");
            _logger.Debug($"Is Encrypted: {stream.IsEncrypted}");
        }
        
        private void DisplayStreamProperties(SslStream stream)
        {
            _logger.Debug($"Can read: {stream.CanRead}, write {stream.CanWrite}");
            _logger.Debug($"Can timeout: {stream.CanTimeout}");
        }
        
        private void DisplayCertificateInformation(SslStream stream)
        {
            _logger.Debug($"Certificate revocation list checked: {stream.CheckCertRevocationStatus}");
                
            X509Certificate localCertificate = stream.LocalCertificate;
            if (stream.LocalCertificate != null)
            {
                _logger.Debug(
                    $"Local cert was issued to {localCertificate.Subject} and is valid from {localCertificate.GetEffectiveDateString()} until {localCertificate.GetExpirationDateString()}.");
            }
            else
            {
                _logger.Debug("Local certificate is null.");
            }
            // Display the properties of the client's certificate.
            X509Certificate remoteCertificate = stream.RemoteCertificate;
            
            if (stream.RemoteCertificate != null)
            {
                _logger.Debug($"Remote cert was issued to {remoteCertificate?.Subject} and is valid from {remoteCertificate?.GetEffectiveDateString()} until {remoteCertificate?.GetExpirationDateString()}.");
            }
            else
            {
                _logger.Debug("Remote certificate is null.");
            }
        }
        
        private void DisplayUsage()
        { 
            _logger.Debug("To start the server specify:");
            _logger.Debug("serverSync certificateFile.cer");
        }

        private class StateObject
        {
            public StateObject(SslStream sslStream, byte[] buffer, IConsumerByteBuffer pooledByteBuffer)
            {
                SslStream = sslStream;
                Buffer = buffer;
                PooledByteBuffer = pooledByteBuffer;
            }
            
            public SslStream SslStream { get; }

            public byte[] Buffer { get; }

            public IConsumerByteBuffer PooledByteBuffer { get; }
        }
    }
}