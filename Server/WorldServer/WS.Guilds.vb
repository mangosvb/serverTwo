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
Imports mangosVB.Common.BaseWriter

Public Module WS_Guilds

    'UMSG_UPDATE_GUILD = 148
    'UMSG_DELETE_GUILD_CHARTER = 704

#Region "WS.Guilds.Constants"

    Public Const PETITION_GUILD_PRICE As Integer = 1000
    Public Const PETITION_GUILD As Integer = 5863       'Guild Charter, ItemFlags = &H2000
    Public Const PETITION_2v2_PRICE As Integer = 800000
    Public Const PETITION_2v2 As Integer = 23560
    Public Const PETITION_3v3_PRICE As Integer = 1200000
    Public Const PETITION_3v3 As Integer = 23561
    Public Const PETITION_5v5_PRICE As Integer = 2000000
    Public Const PETITION_5v5 As Integer = 23562

    Public Const TABARD_ITEM As Integer = 5976
    Public Const GUILD_RANK_MAX As Integer = 9
    Public Const GUILD_RANK_MIN As Integer = 0

#End Region
#Region "WS.Guilds.Petition"

    'ERR_PETITION_FULL
    'ERR_PETITION_NOT_SAME_SERVER
    'ERR_PETITION_NOT_ENOUGH_SIGNATURES
    'ERR_PETITION_CREATOR
    'ERR_PETITION_IN_GUILD
    'ERR_PETITION_ALREADY_SIGNED
    'ERR_PETITION_DECLINED_S
    'ERR_PETITION_SIGNED_S
    'ERR_PETITION_SIGNED
    'ERR_PETITION_OFFERED_S
    Public Enum PetitionSignError As Integer
        PETITIONSIGN_OK = 0                     ':Closes the window
        PETITIONSIGN_ALREADY_SIGNED = 1         'You have already signed that guild charter
        PETITIONSIGN_ALREADY_IN_GUILD = 2       'You are already in a guild
        PETITIONSIGN_CANT_SIGN_OWN = 3          'You can's sign own guild charter
        PETITIONSIGN_NOT_SERVER = 4             'That player is not from your server
    End Enum
    Public Enum PetitionTurnInError As Integer
        PETITIONTURNIN_OK = 0                   ':Closes the window
        PETITIONTURNIN_ALREADY_IN_GUILD = 2     'You are already in a guild
        PETITIONTURNIN_NEED_MORE_SIGNATURES = 4 'You need more signatures
    End Enum

    Public Sub SendPetitionActivate(ByRef c As CharacterObject, ByVal cGUID As ULong)
        If WORLD_CREATUREs.ContainsKey(cGUID) = False Then Exit Sub
        Dim Count As Byte = 3
        If WORLD_CREATUREs(cGUID).CreatureInfo.cNpcFlags And NPCFlags.UNIT_NPC_FLAG_VENDOR Then
            Count = 1
        End If

        Dim packet As New PacketClass(OPCODES.SMSG_PETITION_SHOWLIST)
        packet.AddUInt64(cGUID)
        packet.AddInt8(1)

        If Count = 1 Then
            packet.AddInt32(1) 'Index
            packet.AddInt32(PETITION_GUILD)
            packet.AddInt32(16161) 'Charter display ID
            packet.AddInt32(PETITION_GUILD_PRICE)
            packet.AddInt32(0) 'Unknown
            packet.AddInt32(9) 'Required signatures
        Else
            packet.AddInt32(1) 'Index
            packet.AddInt32(PETITION_2v2)
            packet.AddInt32(16161) 'Charter display ID
            packet.AddInt32(PETITION_2v2_PRICE)
            packet.AddInt32(2) 'Unknown
            packet.AddInt32(2) 'Required signatures

            packet.AddInt32(2) 'Index
            packet.AddInt32(PETITION_3v3)
            packet.AddInt32(16161) 'Charter display ID
            packet.AddInt32(PETITION_3v3_PRICE)
            packet.AddInt32(3) 'Unknown
            packet.AddInt32(3) 'Required signatures

            packet.AddInt32(3) 'Index
            packet.AddInt32(PETITION_5v5)
            packet.AddInt32(16161) 'Charter display ID
            packet.AddInt32(PETITION_5v5_PRICE)
            packet.AddInt32(5) 'Unknown
            packet.AddInt32(5) 'Required signatures
        End If

        c.Client.Send(packet)
        packet.Dispose()
    End Sub
    Public Sub On_CMSG_PETITION_SHOWLIST(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 13 Then Exit Sub
        packet.GetInt16()
        Dim GUID As ULong = packet.GetUInt64

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_PETITION_SHOWLIST [GUID={2:X}]", Client.IP, Client.Port, GUID)

        SendPetitionActivate(Client.Character, GUID)
    End Sub
    Public Sub On_CMSG_PETITION_BUY(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 26 Then Exit Sub
        packet.GetInt16()
        Dim GUID As ULong = packet.GetUInt64
        packet.GetInt64()
        packet.GetInt32()
        Dim Name As String = packet.GetString
        If (packet.Data.Length - 1) < 26 + Name.Length + 5 * 8 + 2 + 1 + 4 + 4 Then Exit Sub
        packet.GetInt64()
        packet.GetInt64()
        packet.GetInt64()
        packet.GetInt64()
        packet.GetInt64()
        packet.GetInt16()
        packet.GetInt8()
        Dim Index As Integer = packet.GetInt32
        packet.GetInt32()
        If WORLD_CREATUREs.ContainsKey(GUID) = False OrElse (WORLD_CREATUREs(GUID).CreatureInfo.cNpcFlags And NPCFlags.UNIT_NPC_FLAG_PETITIONER) = 0 Then Exit Sub

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_PETITION_BUY [GuildName={2}]", Client.IP, Client.Port, Name)

        Dim CharterID As Integer = 0
        Dim CharterPrice As Integer = 0
        Dim Type As Integer = 0
        If (WORLD_CREATUREs(GUID).CreatureInfo.cNpcFlags And NPCFlags.UNIT_NPC_FLAG_TABARDDESIGNER) Then
            If Client.Character.GuildID <> 0 Then Exit Sub
            CharterID = PETITION_GUILD
            CharterPrice = PETITION_GUILD_PRICE
            Type = 9
        Else
            'TODO: Check level and send message
            'TODO: Check for arena teams

            Select Case Index
                Case 1
                    CharterID = PETITION_2v2
                    CharterPrice = PETITION_2v2_PRICE
                    Type = 2
                Case 2
                    CharterID = PETITION_3v3
                    CharterPrice = PETITION_3v3_PRICE
                    Type = 3
                Case 3
                    CharterID = PETITION_5v5
                    CharterPrice = PETITION_5v5_PRICE
                    Type = 5
                Case Else
                    Exit Sub
            End Select
        End If

        If Type = 9 Then
            Dim q As New DataTable
            Database.Query(String.Format("SELECT guild_id FROM guilds WHERE guild_name = '{0}'", Name), q)
            If q.Rows.Count > 0 Then
                SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_NAME_EXISTS, Name)
            End If
            q.Clear()
            If ValidateGuildName(Name) = False Then
                SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_NAME_INVALID, Name)
            End If
        Else
            Dim q As New DataTable
            Database.Query(String.Format("SELECT arena_id FROM arena_teams WHERE arena_name = '{0}'", Name), q)
            If q.Rows.Count > 0 Then
                SendArenaCommandResult(Client, ArenaTeamCommandTypes.ERR_ARENA_TEAM_CREATE_S, Name, "", ArenaTeamCommandErrors.ERR_ARENA_TEAM_NAME_EXISTS_S)
            End If
            q.Clear()
            If ValidateGuildName(Name) = False Then
                SendArenaCommandResult(Client, ArenaTeamCommandTypes.ERR_ARENA_TEAM_CREATE_S, Name, "", ArenaTeamCommandErrors.ERR_ARENA_TEAM_NAME_INVALID)
            End If
        End If

        If ITEMDatabase.ContainsKey(CharterID) = False Then
            Dim response As New PacketClass(OPCODES.SMSG_BUY_FAILED)
            response.AddUInt64(GUID)
            response.AddInt32(CharterID)
            response.AddInt8(BUY_ERROR.BUY_ERR_CANT_FIND_ITEM)
            Client.Send(response)
            response.Dispose()
            Exit Sub
        End If
        If Client.Character.Copper < CharterPrice Then
            Dim response As New PacketClass(OPCODES.SMSG_BUY_FAILED)
            response.AddUInt64(GUID)
            response.AddInt32(CharterID)
            response.AddInt8(BUY_ERROR.BUY_ERR_NOT_ENOUGHT_MONEY)
            Client.Send(response)
            response.Dispose()
            Exit Sub
        End If

        Client.Character.Copper -= CharterPrice
        Client.Character.SetUpdateFlag(EPlayerFields.PLAYER_FIELD_COINAGE, Client.Character.Copper)
        Client.Character.SendCharacterUpdate(False)

        'Client.Character.AddItem(PETITION_ITEM)
        Dim tmpItem As New ItemObject(CharterID, Client.Character.GUID)
        tmpItem.StackCount = 1
        tmpItem.AddEnchantment(tmpItem.GUID - GUID_ITEM, 0, 0, 0)
        If Client.Character.ItemADD(tmpItem) Then
            'Save petition into database
            Database.Update(String.Format("INSERT INTO petitions (petition_id, petition_itemGuid, petition_owner, petition_name, petition_type, petition_signedMembers) VALUES ({0}, {0}, {1}, '{2}', {3}, 0);", tmpItem.GUID - GUID_ITEM, Client.Character.GUID - GUID_PLAYER, Name, Type))
        Else
            'No free inventory slot
            tmpItem.Delete()
        End If
    End Sub

    Public Sub SendPetitionSignatures(ByRef c As CharacterObject, ByVal iGUID As ULong)
        Dim MySQLQuery As New DataTable
        Database.Query("SELECT * FROM petitions WHERE petition_itemGuid = " & iGUID - GUID_ITEM & ";", MySQLQuery)
        If MySQLQuery.Rows.Count = 0 Then Exit Sub

        Dim response As New PacketClass(OPCODES.SMSG_PETITION_SHOW_SIGNATURES)
        response.AddUInt64(iGUID)                                                        'ItemGUID
        response.AddUInt64(MySQLQuery.Rows(0).Item("petition_owner"))                    'GuildOwner
        response.AddInt32(MySQLQuery.Rows(0).Item("petition_id"))                       'PetitionGUID
        response.AddInt8(MySQLQuery.Rows(0).Item("petition_signedMembers"))             'PlayersSigned

        For i As Byte = 1 To MySQLQuery.Rows(0).Item("petition_signedMembers")
            response.AddUInt64(MySQLQuery.Rows(0).Item("petition_signedMember" & i))                     'SignedGUID
            response.AddInt32(0)                                                                        'Unk
        Next

        c.Client.Send(response)
        response.Dispose()
    End Sub
    Public Sub On_CMSG_PETITION_SHOW_SIGNATURES(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 13 Then Exit Sub
        packet.GetInt16()
        Dim GUID As ULong = packet.GetUInt64

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_PETITION_SHOW_SIGNATURES [GUID={2:X}]", Client.IP, Client.Port, GUID)

        SendPetitionSignatures(Client.Character, GUID)
    End Sub
    Public Sub On_CMSG_PETITION_QUERY(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 17 Then Exit Sub
        packet.GetInt16()
        Dim PetitionGUID As Integer = packet.GetInt32
        Dim ItemGUID As ULong = packet.GetUInt64

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_PETITION_QUERY [pGUID={3} iGUID={2:X}]", Client.IP, Client.Port, ItemGUID, PetitionGUID)

        Dim MySQLQuery As New DataTable
        Database.Query("SELECT * FROM petitions WHERE petition_itemGuid = " & ItemGUID - GUID_ITEM & ";", MySQLQuery)
        If MySQLQuery.Rows.Count = 0 Then Exit Sub

        Dim response As New PacketClass(OPCODES.SMSG_PETITION_QUERY_RESPONSE)
        response.AddInt32(MySQLQuery.Rows(0).Item("petition_id"))               'PetitionGUID
        response.AddUInt64(MySQLQuery.Rows(0).Item("petition_owner"))            'GuildOwner
        response.AddString(MySQLQuery.Rows(0).Item("petition_name"))            'GuildName
        response.AddInt8(0)         'Unk1
        If CByte(MySQLQuery.Rows(0).Item("petition_type")) = 9 Then
            response.AddInt32(9)
            response.AddInt32(9)
            response.AddInt32(0) 'bypass client - side limitation, a different value is needed here for each petition
        Else
            response.AddInt32(CByte(MySQLQuery.Rows(0).Item("petition_type")) - 1)
            response.AddInt32(CByte(MySQLQuery.Rows(0).Item("petition_type")) - 1)
            response.AddInt32(CByte(MySQLQuery.Rows(0).Item("petition_type"))) 'bypass client - side limitation, a different value is needed here for each petition
        End If
        '9x int32
        response.AddInt32(0)
        response.AddInt32(0)
        response.AddInt32(0)
        response.AddInt32(0)
        response.AddInt16(0)
        response.AddInt32(0)
        response.AddInt32(0)
        response.AddInt32(0)
        response.AddInt32(0)
        If CByte(MySQLQuery.Rows(0).Item("petition_type")) = 9 Then
            response.AddInt32(0)
        Else
            response.AddInt32(1)
        End If
        Client.Send(response)
        response.Dispose()
    End Sub

    Public Sub On_MSG_PETITION_RENAME(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 14 Then Exit Sub
        packet.GetInt16()
        Dim ItemGUID As ULong = packet.GetUInt64
        Dim NewName As String = packet.GetString

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] MSG_PETITION_RENAME [NewName={3} GUID={2:X}]", Client.IP, Client.Port, ItemGUID, NewName)

        Database.Update("UPDATE petitions SET petition_name = '" & NewName & "' WHERE petition_itemGuid = " & ItemGUID - GUID_ITEM & ";")

        'DONE: Update client-side name information
        Dim response As New PacketClass(OPCODES.MSG_PETITION_RENAME)
        response.AddUInt64(ItemGUID)
        response.AddString(NewName)
        response.AddInt32(ItemGUID - GUID_ITEM)
        Client.Send(response)
        response.Dispose()
    End Sub
    Public Sub On_CMSG_TURN_IN_PETITION(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 13 Then Exit Sub
        packet.GetInt16()
        Dim ItemGUID As ULong = packet.GetUInt64

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_TURN_IN_PETITION [GUID={2:X}]", Client.IP, Client.Port, ItemGUID)

        'DONE: Get info
        Dim q As New DataTable
        Database.Query("SELECT * FROM petitions WHERE petition_itemGuid = " & ItemGUID - GUID_ITEM & " LIMIT 1;", q)
        If q.Rows.Count = 0 Then Exit Sub
        Dim Type As Byte = q.Rows(0).Item("petition_type")
        Dim Name As String = q.Rows(0).Item("petition_name")

        'DONE: Check if already in guild
        If Type = 9 AndAlso Client.Character.IsInGuild Then
            Dim response As New PacketClass(OPCODES.SMSG_TURN_IN_PETITION_RESULTS)
            response.AddInt32(PetitionTurnInError.PETITIONTURNIN_ALREADY_IN_GUILD)
            Client.Send(response)
            response.Dispose()
            Exit Sub
        End If

        'DONE: Check required signs
        Dim RequiredSigns As Byte = 0
        If Type = 9 Then RequiredSigns = 9 Else RequiredSigns = Type - 1
        If q.Rows(0).Item("petition_signedMembers") < RequiredSigns Then
            Dim response As New PacketClass(OPCODES.SMSG_TURN_IN_PETITION_RESULTS)
            response.AddInt32(PetitionTurnInError.PETITIONTURNIN_NEED_MORE_SIGNATURES)
            Client.Send(response)
            response.Dispose()
            Exit Sub
        End If

        Dim q2 As New DataTable
        If Type < 9 Then
            'DONE: Check if in arena team
            Database.Query(String.Format("SELECT member_id FROM arena_members WHERE member_id = {0} AND member_type = {1}", Client.Character.GUID, Type), q2)
            If q2.Rows.Count > 0 Then
                SendArenaCommandResult(Client, ArenaTeamCommandTypes.ERR_ARENA_TEAM_CREATE_S, Name, "", ArenaTeamCommandErrors.ERR_ALREADY_IN_ARENA_TEAM)
                Exit Sub
            End If
        End If

        If Type = 9 Then
            'DONE: Create guild and add members
            Database.Query(String.Format("INSERT INTO guilds (guild_name, guild_leader, guild_cYear, guild_cMonth, guild_cDay) VALUES ('{0}', {1}, {2}, {3}, {4}); SELECT guild_id FROM guilds WHERE guild_name = '{0}';", Name, Client.Character.GUID, Now.Year - 2006, Now.Month, Now.Day), q2)

            AddCharacterToGuild(Client.Character, q2.Rows(0).Item("guild_id"), 0)

            'DONE: Adding 9 more signed characters
            For i As Byte = 1 To 9
                If CHARACTERs.ContainsKey(CType(q.Rows(0).Item("petition_signedMember" & i), ULong)) Then
                    AddCharacterToGuild(CHARACTERs(CType(q.Rows(0).Item("petition_signedMember" & i), ULong)), q2.Rows(0).Item("guild_id"))
                Else
                    AddCharacterToGuild(CType(q.Rows(0).Item("petition_signedMember" & i), ULong), q2.Rows(0).Item("guild_id"))
                End If
            Next
        Else
            Dim Icon As UInteger = packet.GetUInt32
            Dim IconColor As UInteger = packet.GetUInt32
            Dim Border As UInteger = packet.GetUInt32
            Dim BorderColor As UInteger = packet.GetUInt32
            Dim BackGround As UInteger = packet.GetUInt32
            'DONE: Create the team
            q2.Clear()
            Database.Query(String.Format("INSERT INTO arena_teams (arena_name, arena_captain, arena_type, arena_emblemstyle, arena_emblemcolor, arena_borderstyle, arena_bordercolor, arena_background) VALUES ('{0}', {1}, {2}, {3}, {4}, {5}, {6}, {7}); SELECT arena_id FROM arena_teams WHERE arena_name = '{0}';", Name, Client.Character.GUID, Type, Icon, IconColor, Border, BorderColor, BackGround), q2)

            Dim Slot As Byte = 0
            If Type = 2 Then
                Slot = 0
            ElseIf Type = 3 Then
                Slot = 1
            ElseIf Type = 5 Then
                Slot = 2
            Else 'Why?
                Exit Sub
            End If

            'DONE: Add owner to the team
            Database.Update(String.Format("INSERT INTO arena_members (member_id, member_team, member_type) VALUES ({0}, {1}, {2})", Client.Character.GUID, q2.Rows(0).Item("arena_id"), Type))
            Client.Character.ArenaTeamID(Slot) = CUInt(q2.Rows(0).Item("arena_id"))
            Client.Character.SetUpdateFlag(EPlayerFields.PLAYER_FIELD_ARENA_TEAM_INFO_1_1 + (Slot * 6), Client.Character.ArenaTeamID(Slot))
            Client.Character.SendCharacterUpdate()
            SendArenaCommandResult(Client, ArenaTeamCommandTypes.ERR_ARENA_TEAM_CREATE_S, Name, "", ArenaTeamCommandErrors.ERR_INVITED_TO_ARENA_TEAM)

            'DONE: Add members to the team
            For i As Byte = 1 To Type - 1
                Database.Update(String.Format("INSERT INTO arena_members (member_id, member_team, member_type) VALUES ({0}, {1}, {2})", CType(q.Rows(0).Item("petition_signedMember" & i), ULong), q2.Rows(0).Item("arena_id"), Type))
                If CHARACTERs.ContainsKey(CType(q.Rows(0).Item("petition_signedMember" & i), ULong)) Then
                    CHARACTERs(CType(q.Rows(0).Item("petition_signedMember" & i), ULong)).ArenaTeamID(Slot) = CUInt(q2.Rows(0).Item("arena_id"))
                    CHARACTERs(CType(q.Rows(0).Item("petition_signedMember" & i), ULong)).SetUpdateFlag(EPlayerFields.PLAYER_FIELD_ARENA_TEAM_INFO_1_1 + (Slot * 6), CUInt(q2.Rows(0).Item("arena_id")))
                    CHARACTERs(CType(q.Rows(0).Item("petition_signedMember" & i), ULong)).SetUpdateFlag(EPlayerFields.PLAYER_FIELD_ARENA_TEAM_INFO_1_1 + (Slot * 6) + 1, 1) 'Disable remove buttons etc?
                    CHARACTERs(CType(q.Rows(0).Item("petition_signedMember" & i), ULong)).SendCharacterUpdate()
                End If
            Next

            'TODO: Send message "You're now a member of <arena team name>."
        End If

        'DONE: Delete guild charter item
        Client.Character.ItemREMOVE(ItemGUID, True, True)

        Dim success As New PacketClass(OPCODES.SMSG_TURN_IN_PETITION_RESULTS)
        success.AddInt32(0) 'Okay
        Client.Send(success)
        success.Dispose()
    End Sub
    Public Sub On_CMSG_OFFER_PETITION(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 21 Then Exit Sub
        packet.GetInt16()
        Dim PetitionType As Integer = packet.GetInt32
        Dim ItemGUID As ULong = packet.GetUInt64
        Dim GUID As ULong = packet.GetUInt64
        If CHARACTERs.ContainsKey(GUID) = False Then Exit Sub
        'If CHARACTERs(GUID).IgnoreList.Contains(Client.Character.GUID) Then Exit Sub
        If CHARACTERs(GUID).Side <> Client.Character.Side Then Exit Sub

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_OFFER_PETITION [GUID={2:X} Petition={3}]", Client.IP, Client.Port, GUID, ItemGUID)

        SendPetitionSignatures(CHARACTERs(GUID), ItemGUID)
    End Sub
    Public Sub On_CMSG_PETITION_SIGN(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 14 Then Exit Sub
        packet.GetInt16()
        Dim ItemGUID As ULong = packet.GetUInt64
        Dim Unk As Integer = packet.GetInt8

        'TODO: Check if the player already has signed

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_PETITION_SIGN [GUID={2:X} Unk={3}]", Client.IP, Client.Port, ItemGUID, Unk)

        Dim MySQLQuery As New DataTable
        Database.Query("SELECT petition_signedMembers, petition_owner FROM petitions WHERE petition_itemGuid = " & ItemGUID - GUID_ITEM & ";", MySQLQuery)
        If MySQLQuery.Rows.Count = 0 Then Exit Sub

        Database.Update("UPDATE petitions SET petition_signedMembers = petition_signedMembers + 1, petition_signedMember" & (MySQLQuery.Rows(0).Item("petition_signedMembers") + 1) & " = " & Client.Character.GUID & " WHERE petition_itemGuid = " & ItemGUID - GUID_ITEM & ";")

        'DONE: Send result to both players
        Dim response As New PacketClass(OPCODES.SMSG_PETITION_SIGN_RESULTS)
        response.AddUInt64(ItemGUID)
        response.AddUInt64(Client.Character.GUID)
        response.AddInt32(PetitionSignError.PETITIONSIGN_OK)
        Client.SendMultiplyPackets(response)
        If CHARACTERs.ContainsKey(CType(MySQLQuery.Rows(0).Item("petition_owner"), ULong)) Then CHARACTERs(CType(MySQLQuery.Rows(0).Item("petition_owner"), ULong)).Client.SendMultiplyPackets(response)
        response.Dispose()
    End Sub
    Public Sub On_MSG_PETITION_DECLINE(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 13 Then Exit Sub
        packet.GetInt16()
        Dim ItemGUID As ULong = packet.GetUInt64

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] MSG_PETITION_DECLINE [GUID={2:X}]", Client.IP, Client.Port, ItemGUID)

        'DONE: Get petition owner
        Dim q As New DataTable
        Database.Query("SELECT petition_owner FROM petitions WHERE petition_itemGuid = " & ItemGUID - GUID_ITEM & " LIMIT 1;", q)

        'DONE: Send message to player
        Dim response As New PacketClass(OPCODES.MSG_PETITION_DECLINE)
        response.AddUInt64(Client.Character.GUID)
        If q.Rows.Count > 0 AndAlso CHARACTERs.ContainsKey(CType(q.Rows(0).Item("petition_owner"), ULong)) Then CHARACTERs(CType(q.Rows(0).Item("petition_owner"), ULong)).Client.SendMultiplyPackets(response)
        response.Dispose()
    End Sub

#End Region
#Region "WS.Guilds.Handlers"

    'Basic Tabard Framework
    Public Sub SendTabardActivate(ByRef c As CharacterObject, ByVal cGUID As ULong)
        Dim packet As New PacketClass(OPCODES.MSG_TABARDVENDOR_ACTIVATE)
        packet.AddUInt64(cGUID)
        c.Client.Send(packet)
        packet.Dispose()
    End Sub
    Public Sub On_MSG_TABARDVENDOR_ACTIVATE(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 13 Then Exit Sub
        packet.GetInt16()
        Dim GUID As ULong = packet.GetUInt64

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] MSG_TABARDVENDOR_ACTIVATE [GUID={2}]", Client.IP, Client.Port, GUID)

        SendTabardActivate(Client.Character, GUID)
    End Sub
    Public Function GetGuildBankTabPrice(ByVal TabID As Byte) As Integer
        Select Case TabID
            Case 0
                Return 100
            Case 1
                Return 250
            Case 2
                Return 500
            Case 3
                Return 1000
            Case 4
                Return 2500
            Case 5
                Return 5000
            Case Else
                Return 0
        End Select
    End Function

    'Basic Guild Framework
    Public Sub AddCharacterToGuild(ByRef c As CharacterObject, ByVal GuildID As Integer, Optional ByVal GuildRank As Integer = 4)
        Database.Update(String.Format("UPDATE characters SET char_guildId = {0}, char_guildRank = {2}, char_guildOffNote = '', char_guildPNote = '' WHERE char_guid = {1};", GuildID, c.GUID, GuildRank))

        c.GuildID = GuildID
        c.GuildRank = GuildRank
        c.SetUpdateFlag(EPlayerFields.PLAYER_GUILDID, c.GuildID)
        c.SetUpdateFlag(EPlayerFields.PLAYER_GUILDRANK, c.GuildRank)
        c.SendCharacterUpdate(True)
    End Sub
    Public Sub AddCharacterToGuild(ByVal GUID As ULong, ByVal GuildID As Integer, Optional ByVal GuildRank As Integer = 4)
        Database.Update(String.Format("UPDATE characters SET char_guildId = {0}, char_guildRank = {2}, char_guildOffNote = '', char_guildPNote = '' WHERE char_guid = {1};", GuildID, GUID, GuildRank))
    End Sub
    Public Sub RemoveCharacterFromGuild(ByRef c As CharacterObject)
        Database.Update(String.Format("UPDATE characters SET char_guildId = {0}, char_guildRank = 0, char_guildOffNote = '', char_guildPNote = '' WHERE char_guid = {1};", 0, c.GUID))

        c.GuildID = 0
        c.GuildRank = 0
        c.SetUpdateFlag(EPlayerFields.PLAYER_GUILDID, 0)
        c.SetUpdateFlag(EPlayerFields.PLAYER_GUILDRANK, 0)
        c.SendCharacterUpdate(True)
    End Sub
    Public Sub RemoveCharacterFromGuild(ByVal GUID As ULong)
        Database.Update(String.Format("UPDATE characters SET char_guildId = {0}, char_guildRank = 0, char_guildOffNote = '', char_guildPNote = '' WHERE char_guid = {1};", 0, GUID))
    End Sub
    Public Sub BroadcastToGuild(ByRef packet As PacketClass, ByVal GuildID As Integer)
        Dim q As New DataTable
        Database.Query(String.Format("SELECT char_guid FROM characters WHERE char_guildID = {0} AND char_online = 1;", GuildID), q)

        For Each r As DataRow In q.Rows
            If CHARACTERs.ContainsKey(CType(r.Item("char_guid"), Long)) Then
                CType(CHARACTERs(CType(r.Item("char_guid"), Long)), CharacterObject).Client.SendMultiplyPackets(packet)
            End If
        Next
    End Sub
    Public Sub BroadcastChatMessageGuild(ByRef Sender As CharacterObject, ByVal Message As String, ByVal Language As LANGUAGES, ByVal GuildID As Integer)
        'DONE: Check for guild member
        If Sender.GuildID = 0 Then
            SendGuildResult(Sender.Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD)
            Exit Sub
        End If

        'DONE: Check for rights to speak
        If Not Sender.IsGuildRightSet(GuildRankRights.GR_RIGHT_GCHATSPEAK) Then
            SendGuildResult(Sender.Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PERMISSIONS)
            Exit Sub
        End If

        'DONE: Build packet
        Dim packet As PacketClass = BuildChatMessage(Sender.GUID, Message, ChatMsg.CHAT_MSG_GUILD, Language, GetChatFlag(Sender))

        'DONE: Send message to everyone
        Dim q As New DataTable
        Database.Query(String.Format("SELECT char_guid FROM characters WHERE char_guildID = {0} AND char_online = 1;", GuildID), q)

        For Each r As DataRow In q.Rows
            If CHARACTERs.ContainsKey(CType(r.Item("char_guid"), Long)) Then
                If CType(CHARACTERs(CType(r.Item("char_guid"), Long)), CharacterObject).IsGuildRightSet(GuildRankRights.GR_RIGHT_GCHATLISTEN) Then
                    CType(CHARACTERs(CType(r.Item("char_guid"), Long)), CharacterObject).Client.SendMultiplyPackets(packet)
                End If
            End If
        Next

        packet.Dispose()
    End Sub
    Public Sub BroadcastChatMessageOfficer(ByRef Sender As CharacterObject, ByVal Message As String, ByVal Language As LANGUAGES, ByVal GuildID As Integer)
        'DONE: Check for guild member
        If Sender.GuildID = 0 Then
            SendGuildResult(Sender.Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD)
            Exit Sub
        End If

        'DONE: Check for rights to speak
        If Not Sender.IsGuildRightSet(GuildRankRights.GR_RIGHT_OFFCHATSPEAK) Then
            SendGuildResult(Sender.Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PERMISSIONS)
            Exit Sub
        End If

        'DONE: Build packet
        Dim packet As PacketClass = BuildChatMessage(Sender.GUID, Message, ChatMsg.CHAT_MSG_OFFICER, Language, GetChatFlag(Sender))

        'DONE: Send message to everyone
        Dim q As New DataTable
        Database.Query(String.Format("SELECT char_guid FROM characters WHERE char_guildID = {0} AND char_online = 1;", GuildID), q)

        For Each r As DataRow In q.Rows
            If CHARACTERs.ContainsKey(CType(r.Item("char_guid"), Long)) Then
                If CType(CHARACTERs(CType(r.Item("char_guid"), Long)), CharacterObject).IsGuildRightSet(GuildRankRights.GR_RIGHT_OFFCHATLISTEN) Then
                    CType(CHARACTERs(CType(r.Item("char_guid"), Long)), CharacterObject).Client.SendMultiplyPackets(packet)
                End If
            End If
        Next

        packet.Dispose()
    End Sub
    Public Sub SendGuildQuery(ByRef Client As ClientClass, ByVal GuildID As Integer)
        'WARNING: This opcode is used also in character enum, so there must not be used any references to CharacterObject, only ClientClass

        Dim MySQLQuery As New DataTable
        Database.Query("SELECT * FROM guilds WHERE guild_id = " & GuildID & ";", MySQLQuery)
        If MySQLQuery.Rows.Count = 0 Then Throw New ApplicationException("GuildID " & GuildID & " not found in database.")

        Dim response As New PacketClass(OPCODES.SMSG_GUILD_QUERY_RESPONSE)
        response.AddInt32(GuildID)
        response.AddString(MySQLQuery.Rows(0).Item("guild_name"))
        response.AddString(MySQLQuery.Rows(0).Item("guild_rank0"))
        response.AddString(MySQLQuery.Rows(0).Item("guild_rank1"))
        response.AddString(MySQLQuery.Rows(0).Item("guild_rank2"))
        response.AddString(MySQLQuery.Rows(0).Item("guild_rank3"))
        response.AddString(MySQLQuery.Rows(0).Item("guild_rank4"))
        response.AddString(MySQLQuery.Rows(0).Item("guild_rank5"))
        response.AddString(MySQLQuery.Rows(0).Item("guild_rank6"))
        response.AddString(MySQLQuery.Rows(0).Item("guild_rank7"))
        response.AddString(MySQLQuery.Rows(0).Item("guild_rank8"))
        response.AddString(MySQLQuery.Rows(0).Item("guild_rank9"))
        response.AddInt32(CType(MySQLQuery.Rows(0).Item("guild_tEmblemStyle"), Integer))
        response.AddInt32(CType(MySQLQuery.Rows(0).Item("guild_tEmblemColor"), Integer))
        response.AddInt32(CType(MySQLQuery.Rows(0).Item("guild_tBorderStyle"), Integer))
        response.AddInt32(CType(MySQLQuery.Rows(0).Item("guild_tBorderColor"), Integer))
        response.AddInt32(CType(MySQLQuery.Rows(0).Item("guild_tBackgroundColor"), Integer))
        response.AddInt32(0)
        Client.Send(response)
        response.Dispose()
    End Sub
    Public Sub SendGuildRoster(ByRef c As CharacterObject)
        If c.GuildID = 0 Then Exit Sub

        Dim MySQLQuery As New DataTable
        Database.Query("SELECT * FROM guilds WHERE guild_id = " & c.GuildID & ";", MySQLQuery)
        If MySQLQuery.Rows.Count = 0 Then Throw New ApplicationException("GuildID " & c.GuildID & " not found in database.")

        'DONE: Count the ranks
        Dim guildRanksCount As Byte = 0
        If MySQLQuery.Rows(0).Item("guild_rank0") <> "" Then guildRanksCount += 1
        If MySQLQuery.Rows(0).Item("guild_rank1") <> "" Then guildRanksCount += 1
        If MySQLQuery.Rows(0).Item("guild_rank2") <> "" Then guildRanksCount += 1
        If MySQLQuery.Rows(0).Item("guild_rank3") <> "" Then guildRanksCount += 1
        If MySQLQuery.Rows(0).Item("guild_rank4") <> "" Then guildRanksCount += 1
        If MySQLQuery.Rows(0).Item("guild_rank5") <> "" Then guildRanksCount += 1
        If MySQLQuery.Rows(0).Item("guild_rank6") <> "" Then guildRanksCount += 1
        If MySQLQuery.Rows(0).Item("guild_rank7") <> "" Then guildRanksCount += 1
        If MySQLQuery.Rows(0).Item("guild_rank8") <> "" Then guildRanksCount += 1
        If MySQLQuery.Rows(0).Item("guild_rank9") <> "" Then guildRanksCount += 1

        'DONE: Count the members
        Dim Members As New DataTable
        Database.Query("SELECT char_online, char_guid, char_name, char_class, char_level, char_zone_id, char_guildRank, char_guildPNote, char_guildOffNote FROM characters WHERE char_guildId = " & c.GuildID & ";", Members)

        Dim response As New PacketClass(OPCODES.SMSG_GUILD_ROSTER)
        response.AddInt32(Members.Rows.Count)
        response.AddString(MySQLQuery.Rows(0).Item("guild_MOTD"))
        response.AddString(MySQLQuery.Rows(0).Item("guild_info"))
        response.AddInt32(guildRanksCount)
        Dim i As Integer
        For i = 0 To 9
            If MySQLQuery.Rows(0).Item("guild_rank" & i) <> "" Then
                response.AddInt32(MySQLQuery.Rows(0).Item("guild_rank" & i & "_Rights"))
                response.AddInt32(0) 'BankMoney Per Day
                'Bank Tabs
                For j As Byte = 0 To 5
                    response.AddInt32(0) 'BankTab Rights
                    response.AddInt32(0) 'BankTab Stacks Per Day
                Next
            End If
        Next

        Dim Officer As Boolean = c.IsGuildRightSet(GuildRankRights.GR_RIGHT_VIEWOFFNOTE)
        For i = 0 To Members.Rows.Count - 1
            If Members.Rows(i).Item("char_online") = 1 Then
                response.AddUInt64(Members.Rows(i).Item("char_guid"))
                response.AddInt8(1)                         'OnlineFlag
                response.AddString(Members.Rows(i).Item("char_name"))
                response.AddInt32(Members.Rows(i).Item("char_guildRank"))
                response.AddInt8(Members.Rows(i).Item("char_level"))
                response.AddInt8(Members.Rows(i).Item("char_class"))
                response.AddInt8(0) 'Unk, new in 2.4
                response.AddInt32(Members.Rows(i).Item("char_zone_id"))
                response.AddString(Members.Rows(i).Item("char_guildPNote"))
                If Officer Then
                    response.AddString(Members.Rows(i).Item("char_guildOffNote"))
                Else
                    response.AddInt8(0)
                End If
            Else
                response.AddUInt64(Members.Rows(i).Item("char_guid"))
                response.AddInt8(0)                         'OfflineFlag
                response.AddString(Members.Rows(i).Item("char_name"))
                response.AddInt32(Members.Rows(i).Item("char_guildRank"))
                response.AddInt8(Members.Rows(i).Item("char_level"))
                response.AddInt8(Members.Rows(i).Item("char_class"))
                response.AddInt8(0) 'Unk, new in 2.4
                response.AddInt32(Members.Rows(i).Item("char_zone_id"))
                '0 = < 1 hour / 0.1 = 2.4 hours / 1 = 24 hours (1 day)
                '(Time logged out / 86400) = Days offline
                response.AddSingle(1) 'Days offline (need the timestamp saved in the database for logout time)
                response.AddString(Members.Rows(i).Item("char_guildPNote"))
                If Officer Then
                    response.AddString(Members.Rows(i).Item("char_guildOffNote"))
                Else
                    response.AddInt8(0)
                End If
            End If
        Next

        c.Client.Send(response)
        response.Dispose()
    End Sub
    Public Sub SendGuildBankInfo(ByRef c As CharacterObject)
        Dim q As New DataTable
        Database.Query(String.Format("SELECT guild_banktabs, guild_bankmoney FROM guilds WHERE guild_id = {0}", c.GuildID), q)
        If q.Rows.Count = 0 Then Exit Sub
        Dim BankTabCount As Byte = CByte(q.Rows(0).Item("guild_banktabs"))

        Dim response As New PacketClass(OPCODES.SMSG_GUILD_BANK_LIST)
        response.AddUInt64(CULng(q.Rows(0).Item("guild_bankmoney"))) 'Guild bank money
        response.AddInt8(0) 'TabInfo packet must be for TabId 0
        response.AddInt32(&HFFFFFFFF) 'bit 9 must be set for this packet to work
        response.AddInt8(1) 'Tell Client this is a TabInfo packet
        response.AddInt8(BankTabCount) 'here is the number of tabs

        If BankTabCount > 0 Then
            For i As Byte = 0 To BankTabCount - 1
                q.Clear()
                Database.Query(String.Format("SELECT tab_name, tab_icon FROM guildbanktabs WHERE tab_guildid = {0} AND tab_id = {1}", c.GuildID, i), q)
                If q.Rows.Count > 0 Then
                    response.AddString(q.Rows(0).Item("tab_name")) 'Name
                    response.AddString(q.Rows(0).Item("tab_icon")) 'Icon
                Else
                    response.AddString("Unknown Tab")
                    response.AddString("unk")
                End If
            Next
        End If

        response.AddInt8(0) 'Do not send tab content
        c.Client.Send(response)
        response.Dispose()
    End Sub
    Public Sub SendGuildBankContent(ByRef c As CharacterObject, ByVal TabID As Byte)
        Dim response As New PacketClass(OPCODES.SMSG_GUILD_BANK_LIST)
        response.AddUInt64(0) 'Bank money
        response.AddInt8(TabID)
        response.AddInt32(0) 'Remaining slots for today
        response.AddInt8(0) 'Tab content packet
        response.AddInt8(98) 'Guild bank max slots

        For i As Byte = 0 To 97
            response.AddInt8(0)
            response.AddInt32(0)

            Dim entry As Integer = 0
            If entry Then
                response.AddInt32(0) 'RandomProperty ID
                'if RandomPropertyID then response.AddInt32(0)
                response.AddInt8(0) 'Stack count
                response.AddInt32(0) 'Unk
                response.AddInt8(0) 'Unk, 2.4.2
                response.AddInt8(0) 'Number of enchants
                'for each enchant
                ' response.AddInt8(0) 'Enchant slot
                ' response.AddInt32(0) 'Enchant ID
                'next
            End If
        Next
        c.Client.Send(response)
        response.Dispose()
    End Sub
    Public Sub On_CMSG_GUILD_QUERY(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 9 Then Exit Sub
        packet.GetInt16()
        Dim GuildID As Integer = packet.GetInt32

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_QUERY [{2}]", Client.IP, Client.Port, GuildID)

        SendGuildQuery(Client, GuildID)
    End Sub
    Public Sub On_CMSG_GUILD_ROSTER(ByRef packet As PacketClass, ByRef Client As ClientClass)
        'packet.GetInt16()

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_ROSTER", Client.IP, Client.Port)

        SendGuildRoster(Client.Character)
    End Sub
    Public Sub On_CMSG_GUILD_CREATE(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 6 Then Exit Sub
        packet.GetInt16()
        Dim guildName As String = packet.GetString

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_CREATE [{2}]", Client.IP, Client.Port, guildName)

        If Client.Character.IsInGuild Then
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_ALREADY_IN_GUILD)
            Exit Sub
        End If

        'DONE: Create guild data
        Dim MySQLQuery As New DataTable
        Database.Query(String.Format("INSERT INTO guilds (guild_name, guild_leader, guild_cYear, guild_cMonth, guild_cDay) VALUES (""{0}"", {1}, {2}, {3}, {4}); SELECT guild_id FROM guilds WHERE guild_name = ""{0}"";", guildName, Client.Character.GUID, Now.Year - 2006, Now.Month, Now.Day), MySQLQuery)

        AddCharacterToGuild(Client.Character, MySQLQuery.Rows(0).Item("guild_id"), 0)
    End Sub
    Public Sub On_CMSG_GUILD_INFO(ByRef packet As PacketClass, ByRef Client As ClientClass)
        packet.GetInt16()

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_INFO", Client.IP, Client.Port)

        If Not Client.Character.IsInGuild Then
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD)
            Exit Sub
        End If

        'DONE: Get guild data
        Dim q As New DataTable
        Database.Query(String.Format("SELECT guild_name, guild_cYear, guild_cMonth, guild_cDay FROM guilds WHERE guild_id = " & Client.Character.GuildID & ";"), q)
        If q.Rows.Count = 0 Then
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_INTERNAL)
            Exit Sub
        End If

        Dim response As New PacketClass(OPCODES.SMSG_GUILD_INFO)
        response.AddString(q.Rows(0).Item("guild_name"))
        response.AddInt32(q.Rows(0).Item("guild_cDay"))
        response.AddInt32(q.Rows(0).Item("guild_cMonth"))
        response.AddInt32(q.Rows(0).Item("guild_cYear"))
        response.AddInt32(0)
        response.AddInt32(0)
        Client.Send(response)
        response.Dispose()
    End Sub

    'Guild Leader Options
    Public Enum GuildRankRights
        GR_RIGHT_EMPTY = &H40
        GR_RIGHT_GCHATLISTEN = &H41
        GR_RIGHT_GCHATSPEAK = &H42
        GR_RIGHT_OFFCHATLISTEN = &H44
        GR_RIGHT_OFFCHATSPEAK = &H48
        GR_RIGHT_PROMOTE = &HC0
        GR_RIGHT_DEMOTE = &H140
        GR_RIGHT_INVITE = &H50
        GR_RIGHT_REMOVE = &H60
        GR_RIGHT_SETMOTD = &H1040
        GR_RIGHT_EPNOTE = &H2040
        GR_RIGHT_VIEWOFFNOTE = &H4040
        GR_RIGHT_EOFFNOTE = &H8040
        GR_RIGHT_ALL = &HF1FF
    End Enum
    Public Sub On_CMSG_GUILD_RANK(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 14 Then Exit Sub
        packet.GetInt16()
        Dim rankID As Integer = packet.GetInt32
        Dim rankRights As Integer = packet.GetInt32
        Dim rankName As String = packet.GetString

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_RANK [{2}:{3}:{4}]", Client.IP, Client.Port, rankID, rankRights, rankName)

        If Not Client.Character.IsInGuild Then
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD)
            Exit Sub
        ElseIf Not Client.Character.IsGuildLeader Then
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PERMISSIONS)
            Exit Sub
        End If

        Database.Update(String.Format("UPDATE guilds SET guild_rank{1} = ""{2}"", guild_rank{1}_Rights = {3} WHERE guild_id = {0};", Client.Character.GuildID, rankID, rankName.Replace("""", "_").Replace("'", "_"), rankRights))

        SendGuildQuery(Client, Client.Character.GuildID)
        SendGuildRoster(Client.Character)
    End Sub
    Public Sub On_CMSG_GUILD_ADD_RANK(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 6 Then Exit Sub
        packet.GetInt16()
        Dim NewRankName As String = packet.GetString()

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_ADD_RANK [{2}]", Client.IP, Client.Port, NewRankName)

        If Not Client.Character.IsInGuild Then
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD)
            Exit Sub
        ElseIf Not Client.Character.IsGuildLeader Then
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PERMISSIONS)
            Exit Sub
        End If

        Dim MySQLQuery As New DataTable
        Database.Query("SELECT * FROM guilds WHERE guild_id = " & Client.Character.GuildID & ";", MySQLQuery)
        If MySQLQuery.Rows.Count = 0 Then Throw New ApplicationException("GuildID " & Client.Character.GuildID & " not found in database.")

        Dim GuildPos As Integer
        If MySQLQuery.Rows(0).Item("guild_rank0") = "" Then
            GuildPos = 0
        ElseIf MySQLQuery.Rows(0).Item("guild_rank1") = "" Then
            GuildPos = 1
        ElseIf MySQLQuery.Rows(0).Item("guild_rank2") = "" Then
            GuildPos = 2
        ElseIf MySQLQuery.Rows(0).Item("guild_rank3") = "" Then
            GuildPos = 3
        ElseIf MySQLQuery.Rows(0).Item("guild_rank4") = "" Then
            GuildPos = 4
        ElseIf MySQLQuery.Rows(0).Item("guild_rank5") = "" Then
            GuildPos = 5
        ElseIf MySQLQuery.Rows(0).Item("guild_rank6") = "" Then
            GuildPos = 6
        ElseIf MySQLQuery.Rows(0).Item("guild_rank7") = "" Then
            GuildPos = 7
        ElseIf MySQLQuery.Rows(0).Item("guild_rank8") = "" Then
            GuildPos = 8
        ElseIf MySQLQuery.Rows(0).Item("guild_rank9") = "" Then
            GuildPos = 9
        Else
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_INTERNAL)
            Exit Sub
        End If

        Database.Update(String.Format("UPDATE guilds SET guild_rank{1} = ""{2}"", guild_rank{1}_Rights = {3} WHERE guild_id = {0};", Client.Character.GuildID, GuildPos, NewRankName.Replace("""", "_").Replace("'", "_"), GuildRankRights.GR_RIGHT_GCHATLISTEN Or GuildRankRights.GR_RIGHT_GCHATSPEAK))

        SendGuildQuery(Client, Client.Character.GuildID)
        SendGuildRoster(Client.Character)
    End Sub
    Public Sub On_CMSG_GUILD_DEL_RANK(ByRef packet As PacketClass, ByRef Client As ClientClass)
        packet.GetInt16()

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_DEL_RANK", Client.IP, Client.Port)

        If Not Client.Character.IsInGuild Then
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD)
            Exit Sub
        ElseIf Not Client.Character.IsGuildLeader Then
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PERMISSIONS)
            Exit Sub
        End If

        Dim MySQLQuery As New DataTable
        Database.Query("SELECT * FROM guilds WHERE guild_id = " & Client.Character.GuildID & ";", MySQLQuery)
        If MySQLQuery.Rows.Count = 0 Then Throw New ApplicationException("GuildID " & Client.Character.GuildID & " not found in database.")

        Dim GuildPos As Integer
        If MySQLQuery.Rows(0).Item("guild_rank9") <> "" Then
            GuildPos = 9
        ElseIf MySQLQuery.Rows(0).Item("guild_rank8") <> "" Then
            GuildPos = 8
        ElseIf MySQLQuery.Rows(0).Item("guild_rank7") <> "" Then
            GuildPos = 7
        ElseIf MySQLQuery.Rows(0).Item("guild_rank6") <> "" Then
            GuildPos = 6
        ElseIf MySQLQuery.Rows(0).Item("guild_rank5") <> "" Then
            GuildPos = 5
        ElseIf MySQLQuery.Rows(0).Item("guild_rank4") <> "" Then
            GuildPos = 4
        ElseIf MySQLQuery.Rows(0).Item("guild_rank3") <> "" Then
            GuildPos = 3
        ElseIf MySQLQuery.Rows(0).Item("guild_rank2") <> "" Then
            GuildPos = 2
        ElseIf MySQLQuery.Rows(0).Item("guild_rank1") <> "" Then
            GuildPos = 1
        ElseIf MySQLQuery.Rows(0).Item("guild_rank0") <> "" Then
            GuildPos = 0
        Else
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_INTERNAL)
            Exit Sub
        End If

        Database.Update(String.Format("UPDATE guilds SET guild_rank{1} = ""{2}"", guild_rank{1}_Rights = {3} WHERE guild_id = {0};", Client.Character.GuildID, GuildPos, "", 0))

        SendGuildQuery(Client, Client.Character.GuildID)
        SendGuildRoster(Client.Character)
    End Sub
    Public Sub On_CMSG_GUILD_INFO_TEXT(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 6 Then Exit Sub
        packet.GetInt16()
        Dim guildInfo As String = packet.GetString

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_INFO_TEXT", Client.IP, Client.Port)

        If Not Client.Character.IsInGuild Then
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD)
            Exit Sub
        ElseIf Not Client.Character.IsGuildLeader Then
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PERMISSIONS)
            Exit Sub
        End If

        Database.Update(String.Format("UPDATE guilds SET guild_info = ""{1}"" WHERE guild_id = {0};", Client.Character.GuildID, guildInfo.Replace("""", "_").Replace("'", "_")))
    End Sub
    Public Sub On_CMSG_GUILD_LEADER(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 6 Then Exit Sub
        packet.GetInt16()
        Dim playerName As String = packet.GetString

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_LEADER [{2}]", Client.IP, Client.Port, playerName)

        If Not Client.Character.IsInGuild Then
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD)
            Exit Sub
        ElseIf Not Client.Character.IsGuildLeader Then
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PERMISSIONS)
            Exit Sub
        End If

        'DONE: Find new leader's GUID
        Dim MySQLQuery As New DataTable
        Database.Query("SELECT char_guid, char_guildId, char_guildrank FROM characters WHERE char_name = '" & playerName & "';", MySQLQuery)
        If MySQLQuery.Rows.Count = 0 Then
            SendGuildResult(Client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_PLAYER_NOT_FOUND, playerName)
            Exit Sub
        ElseIf MySQLQuery.Rows(0).Item("char_guildId") <> Client.Character.GuildID Then
            SendGuildResult(Client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD_S, playerName)
            Exit Sub
        End If

        Client.Character.GuildRank = CByte(MySQLQuery.Rows(0).Item("char_guildrank"))
        Client.Character.SetUpdateFlag(EPlayerFields.PLAYER_GUILDRANK, Client.Character.GuildRank)
        Client.Character.SendCharacterUpdate()
        If CHARACTERs.ContainsKey(CULng(MySQLQuery.Rows(0).Item("char_guid"))) Then
            CHARACTERs(CULng(MySQLQuery.Rows(0).Item("char_guid"))).GuildRank = 0
            CHARACTERs(CULng(MySQLQuery.Rows(0).Item("char_guid"))).SetUpdateFlag(EPlayerFields.PLAYER_GUILDRANK, 0)
            CHARACTERs(CULng(MySQLQuery.Rows(0).Item("char_guid"))).SendCharacterUpdate()
        End If
        Database.Update(String.Format("UPDATE guilds SET guild_leader = ""{1}"" WHERE guild_id = {0};", Client.Character.GuildID, MySQLQuery.Rows(0).Item("char_guid")))
        Database.Update(String.Format("UPDATE characters SET char_guildRank = {0} WHERE char_guid = {1};", 0, MySQLQuery.Rows(0).Item("char_guid")))
        Database.Update(String.Format("UPDATE characters SET char_guildRank = {0} WHERE char_guid = {1};", Client.Character.GuildRank, Client.Character.GUID))

        'DONE: Send notify message
        Dim response As New PacketClass(OPCODES.SMSG_GUILD_EVENT)
        response.AddInt8(GuildEvent.LEADER_CHANGED)
        response.AddInt8(2)
        response.AddString(Client.Character.Name)
        response.AddString(playerName)
        BroadcastToGuild(response, Client.Character.GuildID)
        response.Dispose()
    End Sub
    Public Sub On_MSG_SAVE_GUILD_EMBLEM(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 34 Then Exit Sub
        packet.GetInt16()
        Dim unk0 As Integer = packet.GetInt32
        Dim unk1 As Integer = packet.GetInt32
        Dim tEmblemStyle As Integer = packet.GetInt32
        Dim tEmblemColor As Integer = packet.GetInt32
        Dim tBorderStyle As Integer = packet.GetInt32
        Dim tBorderColor As Integer = packet.GetInt32
        Dim tBackgroundColor As Integer = packet.GetInt32

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] MSG_SAVE_GUILD_EMBLEM [{2},{3}] [{4}:{5}:{6}:{7}:{8}]", Client.IP, Client.Port, unk0, unk1, tEmblemStyle, tEmblemColor, tBorderStyle, tBorderColor, tBackgroundColor)

        If Not Client.Character.IsInGuild Then
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD)
            Exit Sub
        ElseIf Not Client.Character.IsGuildLeader Then
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PERMISSIONS)
            Exit Sub
        End If

        Database.Update(String.Format("UPDATE guilds SET guild_tEmblemStyle = {1}, guild_tEmblemColor = {2}, guild_tBorderStyle = {3}, guild_tBorderColor = {4}, guild_tBackgroundColor = {5} WHERE guild_id = {0};", Client.Character.GuildID, tEmblemStyle, tEmblemColor, tBorderStyle, tBorderColor, tBackgroundColor))

        SendGuildQuery(Client, Client.Character.GuildID)

        Dim packet_event As New PacketClass(OPCODES.SMSG_GUILD_EVENT)
        packet_event.AddInt8(GuildEvent.TABARDCHANGE)
        packet_event.AddInt32(Client.Character.GuildID)
        Client.Send(packet_event)
        packet_event.Dispose()

        'TODO: This tabard design costs 10g!
    End Sub
    Public Sub On_CMSG_GUILD_DISBAND(ByRef packet As PacketClass, ByRef Client As ClientClass)
        'packet.GetInt16()

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_DISBAND", Client.IP, Client.Port)

        If Not Client.Character.IsInGuild Then
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD)
            Exit Sub
        ElseIf Not Client.Character.IsGuildLeader Then
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PERMISSIONS)
            Exit Sub
        End If

        'DONE: Clear all members
        Dim q As New DataTable
        Database.Query(String.Format("SELECT char_guid FROM characters WHERE char_guildID = {0};", Client.Character.GuildID), q)

        Dim response As New PacketClass(OPCODES.SMSG_GUILD_EVENT)
        response.AddInt8(GuildEvent.DISBANDED)
        response.AddInt8(0)

        Dim GuildID As Integer = Client.Character.GuildID

        For Each r As DataRow In q.Rows
            If CHARACTERs.ContainsKey(CType(r.Item("char_guid"), Long)) Then
                RemoveCharacterFromGuild(CType(CHARACTERs(CType(r.Item("char_guid"), Long)), CharacterObject))
                CType(CHARACTERs(CType(r.Item("char_guid"), Long)), CharacterObject).Client.SendMultiplyPackets(response)
            Else
                RemoveCharacterFromGuild(CType(r.Item("char_guid"), Long))
            End If
        Next

        response.Dispose()

        'DONE: Delete guild information
        Database.Update("DELETE FROM guilds WHERE guild_id = " & GuildID & ";")
        Database.Update("DELETE FROM guildbanktabs WHERE tab_guildid = " & GuildID & ";")
    End Sub

    'Members Options
    Public Sub SendGuildMOTD(ByRef c As CharacterObject)
        If c.IsInGuild Then
            Dim MySQLQuery As New DataTable
            Database.Query("SELECT guild_MOTD FROM guilds WHERE guild_id = " & c.GuildID & ";", MySQLQuery)
            If MySQLQuery.Rows.Count = 0 Then Throw New ApplicationException("GuildID " & c.GuildID & " not found in database.")

            If MySQLQuery.Rows(0).Item("guild_MOTD") <> "" Then
                Dim response As New PacketClass(OPCODES.SMSG_GUILD_EVENT)
                response.AddInt8(GuildEvent.MOTD)
                response.AddInt8(1)
                response.AddString(MySQLQuery.Rows(0).Item("guild_MOTD"))
                c.Client.Send(response)
                response.Dispose()
            End If
        End If
    End Sub
    Public Sub On_CMSG_GUILD_MOTD(ByRef packet As PacketClass, ByRef Client As ClientClass)
        'Isn't the client even sending a null terminator for the motd if it's empty?
        If (packet.Data.Length - 1) < 6 Then Exit Sub
        packet.GetInt16()
        Dim motd As String = ""
        If packet.Length <> 4 Then motd = packet.GetString

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_MOTD", Client.IP, Client.Port)

        If Not Client.Character.IsInGuild Then
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD)
            Exit Sub
        ElseIf Not Client.Character.IsGuildRightSet(GuildRankRights.GR_RIGHT_SETMOTD) Then
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PERMISSIONS)
            Exit Sub
        End If

        Database.Update(String.Format("UPDATE guilds SET guild_MOTD = ""{1}"" WHERE guild_id = ""{0}"";", Client.Character.GuildID, motd.Replace("""", "_").Replace("'", "_")))

        Dim response As New PacketClass(OPCODES.SMSG_GUILD_EVENT)
        response.AddInt8(GuildEvent.MOTD)
        response.AddInt8(1)
        response.AddString(motd)

        'DONE: Send message to everyone in the guild
        BroadcastToGuild(response, Client.Character.GuildID)

        response.Dispose()
    End Sub
    Public Sub On_CMSG_GUILD_SET_OFFICER_NOTE(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 6 Then Exit Sub
        packet.GetInt16()
        Dim playerName As String = packet.GetString
        If (packet.Data.Length - 1) < (6 + playerName.Length + 1) Then Exit Sub
        Dim Note As String = packet.GetString

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_SET_OFFICER_NOTE [{2}]", Client.IP, Client.Port, playerName)

        If Not Client.Character.IsInGuild Then
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD)
            Exit Sub
        ElseIf Not Client.Character.IsGuildRightSet(GuildRankRights.GR_RIGHT_EOFFNOTE) Then
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PERMISSIONS)
            Exit Sub
        End If

        Database.Update(String.Format("UPDATE characters SET char_guildOffNote = ""{1}"" WHERE char_name = ""{0}"";", playerName, Note.Replace("""", "_").Replace("'", "_")))

        SendGuildRoster(Client.Character)
    End Sub
    Public Sub On_CMSG_GUILD_SET_PUBLIC_NOTE(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 6 Then Exit Sub
        packet.GetInt16()
        Dim playerName As String = packet.GetString
        If (packet.Data.Length - 1) < (6 + playerName.Length + 1) Then Exit Sub
        Dim Note As String = packet.GetString

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_SET_PUBLIC_NOTE [{2}]", Client.IP, Client.Port, playerName)

        If Not Client.Character.IsInGuild Then
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD)
            Exit Sub
        ElseIf Not Client.Character.IsGuildRightSet(GuildRankRights.GR_RIGHT_EPNOTE) Then
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PERMISSIONS)
            Exit Sub
        End If

        Database.Update(String.Format("UPDATE characters SET char_guildPNote = ""{1}"" WHERE char_name = ""{0}"";", playerName, Note.Replace("""", "_").Replace("'", "_")))

        SendGuildRoster(Client.Character)
    End Sub
    Public Sub On_CMSG_GUILD_REMOVE(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 6 Then Exit Sub
        packet.GetInt16()
        Dim playerName As String = packet.GetString

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_REMOVE [{2}]", Client.IP, Client.Port, playerName)

        'DONE: Player1 checks
        If Not Client.Character.IsInGuild Then
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD)
            Exit Sub
        ElseIf Not Client.Character.IsGuildRightSet(GuildRankRights.GR_RIGHT_REMOVE) Then
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PERMISSIONS)
            Exit Sub
        End If

        'DONE: Find player2's guid
        Dim q As New DataTable
        Database.Query("SELECT char_guid FROM characters WHERE char_name = '" & playerName.Replace("'", "_") & "';", q)

        'DONE: Removed checks
        If q.Rows.Count = 0 Then
            SendGuildResult(Client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_PLAYER_NOT_FOUND, playerName)
            Exit Sub
        ElseIf Not CHARACTERs.ContainsKey(CType(q.Rows(0).Item("char_guid"), Long)) Then
            SendGuildResult(Client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_PLAYER_NOT_FOUND, playerName)
            Exit Sub
        End If

        Dim c As CharacterObject = CType(CHARACTERs(CType(q.Rows(0).Item("char_guid"), Long)), CharacterObject)

        If c.IsGuildLeader Then
            SendGuildResult(Client, GuildCommand.GUILD_QUIT_S, GuildError.GUILD_LEADER_LEAVE)
            Exit Sub
        End If

        RemoveCharacterFromGuild(c)

        'DONE: Send guild event
        Dim response As New PacketClass(OPCODES.SMSG_GUILD_EVENT)
        response.AddInt8(GuildEvent.REMOVED)
        response.AddInt8(2)
        response.AddString(playerName)
        response.AddString(c.Name)
        BroadcastToGuild(response, c.GuildID)
        response.Dispose()
    End Sub
    Public Sub On_CMSG_GUILD_PROMOTE(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 6 Then Exit Sub
        packet.GetInt16()
        Dim playerName As String = CapitalizeName(packet.GetString)

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_PROMOTE [{2}]", Client.IP, Client.Port, playerName)

        If Not Client.Character.IsInGuild Then
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD)
            Exit Sub
        ElseIf Not Client.Character.IsGuildRightSet(GuildRankRights.GR_RIGHT_PROMOTE) Then
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PERMISSIONS)
            Exit Sub
        End If

        'DONE: Find promoted player's guid
        Dim q As New DataTable
        Database.Query("SELECT char_guid FROM characters WHERE char_name = '" & playerName.Replace("'", "_") & "';", q)

        'DONE: Promoted checks
        If q.Rows.Count = 0 Then
            SendGuildResult(Client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_NAME_INVALID)
            Exit Sub
        ElseIf Not CHARACTERs.ContainsKey(CType(q.Rows(0).Item("char_guid"), Long)) Then
            SendGuildResult(Client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_PLAYER_NOT_FOUND, playerName)
            Exit Sub
        End If
        Dim c As CharacterObject = CType(CHARACTERs(CType(q.Rows(0).Item("char_guid"), Long)), CharacterObject)
        If c.GuildID <> Client.Character.GuildID Then
            SendGuildResult(Client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD_S, playerName)
            Exit Sub
        ElseIf (c.GuildRank - 1) <= Client.Character.GuildRank Then
            SendGuildResult(Client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_PERMISSIONS)
            Exit Sub
        ElseIf c.GuildRank = GUILD_RANK_MIN Then
            SendGuildResult(Client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_INTERNAL)
            Exit Sub
        End If

        'DONE: Do the real update
        c.GuildRank -= 1
        Database.Update(String.Format("UPDATE characters SET char_guildRank = {0} WHERE char_guid = {1};", c.GuildRank, c.GUID))
        c.SetUpdateFlag(EPlayerFields.PLAYER_GUILDRANK, c.GuildRank)
        c.SendCharacterUpdate(True)

        'DONE: Get rank name
        q.Clear()
        Database.Query(String.Format("SELECT guild_rank{0} FROM guilds WHERE guild_id = {1} LIMIT 1;", c.GuildRank, c.GuildID), q)
        If q.Rows.Count = 0 Then Throw New ApplicationException("Guild rank " & c.GuildRank & " for guild " & c.GuildID & " not found!")

        'DONE: Send event to guild
        Dim response As New PacketClass(OPCODES.SMSG_GUILD_EVENT)
        response.AddInt8(GuildEvent.PROMOTION)
        response.AddInt8(3)
        response.AddString(c.Name)
        response.AddString(playerName)
        response.AddString(q.Rows(0).Item("guild_rank" & c.GuildRank))
        BroadcastToGuild(response, c.GuildID)
        response.Dispose()
    End Sub
    Public Sub On_CMSG_GUILD_DEMOTE(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 6 Then Exit Sub
        packet.GetInt16()
        Dim playerName As String = CapitalizeName(packet.GetString)

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_DEMOTE [{2}]", Client.IP, Client.Port, playerName)

        If Not Client.Character.IsInGuild Then
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD)
            Exit Sub
        ElseIf Not Client.Character.IsGuildRightSet(GuildRankRights.GR_RIGHT_PROMOTE) Then
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PERMISSIONS)
            Exit Sub
        End If

        'DONE: Find promoted player's guid
        Dim q As New DataTable
        Database.Query("SELECT char_guid FROM characters WHERE char_name = '" & playerName.Replace("'", "_") & "';", q)

        'DONE: Promoted checks
        If q.Rows.Count = 0 Then
            SendGuildResult(Client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_NAME_INVALID)
            Exit Sub
        ElseIf Not CHARACTERs.ContainsKey(CType(q.Rows(0).Item("char_guid"), Long)) Then
            SendGuildResult(Client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_PLAYER_NOT_FOUND, playerName)
            Exit Sub
        End If
        Dim c As CharacterObject = CType(CHARACTERs(CType(q.Rows(0).Item("char_guid"), Long)), CharacterObject)
        If c.GuildID <> Client.Character.GuildID Then
            SendGuildResult(Client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD_S, playerName)
            Exit Sub
        ElseIf c.GuildRank <= Client.Character.GuildRank Then
            SendGuildResult(Client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_PERMISSIONS)
            Exit Sub
        ElseIf c.GuildRank = GUILD_RANK_MAX Then
            SendGuildResult(Client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_INTERNAL)
            Exit Sub
        End If

        'DONE: Max defined rank check
        q.Clear()
        Database.Query(String.Format("SELECT guild_rank{0} FROM guilds WHERE guild_id = {1} LIMIT 1;", c.GuildRank + 1, c.GuildID), q)
        If q.Rows.Count = 0 Then
            SendGuildResult(Client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_INTERNAL)
            Exit Sub
        ElseIf Trim(q.Rows(0).Item("guild_rank" & c.GuildRank + 1)) = "" Then
            SendGuildResult(Client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_INTERNAL)
            Exit Sub
        End If

        'DONE: Do the real update
        c.GuildRank += 1
        Database.Update(String.Format("UPDATE characters SET char_guildRank = {0} WHERE char_guid = {1};", c.GuildRank, c.GUID))
        c.SetUpdateFlag(EPlayerFields.PLAYER_GUILDRANK, c.GuildRank)
        c.SendCharacterUpdate(True)

        'DONE: Get rank name
        q.Clear()
        Database.Query(String.Format("SELECT guild_rank{0} FROM guilds WHERE guild_id = {1} LIMIT 1;", c.GuildRank, c.GuildID), q)
        If q.Rows.Count = 0 Then Throw New ApplicationException("Guild rank " & c.GuildRank & " for guild " & c.GuildID & " not found!")

        'DONE: Send event to guild
        Dim response As New PacketClass(OPCODES.SMSG_GUILD_EVENT)
        response.AddInt8(GuildEvent.DEMOTION)
        response.AddInt8(3)
        response.AddString(c.Name)
        response.AddString(playerName)
        response.AddString(q.Rows(0).Item("guild_rank" & c.GuildRank))
        BroadcastToGuild(response, c.GuildID)
        response.Dispose()
    End Sub

    'User Options
    Public Sub On_CMSG_GUILD_INVITE(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 6 Then Exit Sub
        packet.GetInt16()
        Dim playerName As String = packet.GetString

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_INVITE [{2}]", Client.IP, Client.Port, playerName)

        'DONE: Inviter checks
        If Not Client.Character.IsInGuild Then
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD)
            Exit Sub
        ElseIf Not Client.Character.IsGuildRightSet(GuildRankRights.GR_RIGHT_INVITE) Then
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PERMISSIONS)
            Exit Sub
        End If

        'DONE: Find invited player's guid
        Dim q As New DataTable
        Database.Query("SELECT char_guid FROM characters WHERE char_name = '" & playerName.Replace("'", "_") & "';", q)

        'DONE: Invited checks
        If q.Rows.Count = 0 Then
            SendGuildResult(Client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_NAME_INVALID)
            Exit Sub
        ElseIf Not CHARACTERs.ContainsKey(CType(q.Rows(0).Item("char_guid"), Long)) Then
            SendGuildResult(Client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_PLAYER_NOT_FOUND, playerName)
            Exit Sub
        End If

        Dim c As CharacterObject = CType(CHARACTERs(CType(q.Rows(0).Item("char_guid"), Long)), CharacterObject)
        If c.GuildID <> 0 Then
            SendGuildResult(Client, GuildCommand.GUILD_INVITE_S, GuildError.ALREADY_IN_GUILD, playerName)
            Exit Sub
        ElseIf c.Side <> Client.Character.Side Then
            SendGuildResult(Client, GuildCommand.GUILD_INVITE_S, GuildError.GUILD_NOT_ALLIED, playerName)
            Exit Sub
        ElseIf c.GuildInvited <> 0 Then
            SendGuildResult(Client, GuildCommand.GUILD_INVITE_S, GuildError.ALREADY_INVITED_TO_GUILD, playerName)
            Exit Sub
        End If

        'DONE: Get guild info and send invitation
        q.Clear()
        Database.Query("SELECT guild_name FROM guilds WHERE guild_id = " & Client.Character.GuildID & ";", q)
        If q.Rows.Count = 0 Then
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_INTERNAL)
            Exit Sub
        End If

        Dim response As New PacketClass(OPCODES.SMSG_GUILD_INVITE)
        response.AddString(Client.Character.Name)
        response.AddString(q.Rows(0).Item("guild_name"))
        c.Client.Send(response)
        response.Dispose()

        c.GuildInvited = Client.Character.GuildID
        c.GuildInvitedBy = Client.Character.GUID
    End Sub
    Public Sub On_CMSG_GUILD_ACCEPT(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If Client.Character.GuildInvited = 0 Then Throw New ApplicationException("Character accepting guild invitation whihtout being invited.")

        AddCharacterToGuild(Client.Character, Client.Character.GuildInvited)
        Client.Character.GuildInvited = 0

        Dim response As New PacketClass(OPCODES.SMSG_GUILD_EVENT)
        response.AddInt8(GuildEvent.JOINED)
        response.AddInt8(1)
        response.AddString(Client.Character.Name)
        BroadcastToGuild(response, Client.Character.GuildID)
        response.Dispose()
    End Sub
    Public Sub On_CMSG_GUILD_DECLINE(ByRef packet As PacketClass, ByRef Client As ClientClass)
        Client.Character.GuildInvited = 0

        If CHARACTERs.ContainsKey(CType(Client.Character.GuildInvitedBy, Long)) Then
            Dim response As New PacketClass(OPCODES.SMSG_GUILD_DECLINE)
            response.AddString(Client.Character.Name)
            CHARACTERs(CType(Client.Character.GuildInvitedBy, Long)).Client.Send(response)
            response.Dispose()
        End If
    End Sub
    Public Sub On_CMSG_GUILD_LEAVE(ByRef packet As PacketClass, ByRef Client As ClientClass)
        'packet.GetInt16()

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_LEAVE", Client.IP, Client.Port)

        'DONE: Checks
        If Not Client.Character.IsInGuild Then
            SendGuildResult(Client, GuildCommand.GUILD_CREATE_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD)
            Exit Sub
        ElseIf Client.Character.IsGuildLeader Then
            SendGuildResult(Client, GuildCommand.GUILD_QUIT_S, GuildError.GUILD_LEADER_LEAVE)
            Exit Sub
        End If

        Dim GuildID As Integer = Client.Character.GuildID

        RemoveCharacterFromGuild(Client.Character)
        SendGuildResult(Client, GuildCommand.GUILD_QUIT_S, GuildError.GUILD_PLAYER_NO_MORE_IN_GUILD, Client.Character.Name)

        Dim response As New PacketClass(OPCODES.SMSG_GUILD_EVENT)
        response.AddInt8(GuildEvent.LEFT)
        response.AddInt8(1)
        response.AddString(Client.Character.Name)
        BroadcastToGuild(response, GuildID)
        response.Dispose()
    End Sub

    'Guild Bank
    Public Sub On_CMSG_GUILD_BANKER_ACTIVATE(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 14 Then Exit Sub
        packet.GetInt16()
        Dim GUID As ULong = packet.GetUInt64
        Dim Unk As Byte = packet.GetInt8
        If WORLD_GAMEOBJECTs.ContainsKey(GUID) = False OrElse WORLD_GAMEOBJECTs(GUID).Type <> GameObjectType.GAMEOBJECT_TYPE_GUILD_BANK Then Exit Sub
        If Client.Character.GuildID = 0 Then Exit Sub
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_BANKER_ACTIVATE [GUID={2:X} Unk={3}]", Client.IP, Client.Port, GUID, Unk)

        If Client.Character.GuildID > 0 Then
            SendGuildBankInfo(Client.Character)
            Exit Sub
        End If

        SendGuildResult(Client, GuildCommand.GUILD_BANK_S, GuildError.GUILD_PLAYER_NOT_IN_GUILD)
    End Sub
    Public Sub On_CMSG_GUILD_BANK_QUERY_TAB(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 15 Then Exit Sub
        packet.GetInt16()
        Dim GUID As ULong = packet.GetUInt64
        Dim TabID As Byte = packet.GetInt8
        Dim Unk1 As Byte = packet.GetInt8
        If WORLD_GAMEOBJECTs.ContainsKey(GUID) = False OrElse WORLD_GAMEOBJECTs(GUID).Type <> GameObjectType.GAMEOBJECT_TYPE_GUILD_BANK Then Exit Sub
        If Client.Character.GuildID = 0 Then Exit Sub
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_BANK_QUERY_TAB [GUID={2:X} TabID={3} Unk1={4}]", Client.IP, Client.Port, GUID, TabID, Unk1)

        'TODO: Send withraw

        SendGuildBankContent(Client.Character, TabID)
    End Sub
    Public Sub On_CMSG_GUILD_BANK_BUY_TAB(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 14 Then Exit Sub
        packet.GetInt16()
        Dim GUID As ULong = packet.GetUInt64
        Dim TabID As Byte = packet.GetInt8
        If WORLD_GAMEOBJECTs.ContainsKey(GUID) = False OrElse WORLD_GAMEOBJECTs(GUID).Type <> GameObjectType.GAMEOBJECT_TYPE_GUILD_BANK Then Exit Sub
        If Client.Character.GuildID = 0 Then Exit Sub
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_BANK_BUY_TAB [GUID={2:X} TabID={3}]", Client.IP, Client.Port, GUID, TabID)

        Dim BankTabPrice As Integer = GetGuildBankTabPrice(TabID) * 10000 'Turn it into gold
        If BankTabPrice = 0 Then Exit Sub
        If Client.Character.Copper < BankTabPrice Then Exit Sub

        Dim q As New DataTable
        Database.Query(String.Format("SELECT guild_banktabs FROM guilds WHERE guild_id = {0}", Client.Character.GuildID), q)
        If q.Rows.Count = 0 Then Exit Sub
        If CByte(q.Rows(0).Item("guild_banktabs")) >= 6 Then Exit Sub
        If CByte(q.Rows(0).Item("guild_banktabs")) <> TabID Then Exit Sub

        'TODO: Create the tab
        Database.Update(String.Format("DELETE FROM guildbanktabs WHERE tab_guildid={0} AND tab_id={1}", Client.Character.GuildID, CByte(q.Rows(0).Item("guild_banktabs"))))
        Database.Update(String.Format("INSERT INTO guildbanktabs (tab_id, tab_guildid, tab_name, tab_icon, tab_text) VALUES ({1},{0},'Tab {0}','','')", Client.Character.GuildID, CByte(q.Rows(0).Item("guild_banktabs"))))
        Database.Update(String.Format("UPDATE guilds SET guild_banktabs = {1} WHERE guild_id = {0}", Client.Character.GuildID, CByte(q.Rows(0).Item("guild_banktabs")) + 1))
        Client.Character.Copper -= BankTabPrice
        Client.Character.SetUpdateFlag(EPlayerFields.PLAYER_FIELD_COINAGE, Client.Character.Copper)
        Client.Character.SendCharacterUpdate(False)
        SendGuildRoster(Client.Character)
        SendGuildBankInfo(Client.Character)
    End Sub
    Public Sub On_CMSG_GUILD_BANK_DEPOSIT_MONEY(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 17 Then Exit Sub
        packet.GetInt16()
        Dim GUID As ULong = packet.GetUInt64
        Dim Money As UInteger = packet.GetUInt32
        If WORLD_GAMEOBJECTs.ContainsKey(GUID) = False OrElse WORLD_GAMEOBJECTs(GUID).Type <> GameObjectType.GAMEOBJECT_TYPE_GUILD_BANK Then Exit Sub
        If Client.Character.GuildID = 0 OrElse Money > Client.Character.Copper Then Exit Sub
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_BANK_DEPOSIT_MONEY [GUID={2:X} Money={3}]", Client.IP, Client.Port, GUID, Money)

        Dim q As New DataTable
        Database.Query(String.Format("SELECT guild_bankmoney FROM guilds WHERE guild_id = {0}", Client.Character.GuildID), q)
        If q.Rows.Count = 0 Then Exit Sub

        If (CULng(q.Rows(0).Item("guild_bankmoney")) + CULng(Money)) > ULong.MaxValue Then Money = ULong.MaxValue - CULng(q.Rows(0).Item("guild_bankmoney"))
        Dim guildMoney As ULong = CULng(q.Rows(0).Item("guild_bankmoney")) + Money

        Client.Character.Copper -= Money
        Client.Character.SetUpdateFlag(EPlayerFields.PLAYER_FIELD_COINAGE, Client.Character.Copper)
        Client.Character.SendCharacterUpdate(False)

        Database.Update(String.Format("UPDATE guilds SET guild_bankmoney = '{1}' WHERE guild_id = {0}", Client.Character.GuildID, guildMoney))
    End Sub
    Public Sub On_CMSG_SET_GUILD_BANK_TEXT(ByRef packet As PacketClass, ByRef Client As ClientClass)
        Try
            If (packet.Data.Length - 1) < 7 Then Exit Sub
            packet.GetInt16()
            Dim TabID As Byte = packet.GetInt8
            Dim Text As String = packet.GetString
            If Client.Character.GuildID = 0 OrElse TabID >= 6 Then Exit Sub
            Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_SET_GUILD_BANK_TEXT [TabID={2}]", Client.IP, Client.Port, TabID)

            'Done: Limit the text size to 500
            If Text.Length > 0 Then
                If System.Text.UTF8Encoding.UTF8.GetByteCount(Text.ToCharArray) > 500 Then
                    Dim tmpBytes(499) As Byte
                    Array.Copy(System.Text.UTF8Encoding.UTF8.GetBytes(Text.ToCharArray), 0, tmpBytes, 0, 500)
                    Text = System.Text.UTF8Encoding.UTF8.GetString(tmpBytes)
                End If
            End If
            Log.WriteLine(LogType.DEBUG, "Text = {0}", Text)

            Database.Update(String.Format("UPDATE guildbanktabs SET tab_text='{2}' WHERE tab_guildid = {0} AND tab_id = {1} and tab_text <> '{2}'", Client.Character.GuildID, TabID, Text))
        Catch e As Exception
            Log.WriteLine(LogType.CRITICAL, "Error while setting tab text.{0}", vbNewLine & e.ToString)
        End Try
    End Sub
    Public Sub On_CMSG_GUILD_BANK_UPDATE_TAB(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 15 Then Exit Sub
        packet.GetInt16()
        Dim GUID As ULong = packet.GetUInt64
        Dim TabID As Byte = packet.GetInt8
        Dim Name As String = packet.GetString
        If (packet.Data.Length - 1) < 15 + Name.Length + 1 Then Exit Sub
        Dim Icon As String = packet.GetString
        If WORLD_GAMEOBJECTs.ContainsKey(GUID) = False OrElse WORLD_GAMEOBJECTs(GUID).Type <> GameObjectType.GAMEOBJECT_TYPE_GUILD_BANK Then Exit Sub
        If Client.Character.GuildID = 0 OrElse TabID >= 6 OrElse Name.Length = 0 OrElse Icon.Length = 0 Then Exit Sub
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GUILD_BANK_UPDATE_TAB [GUID={2:X} TabID={2}]", Client.IP, Client.Port, GUID, TabID)

        Database.Update(String.Format("UPDATE guildbanktabs SET tab_name='{2}', tab_icon='{3}' WHERE tab_guildid = {0} AND tab_id = {1}", Client.Character.GuildID, TabID, Name, Icon))

        SendGuildBankInfo(Client.Character)
        SendGuildBankContent(Client.Character, TabID)
    End Sub
    Public Sub On_MSG_GUILD_BANK_LOG_QUERY(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 6 Then Exit Sub
        packet.GetInt16()
        Dim TabID As Byte = packet.GetInt8
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] MSG_GUILD_BANK_LOG_QUERY [TabID={2}]", Client.IP, Client.Port, TabID)
    End Sub
    Public Sub On_MSG_QUERY_GUILD_BANK_TEXT(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 6 Then Exit Sub
        packet.GetInt16()
        Dim TabID As Byte = packet.GetInt8
        If Client.Character.GuildID = 0 OrElse TabID >= 6 Then Exit Sub
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] MSG_QUERY_GUILD_BANK_TEXT [TabID={2}]", Client.IP, Client.Port, TabID)

        Dim q As New DataTable
        Database.Query(String.Format("SELECT tab_text FROM guildbanktabs WHERE tab_guildid = {0} AND tab_id = {1}", Client.Character.GuildID, TabID), q)
        If q.Rows.Count = 0 Then Exit Sub

        Dim response As New PacketClass(OPCODES.MSG_QUERY_GUILD_BANK_TEXT)
        response.AddInt8(TabID)
        response.AddString(q.Rows(0).Item("tab_text"))
        Client.Send(response)
        response.Dispose()
    End Sub

    'Helping Subs
    Public Enum GuildCommand As Byte
        GUILD_CREATE_S = &H0
        GUILD_INVITE_S = &H1
        GUILD_QUIT_S = &H3
        GUILD_FOUNDER_S = &HE
        GUILD_UNK1 = &H10
        GUILD_BANK_S = &H15
        GUILD_UNK3 = &H16
    End Enum
    Public Enum GuildError As Byte
        GUILD_PLAYER_NO_MORE_IN_GUILD = &H0
        GUILD_INTERNAL = &H1
        GUILD_ALREADY_IN_GUILD = &H2
        ALREADY_IN_GUILD = &H3
        INVITED_TO_GUILD = &H4
        ALREADY_INVITED_TO_GUILD = &H5
        GUILD_NAME_INVALID = &H6
        GUILD_NAME_EXISTS = &H7
        GUILD_LEADER_LEAVE = &H8
        GUILD_PERMISSIONS = &H8
        GUILD_PLAYER_NOT_IN_GUILD = &H9
        GUILD_PLAYER_NOT_IN_GUILD_S = &HA
        GUILD_PLAYER_NOT_FOUND = &HB
        GUILD_NOT_ALLIED = &HC
        GUILD_RANK_TOO_HIGH_S = &HD
        GUILD_ALREADY_LOWEST_RANK_S = &HE
        GUILD_TEMP_ERROR = &H11
        GUILD_RANK_IN_USE = &H12
        GUILD_IGNORE = &H13
        GUILD_ERR_UNK1 = &H17
        GUILD_WITHDRAW_TOO_MUCH = &H18
        GUILD_BANK_NO_MONEY = &H19
        GUILD_BANK_TAB_IS_FULL = &H1B
        GUILD_BANK_ITEM_NOT_FOUND = &H1C
    End Enum
    Public Sub SendGuildResult(ByRef Client As ClientClass, ByVal Command As GuildCommand, ByVal Result As GuildError, Optional ByVal Text As String = "")
        Dim response As New PacketClass(OPCODES.SMSG_GUILD_COMMAND_RESULT)
        response.AddInt32(Command)
        response.AddString(Text)
        response.AddInt32(Result)
        Client.Send(response)
        response.Dispose()
    End Sub

    Public Enum GuildEvent As Byte
        PROMOTION = 0           'uint8(2), string(name), string(rankName)
        DEMOTION = 1            'uint8(2), string(name), string(rankName)
        MOTD = 2                'uint8(1), string(text)                                             'Guild message of the day: <text>
        JOINED = 3              'uint8(1), string(name)                                             '<name> has joined the guild.
        LEFT = 4                'uint8(1), string(name)                                             '<name> has left the guild.
        REMOVED = 5             '??
        LEADER_IS = 6           'uint8(1), string(name                                              '<name> is the leader of your guild.
        LEADER_CHANGED = 7      'uint8(2), string(oldLeaderName), string(newLeaderName)
        DISBANDED = 8           'uint8(0)                                                           'Your guild has been disbanded.
        TABARDCHANGE = 9        '??
    End Enum

#End Region

End Module