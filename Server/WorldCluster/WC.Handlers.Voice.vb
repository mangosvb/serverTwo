'
' Copyright (C) 2013-2019 getMaNGOS <https://getMangos.eu>
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

Public Module WC_Handlers_Voice

    Private VOICE_CHANNEL_ID As ULong = &H4BC500000000D1E1UL

    Public Sub SendVoiceSystemStatus(ByRef Client As ClientClass, ByRef Character As CharacterObject)
        Dim p As New PacketClass(OPCODES.SMSG_FEATURE_SYSTEM_STATUS)
        p.AddInt8(2)                         'unk

        'enable(1)/disable(0) voice chat interface in client
        If VOICE_SERVER Is Nothing Then
            p.AddInt8(0)
        Else
            p.AddInt8(1)
        End If

        Client.Send(p)
        p.Dispose()
    End Sub
    Public Sub SendVoiceSystemRoster(ByRef Client As ClientClass)
        'NOTE: Client is smart and won't send to VoiceServer if it is alone in the channel
        'NOTE: User Flags
        '           Flags1: 0x01 = Leader, 0x40 = ?, 0x06 = Player
        '           Flags2: 0x80 = ?, 0x0C = ?

        Dim p As New PacketClass(OPCODES.SMSG_VOICE_SESSION_ROSTER_UPDATE)
        p.AddUInt64(VOICE_CHANNEL_ID)               'Channel ID
        p.AddUInt16(1)                              'Voice Channel ID
        p.AddInt8(0)                                'Channel Type (0=channel,2=party)
        p.AddString("Test Channel")
        p.AddByteArray(VOICE_SERVER_EncryptionKey)  'Encryption key (16 bytes)
        p.AddUInt32(VOICE_SERVER_Host)              'VS Host, these dont appear to be in network byte order
        p.AddUInt16(VOICE_SERVER_Port)              'VS Port

        p.AddInt8(2)                                'User Count

        'Add ourself as user #0
        p.AddUInt64(Client.Character.GUID)          'Char GUID
        p.AddInt8(&HE)                              'Char Flags1
        p.AddInt8(&H6)                              'Char Flags2

        'Add others
        'For Each c As CharacterObject In Members
        '    p.AddUInt64(c.GUID)            'User GUID
        '    p.AddInt8(i)                   'User ID (Zero Based)
        '    p.AddInt8(&H80)                'User Flags1
        '    p.AddInt8(&H46)                'User Flags2
        'Next

        p.AddUInt64(&HFFFF)         'Debug GUID
        p.AddInt8(&H80)             'Debug Flags1
        p.AddInt8(&H46)             'Debug Flags2

        Client.Send(p)
        p.Dispose()
    End Sub

    Public Sub On_CMSG_VOICE_SESSION_ENABLE(ByRef packet As PacketClass, ByRef Client As ClientClass)
        packet.GetInt16()
        Dim isVoiceEnabled As Byte = packet.GetInt8
        Dim isMicrophoneEnabled As Byte = packet.GetInt8

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_VOICE_SESSION_ENABLE [Voice: {2}, Microphone: {3}]", Client.IP, Client.Port, isVoiceEnabled, isMicrophoneEnabled)

        If Client.Character IsNot Nothing Then
            'Only ingame chat possible

            'If isVoiceEnabled Then
            '    Dim response As New PacketClass(OPCODES.SMSG_AVAILABLE_VOICE_CHANNEL)
            '    response.AddUInt64(VOICE_CHANNEL_ID)            'Channel ID
            '    response.AddInt8(0)                             'Type (00=custom channel, 03=party, 04=raid?)
            '    packet.AddString("Test Channel")                'Name (not applicable to party/raid)
            '    response.AddUInt64(Client.Character.GUID)       'Player GUID
            '    Client.Send(response)
            '    response.Dispose()
            'Else
            '    Dim response As New PacketClass(OPCODES.SMSG_VOICE_SESSION_LEAVE)
            '    response.AddUInt64(Client.Character.GUID)       'Player GUID
            '    response.AddUInt64(VOICE_CHANNEL_ID)            'Channel ID
            '    Client.Send(response)
            '    response.Dispose()
            'End If
        End If

    End Sub
    Public Sub On_CMSG_SET_ACTIVE_VOICE_CHANNEL(ByRef packet As PacketClass, ByRef Client As ClientClass)
        packet.GetInt16()
        Dim Type As VoiceChannelType = packet.GetUInt32
        Dim Channel As String = packet.GetString

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_SET_ACTIVE_VOICE_CHANNEL [{2}{3}]", Client.IP, Client.Port, Type, ":" & Channel)

        If Type <> VoiceChannelType.NONE Then
            If Type <> VoiceChannelType.PARTY Then
                If Type <> VoiceChannelType.CHANNEL Then
                    If CHAT_CHANNELs.ContainsKey(Channel) And TypeOf CHAT_CHANNELs(Channel) Is VoiceChatChannelClass Then
                        CType(CHAT_CHANNELs(Channel), VoiceChatChannelClass).VoiceUpdate(Client.Character)
                    End If
                End If
            End If
        End If
    End Sub

    Public Sub On_CMSG_CHANNEL_VOICE_ON(ByRef packet As PacketClass, ByRef Client As ClientClass)
        packet.GetInt16()
        Dim ChannelName As String = packet.GetString

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_CHANNEL_VOICE_ON [{2}]", Client.IP, Client.Port, ChannelName)

        ChannelName = ChannelName.ToUpper
        If CHAT_CHANNELs.ContainsKey(ChannelName) AndAlso (TypeOf CHAT_CHANNELs(ChannelName) Is VoiceChatChannelClass) Then
            CType(CHAT_CHANNELs(ChannelName), VoiceChatChannelClass).VoiceEnable(Client.Character)
        End If
    End Sub
    Public Sub On_CMSG_CHANNEL_VOICE_OFF(ByRef packet As PacketClass, ByRef Client As ClientClass)
        packet.GetInt16()
        Dim ChannelName As String = packet.GetString

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_CHANNEL_VOICE_OFF [{2}]", Client.IP, Client.Port, ChannelName)

        ChannelName = ChannelName.ToUpper
        If CHAT_CHANNELs.ContainsKey(ChannelName) AndAlso (TypeOf CHAT_CHANNELs(ChannelName) Is VoiceChatChannelClass) Then
            CType(CHAT_CHANNELs(ChannelName), VoiceChatChannelClass).VoiceDisable(Client.Character)
        End If
    End Sub

End Module