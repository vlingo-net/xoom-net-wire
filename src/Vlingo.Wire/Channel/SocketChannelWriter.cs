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
        private Socket _channel;
        private readonly Address _address;
        private readonly ILogger _logger;
        private readonly ManualResetEvent _sendManualResetEvent;
        private readonly ManualResetEvent _readManualResetEvent;

        public SocketChannelWriter(Address address, ILogger logger)
        {
            _address = address;
            _logger = logger;
            _channel = null;
            _sendManualResetEvent = new ManualResetEvent(false);
            _readManualResetEvent = new ManualResetEvent(false);
        }

        public void Close()
        {
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
            _channel = PreparedChannel();

            var totalBytesWritten = 0;
            if (_channel == null)
            {
                return totalBytesWritten;
            }

            try
            {
                while (buffer.HasRemaining())
                {
                    var bytes = new byte[buffer.Length];
                    var readResult = buffer.BeginRead(bytes, 0, bytes.Length, new AsyncCallback(ReadCallback), buffer);
                    _readManualResetEvent.WaitOne();

                    var ms = (MemoryStream)readResult.AsyncState;
                    totalBytesWritten += ms.ToArray().Length;

                    _channel.BeginSend(bytes, 0, bytes.Length, SocketFlags.None, new AsyncCallback(SendCallback), _channel);
                    _sendManualResetEvent.WaitOne();

                    _readManualResetEvent.Reset();
                    _sendManualResetEvent.Reset();
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Write to channel failed because: {e.Message}", e);
                Close();
            }

            return totalBytesWritten;
        }

        private void ReadCallback(IAsyncResult ar)
        {
            try
            {
                var ms = (MemoryStream)ar.AsyncState;
                ms.EndRead(ar);
                _readManualResetEvent.Set();
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
                var channel = (Socket)ar.AsyncState;
                channel.EndSend(ar);
                _sendManualResetEvent.Set();
            }
            catch (Exception e)
            {
                _logger.Error($"{this}: Failed to send to channel because: {e.Message}", e);
                Close();
            }
        }

        public override string ToString() => $"SocketChannelWriter[address={_address}, channel={_channel}]";

        private Socket PreparedChannel()
        {
            try
            {
                if (_channel != null)
                {
                    if (_channel.Poll(10000, SelectMode.SelectWrite))
                    {
                        return _channel;
                    }

                    Close();
                }

                var channel = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                channel.Connect(_address.HostName, _address.Port); // TODO: This could be async but it is now blocking
                return channel;
            }
            catch (Exception e)
            {
                _logger.Error($"{this}: Failed to prepare channel because: {e.Message}", e);
                Close();
            }

            return null;
        }
    }
}