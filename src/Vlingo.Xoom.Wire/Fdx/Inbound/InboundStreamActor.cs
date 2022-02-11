// Copyright © 2012-2022 VLINGO LABS. All rights reserved.
//
// This Source Code Form is subject to the terms of the
// Mozilla Public License, v. 2.0. If a copy of the MPL
// was not distributed with this file, You can obtain
// one at https://mozilla.org/MPL/2.0/.

using System;
using Vlingo.Xoom.Actors;
using Vlingo.Xoom.Common;
using Vlingo.Xoom.Wire.Channel;
using Vlingo.Xoom.Wire.Message;
using Vlingo.Xoom.Wire.Nodes;

namespace Vlingo.Xoom.Wire.Fdx.Inbound;

public class InboundStreamActor: Actor, IInboundStream, IChannelReaderConsumer, IScheduled<object>
{
    private readonly AddressType _addressType;
    private ICancellable? _cancellable;
    private readonly IInboundStreamInterest _interest;
    private readonly long _probeInterval;
    private readonly IChannelReader _reader;

    public InboundStreamActor(
        IInboundStreamInterest interest,
        AddressType addressType,
        IChannelReader reader,
        long probeInterval)
    {
        _interest = interest;
        _addressType = addressType;
        _reader = reader;
        _probeInterval = probeInterval;
    }
        
    //=========================================
    // Scheduled
    //=========================================
        
    public void IntervalSignal(IScheduled<object> scheduled, object data)
    {
        _reader.ProbeChannel();
    }
        
    //=========================================
    // Startable
    //=========================================

    public override void Start()
    {
        if (IsStopped)
        {
            return;
        }
            
        Logger.Debug($"Inbound stream listening: for '{_reader.Name}'");
            
        try 
        {
            _reader.OpenFor(this);
        } 
        catch (Exception e)
        {
            _reader.Close();
            Logger.Error("OpenFor failed", e);
            throw new InvalidOperationException(e.Message, e);
        }
            
        _cancellable = Stage.Scheduler.Schedule(SelfAs<IScheduled<object?>>(), null, TimeSpan.FromMilliseconds(1000), TimeSpan.FromMilliseconds(_probeInterval));
    }
        
    //=========================================
    // Stoppable
    //=========================================

    public override void Stop()
    {
        if (_cancellable != null)
        {
            _cancellable.Cancel();
            _cancellable = null;
        }

        _reader?.Close();
            
        base.Stop();
    }
        
    //=========================================
    // InboundReaderConsumer
    //=========================================

    public void Consume(RawMessage message) => 
        _interest.HandleInboundStreamMessage(_addressType, RawMessage.Copy(message));
}