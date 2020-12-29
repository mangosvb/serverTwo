'
' Copyright (C) 2013-2021 getMaNGOS <https://getMangos.eu>
'
' This program is free software; you can redistribute it and/or modify
' it under the terms of the GNU General Public License as published by
' the Free Software Foundation; either version 2 of the License, or
' (at your option) any later version.
'
' This program is distributed in the hope that it will be useful,
' but WITHOUT ANY WARRANTY; without even the implied warranty of
' MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
' GNU General Public License for more details.
'
' You should have received a copy of the GNU General Public License
' along with this program; if not, write to the Free Software
' Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
'
Imports System.Threading
Imports System.Net.Sockets
Imports System.Xml.Serialization
Imports System.IO
Imports System.Net
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports mangosVB.Common.BaseWriter
Imports mangosVB.Common

Public Module WC_Handlers_Misc

    Public Sub On_CMSG_QUERY_TIME(ByRef packet As PacketClass, ByRef Client As ClientClass)
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_QUERY_TIME", Client.IP, Client.Port)
        Dim response As New PacketClass(OPCODES.SMSG_QUERY_TIME_RESPONSE)
        response.AddInt32(GetTimestamp(Now))
        Client.Send(response)
        response.Dispose()
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] SMSG_QUERY_TIME_RESPONSE", Client.IP, Client.Port)
    End Sub
    Public Sub On_CMSG_KEEP_ALIVE(ByRef packet As PacketClass, ByRef Client As ClientClass)
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_KEEP_ALIVE", Client.IP, Client.Port)
    End Sub
    Public Sub On_CMSG_WARDEN_DATA(ByRef packet As PacketClass, ByRef Client As ClientClass)
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_WARDEN_DATA", Client.IP, Client.Port)
    End Sub
    Public Sub On_CMSG_TIME_SYNC_RESP(ByRef packet As PacketClass, ByRef Client As ClientClass)
        packet.GetInt16()
        Dim c As UInteger = packet.GetUInt32
        Dim t As Integer = packet.GetInt32

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_TIME_SYNC_RESP [{2}, {3}, {4}ms]", Client.IP, Client.Port, c, t, timeGetTime - t)
    End Sub

    Public Sub On_CMSG_NEXT_CINEMATIC_CAMERA(ByRef packet As PacketClass, ByRef Client As ClientClass)
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_NEXT_CINEMATIC_CAMERA", Client.IP, Client.Port)
    End Sub
    Public Sub On_CMSG_COMPLETE_CINEMATIC(ByRef packet As PacketClass, ByRef Client As ClientClass)
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_COMPLETE_CINEMATIC", Client.IP, Client.Port)
    End Sub
    Public Sub On_CMSG_SET_TAXI_BENCHMARK_MODE(ByRef packet As PacketClass, ByRef Client As ClientClass)
        packet.GetInt16()
        Dim Mode As Byte = packet.GetInt8
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_SET_TAXI_BENCHMARK_MODE [{2}]", Client.IP, Client.Port, Mode)
    End Sub

    Public Sub On_CMSG_PLAYED_TIME(ByRef packet As PacketClass, ByRef Client As ClientClass)
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_PLAYED_TIME", Client.IP, Client.Port)

        Dim response As New PacketClass(OPCODES.SMSG_PLAYED_TIME)
        response.AddInt32(1)
        response.AddInt32(1)
        Client.Send(response)
        response.Dispose()
    End Sub
    Public Sub On_CMSG_INSPECT(ByRef packet As PacketClass, ByRef Client As ClientClass)
        packet.GetInt16()
        Dim GUID As ULong = packet.GetUInt64
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_INSPECT [{GUID={2:X}}]", Client.IP, Client.Port, GUID)
    End Sub

    Public Sub On_MSG_MOVE_HEARTBEAT(ByRef packet As PacketClass, ByRef Client As ClientClass)
        Try
            Client.Character.GetWorld.ClientPacket(Client.Index, packet.Data)
        Catch
            WS.Disconnect("NULL", New Integer() {Client.Character.Map})
            Exit Sub
        End Try

        'DONE: Save location on cluster
        Client.Character.PositionX = packet.GetFloat(15)
        Client.Character.PositionY = packet.GetFloat
        Client.Character.PositionZ = packet.GetFloat

        'DONE: Sync your location to other party / raid members
        If Client.Character.IsInGroup Then
            Dim statsPacket As New PacketClass(OPCODES.MSG_NULL_ACTION)
            statsPacket.Data = Client.Character.GetWorld.GroupMemberStats(Client.Character.GUID, PartyMemberStatsFlag.GROUP_UPDATE_FLAG_POSITION + PartyMemberStatsFlag.GROUP_UPDATE_FLAG_ZONE)
            Client.Character.Group.BroadcastToOutOfRange(statsPacket, Client.Character)
            statsPacket.Dispose()
        End If
    End Sub
    Public Sub On_CMSG_CANCEL_TRADE(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If Client.Character IsNot Nothing AndAlso Client.Character.IsInWorld Then
            Try
                Client.Character.GetWorld.ClientPacket(Client.Index, packet.Data)
            Catch
                WS.Disconnect("NULL", New Integer() {Client.Character.Map})
            End Try
        Else
            Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_CANCEL_TRADE", Client.IP, Client.Port)
        End If
    End Sub

End Module