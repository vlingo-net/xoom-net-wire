// Copyright © 2012-2018 Vaughn Vernon. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using Vlingo.Actors;
using Vlingo.Wire.Message;
using Vlingo.Wire.Node;

namespace Vlingo.Wire.Channel
{
    public class SocketChannelWriter
    {
        private Socket? _channel;
        private readonly Address _address;
        private readonly ILogger _logger;
        private readonly ManualResetEvent _sendDone;
        private readonly AutoResetEvent _readDone;
        private readonly AutoResetEvent _connectDone;
        private int _retries;

        public SocketChannelWriter(Address address, ILogger logger)
        {
            _address = address;
            _logger = logger;
            _channel = null;
            _sendDone = new ManualResetEvent(false);
            _readDone = new AutoResetEvent(false);
            _connectDone = new AutoResetEvent(false);
            _retries = 0;
        }

        public void Close()
        {
            if (IsClosed)
            {
                return;
            }
            
            if (_channel != null)
            {
                try
                {
                    _channel.Close();
                }
                catch (Exception e)
                {
                    _logger.Error($"Channel close failed because: {e.Message}", e);
                }
                finally
                {
                    IsClosed = true;
                }
            }

            _channel = null;
        }

        public int Write(RawMessage message, MemoryStream buffer)
        {
            buffer.Clear();
            message.CopyBytesTo(buffer);
            buffer.Flip();
            return Write(buffer);
        }

        public int Write(MemoryStream buffer)
        {
            while (_channel == null && _retries < 10)
            {
                _channel = PreparedChannel();
            }

            var totalBytesWritten = 0;
            if (_channel == null)
            {
                return totalBytesWritten;
            }

            try
            {
                if (_channel.IsSocketConnected())
                {
                    while (buffer.HasRemaining())
                    {
                        var bytes = new byte[buffer.Length];
                        buffer.BeginRead(bytes, 0, bytes.Length, ReadCallback, buffer);
                        _readDone.WaitOne();

                        totalBytesWritten += bytes.Length;
                        _channel.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, SendCallback, _channel);
                        // _sendDone.WaitOne();
                    }
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Write to channel failed because: {e.Message}", e);
                Close();
            }

            return totalBytesWritten;
        }
        
        public bool IsClosed { get; private set; }

        private void ReadCallback(IAsyncResult ar)
        {
            try
            {
                var ms = (MemoryStream) ar.AsyncState;
                ms.EndRead(ar);
                _readDone.Set();
            }
            catch (Exception e)
            {
                _logger.Error($"{this}: Failed to read from memory stream because: {e.Message}", e);
                Close();
            }
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                var channel = (Socket) ar.AsyncState;
                channel.EndSend(ar);
                // _sendDone.Set();
            }
            catch (Exception e)
            {
                _logger.Error($"{this}: Failed to send to channel because: {e.Message}", e);
                Close();
            }
        }

        public override string ToString() => $"SocketChannelWriter[address={_address}, channel={_channel}]";

        private Socket? PreparedChannel()
        {
            try
            {
                if (!IsClosed && _channel != null)
                {
                    if (_channel.Poll(10000, SelectMode.SelectWrite))
                    {
                        _retries = 0;
                        return _channel;
                    }

                    _logger.Info($"{this}: Closing socket...");
                    Close();
                }

                var channel = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                channel.BeginConnect(_address.HostName, _address.Port, ConnectCallback, channel);
                _connectDone.WaitOne();
                _retries = 0;
                return channel;
            }
            catch (Exception e)
            {
                ++_retries;
                _logger.Error($"{this}: Failed to prepare channel because: {e.Message}", e);
                Close();
            }

            return null;
        }

        private void ConnectCallback(IAsyncResult ar)
        {
            try
            {
                var channel = (Socket) ar.AsyncState;
                channel.EndConnect(ar);
                _logger.Debug($"{this}: Socket successfully connected to remote enpoint {channel.RemoteEndPoint}");
                IsClosed = false;
                _connectDone.Set();
            }
            catch (Exception e)
            {
                _logger.Error($"{this}: Failed to connect to channel because: {e.Message}", e);
                Close();
            }
        }
    }
}