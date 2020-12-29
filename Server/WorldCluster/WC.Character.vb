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
Imports System.Reflection
Imports mangosVB.Common
Imports mangosVB.Common.BaseWriter

Public Module WC_Character

    Class CharacterObject
        Implements IDisposable

        Public GUID As ULong
        Public Client As ClientClass

        Public IsInWorld As Boolean = False
        Public Map As UInteger
        Public Zone As UInteger
        Public PositionX As Single
        Public PositionY As Single
        Public PositionZ As Single

        Public Access As AccessLevel
        Public Name As String
        Public Level As Integer
        Public Race As Races
        Public Classe As Classes
        Public Gender As Byte
        Public Time As Date = Now()
        Public Latency As Integer = 0

        Public IgnoreList As New List(Of ULong)
        Public JoinedChannels As New List(Of String)

        Public AFK As Boolean
        Public DND As Boolean
        Public AfkMessage As String

        Public Group As Group = Nothing
        Public GroupFlags As Byte = 0
        Public GroupInvitedFlag As Boolean = False

        Public ReadOnly Property IsInGroup() As Boolean
            Get
                Return (Group IsNot Nothing) AndAlso (GroupInvitedFlag = False)
            End Get
        End Property

        Public ReadOnly Property IsGroupLeader() As Boolean
            Get
                If Group Is Nothing Then Return False
                Return (Group.Members(Group.Leader) Is Me)
            End Get
        End Property

        Public ReadOnly Property IsInRaid() As Boolean
            Get
                Return ((Not (Group Is Nothing)) AndAlso (Group.Type = GroupType.RAID))
            End Get
        End Property

        Public ReadOnly Property GetWorld() As IWorld
            Get
                Return WS.Worlds(Map)
            End Get
        End Property

        Public Sub ReLoad()
            'DONE: Get character info from DB
            Dim MySQLQuery As New DataTable
            Database.Query(String.Format("SELECT * FROM characters WHERE char_guid = {0};", GUID), MySQLQuery)
            If MySQLQuery.Rows.Count > 0 Then
                Race = CType(MySQLQuery.Rows(0).Item("char_race"), Byte)
                Classe = CType(MySQLQuery.Rows(0).Item("char_class"), Byte)
                Gender = CType(MySQLQuery.Rows(0).Item("char_gender"), Byte)

                Name = CType(MySQLQuery.Rows(0).Item("char_name"), String)
                Level = CType(MySQLQuery.Rows(0).Item("char_level"), Byte)
                Access = CType(MySQLQuery.Rows(0).Item("char_access"), Byte)

                Zone = CType(MySQLQuery.Rows(0).Item("char_zone_id"), UInteger)
                Map = CType(MySQLQuery.Rows(0).Item("char_map_id"), UInteger)

                PositionX = CType(MySQLQuery.Rows(0).Item("char_positionX"), Single)
                PositionY = CType(MySQLQuery.Rows(0).Item("char_positionY"), Single)
                'Else
                '    Log.WriteLine(LogType.DATABASE, "Failed to load expected results from:")
                '    Log.WriteLine(LogType.DATABASE, String.Format("SELECT * FROM characters WHERE char_guid = {0};", GUID))
            End If
        End Sub

        Public Sub New(ByVal g As ULong, ByRef c As ClientClass)
            GUID = g
            Client = c

            ReLoad()

            LoadIgnoreList(Me)

            CHARACTERs_Lock.AcquireWriterLock(DEFAULT_LOCK_TIMEOUT)
            CHARACTERs.Add(GUID, Me)
            CHARACTERs_Lock.ReleaseWriterLock()
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
            Client = Nothing

            'NOTE: Don't leave group on normal disconnect, only on logout
            If IsInGroup Then
                'DONE: Tell the group the member is offline
                Dim response As PacketClass = BuildPartyMemberStatsOffline(GUID)
                Group.Broadcast(response)
                response.Dispose()

                'DONE: Set new leader and loot master
                Group.NewLeader(Me)
                Group.SendGroupList()
            End If

            'DONE: Notify friends for logout
            NotifyFriendStatus(Me, FriendResult.FRIEND_OFFLINE)

            'DONE: Leave chat
            While JoinedChannels.Count > 0
                If CHAT_CHANNELs.ContainsKey(JoinedChannels(0)) Then
                    CHAT_CHANNELs(JoinedChannels(0)).Part(Me)
                Else
                    JoinedChannels.RemoveAt(0)
                End If
            End While

            CHARACTERs_Lock.AcquireWriterLock(DEFAULT_LOCK_TIMEOUT)
            CHARACTERs.Remove(GUID)
            CHARACTERs_Lock.ReleaseWriterLock()
        End Sub

        Public Sub Transfer(ByVal posX As Single, ByVal posY As Single, ByVal posZ As Single, ByVal ori As Single, ByVal map As Integer)
            Dim p As New PacketClass(OPCODES.SMSG_TRANSFER_PENDING)
            p.AddInt32(map)
            Client.Send(p)
            p.Dispose()

            'Actions Here
            Me.IsInWorld = False
            GetWorld.ClientDisconnect(Client.Index)

            Database.Update(String.Format("UPDATE characters SET char_positionX = {0}, char_positionY = {1}, char_positionZ = {2}, char_orientation = {3}, char_map_id = {4} WHERE char_guid = {5};", _
                                          Trim(Str(posX)), Trim(Str(posY)), Trim(Str(posZ)), Trim(Str(ori)), map, GUID))

            'Do global transfer
            WS.ClientTransfer(Client.Index, posX, posY, posZ, ori, map)
        End Sub

        'Login
        Public Sub OnLogin()
            'DONE: Update character status in database
            Database.Update("UPDATE characters SET char_online = 1 WHERE char_guid = " & GUID & ";")

            'TODO: SMSG_ACCOUNT_DATA_MD5
            SendAccountMD5(Client, Me)

            'DONE: SMSG_TRIGGER_CINEMATIC
            Dim q As New DataTable
            Database.Query(String.Format("SELECT char_moviePlayed FROM characters WHERE char_guid = {0} AND char_moviePlayed = 0;", GUID), q)
            If q.Rows.Count > 0 Then
                Database.Update("UPDATE characters SET char_moviePlayed = 1 WHERE char_guid = " & GUID & ";")
                SendTrigerCinematic(Client, Me)
            End If

            'DONE: SMSG_LOGIN_SETTIMESPEED
            SendGameTime(Client, Me)

            'DONE: Voice System status
            SendVoiceSystemStatus(Client, Me)

            'DONE: Server Message Of The Day
            SendMessageMOTD(Client, "Welcome to World of Warcraft.")
            SendMessageMOTD(Client, String.Format("This server is using {0} v.{1}", SetColor("[MaNGOSvb]", 200, 255, 200), [Assembly].GetExecutingAssembly().GetName().Version))

            'DONE: SMSG_CONTACT_LIST
            SendContactList(Client, Me)

            'DONE: Send "Friend online"
            NotifyFriendStatus(Me, FriendResult.FRIEND_ONLINE)

            'DONE: Put back character in group if disconnected
            For Each tmpGroup As KeyValuePair(Of Long, Group) In GROUPs
                For i As Byte = 0 To tmpGroup.Value.Members.Length - 1
                    If tmpGroup.Value.Members(i) IsNot Nothing AndAlso tmpGroup.Value.Members(i).GUID = GUID Then
                        tmpGroup.Value.Members(i) = Me
                        tmpGroup.Value.SendGroupList()

                        Dim response As New PacketClass(0)
                        response.Data = Me.GetWorld.GroupMemberStats(GUID, 0)
                        tmpGroup.Value.BroadcastToOther(response, Me)
                        response.Dispose()
                        Exit Sub
                    End If
                Next
            Next
        End Sub

        Public Sub OnLogout()
            'DONE: Update character status in database
            Database.Update("UPDATE characters SET char_online = 0 WHERE char_guid = " & GUID & ";")

            'DONE: Leave group
            If IsInGroup Then
                Group.Leave(Me)
            End If

            'DONE: Leave chat
            While JoinedChannels.Count > 0
                If CHAT_CHANNELs.ContainsKey(JoinedChannels(0)) Then
                    CHAT_CHANNELs(JoinedChannels(0)).Part(Me)
                Else
                    JoinedChannels.RemoveAt(0)
                End If
            End While
        End Sub

        'Chat
        Public ChatFlag As ChatFlag = ChatFlag.FLAG_NONE
        Public Sub SendChatMessage(ByRef GUID As ULong, ByVal Message As String, ByVal msgType As ChatMsg, ByVal msgLanguage As Integer, Optional ByVal ChannelName As String = "Global")
            Dim msgChatFlag As ChatFlag = ChatFlag
            If msgType = ChatMsg.CHAT_MSG_REPLY OrElse msgType = ChatMsg.CHAT_MSG_WHISPER Then msgChatFlag = CHARACTERs(GUID).ChatFlag
            Dim packet As PacketClass = BuildChatMessage(GUID, Message, msgType, msgLanguage, msgChatFlag, ChannelName)
            Client.Send(packet)
            packet.Dispose()
        End Sub
    End Class

    Public Function GetCharacterGUIDByName(ByVal Name As String) As ULong
        Dim GUID As ULong = 0

        CHARACTERs_Lock.AcquireReaderLock(DEFAULT_LOCK_TIMEOUT)
        For Each c As KeyValuePair(Of ULong, CharacterObject) In CHARACTERs
            If UCase(c.Value.Name) = UCase(Name) Then
                GUID = c.Value.GUID
                Exit For
            End If
        Next
        CHARACTERs_Lock.ReleaseReaderLock()

        If GUID = 0 Then
            Dim q As New DataTable
            Database.Query(String.Format("SELECT char_guid FROM characters WHERE char_name = ""{0}"";", EscapeString(Name)), q)

            If q.Rows.Count > 0 Then
                Return CType(q.Rows(0).Item("char_guid"), ULong)
            Else
                Return 0
            End If
        Else
            Return GUID
        End If
    End Function

    Public Function GetCharacterNameByGUID(ByVal GUID As String) As String
        If CHARACTERs.ContainsKey(GUID) Then
            Return CHARACTERs(GUID).Name
        Else
            Dim q As New DataTable
            Database.Query(String.Format("SELECT char_name FROM characters WHERE char_guid = ""{0}"";", GUID), q)

            If q.Rows.Count > 0 Then
                Return CType(q.Rows(0).Item("char_name"), String)
            Else
                Return ""
            End If
        End If
    End Function

End Module