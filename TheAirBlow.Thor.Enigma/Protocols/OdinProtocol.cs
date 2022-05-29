// Copyright © TheAirBlow 2022 <theairblow.help@gmail.com>
// This Source Code Form is subject to the terms of the Mozilla Public
// License, v2.0. If a copy of the MPL was not distributed with this
// file, You can obtain one at http://mozilla.org/MPL/2.0/.

using System;
using System.IO;
using LibUsbDotNet;
using TheAirBlow.Thor.Enigma.Protocols.Odin;
using TheAirBlow.Thor.Enigma.Receivers;
using TheAirBlow.Thor.Enigma.Senders;

namespace TheAirBlow.Thor.Enigma.Protocols;

/// <summary>
/// Odin v2/v3 protocol
/// </summary>
public class OdinProtocol : Protocol
{
    private bool _done;
    
    /// <summary>
    /// Create a new instance of OdinProtocol
    /// </summary>
    /// <param name="writer">Writer</param>
    /// <param name="reader">Reader</param>
    public OdinProtocol(UsbEndpointWriter writer, 
        UsbEndpointReader reader) : base(writer, reader) { }

    /// <summary>
    /// Do a handshake and start a session
    /// </summary>
    public override bool Handshake()
    {
        var rec = Send(new StringSender("ODIN"), 
            new StringAck("LOKE"));
        if (rec != null) {
            _done = true;
            Send(new BasicCmdSender((int)PacketType.SessionStart, 
                    (int)SessionStart.BeginSession),
                new ByteAck((int)PacketType.SessionStart), true);
        }
        return rec != null;
    }

    /// <summary>
    /// Closes the session
    /// </summary>
    public override void Dispose()
    {
        if (_done) 
            Send(new BasicCmdSender((int)PacketType.SessionEnd, 
                    (int)SessionEnd.EndSession),
            new ByteAck((int)PacketType.SessionEnd), true);
    }

    /// <summary>
    /// Get Device Information
    /// </summary>
    /// <returns>Device Information</returns>
    public DeviceInfo GetDeviceInfo()
    {
        var res = (BasicCmdReceiver)Send(new BasicCmdSender(
                (int)PacketType.DeviceInfo, 0x00),
            new BasicCmdReceiver((int)PacketType.DeviceInfo), true);
        var size = res.Arguments[0];
        using var memory = new MemoryStream();
        for (var i = 0; i < Math.Floor((decimal)size / 500); i++) {
            var res2 = (RawByteBuffer)Send(new BasicCmdSender
                    ((int)PacketType.DeviceInfo, 0x01),
                new RawByteBuffer(), true);
            memory.Write(res2.Data);
        }
        
        Send(new BasicCmdSender((int)PacketType.DeviceInfo, 0x02),
            new BasicCmdReceiver((int)PacketType.DeviceInfo), true);

        return new DeviceInfo(memory.ToArray());
    }

    /// <summary>
    /// Dump PIT on device
    /// </summary>
    /// <param name="target">Target stream</param>
    public void DumpPit(Stream target)
    {
        var res = (BasicCmdReceiver)Send(new BasicCmdSender(
                (int)PacketType.PitXmit, (int)XmitShared.RequestDump),
            new BasicCmdReceiver((int)PacketType.PitXmit), true);
        var size = res.Arguments[0];
        for (var i = 0; i < Math.Floor((decimal)size / 500); i++) {
            var res2 = (RawByteBuffer)Send(new BasicCmdSender
                    ((int)PacketType.PitXmit, (int)XmitShared.Begin),
                new RawByteBuffer(), true);
            target.Write(res2.Data);
        }
        
        Send(new BasicCmdSender((int)PacketType.PitXmit, 
                (int)XmitShared.End),
            new BasicCmdReceiver((int)PacketType.PitXmit), true);
    }

    /// <summary>
    /// Reboot the device
    /// </summary>
    public void Reboot()
        => Send(new BasicCmdSender((int)PacketType.SessionEnd,
                (int)SessionEnd.Reboot),
            new ByteAck((int) PacketType.SessionEnd), true);
    
    /// <summary>
    /// Power off the device
    /// </summary>
    public void Shutdown()
        => Send(new BasicCmdSender((int)PacketType.SessionEnd,
                (int)SessionEnd.Shutdown),
            new ByteAck((int) PacketType.SessionEnd), true);
    
    /// <summary>
    /// Reboot into Odin
    /// </summary>
    public void OdinReboot()
        => Send(new BasicCmdSender((int)PacketType.SessionEnd,
                (int)SessionEnd.OdinReboot),
            new ByteAck((int) PacketType.SessionEnd), true);
}