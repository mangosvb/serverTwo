'
' Copyright (C) 2013 getMaNGOS <http://www.getMangos.co.uk>
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
Imports System.Collections.Generic
Imports mangosVB.Common.BaseWriter

Public Module WS_Handlers_Misc

    Public Function SelectMonsterSay(ByVal MonsterID As Integer) As String
        ' Select Random Text Field From Monster Say HashTable(s)
        ' TODO: Allow This To Work With Different Monster Say Events Besides Combat
        Dim TextCount As Integer = 0
        Dim RandomText As Integer = 0

        If Trim((CType(MonsterSay(MonsterID), TMonsterSay)).Text0) <> "" Then TextCount += 1
        If Trim((CType(MonsterSay(MonsterID), TMonsterSay)).Text1) <> "" Then TextCount += 1
        If Trim((CType(MonsterSay(MonsterID), TMonsterSay)).Text2) <> "" Then TextCount += 1
        If Trim((CType(MonsterSay(MonsterID), TMonsterSay)).Text3) <> "" Then TextCount += 1
        If Trim((CType(MonsterSay(MonsterID), TMonsterSay)).Text4) <> "" Then TextCount += 1

        RandomText = Rnd.Next(1, TextCount + 1)

        SelectMonsterSay = ""

        Select Case RandomText
            Case 1
                SelectMonsterSay = (CType(MonsterSay(MonsterID), TMonsterSay)).Text0
            Case 2
                SelectMonsterSay = (CType(MonsterSay(MonsterID), TMonsterSay)).Text1
            Case 3
                SelectMonsterSay = (CType(MonsterSay(MonsterID), TMonsterSay)).Text2
            Case 4
                SelectMonsterSay = (CType(MonsterSay(MonsterID), TMonsterSay)).Text3
            Case 5
                SelectMonsterSay = (CType(MonsterSay(MonsterID), TMonsterSay)).Text4
        End Select

    End Function

    Public Sub On_CMSG_NAME_QUERY(ByRef packet As PacketClass, ByRef Client As ClientClass)
        Try
            If (packet.Data.Length - 1) < 13 Then Exit Sub
            packet.GetInt16()
            Dim GUID As ULong = packet.GetUInt64()
            Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_NAME_QUERY [GUID={2:X}]", Client.IP, Client.Port, GUID)
            Dim SMSG_NAME_QUERY_RESPONSE As New PacketClass(OPCODES.SMSG_NAME_QUERY_RESPONSE)

            'RESERVED For Warden Bot
            If GUID = WardenGUID Then
                SMSG_NAME_QUERY_RESPONSE.AddUInt64(GUID)
                SMSG_NAME_QUERY_RESPONSE.AddString(WardenNAME)
                SMSG_NAME_QUERY_RESPONSE.AddInt8(0)
                SMSG_NAME_QUERY_RESPONSE.AddInt32(1)
                SMSG_NAME_QUERY_RESPONSE.AddInt32(1)
                SMSG_NAME_QUERY_RESPONSE.AddInt32(1)
                'SMSG_NAME_QUERY_RESPONSE.AddInt8(0)
                Client.Send(SMSG_NAME_QUERY_RESPONSE)
                SMSG_NAME_QUERY_RESPONSE.Dispose()
                Exit Sub
            End If

            'Asking for player name
            If GuidIsPlayer(GUID) Then
                If CHARACTERs.ContainsKey(GUID) = True Then
                    SMSG_NAME_QUERY_RESPONSE.AddUInt64(GUID)
                    SMSG_NAME_QUERY_RESPONSE.AddString(CType(CHARACTERs(GUID), CharacterObject).Name)
                    SMSG_NAME_QUERY_RESPONSE.AddInt8(0) ' Realm Name I think( if player from another realm)?
                    SMSG_NAME_QUERY_RESPONSE.AddInt32(CType(CHARACTERs(GUID), CharacterObject).Race)
                    SMSG_NAME_QUERY_RESPONSE.AddInt32(CType(CHARACTERs(GUID), CharacterObject).Gender)
                    SMSG_NAME_QUERY_RESPONSE.AddInt32(CType(CHARACTERs(GUID), CharacterObject).Classe)
                    'SMSG_NAME_QUERY_RESPONSE.AddInt8(0) ' Unknown (came in 2.4) | Removed in WoTLK
                    Client.Send(SMSG_NAME_QUERY_RESPONSE)
                    SMSG_NAME_QUERY_RESPONSE.Dispose()
                    Exit Sub
                Else
                    Dim MySQLQuery As New DataTable
                    Database.Query(String.Format("SELECT char_name, char_race, char_class, char_gender FROM characters WHERE char_guid = ""{0}"";", GUID), MySQLQuery)

                    If MySQLQuery.Rows.Count > 0 Then
                        SMSG_NAME_QUERY_RESPONSE.AddUInt64(GUID)
                        SMSG_NAME_QUERY_RESPONSE.AddString(CType(MySQLQuery.Rows(0).Item("char_name"), String))
                        SMSG_NAME_QUERY_RESPONSE.AddInt8(0)
                        SMSG_NAME_QUERY_RESPONSE.AddInt32(CType(MySQLQuery.Rows(0).Item("char_race"), Integer))
                        SMSG_NAME_QUERY_RESPONSE.AddInt32(CType(MySQLQuery.Rows(0).Item("char_gender"), Integer))
                        SMSG_NAME_QUERY_RESPONSE.AddInt32(CType(MySQLQuery.Rows(0).Item("char_class"), Integer))
                        'SMSG_NAME_QUERY_RESPONSE.AddInt8(0) ' Unknown (came in 2.4) | Removed in WoTLK
                        Client.Send(SMSG_NAME_QUERY_RESPONSE)
                        SMSG_NAME_QUERY_RESPONSE.Dispose()
                    Else
                        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] SMSG_NAME_QUERY_RESPONSE [Character GUID={2:X} not found]", Client.IP, Client.Port, GUID)
                    End If

                    MySQLQuery.Dispose()
                    Exit Sub
                End If
            End If

            'Asking for creature name (only used in quests?)
            If GuidIsCreature(GUID) Then
                If WORLD_CREATUREs.ContainsKey(GUID) Then
                    SMSG_NAME_QUERY_RESPONSE.AddUInt64(GUID)
                    SMSG_NAME_QUERY_RESPONSE.AddString(CType(WORLD_CREATUREs(GUID), CreatureObject).Name)
                    SMSG_NAME_QUERY_RESPONSE.AddInt8(0)
                    SMSG_NAME_QUERY_RESPONSE.AddInt32(0)
                    SMSG_NAME_QUERY_RESPONSE.AddInt32(0)
                    SMSG_NAME_QUERY_RESPONSE.AddInt32(0)
                    'SMSG_NAME_QUERY_RESPONSE.AddInt8(0) ' Unknown (came in 2.4) | Removed in WoTLK
                    Client.Send(SMSG_NAME_QUERY_RESPONSE)
                    SMSG_NAME_QUERY_RESPONSE.Dispose()
                Else
                    Log.WriteLine(LogType.DEBUG, "[{0}:{1}] SMSG_NAME_QUERY_RESPONSE [Creature GUID={2:X} not found]", Client.IP, Client.Port, GUID)
                End If
            End If
        Catch e As Exception
            Log.WriteLine(LogType.CRITICAL, "Error at name query.{0}", vbNewLine & e.ToString)
        End Try
    End Sub

    Public Sub On_CMSG_TUTORIAL_FLAG(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 9 Then Exit Sub
        packet.GetInt16()
        Dim Flag As Integer = packet.GetInt32()
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_TUTORIAL_FLAG [flag={2}]", Client.IP, Client.Port, Flag)

        Client.Character.TutorialFlags((Flag \ 8)) = Client.Character.TutorialFlags((Flag \ 8)) + (1 << 7 - (Flag Mod 8))
        Client.Character.SaveCharacter()
    End Sub
    Public Sub On_CMSG_TUTORIAL_CLEAR(ByRef packet As PacketClass, ByRef Client As ClientClass)
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_TUTORIAL_CLEAR", Client.IP, Client.Port)

        Dim i As Integer
        For i = 0 To 31
            Client.Character.TutorialFlags(i) = 255
        Next
        Client.Character.SaveCharacter()
    End Sub
    Public Sub On_CMSG_TUTORIAL_RESET(ByRef packet As PacketClass, ByRef Client As ClientClass)
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_TUTORIAL_RESET", Client.IP, Client.Port)

        Dim i As Integer
        For i = 0 To 31
            Client.Character.TutorialFlags(i) = 0
        Next
        Client.Character.SaveCharacter()
    End Sub
    Public Sub On_CMSG_TOGGLE_HELM(ByRef packet As PacketClass, ByRef Client As ClientClass)
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_TOGGLE_HELM", Client.IP, Client.Port)

        If (Client.Character.cPlayerFlags And PlayerFlags.PLAYER_FLAG_HIDE_HELM) Then
            Client.Character.cPlayerFlags = Client.Character.cPlayerFlags And (Not PlayerFlags.PLAYER_FLAG_HIDE_HELM)
        Else
            Client.Character.cPlayerFlags = Client.Character.cPlayerFlags Or PlayerFlags.PLAYER_FLAG_HIDE_HELM
        End If

        Client.Character.SetUpdateFlag(EPlayerFields.PLAYER_FLAGS, Client.Character.cPlayerFlags)
        Client.Character.SendCharacterUpdate(True)
    End Sub
    Public Sub On_CMSG_TOGGLE_CLOAK(ByRef packet As PacketClass, ByRef Client As ClientClass)
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_TOGGLE_CLOAK", Client.IP, Client.Port)

        If (Client.Character.cPlayerFlags And PlayerFlags.PLAYER_FLAG_HIDE_CLOAK) Then
            Client.Character.cPlayerFlags = Client.Character.cPlayerFlags And (Not PlayerFlags.PLAYER_FLAG_HIDE_CLOAK)
        Else
            Client.Character.cPlayerFlags = Client.Character.cPlayerFlags Or PlayerFlags.PLAYER_FLAG_HIDE_CLOAK
        End If

        Client.Character.SetUpdateFlag(EPlayerFields.PLAYER_FLAGS, Client.Character.cPlayerFlags)
        Client.Character.SendCharacterUpdate(True)
    End Sub
    Public Sub On_CMSG_MOUNTSPECIAL_ANIM(ByRef packet As PacketClass, ByRef Client As ClientClass)
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_MOUNTSPECIAL_ANIM", Client.IP, Client.Port)

        Dim response As New PacketClass(OPCODES.SMSG_MOUNTSPECIAL_ANIM)
        response.AddPackGUID(Client.Character.GUID)
        Client.Character.SendToNearPlayers(response)
        response.Dispose()
    End Sub
    Public Sub On_CMSG_EMOTE(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 9 Then Exit Sub
        packet.GetInt16()
        Dim emoteID As Integer = packet.GetInt32
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_EMOTE [{2}]", Client.IP, Client.Port, emoteID)

        Dim response As New PacketClass(OPCODES.SMSG_EMOTE)
        response.AddInt32(emoteID)
        response.AddUInt64(Client.Character.GUID)
        Client.SendMultiplyPackets(response)
        Client.Character.SendToNearPlayers(response)
        response.Dispose()
    End Sub
    Public Sub On_CMSG_TEXT_EMOTE(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 21 Then Exit Sub
        packet.GetInt16()
        Dim TextEmote As Integer = packet.GetInt32
        Dim Unk As Integer = packet.GetInt32
        Dim GUID As ULong = packet.GetUInt64

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_TEXT_EMOTE [TextEmote={2} Unk={3}]", Client.IP, Client.Port, TextEmote, Unk)

        'DONE: Send Emote animation
        If EmotesText.ContainsKey(TextEmote) Then
            Dim SMSG_EMOTE As New PacketClass(OPCODES.SMSG_EMOTE)
            SMSG_EMOTE.AddInt32(EmotesText(TextEmote))
            SMSG_EMOTE.AddUInt64(Client.Character.GUID)
            Client.SendMultiplyPackets(SMSG_EMOTE)
            Client.Character.SendToNearPlayers(SMSG_EMOTE)
            SMSG_EMOTE.Dispose()

            Client.Character.cEmoteState = EmotesText(TextEmote)
            Client.Character.SetUpdateFlag(EUnitFields.UNIT_NPC_EMOTESTATE, Client.Character.cEmoteState)
            Client.Character.SendCharacterUpdate(True)
        End If

        'DONE: Find Creature/Player with the recv GUID
        Dim secondName As String = ""
        If GUID > 0 Then
            If CHARACTERs.ContainsKey(GUID) Then
                secondName = CHARACTERs(GUID).Name
            ElseIf WORLD_CREATUREs.ContainsKey(GUID) Then
                secondName = WORLD_CREATUREs(GUID).Name
            End If
        End If

        Dim SMSG_TEXT_EMOTE As New PacketClass(OPCODES.SMSG_TEXT_EMOTE)
        SMSG_TEXT_EMOTE.AddUInt64(Client.Character.GUID)
        SMSG_TEXT_EMOTE.AddInt32(TextEmote)
        SMSG_TEXT_EMOTE.AddInt32(&HFF)
        SMSG_TEXT_EMOTE.AddInt32(secondName.Length + 1)
        SMSG_TEXT_EMOTE.AddString(secondName)
        Client.SendMultiplyPackets(SMSG_TEXT_EMOTE)
        Client.Character.SendToNearPlayers(SMSG_TEXT_EMOTE)

        SMSG_TEXT_EMOTE.Dispose()

        'TODO: Set stand state and send updates - EMOTE.SIT, EMOTE.STAND, EMOTE.KNEEL, EMOTE.SLEEP
    End Sub

    Public Sub On_MSG_CORPSE_QUERY(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If Client.Character.corpseGUID = 0 Then Exit Sub

        'DONE: Send corpse coords
        Dim MSG_CORPSE_QUERY As New PacketClass(OPCODES.MSG_CORPSE_QUERY)
        MSG_CORPSE_QUERY.AddInt8(1)
        MSG_CORPSE_QUERY.AddInt32(Client.Character.corpseMapID)
        MSG_CORPSE_QUERY.AddSingle(Client.Character.corpsePositionX)
        MSG_CORPSE_QUERY.AddSingle(Client.Character.corpsePositionY)
        MSG_CORPSE_QUERY.AddSingle(Client.Character.corpsePositionZ)
        MSG_CORPSE_QUERY.AddInt32(Client.Character.corpseMapID)                '0-Normal 1-Corpse in instance
        Client.Send(MSG_CORPSE_QUERY)
        MSG_CORPSE_QUERY.Dispose()

        'DONE: Send ping on minimap
        Dim MSG_MINIMAP_PING As New PacketClass(OPCODES.MSG_MINIMAP_PING)
        MSG_MINIMAP_PING.AddUInt64(Client.Character.corpseGUID)
        MSG_MINIMAP_PING.AddSingle(Client.Character.corpsePositionX)
        MSG_MINIMAP_PING.AddSingle(Client.Character.corpsePositionY)
        Client.Send(MSG_MINIMAP_PING)
        MSG_MINIMAP_PING.Dispose()
    End Sub
    Public Sub On_CMSG_REPOP_REQUEST(ByRef packet As PacketClass, ByRef Client As ClientClass)
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_REPOP_REQUEST [GUID={2:X}]", Client.IP, Client.Port, Client.Character.GUID)
        Client.Character.repopTimer.Dispose()
        Client.Character.repopTimer = Nothing
        CharacterRepop(Client)
    End Sub
    Public Sub On_CMSG_RECLAIM_CORPSE(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 13 Then Exit Sub
        packet.GetInt16()
        Dim GUID As ULong = packet.GetUInt64
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_RECLAIM_CORPSE [GUID={2:X}]", Client.IP, Client.Port, GUID)

        CharacterResurrect(Client.Character)
    End Sub
    Public Sub CharacterRepop(ByRef Client As ClientClass)
        Try
            'DONE: Make really dead
            Client.Character.Mana.Current = 0
            Client.Character.Rage.Current = 0
            Client.Character.Energy.Current = 0
            Client.Character.Life.Current = 1
            Client.Character.DEAD = True
            Client.Character.cUnitFlags = &H8
            Client.Character.cDynamicFlags = 0
            Client.Character.cPlayerFlags = Client.Character.cPlayerFlags Or PlayerFlags.PLAYER_FLAG_DEAD
            SendCorpseReclaimDelay(Client, Client.Character, 30)

            'DONE: Update to see only dead
            Client.Character.Invisibility = InvisibilityLevel.DEAD
            Client.Character.CanSeeInvisibility = InvisibilityLevel.DEAD
            UpdateCell(Client.Character)

            'DONE: Clear some things like spells, flags and timers
            Client.Character.StopMirrorTimer(MirrorTimer.FATIGUE)
            Client.Character.StopMirrorTimer(MirrorTimer.DROWNING)
            If Not (Client.Character.underWaterTimer Is Nothing) Then
                Client.Character.underWaterTimer.Dispose()
                Client.Character.underWaterTimer = Nothing
            End If

            'DONE: Spawn Corpse
            Dim myCorpse As New CorpseObject(Client.Character)
            myCorpse.Save()
            myCorpse.AddToWorld()

            'DONE: Remove all auras
            For i As Integer = 0 To MAX_AURA_EFFECTs - 1
                If Not Client.Character.ActiveSpells(i) Is Nothing Then Client.Character.RemoveAura(i, Client.Character.ActiveSpells(i).SpellCaster)
            Next

            'DONE: Ghost aura
            Client.Character.SetWaterWalk()
            Client.Character.SetMoveUnroot()
            If Client.Character.Race = Races.RACE_NIGHT_ELF Then
                Client.Character.ApplySpell(20584)
            Else
                Client.Character.ApplySpell(8326)
            End If

            'DONE: If you've died on a place that requires flying mount to get to you'll be alive at the graveyard
            Dim RessurrectAlive As Boolean = AreaTable(GetAreaFlag(Client.Character.positionX, Client.Character.positionY, Client.Character.MapID)).NeedFlyingMount()

            Client.Character.SetUpdateFlag(EUnitFields.UNIT_FIELD_HEALTH, 1)
            Client.Character.SetUpdateFlag(EUnitFields.UNIT_FIELD_POWER1 + Client.Character.ManaType, 0)
            Client.Character.SetUpdateFlag(EPlayerFields.PLAYER_FLAGS, Client.Character.cPlayerFlags)
            Client.Character.SetUpdateFlag(EUnitFields.UNIT_FIELD_FLAGS, Client.Character.cUnitFlags)
            Client.Character.SetUpdateFlag(EUnitFields.UNIT_DYNAMIC_FLAGS, Client.Character.cDynamicFlags)

            Client.Character.SetUpdateFlag(EUnitFields.UNIT_FIELD_BYTES_1, &H1000000)       'Set standing so always be standing
            Client.Character.SendCharacterUpdate()

            'DONE: Get closest graveyard
            GoToNearestGraveyard(Client.Character, RessurrectAlive)
        Catch e As Exception
            Log.WriteLine(LogType.FAILED, "Error on repop: {0}", e.ToString)
        End Try
    End Sub
    Public Sub CharacterResurrect(ByRef Character As CharacterObject)
        'DONE: Make really alive
        Character.Mana.Current = 0
        Character.Rage.Current = 0
        Character.Energy.Current = 0
        Character.Life.Current = Character.Life.Maximum / 2
        Character.DEAD = False
        Character.cPlayerFlags = Character.cPlayerFlags And (Not PlayerFlags.PLAYER_FLAG_DEAD)

        'DONE: Update to see only alive
        Character.InvisibilityReset()
        UpdateCell(Character)
        Character.SetLandWalk()

        If Character.Race = Races.RACE_NIGHT_ELF Then
            Character.RemoveAuraBySpell(20584)
        Else
            Character.RemoveAuraBySpell(8326)
        End If

        Character.SetUpdateFlag(EUnitFields.UNIT_FIELD_HEALTH, Character.Life.Current)
        Character.SetUpdateFlag(EPlayerFields.PLAYER_FLAGS, Character.cPlayerFlags)
        Character.SetUpdateFlag(EUnitFields.UNIT_FIELD_FLAGS, Character.cUnitFlags)
        Character.SetUpdateFlag(EUnitFields.UNIT_DYNAMIC_FLAGS, Character.cDynamicFlags)
        Character.SendCharacterUpdate()

        'DONE: Spawn Bones, Delete Corpse
        If Character.corpseGUID <> 0 Then
            CType(WORLD_CORPSEOBJECTs(Character.corpseGUID), CorpseObject).ConvertToBones()
            Character.corpseGUID = 0
            Character.corpseMapID = 0
            Character.corpsePositionX = 0
            Character.corpsePositionY = 0
            Character.corpsePositionZ = 0
        End If
    End Sub

    Public Sub On_CMSG_TOGGLE_PVP(ByRef packet As PacketClass, ByRef Client As ClientClass)
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_TOGGLE_PVP", Client.IP, Client.Port)

        Client.Character.isPvP = Not Client.Character.isPvP
        Client.Character.SetUpdateFlag(EUnitFields.UNIT_FIELD_FLAGS, Client.Character.cUnitFlags)
        Client.Character.SendCharacterUpdate()
    End Sub
    Public Sub On_MSG_INSPECT_HONOR_STATS(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 13 Then Exit Sub
        packet.GetInt16()
        Dim GUID As ULong = packet.GetUInt64

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] MSG_INSPECT_HONOR_STATS [{2:X}]", Client.IP, Client.Port, GUID)

        Dim response As New PacketClass(OPCODES.MSG_INSPECT_HONOR_STATS)
        response.AddUInt64(GUID)

        response.AddInt8(0)  'PLAYER_FIELD_HONOR_BAR                    - Rank, filling bar, PLAYER_BYTES_3, ??
        response.AddInt32(0) 'PLAYER_FIELD_SESSION_KILLS                - Today Honorable and Dishonorable Kills
        response.AddInt32(0) 'PLAYER_FIELD_YESTERDAY_KILLS              - Yesterday Honorable Kills
        response.AddInt32(0) 'PLAYER_FIELD_LAST_WEEK_KILLS              - Last Week Honorable Kills
        response.AddInt32(0) 'PLAYER_FIELD_THIS_WEEK_KILLS              - This Week Honorable kills
        response.AddInt32(0) 'PLAYER_FIELD_LIFETIME_HONORABLE_KILLS     - Lifetime Honorable Kills
        response.AddInt32(0) 'PLAYER_FIELD_LIFETIME_DISHONORABLE_KILLS  - Lifetime Dishonorable Kills
        response.AddInt32(0) 'PLAYER_FIELD_YESTERDAY_CONTRIBUTION       - Yesterday Honor
        response.AddInt32(0) 'PLAYER_FIELD_LAST_WEEK_CONTRIBUTION       - Last Week Honor
        response.AddInt32(0) 'PLAYER_FIELD_THIS_WEEK_CONTRIBUTION       - This Week Honor
        response.AddInt32(0) 'PLAYER_FIELD_LAST_WEEK_RANK               - Last Week Standing
        response.AddInt8(0)  'Highest Rank

        Client.Send(response)
        response.Dispose()
    End Sub

    Public Sub On_CMSG_ALTER_APPEARANCE(ByRef packet As PacketClass, ByRef Client As ClientClass)
        Try
            packet.GetInt16()
            Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_ALTER_APPEARANCE [{2:X}]", Client.IP, Client.Port, Client.Character.GUID)

            Dim cost As Integer = 0
            Dim Hair, Color, Facialhair As Integer

            Hair = packet.GetInt32
            Color = packet.GetInt32
            Facialhair = packet.GetInt32

            'TODO: Need to lookup HAIR and FACIALHAIR keys in BarberShopStyles dictionary

            'new costs of the changes.
            If Hair <> Client.Character.HairStyle Then
                cost += 24800
            End If
            If Hair = Client.Character.HairStyle AndAlso Color <> Client.Character.HairColor Then
                cost += 12400
            End If
            If Facialhair <> Client.Character.FacialHair Then
                cost += 18600
            End If
            If Client.Character.Level = 1 Then
                cost = 0
            End If

            If Client.Character.Copper < cost Then
                Dim SMSG_BARBER_SHOP_RESULT As New PacketClass(OPCODES.SMSG_BARBER_SHOP_RESULT)
                SMSG_BARBER_SHOP_RESULT.AddInt32(0) 'Not enough money
                Client.Send(SMSG_BARBER_SHOP_RESULT)
                SMSG_BARBER_SHOP_RESULT.Dispose()
                Exit Sub
            Else
                Dim SMSG_BARBER_SHOP_RESULT As New PacketClass(OPCODES.SMSG_BARBER_SHOP_RESULT)
                SMSG_BARBER_SHOP_RESULT.AddInt32(1) 'Success
                Client.Send(SMSG_BARBER_SHOP_RESULT)
                SMSG_BARBER_SHOP_RESULT.Dispose()
            End If

            Client.Character.HairStyle = Hair
            Client.Character.HairColor = Color
            Client.Character.FacialHair = Facialhair

            'Send character updates: Hair,color,facialhair, money.
            With Client.Character
                .Copper -= cost
                .StandState = 0 'Stand-up.
                .SetUpdateFlag(EPlayerFields.PLAYER_BYTES, (.Skin + (CType(.Face, Integer) << 8) + (CType(.HairStyle, Integer) << 16) + (CType(.HairColor, Integer) << 24)))
                .SetUpdateFlag(EPlayerFields.PLAYER_BYTES_2, (.FacialHair + (&HEE << 8) + (CType(.Items_AvailableBankSlots, Integer) << 16) + (CType(.RestState, Integer) << 24)))
                .SetUpdateFlag(EPlayerFields.PLAYER_FIELD_COINAGE, .Copper)
                .SetUpdateFlag(EUnitFields.UNIT_FIELD_BYTES_1, .cBytes1)
                .SendCharacterUpdate()
            End With

        Catch e As Exception
            Log.WriteLine(LogType.FAILED, e.Message)
        End Try
    End Sub

    Public Sub On_CMSG_MOVE_FALL_RESET(ByRef packet As PacketClass, ByRef Client As ClientClass)
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_MOVE_FALL_RESET", Client.IP, Client.Port)
        DumpPacket(packet.Data)
    End Sub
    Public Sub On_CMSG_BATTLEFIELD_STATUS(ByRef packet As PacketClass, ByRef Client As ClientClass)
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_BATTLEFIELD_STATUS", Client.IP, Client.Port)
    End Sub
    Public Sub On_CMSG_SET_ACTIVE_MOVER(ByRef packet As PacketClass, ByRef Client As ClientClass)
        packet.GetInt16()
        Dim GUID As ULong = packet.GetUInt64
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_SET_ACTIVE_MOVER [GUID={2:X}]", Client.IP, Client.Port, GUID)
    End Sub
    Public Sub On_CMSG_MEETINGSTONE_INFO(ByRef packet As PacketClass, ByRef Client As ClientClass)
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_MEETINGSTONE_INFO", Client.IP, Client.Port)
    End Sub

End Module