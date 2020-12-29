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
Imports System.Collections.Generic
Imports mangosVB.Common.BaseWriter

#Region "WS.Commands.Attributes"
<AttributeUsage(AttributeTargets.Method, Inherited:=False, AllowMultiple:=True)> _
    Public Class ChatCommandAttribute
    Inherits System.Attribute

    Private Command As String = ""
    Private CommandHelp As String = "No information available."
    Private CommandAccess As AccessLevel = AccessLevel.GameMaster

    Public Sub New(ByVal cmdName As String, Optional ByVal cmdHelp As String = "No information available.", Optional ByVal cmdAccess As AccessLevel = AccessLevel.GameMaster)
        Command = cmdName
        CommandHelp = cmdHelp
        CommandAccess = cmdAccess
    End Sub

    Public Property cmdName() As String
        Get
            Return Command
        End Get
        Set(ByVal Value As String)
            Command = Value
        End Set
    End Property
    Public Property cmdHelp() As String
        Get
            Return CommandHelp
        End Get
        Set(ByVal Value As String)
            CommandHelp = Value
        End Set
    End Property
    Public Property cmdAccess() As AccessLevel
        Get
            Return CommandAccess
        End Get
        Set(ByVal Value As AccessLevel)
            CommandAccess = Value
        End Set
    End Property

End Class
#End Region

Public Module WS_Commands

#Region "WS.Commands.Framework"

    Public Const WardenGUID As ULong = Integer.MaxValue
    Public Const WardenNAME As String = "Warden"
    Public Enum AccessLevel As Byte
        Trial = 0
        Player = 1
        GameMaster = 2
        Developer = 3
        Admin = 4
    End Enum

    Public ChatCommands As New Dictionary(Of String, ChatCommand)
    Public ScriptedChatCommands As ScriptedObject
    Public Class ChatCommand
        Public CommandHelp As String
        Public CommandAccess As AccessLevel = AccessLevel.GameMaster
        Public CommandDelegate As ChatCommandDelegate
    End Class
    Public Delegate Function ChatCommandDelegate(ByRef c As CharacterObject, ByVal Message As String) As Boolean

    Public Sub RegisterChatCommands()
        ScriptedChatCommands = New ScriptedObject("scripts\Commands.vb", "modules\Commands.dll", False)

        For Each tmpModule As Type In [Assembly].GetExecutingAssembly.GetTypes
            For Each tmpMethod As MethodInfo In tmpModule.GetMethods
                Dim infos() As ChatCommandAttribute = tmpMethod.GetCustomAttributes(GetType(ChatCommandAttribute), True)

                If infos.Length <> 0 Then
                    For Each info As ChatCommandAttribute In infos
                        Dim cmd As New ChatCommand
                        cmd.CommandHelp = info.cmdHelp
                        cmd.CommandAccess = info.cmdAccess
                        cmd.CommandDelegate = ChatCommandDelegate.CreateDelegate(GetType(ChatCommandDelegate), tmpMethod)

                        ChatCommands.Add(UCase(info.cmdName), cmd)
#If DEBUG Then
                        Log.WriteLine(mangosVB.Common.BaseWriter.LogType.INFORMATION, "Command found: {0}", UCase(info.cmdName))
#End If
                    Next
                End If
            Next
        Next

        If ScriptedChatCommands.ass IsNot Nothing Then
            For Each tmpModule As Type In ScriptedChatCommands.ass.GetTypes
                For Each tmpMethod As MethodInfo In tmpModule.GetMethods
                    Dim infos() As ChatCommandAttribute = tmpMethod.GetCustomAttributes(GetType(ChatCommandAttribute), True)

                    If infos.Length <> 0 Then
                        For Each info As ChatCommandAttribute In infos
                            Dim cmd As New ChatCommand
                            cmd.CommandHelp = info.cmdHelp
                            cmd.CommandAccess = info.cmdAccess
                            cmd.CommandDelegate = ChatCommandDelegate.CreateDelegate(GetType(ChatCommandDelegate), tmpMethod)

                            ChatCommands.Add(UCase(info.CmdName), cmd)
#If DEBUG Then
                            Log.WriteLine(mangosVB.Common.BaseWriter.LogType.INFORMATION, "Command found: {0}", UCase(info.cmdName))
#End If
                        Next
                    End If
                Next
            Next
        End If
    End Sub
    Public Sub OnCommand(ByRef Client As ClientClass, ByVal Message As String)
        Try
            'DONE: Find the command
            Dim tmp() As String = Split(Message, " ", 2)
            Dim Command As ChatCommand = Nothing
            If ChatCommands.ContainsKey(UCase(tmp(0))) Then
                Command = ChatCommands(UCase(tmp(0)))
            End If

            'DONE: Build argument string
            Dim Arguments As String = ""
            If tmp.Length = 2 Then Arguments = Trim(tmp(1))

            'DONE: Get character name (there can be no character after the command)
            Dim Name As String = Client.Character.Name

            If Command Is Nothing Then
                Client.Character.CommandResponse("Unknown command.")
            ElseIf Command.CommandAccess > Client.Character.Access Then
                Client.Character.CommandResponse("This command is not available for your access level.")
            ElseIf Not Command.CommandDelegate(Client.Character, Arguments) Then
                Client.Character.CommandResponse(Command.CommandHelp)
            Else
                Log.WriteLine(LogType.USER, "[{0}:{1}] {2} used command: {3}", Client.IP, Client.Port, Name, Message)
            End If

        Catch err As Exception
            Log.WriteLine(LogType.FAILED, "[{0}:{1}] Client command caused error! {3}{2}", Client.IP, Client.Port, err.ToString, vbNewLine)
            Client.Character.CommandResponse(String.Format("Your command caused error:" & vbNewLine & " [{0}]", err.Message))
        End Try
    End Sub

#End Region
#Region "WS.Commands.InternalCommands"

    <ChatCommandAttribute("Help", "HELP <CMD>" & vbNewLine & "Displays usage information about command, if no command specified - displays list of available commands.")> _
    Public Function Help(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If Trim(Message) <> "" Then
            Dim Command As ChatCommand = CType(ChatCommands(Trim(UCase(Message))), ChatCommand)
            If Command Is Nothing Then
                c.CommandResponse("Unknown command.")
            ElseIf Command.CommandAccess > c.Access Then
                c.CommandResponse("This command is not available for your access level.")
            Else
                c.CommandResponse(Command.CommandHelp)
            End If
        Else
            Dim cmdList As String = "Listing available commands:" & vbNewLine
            For Each Command As KeyValuePair(Of String, ChatCommand) In ChatCommands
                If CType(Command.Value, ChatCommand).CommandAccess <= c.Access Then cmdList += UCase(Command.Key) & ", "
            Next
            cmdList += vbNewLine + "Use HELP <CMD> for usage information about particular command."
            c.CommandResponse(cmdList)
        End If

        Return True
    End Function

    '****************************************** DEVELOPER COMMANDs *************************************************
    Dim x As Integer = 0
    <ChatCommandAttribute("Test", "This is test command used for debugging. Do not use if you don't know what it does!", AccessLevel.Developer)> _
    Public Function cmdTest(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        WORLD_GAMEOBJECTs(c.TargetGUID).orientation = 0
        WORLD_GAMEOBJECTs(c.TargetGUID).Rotations(0) = 0
        WORLD_GAMEOBJECTs(c.TargetGUID).Rotations(1) = 0
        WORLD_GAMEOBJECTs(c.TargetGUID).Rotations(2) = 0
        WORLD_GAMEOBJECTs(c.TargetGUID).Rotations(3) = 0

        Select Case x
            Case 0
                WORLD_GAMEOBJECTs(c.TargetGUID).orientation = c.orientation
            Case 1
                WORLD_GAMEOBJECTs(c.TargetGUID).Rotations(0) = c.orientation
            Case 2
                WORLD_GAMEOBJECTs(c.TargetGUID).Rotations(1) = c.orientation
            Case 3
                WORLD_GAMEOBJECTs(c.TargetGUID).Rotations(2) = c.orientation
            Case 4
                WORLD_GAMEOBJECTs(c.TargetGUID).Rotations(3) = c.orientation
        End Select

        x += 1
        If x = 5 Then x = 0

        Dim packet As New PacketClass(OPCODES.SMSG_UPDATE_OBJECT)
        packet.AddInt32(1)
        'packet.AddInt8(0)
        Dim tmpUpdate As New UpdateClass(FIELD_MASK_SIZE_GAMEOBJECT)
        WORLD_GAMEOBJECTs(c.TargetGUID).FillAllUpdateFlags(tmpUpdate, c)
        tmpUpdate.AddToPacket(packet, ObjectUpdateType.UPDATETYPE_VALUES, CType(WORLD_GAMEOBJECTs(c.TargetGUID), GameObjectObject))
        tmpUpdate.Dispose()

        c.Client.Send(packet)

        packet.Dispose()

        Return True
    End Function
    <ChatCommandAttribute("Test2", "This is test command used for debugging. Do not use if you don't know what it does!", AccessLevel.Developer)> _
    Public Function cmdTest2(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        c.SetCanFly()
        'Dim packet As New PacketClass(811)
        'packet.AddInt32(4)
        'c.Client.Send(packet)
        Return True
    End Function
    Dim currentSpError As SpellFailedReason = SpellFailedReason.CAST_NO_ERROR
    <ChatCommandAttribute("SpellFailedMSG", "SPELLFAILEDMSG <optional ID> - Sends test spell failed message.", AccessLevel.Developer)> _
    Public Function cmdSpellFailed(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If Message = "" Then
            currentSpError += 1
        Else
            currentSpError = Message
        End If
        SendCastResult(currentSpError, c.Client, 133, 0)
        c.CommandResponse(String.Format("Sent spell failed message:{2} {0} = {1}", currentSpError, CType(currentSpError, Integer), vbNewLine))
        Return True
    End Function
    Dim currentInvError As InventoryChangeFailure = InventoryChangeFailure.EQUIP_ERR_OK
    <ChatCommandAttribute("InvFailedMSG", "INVFAILEDMSG <optional ID> - Sends test inventory failed message.", AccessLevel.Developer)> _
    Public Function cmdInventoryFailed(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If Message = "" Then
            currentInvError += 1
        Else
            currentInvError = Message
        End If
        Dim response As New PacketClass(OPCODES.SMSG_INVENTORY_CHANGE_FAILURE)
        response.AddInt8(currentInvError)
        response.AddUInt64(0)
        response.AddUInt64(0)
        response.AddInt8(0)
        c.Client.Send(response)
        response.Dispose()
        c.CommandResponse(String.Format("Sent spell failed message:{2} {0} = {1}", currentInvError, CType(currentInvError, Integer), vbNewLine))
        Return True
    End Function
    Dim currentInstanceResetError As ResetFailedReason = ResetFailedReason.INSTANCE_RESET_FAILED_ZONING
    <ChatCommandAttribute("InstanceResetFailedMSG", "INSTANCERESETFAILEDMSG <optional ID> - Sends test inventory failed message.", AccessLevel.Developer)> _
    Public Function cmdInstanceResetFailedReason(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If Message = "" Then
            currentInstanceResetError += 1
        Else
            currentInstanceResetError = Message
        End If
        SendResetInstanceFailed(c.Client, c.MapID, currentInstanceResetError)
        c.CommandResponse(String.Format("Sent instance failed message:{2} {0} = {1}", currentInstanceResetError, CType(currentInstanceResetError, Integer), vbNewLine))
        Return True
    End Function

    <ChatCommandAttribute("CastSpell", "CASTSPELL <SpellID> <Target> - Selected unit will start casting spell. Target can be ME or SELF.", AccessLevel.Developer)> _
    Public Function cmdCastSpellMe(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        Dim tmp As String() = Split(Message, " ", 2)
        Dim SpellID As Integer = tmp(0)
        Dim Target As String = UCase(tmp(1))

        If WORLD_CREATUREs.ContainsKey(c.TargetGUID) Then
            Select Case Target
                Case "ME"
                    CType(WORLD_CREATUREs(c.TargetGUID), CreatureObject).CastSpell(SpellID, c)
                Case "SELF"
                    CType(WORLD_CREATUREs(c.TargetGUID), CreatureObject).CastSpell(SpellID, CType(WORLD_CREATUREs(c.TargetGUID), CreatureObject))
            End Select
        Else
            c.CommandResponse(String.Format("GUID=[{0:X}] not found or unsupported.", c.TargetGUID))
        End If

        Return True
    End Function
    <ChatCommandAttribute("CreateGuild", "CreateGuild <Name> - Creates a guild.", AccessLevel.Developer)> _
    Public Function cmdCreateGuild(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        Dim tmp As String() = Split(Message, "", 2)
        Dim GuildName As String = tmp(0)

        Dim MySQLQuery As New DataTable
        Database.Query(String.Format("INSERT INTO guilds (guild_name, guild_leader, guild_cYear, guild_cMonth, guild_cDay) VALUES (""{0}"", {1}, {2}, {3}, {4}); SELECT guild_id FROM guilds WHERE guild_name = ""{0}"";", GuildName, c.GUID, Now.Year - 2006, Now.Month, Now.Day), MySQLQuery)

        AddCharacterToGuild(c, MySQLQuery.Rows(0).Item("guild_id"), 0)
        Return True
    End Function
    <ChatCommandAttribute("Cast", "CAST <SpellID> - You will start casting spell on selected target.", AccessLevel.Developer)> _
    Public Function cmdCastSpell(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        Dim tmp As String() = Split(Message, " ", 2)
        Dim SpellID As Integer = tmp(0)

        If WORLD_CREATUREs.ContainsKey(c.TargetGUID) Then
            Dim Targets As New SpellTargets
            Targets.SetTarget_UNIT(WORLD_CREATUREs(c.TargetGUID))
            CType(SPELLs(SpellID), SpellInfo).Cast(1, CType(c, CharacterObject), Targets)
        ElseIf CHARACTERS.ContainsKey(c.TargetGUID) Then
            Dim Targets As New SpellTargets
            Targets.SetTarget_UNIT(CHARACTERS(c.TargetGUID))
            CType(SPELLs(SpellID), SpellInfo).Cast(1, CType(c, CharacterObject), Targets)
        Else
            c.CommandResponse(String.Format("GUID=[{0:X}] not found or unsupported.", c.TargetGUID))
        End If

        Return True
    End Function
    <ChatCommandAttribute("Save", "SAVE - Saves your character.", AccessLevel.GameMaster)> _
    Public Function cmdSave(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        c.Save()
        c.CommandResponse("Character saved")
        Return True
    End Function
    <ChatCommandAttribute("AddWardenToParty", "This command will add the command bot to you group.", AccessLevel.Developer)> _
    Public Function cmdAddWardenToParty(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        'Dim Warden As New CharacterObject
        'Warden.Name = WardenNAME
        'Warden.GUID = WardenGUID
        'Warden.Client = New ClientClass
        'Warden.Client.DEBUG_CONNECTION = True

        'c.Party = New BaseParty(c)
        'c.Party.AddCharacter(Warden)

        c.CommandResponse("This command is disabled for now")
        Return True
    End Function
    <ChatCommandAttribute("Spawns", "SPAWNS - Tells you the spawn in memory information.", AccessLevel.Developer)> _
    Public Function cmdSpawns(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        c.CommandResponse("Spawns loaded in server memory:")
        c.CommandResponse("-------------------------------")
        c.CommandResponse("Creatures: " & WORLD_CREATUREs.Count)
        c.CommandResponse("GameObjects: " & WORLD_GAMEOBJECTs.Count)

        Return True
    End Function
    <ChatCommandAttribute("Near", "NEAR - Tells you the near objects count.", AccessLevel.Developer)> _
    Public Function cmdNear(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        c.CommandResponse("Near objects:")
        c.CommandResponse("-------------------------------")
        c.CommandResponse("Players: " & c.playersNear.Count)
        c.CommandResponse("Creatures: " & c.creaturesNear.Count)
        c.CommandResponse("GameObjects: " & c.gameObjectsNear.Count)
        c.CommandResponse("Corpses: " & c.corpseObjectsNear.Count)
        c.CommandResponse("-------------------------------")
        c.CommandResponse("You are seen by: " & c.SeenBy.Count)
        Return True
    End Function

    <ChatCommandAttribute("SetWaterWalk", "SETWATERWALK <TRUE/FALSE> - Enables/Disables walking over water for selected target.", AccessLevel.Developer)> _
    Public Function cmdSetWaterWalk(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If c.TargetGUID = 0 Then
            c.CommandResponse("Select target first!")
            Return True
        End If

        If CHARACTERs.ContainsKey(c.TargetGUID) Then
            If UCase(Message) = "TRUE" Then
                CType(CHARACTERs(c.TargetGUID), CharacterObject).SetWaterWalk()
            ElseIf UCase(Message) = "FALSE" Then
                CType(CHARACTERs(c.TargetGUID), CharacterObject).SetLandWalk()
            Else
                Return False
            End If
        Else
            c.CommandResponse("Select target is not character!")
            Return True
        End If
    End Function
    <ChatCommandAttribute("AI", "AI <ENABLE/DISABLE> - Enables/Disables AI updating.", AccessLevel.Developer)> _
    Public Function cmdAI(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If UCase(Message) = "ENABLE" Then
            AIManager.AIManagerTimer.Change(TAIManager.UPDATE_TIMER, TAIManager.UPDATE_TIMER)
            c.CommandResponse("AI is enabled.")
        ElseIf UCase(Message) = "DISABLE" Then
            AIManager.AIManagerTimer.Change(Timeout.Infinite, Timeout.Infinite)
            c.CommandResponse("AI is disabled.")
        Else
            Return False
        End If

        Return True
    End Function
    <ChatCommandAttribute("AIState", "AIState - Shows debug information about AI state of selected creature.", AccessLevel.Developer)> _
    Public Function cmdAIState(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If c.TargetGUID = 0 Then
            c.CommandResponse("Select target first!")
            Exit Function
        End If
        If Not WORLD_CREATUREs.ContainsKey(c.TargetGUID) Then
            c.CommandResponse("Selected target is not creature!")
            Exit Function
        End If

        If WORLD_CREATUREs(c.TargetGUID).aiScript Is Nothing Then
            c.CommandResponse("This creature doesn't have AI")
        Else
            With WORLD_CREATUREs(c.TargetGUID)
                c.CommandResponse(String.Format("Information for creature [{0}]:{1}ai = {2}{1}state = {3}{1}respawn time = {4}{1}spawnID = {5}{1}Current Waypoint = {6}", .Name, vbNewLine, .aiScript.ToString, .aiScript.State.ToString, .CreatureInfo.RespawnTime, .SpawnID, .CurrentWaypoint))
                c.CommandResponse("Hate table:")
                For Each u As KeyValuePair(Of BaseUnit, Integer) In .aiScript.aiHateTable
                    c.CommandResponse(String.Format("{0:X} = {1} hate", u.Key.GUID, u.Value))
                Next
            End With
        End If

        Return True
    End Function

    '****************************************** CHAT COMMANDs ******************************************************
    <ChatCommandAttribute("Broadcast", "BROADCAST <TEXT> - Send text message to all players on the server.", AccessLevel.GameMaster)> _
    Public Function cmdBroadcast(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If Message = "" Then Return False

        CHARACTERs_Lock.AcquireReaderLock(DEFAULT_LOCK_TIMEOUT)
        For Each Character As KeyValuePair(Of ULong, CharacterObject) In CHARACTERs
            SendMessageSystem(Character.Value.Client, "System Message: " & SetColor(Message, 255, 0, 0))
        Next
        CHARACTERs_Lock.ReleaseReaderLock()

        Return True
    End Function
    <ChatCommandAttribute("MSGGame", "MSGGAME <TEXT> - Send text message to all players on the server.", AccessLevel.GameMaster), _
     ChatCommandAttribute("GameMessage", "GAMEMESSAGE <TEXT> - Send text message to all players on the server.", AccessLevel.GameMaster)> _
    Public Function cmdGameMessage(ByRef c As CharacterObject, ByVal Text As String) As Boolean
        If Text = "" Then Return False

        Dim packet As New PacketClass(OPCODES.SMSG_AREA_TRIGGER_MESSAGE)
        packet.AddInt32(0)
        packet.AddString(Text)
        packet.AddInt8(0)

        packet.UpdateLength()
        WS.Cluster.Broadcast(packet.Data)
        packet.Dispose()
        Return True
    End Function
    <ChatCommandAttribute("MSGServer", "MSGSERVER <TYPE> <TEXT> - Send text message to all players on the server.", AccessLevel.GameMaster), _
     ChatCommandAttribute("ServerMessage", "SERVERMESSAGE <TYPE> <TEXT> - Send text message to all players on the server.", AccessLevel.GameMaster)> _
    Public Function cmdServerMessage(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        '1,"[SERVER] Shutdown in %s"
        '2,"[SERVER] Restart in %s"
        '3,"%s"
        '4,"[SERVER] Shutdown cancelled"
        '5,"[SERVER] Restart cancelled"

        Dim tmp() As String = Split(Message, " ", 2)
        If tmp.Length <> 2 Then Return False
        Dim Type As Integer = tmp(0)
        Dim Text As String = tmp(1)

        Dim packet As New PacketClass(OPCODES.SMSG_SERVER_MESSAGE)
        packet.AddInt32(Type)
        packet.AddString(Text)

        packet.UpdateLength()
        WS.Cluster.Broadcast(packet.Data)
        packet.Dispose()

        Return True
    End Function
    <ChatCommandAttribute("MSGNotify", "MSGNOTIFY <TEXT> - Send text message to all players on the server.", AccessLevel.GameMaster), _
     ChatCommandAttribute("NotifyMessage", "NOTIFYMESSAGE <TEXT> - Send text message to all players on the server.", AccessLevel.GameMaster)> _
    Public Function cmdNotificationMessage(ByRef c As CharacterObject, ByVal Text As String) As Boolean
        If Text = "" Then Return False

        Dim packet As New PacketClass(OPCODES.SMSG_NOTIFICATION)
        packet.AddString(Text)

        packet.UpdateLength()
        WS.Cluster.Broadcast(packet.Data)
        packet.Dispose()

        Return True
    End Function
    <ChatCommandAttribute("Say", "SAY <TEXT> - Target Player / NPC will say this.", AccessLevel.GameMaster)> _
    Public Function cmdSay(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If Message = "" Then Return False
        If c.TargetGUID = 0 Then Return False

        If GuidIsPlayer(c.TargetGUID) Then
            CType(CHARACTERs(c.TargetGUID), CharacterObject).SendChatMessage(CType(CHARACTERs(c.TargetGUID), CharacterObject), Message, ChatMsg.CHAT_MSG_SAY, LANGUAGES.LANG_UNIVERSAL, , True)
        ElseIf GuidIsCreature(c.TargetGUID) Then
            CType(WORLD_CREATUREs(c.TargetGUID), CreatureObject).SendChatMessage(Message, ChatMsg.CHAT_MSG_MONSTER_SAY, LANGUAGES.LANG_UNIVERSAL, c.GUID)
        Else
            Return False
        End If

        Return True
    End Function

    <ChatCommandAttribute("MySpeed", "MYSPEED - Displays all your current speed.", AccessLevel.GameMaster)> _
    Public Function cmdMySpeed(ByRef c As CharacterObject, ByVal tCopper As String) As Boolean
        c.CommandResponse("WalkSpeed: " & c.WalkSpeed)
        c.CommandResponse("RunSpeed:" & c.RunSpeed)
        c.CommandResponse("RunBackSpeed:" & c.RunBackSpeed)
        c.CommandResponse("SwimSpeed:" & c.SwimSpeed)
        c.CommandResponse("SwimBackSpeed:" & c.SwimBackSpeed)
        c.CommandResponse("Turnrate:" & c.TurnRate)
        c.CommandResponse("FlySpeed:" & c.FlySpeed)
        c.CommandResponse("FlyBackSpeed:" & c.FlyBackSpeed)

        Return True
    End Function

    <ChatCommandAttribute("MyAP", "MYAP - Displays your attack power.", AccessLevel.GameMaster)> _
    Public Function cmdMyAttackPower(ByRef c As CharacterObject, ByVal tCopper As String) As Boolean
        c.CommandResponse("AttackPower: " & c.AttackPower)
        c.CommandResponse("AttackPowerMods: " & c.AttackPowerMods)
        c.CommandResponse("RangedAttackPower: " & c.AttackPowerRanged)
        c.CommandResponse("RangedAttackPowerMods: " & c.AttackPowerModsRanged)

        Return True
    End Function

    '****************************************** DEBUG COMMANDs ******************************************************
    <ChatCommandAttribute("GetMax", "GETMAX - Get all spells and skills maxed out for your level.", AccessLevel.Admin)> _
    Public Function cmdGetMax(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        'DONE: Max out all skills you know
        For Each skill As KeyValuePair(Of Integer, TSkill) In c.Skills
            skill.Value.Current = skill.Value.Maximum
            c.SetUpdateFlag(EPlayerFields.PLAYER_SKILL_INFO_1_1 + c.SkillsPositions(skill.Key) * 3 + 1, c.Skills(skill.Key).GetSkill)
        Next
        c.SendCharacterUpdate(False)

        'DONE: Get all spells
        Dim q As New DataTable
        Database.Query(String.Format("SELECT entry FROM trainer_defs WHERE req_class = {0} AND trainer_type = 0 ORDER BY entry", CByte(c.Classe)), q)
        For Each row As DataRow In q.Rows
            Dim q2 As New DataTable
            Database.Query(String.Format("SELECT spellid FROM trainer_spells WHERE entry = {0}", row.Item("entry")), q2)
            For Each row2 As DataRow In q2.Rows
                If c.HaveSpell(CInt(row2.Item("spellid"))) = False Then c.LearnSpell(CInt(row2.Item("spellid")))
            Next
        Next

        Return True
    End Function
    <ChatCommandAttribute("AuraUpdate", "AURAUPDATE - Sends an aura update for your target.", AccessLevel.Admin)> _
    Public Function cmdAuraUpdate(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If GuidIsPlayer(c.TargetGUID) AndAlso CHARACTERs.ContainsKey(c.TargetGUID) Then
            CHARACTERs(c.TargetGUID).SendAuraUpdate()
            c.CommandResponse("Update sent.")
        ElseIf GuidIsCreature(c.TargetGUID) AndAlso WORLD_CREATUREs.ContainsKey(c.TargetGUID) Then
            WORLD_CREATUREs(c.TargetGUID).SendAuraUpdate()
            c.CommandResponse("Update sent.")
        Else
            c.CommandResponse("Target not found.")
            Return True
        End If

        Return True
    End Function
    <ChatCommandAttribute("SetLevel", "SETLEVEL <LEVEL> - Set the level of selected character.", AccessLevel.Admin)> _
    Public Function cmdSetLevel(ByRef c As CharacterObject, ByVal tLevel As String) As Boolean
        If IsNumeric(tLevel) = False Then Return False

        Dim Level As Integer = tLevel
        If Level > MAX_LEVEL Then Level = MAX_LEVEL
        If Level > 255 Then Level = 255

        If CHARACTERs.ContainsKey(c.TargetGUID) = False Then
            c.CommandResponse("Target not found or not character.")
            Return True
        End If

        CHARACTERs(c.TargetGUID).SetLevel(Level)

        Return True
    End Function
    <ChatCommandAttribute("SetInvisible", "SETINVISIBLE <VALUE> - set invisible level. Use ON or OFF.")> _
    Public Function cmdSetInvisible(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If c.TargetGUID = 0 Then
            c.CommandResponse("Select target first!")
            Return True
        End If
        If (GuidIsPlayer(c.TargetGUID) = False) And (CHARACTERs.ContainsKey(c.TargetGUID) = False) Then
            c.CommandResponse("Please select character")
            Return False
        End If
        If Message = "" Then Return False
        Dim tmp() As String = Split(Message, " ", 1)
        Dim value1 As String = tmp(0)

        If GuidIsPlayer(c.TargetGUID) = False And CHARACTERs.ContainsKey(c.TargetGUID) = False Then
            c.CommandResponse("Please select character")
            Return False
        End If
        If value1 = "ON" Or value1 = "on" Then
            CHARACTERs(c.TargetGUID).Invisibility = InvisibilityLevel.GM
            c.CommandResponse("The invisible enable on " & CHARACTERs(c.TargetGUID).Name)
            Return True
        End If

        If value1 = "OFF" Or value1 = "off" Then
            CHARACTERs(c.TargetGUID).Invisibility = InvisibilityLevel.VISIBLE
            c.CommandResponse("The invisible disable on " & CHARACTERs(c.TargetGUID).Name)
            Return True
        End If
    End Function
    <ChatCommandAttribute("AddXP", "ADDXP <XP> - Add X experience points to selected character.", AccessLevel.Admin)> _
    Public Function cmdAddXP(ByRef c As CharacterObject, ByVal tXP As String) As Boolean
        If IsNumeric(tXP) = False Then Return False

        Dim XP As Integer = tXP

        If CHARACTERs.ContainsKey(c.TargetGUID) Then
            CHARACTERs(c.TargetGUID).AddXP(XP)
        Else
            c.CommandResponse("Target not found or not character.")
        End If

        Return True
    End Function
    <ChatCommandAttribute("AddTP", "ADDTP <POINTs> - Add X talent points to selected character.", AccessLevel.Admin)> _
    Public Function cmdAddTP(ByRef c As CharacterObject, ByVal tTP As String) As Boolean
        If IsNumeric(tTP) = False Then Return False

        Dim TP As Integer = tTP

        If CHARACTERs.ContainsKey(c.TargetGUID) Then
            CHARACTERs(c.TargetGUID).TalentPoints += TP
            CHARACTERs(c.TargetGUID).SetUpdateFlag(EPlayerFields.PLAYER_CHARACTER_POINTS1, CType(CHARACTERs(c.TargetGUID).TalentPoints, Integer))
            CHARACTERs(c.TargetGUID).SaveCharacter()
        Else
            c.CommandResponse("Target not found or not character.")
        End If

        Return True
    End Function
    <ChatCommandAttribute("AddHonor", "ADDHONOR <POINTs> - Add X honor points to selected character.", AccessLevel.Admin)> _
    Public Function cmdAddHonor(ByRef c As CharacterObject, ByVal tHONOR As String) As Boolean
        If IsNumeric(tHONOR) = False Then Return False

        Dim Honor As Integer = tHONOR

        If CHARACTERs.ContainsKey(c.TargetGUID) Then
            CHARACTERs(c.TargetGUID).HonorCurrency += Honor
            CHARACTERs(c.TargetGUID).SetUpdateFlag(EPlayerFields.PLAYER_FIELD_HONOR_CURRENCY, CHARACTERs(c.TargetGUID).HonorCurrency)
            CHARACTERs(c.TargetGUID).SendCharacterUpdate(False)
        Else
            c.CommandResponse("Target not found or not character.")
        End If

        Return True
    End Function
    <ChatCommandAttribute("AddArena", "ADDARENA <POINTs> - Add X arena points to selected character.", AccessLevel.Admin)> _
    Public Function cmdAddArena(ByRef c As CharacterObject, ByVal tHONOR As String) As Boolean
        If IsNumeric(tHONOR) = False Then Return False

        Dim Honor As Integer = tHONOR

        If CHARACTERs.ContainsKey(c.TargetGUID) Then
            CHARACTERs(c.TargetGUID).ArenaCurrency += Honor
            CHARACTERs(c.TargetGUID).SetUpdateFlag(EPlayerFields.PLAYER_FIELD_ARENA_CURRENCY, CHARACTERs(c.TargetGUID).ArenaCurrency)
            CHARACTERs(c.TargetGUID).SendCharacterUpdate(False)
        Else
            c.CommandResponse("Target not found or not character.")
        End If

        Return True
    End Function

    <ChatCommandAttribute("EditUnitFlag", "EDITUNITFLAG <UNITFLAG> - Change your unitflag.", AccessLevel.Developer)> _
    Public Function cmdEditUnitflag(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If IsNumeric(Message) = False AndAlso InStr(Message, "0x") = 0 Then Return False
        If InStr(Message, "0x") > 0 Then
            c.cUnitFlags = Val("&H" & Message.Replace("0x", ""))
        Else
            c.cUnitFlags = Message
        End If
        c.SetUpdateFlag(EUnitFields.UNIT_FIELD_FLAGS, c.cUnitFlags)
        c.SendCharacterUpdate()

        Return True
    End Function
    <ChatCommandAttribute("EditPlayerFlag", "EDITPLAYERFLAG <PLAYERFLAG> - Change your playerflag.", AccessLevel.Developer)> _
    Public Function cmdEditPlayerflag(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If IsNumeric(Message) = False AndAlso InStr(Message, "0x") = 0 Then Return False
        If InStr(Message, "0x") > 0 Then
            c.cPlayerFlags = Val("&H" & Message.Replace("0x", ""))
        Else
            c.cPlayerFlags = Message
        End If
        c.SetUpdateFlag(EPlayerFields.PLAYER_FLAGS, c.cPlayerFlags)
        c.SendCharacterUpdate()

        Return True
    End Function
    <ChatCommandAttribute("GroupUpdate", "GROUPUPDATE - Get a groupupdate for selected player.", AccessLevel.Developer)> _
    Public Function cmdGroupUpdate(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If c.TargetGUID <> 0 AndAlso GuidIsPlayer(c.TargetGUID) Then
            Dim response As PacketClass = BuildPartyMemberStats(CHARACTERs(c.TargetGUID), PartyMemberStatsFlag.GROUP_UPDATE_FULL)
            c.Client.Send(response)
            response.Dispose()
            Return True
        End If

        Return False
    End Function

    <ChatCommandAttribute("AddItem", "ADDITEM <ID> <optional COUNT> - Add Y items with id X to selected character.")> _
    Public Function cmdAddItem(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        Dim tmp() As String = Split(Message, " ", 2)
        If tmp.Length < 1 Then Return False

        Dim id As Integer = tmp(0)
        Dim Count As Integer = 1
        If tmp.Length = 2 Then Count = tmp(1)

        Dim newItem As New ItemObject(id, c.GUID)
        newItem.StackCount = Count
        If c.ItemADD(newItem) Then
            c.LogLootItem(newItem, Count, False, True)
        Else
            newItem.Delete()
        End If

        Return True
    End Function
    <ChatCommandAttribute("AddMoney", "ADDMONEY <XP> - Add X copper yours.")> _
    Public Function cmdAddMoney(ByRef c As CharacterObject, ByVal tCopper As String) As Boolean
        If tCopper = "" Then Return False

        Dim Copper As ULong = tCopper

        If CType(c.Copper, ULong) + Copper > UInteger.MaxValue Then
            c.CommandResponse("Can't store that much copper")
        Else
            c.Copper += Copper
            c.SetUpdateFlag(EPlayerFields.PLAYER_FIELD_COINAGE, c.Copper)
            c.SendCharacterUpdate(False)
        End If

        Return True
    End Function

    <ChatCommandAttribute("LearnSkill", "LearnSkill <ID> <CURRENT> <MAX> - Add skill id X with value Y of Z to selected character.")> _
    Public Function cmdLearnSkill(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If Message = "" Then Return False

        If CHARACTERs.ContainsKey(c.TargetGUID) Then
            Dim tmp() As String
            tmp = Split(Trim(Message), " ")

            Dim SkillID As Integer = tmp(0)
            Dim Current As Int16 = tmp(1)
            Dim Maximum As Int16 = tmp(2)

            If CHARACTERs(c.TargetGUID).Skills.ContainsKey(SkillID) Then
                CType(CHARACTERs(c.TargetGUID).Skills(SkillID), TSkill).Base = Maximum
                CType(CHARACTERs(c.TargetGUID).Skills(SkillID), TSkill).Current = Current
            Else
                CHARACTERs(c.TargetGUID).LearnSkill(SkillID, Current, Maximum)
            End If

            CHARACTERs(c.TargetGUID).FillAllUpdateFlags()
            CHARACTERs(c.TargetGUID).SendUpdate()
        Else
            c.CommandResponse("Target not found or not character.")
        End If

        Return True
    End Function

    <ChatCommandAttribute("LearnSpell", "LearnSpell <ID> - Add spell X to selected character.")> _
    Public Function cmdLearnSpell(ByRef c As CharacterObject, ByVal tID As String) As Boolean
        If tID = "" Then Return False

        Dim ID As Integer = tID

        If CHARACTERs.ContainsKey(c.TargetGUID) Then
            CHARACTERs(c.TargetGUID).LearnSpell(ID)
        Else
            c.CommandResponse("Target not found or not character.")
        End If

        Return True
    End Function

    <ChatCommandAttribute("ShowTaxi", "SHOWTAXI - Unlock all taxi locations.")> _
    Public Function cmdShowTaxi(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        c.TaxiZones.SetAll(True)
        Return True
    End Function

    <ChatCommandAttribute("SET", "SET <INDEX> <VALUE> - Set update value (A9).")> _
    Public Function cmdSetUpdateField(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If Message = "" Then Return False
        Dim tmp() As String = Split(Message, " ", 2)

        SetUpdateValue(c.TargetGUID, tmp(0), tmp(1), c.Client)
        Return True
    End Function

    <ChatCommandAttribute("SetRunSpeed", "SETRUNSPEED <VALUE> - Change your run speed.")> _
    Public Function cmdSetRunSpeed(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If Message = "" Then Return False
        If c.TargetGUID = 0 Then
            c.ChangeSpeedForced(WS_CharManagment.CharacterObject.ChangeSpeedType.RUN, Message)
            c.CommandResponse("Your RunSpeed is changed to " & Message)
            Return True
        End If
        If GuidIsPlayer(c.TargetGUID) AndAlso CHARACTERs.ContainsKey(c.TargetGUID) Then
            CHARACTERs(c.TargetGUID).ChangeSpeedForced(CharacterObject.ChangeSpeedType.RUN, Message)
            CHARACTERs(c.TargetGUID).SystemMessage(" Your RunSpeed was changed by " & c.Name)
            Return True
        Else
            c.CommandResponse(" Please Select Character")
            Return True
        End If

    End Function

    <ChatCommandAttribute("SetSwimSpeed", "SETSWIMSPEED <VALUE> - Change your swim speed.")> _
    Public Function cmdSetSwimSpeed(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If Message = "" Then Return False
        c.ChangeSpeedForced(WS_CharManagment.CharacterObject.ChangeSpeedType.SWIM, Message)
        c.CommandResponse("Your SwimSpeed is changed to " & Message)
        Return True
    End Function

    <ChatCommandAttribute("SetRunBackSpeed", "SETRUNBACKSPEED <VALUE> - Change your run back speed.")> _
    Public Function cmdSetRunBackSpeed(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If Message = "" Then Return False
        c.ChangeSpeedForced(WS_CharManagment.CharacterObject.ChangeSpeedType.SWIMBACK, Message)
        c.CommandResponse("Your RunBackSpeed is changed to " & Message)
        Return True
    End Function

    <ChatCommandAttribute("SetFlySpeed", "SETFLYSPEED <VALUE> - Change your fly speed.", AccessLevel.GameMaster)> _
    Public Function cmdSetFlySpeed(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If Message = "" Then Return False
        c.ChangeSpeedForced(WS_CharManagment.CharacterObject.ChangeSpeedType.FLY, Message)
        c.CommandResponse("Your FlySpeed is changed to " & Message)
        Return True
    End Function

    <ChatCommandAttribute("SetFlyBackSpeed", "SETFLYBACKSPEED <VALUE> - Change your fly back speed.", AccessLevel.GameMaster)> _
    Public Function cmdSetFlyBackSpeed(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If Message = "" Then Return False
        c.ChangeSpeedForced(WS_CharManagment.CharacterObject.ChangeSpeedType.FLYBACK, Message)
        c.CommandResponse("Your FlyBackSpeed is changed to " & Message)
        Return True
    End Function

    <ChatCommandAttribute("SetReputation", "SETREPUTATION <FACTION> <VALUE> - Change your reputation standings.", AccessLevel.GameMaster)> _
    Public Function cmdSetReputation(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If Message = "" Then Return False
        Dim tmp() As String = Split(Message, " ", 2)
        c.SetReputation(tmp(0), tmp(1))
        Return True
    End Function

    <ChatCommandAttribute("Mount", "Will mount you to specified model ID.", AccessLevel.GameMaster)> _
    Public Function cmdMount(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        Dim value As Integer = Message
        c.SetUpdateFlag(EUnitFields.UNIT_FIELD_MOUNTDISPLAYID, value)
        c.SendCharacterUpdate(True)
        Return True
    End Function

    <ChatCommandAttribute("FlyMountEnable", "Will enable fly mount mode.", AccessLevel.GameMaster)> _
    Public Function cmdFlyMountEnable(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        c.SetCanFly()
        Return True
    End Function

    <ChatCommandAttribute("FlyMountDisable", "Will disable fly mount mode.", AccessLevel.GameMaster)> _
    Public Function cmdFlyMountDisable(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        c.UnSetCanFly()
        Return True
    End Function

    <ChatCommandAttribute("Hurt", "Hurt selected character.", AccessLevel.GameMaster)> _
    Public Function cmdHurt(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If c.TargetGUID = 0 Then
            c.CommandResponse("Select target first!")
            Return True
        End If

        If CHARACTERs.ContainsKey(c.TargetGUID) Then
            CType(CHARACTERs(c.TargetGUID), CharacterObject).Life.Current -= CType(CHARACTERs(c.TargetGUID), CharacterObject).Life.Maximum * 0.1
            CType(CHARACTERs(c.TargetGUID), CharacterObject).SetUpdateFlag(EUnitFields.UNIT_FIELD_HEALTH, CType(CHARACTERs(c.TargetGUID), CharacterObject).Life.Current)
            CType(CHARACTERs(c.TargetGUID), CharacterObject).SendCharacterUpdate()
            Return True
        End If

        Return True
    End Function

    <ChatCommandAttribute("Root", "Instantly root selected character.", AccessLevel.GameMaster)> _
    Public Function cmdRoot(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If c.TargetGUID = 0 Then
            c.CommandResponse("Select target first!")
            Return True
        End If

        If CHARACTERs.ContainsKey(c.TargetGUID) Then
            CType(CHARACTERs(c.TargetGUID), CharacterObject).SetMoveRoot()
            Return True
        End If

        Return True
    End Function

    <ChatCommandAttribute("UnRoot", "Instantly unroot selected character.", AccessLevel.GameMaster)> _
    Public Function cmdUnRoot(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If c.TargetGUID = 0 Then
            c.CommandResponse("Select target first!")
            Return True
        End If

        If CHARACTERs.ContainsKey(c.TargetGUID) Then
            CType(CHARACTERs(c.TargetGUID), CharacterObject).SetMoveUnroot()
            Return True
        End If

        Return True
    End Function

    <ChatCommandAttribute("Revive", "Instantly revive selected character.", AccessLevel.GameMaster)> _
    Public Function cmdRevive(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If c.TargetGUID = 0 Then
            c.CommandResponse("Select target first!")
            Return True
        End If

        If CHARACTERs.ContainsKey(c.TargetGUID) Then
            CharacterResurrect(CType(CHARACTERs(c.TargetGUID), CharacterObject))
            Return True
        End If

        Return True
    End Function
    <ChatCommandAttribute("GoToGraveyard", "Instantly teleports selected character to nearest graveyard.", AccessLevel.GameMaster)> _
    Public Function cmdGoToGraveyard(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If c.TargetGUID = 0 Then
            c.CommandResponse("Select target first!")
            Return True
        End If

        If CHARACTERs.ContainsKey(c.TargetGUID) Then
            GoToNearestGraveyard(CType(CHARACTERs(c.TargetGUID), CharacterObject))
            Return True
        End If

        Return True
    End Function
    <ChatCommandAttribute("GoToStart", "GOTOSTART <RACE> - Instantly teleports selected character to specified race start location.", AccessLevel.GameMaster)> _
    Public Function cmdGoToStart(ByRef c As CharacterObject, ByVal StringRace As String) As Boolean
        If c.TargetGUID = 0 Then
            c.CommandResponse("Select target first!")
            Return True
        End If

        If CHARACTERs.ContainsKey(c.TargetGUID) Then
            Dim Info As New DataTable
            Dim Character As CharacterObject = CHARACTERs(c.TargetGUID)
            Dim Race As Races

            Select Case UCase(StringRace)
                Case "DRAENEI", "DR"
                    Race = Races.RACE_DRAENEI
                Case "BLOODELF", "BE"
                    Race = Races.RACE_BLOOD_ELF
                Case "DWARF", "DW"
                    Race = Races.RACE_DWARF
                Case "GNOME", "GN"
                    Race = Races.RACE_GNOME
                Case "HUMAN", "HU"
                    Race = Races.RACE_HUMAN
                Case "NIGHTELF", "NE"
                    Race = Races.RACE_NIGHT_ELF
                Case "ORC", "OR"
                    Race = Races.RACE_ORC
                Case "TAUREN", "TA"
                    Race = Races.RACE_TAUREN
                Case "TROLL", "TR"
                    Race = Races.RACE_TROLL
                Case "UNDEAD", "UN"
                    Race = Races.RACE_UNDEAD
                Case Else
                    c.CommandResponse("Unknown race. Use DR,BE,DW,GN,HU,NE,OR,TA,TR,UN for race.")
                    Return True
            End Select

            Database.Query(String.Format("SELECT * FROM playercreateinfo WHERE race = {0};", CType(Race, Integer)), Info)
            Character.Teleport(Info.Rows(0).Item("positionX"), Info.Rows(0).Item("positionY"), Info.Rows(0).Item("positionZ"), 0, Info.Rows(0).Item("mapID"))
            Return True
        End If

        Return True
    End Function
    <ChatCommandAttribute("Summon", "SUMMON <NAME> - Instantly teleports the player to you.", AccessLevel.GameMaster)> _
    Public Function cmdSummon(ByRef c As CharacterObject, ByVal Name As String) As Boolean
        Dim GUID As ULong = GetGUID(CapitalizeName(Name))
        If CHARACTERs.ContainsKey(GUID) Then
            CType(CHARACTERs(GUID), CharacterObject).Teleport(c.positionX, c.positionY, c.positionZ, c.orientation, c.MapID)
            Return True
        Else
            c.CommandResponse("Player not found.")
            Return True
        End If
    End Function
    <ChatCommandAttribute("Appear", "APPEAR <NAME> - Instantly teleports you to the player.", AccessLevel.GameMaster)> _
    Public Function cmdAppear(ByRef c As CharacterObject, ByVal Name As String) As Boolean
        Dim GUID As ULong = GetGUID(CapitalizeName(Name))
        If CHARACTERs.ContainsKey(GUID) Then
            With CType(CHARACTERs(GUID), CharacterObject)
                c.Teleport(.positionX, .positionY, .positionZ, .orientation, .MapID)
            End With
            Return True
        Else
            c.CommandResponse("Player not found.")
            Return True
        End If
    End Function

    <ChatCommandAttribute("GPS", "GPS - Tells you where you are located.", AccessLevel.GameMaster)> _
    Public Function cmdGPS(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        c.CommandResponse("X: " & c.positionX)
        c.CommandResponse("Y: " & c.positionY)
        c.CommandResponse("Z: " & c.positionZ)
        c.CommandResponse("Orientation: " & c.orientation)
        c.CommandResponse("Map: " & c.MapID)
        Return True
    End Function

    <ChatCommandAttribute("Port", "PORT <X>,<Y>,<Z>,<ORIENTATION>,<MAP> - Teleports Character To Given Coordinates.", AccessLevel.GameMaster)> _
    Public Function cmdPort(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If Message = "" Then Return False

        Dim tmp() As String
        tmp = Split(Trim(Message), ",")

        Dim posX As Single = tmp(0)
        Dim posY As Single = tmp(1)
        Dim posZ As Single = tmp(2)
        Dim posO As Single = tmp(3)
        Dim posMap As Integer = tmp(4)

        c.Teleport(posX, posY, posZ, posO, posMap)
        Return True
    End Function

    <ChatCommandAttribute("PortByName", "PORT <LocationName> - Teleports Character To The LocationName Location. Use PortByName list to get a list of locations.", AccessLevel.GameMaster)> _
    Public Function cmdPortByName(ByRef c As CharacterObject, ByVal location As String) As Boolean

        If location = "" Then Return False

        Dim posX As Single = 0
        Dim posY As Single = 0
        Dim posZ As Single = 0
        Dim posO As Single = 0
        Dim posMap As Integer = 0

        If UCase(location) = "LIST" Then
            Dim cmdList As String = "Listing of available locations:" & vbNewLine

            Dim ListSQLQuery As New DataTable
            Database.Query("SELECT * FROM world_cmdteleports", ListSQLQuery)

            For Each LocationRow As DataRow In ListSQLQuery.Rows
                cmdList += LocationRow.Item("name") & ", "
            Next
            c.CommandResponse(cmdList)
            Return True
        End If

        Dim MySQLQuery As New DataTable
        Database.Query(String.Format("SELECT * FROM world_cmdteleports WHERE name = '{0}' LIMIT 1;", location), MySQLQuery)

        If MySQLQuery.Rows.Count > 0 Then
            posX = CType(MySQLQuery.Rows(0).Item("positionX"), Single)
            posY = CType(MySQLQuery.Rows(0).Item("positionY"), Single)
            posZ = CType(MySQLQuery.Rows(0).Item("positionZ"), Single)
            posMap = CType(MySQLQuery.Rows(0).Item("MapId"), Integer)
            c.Teleport(posX, posY, posZ, posO, posMap)
        Else
            c.CommandResponse(String.Format("Location {0} NOT found in Database", location))
        End If

        Return True
    End Function

    '****************************************** ACCOUNT MANAGMENT COMMANDs ******************************************
    <ChatCommandAttribute("Slap", "SLAP <DAMAGE> - Slap target creature or player for X damage.")> _
    Public Function cmdSlap(ByRef c As CharacterObject, ByVal tDamage As String) As Boolean
        Dim Damage As Integer = tDamage

        If GuidIsCreature(c.TargetGUID) Then
            CType(WORLD_CREATUREs(c.TargetGUID), CreatureObject).DealDamage(Damage)
        ElseIf GuidIsPlayer(c.TargetGUID) Then
            CType(CHARACTERs(c.TargetGUID), CharacterObject).DealDamage(Damage)
            CType(CHARACTERs(c.TargetGUID), CharacterObject).SystemMessage(c.Name & " slaps you for " & Damage & " damage.")
        Else
            c.CommandResponse("Not supported target selected.")
        End If

        Return True
    End Function
    <ChatCommandAttribute("Kick", "KICK <optional NAME> - Kick selected player or character with name specified if found.")> _
    Public Function cmdKick(ByRef c As CharacterObject, ByVal Name As String) As Boolean
        If Name = "" Then

            'DONE: Kick by selection
            If c.TargetGUID = 0 Then
                c.CommandResponse("No target selected.")
            ElseIf CHARACTERs.ContainsKey(c.TargetGUID) Then
                'DONE: Kick gracefully
                c.CommandResponse(String.Format("Character [{0}] kicked form server.", CType(CHARACTERs(c.TargetGUID), CharacterObject).Name))
                Log.WriteLine(LogType.INFORMATION, "[{0}:{1}] Character [{3}] kicked by [{2}].", c.Client.IP.ToString, c.Client.Port, c.Client.Character.Name, CHARACTERs(c.TargetGUID).Name)
                CHARACTERs(c.TargetGUID).Logout()
            Else
                c.CommandResponse(String.Format("Character GUID=[{0}] not found.", c.TargetGUID))
            End If

        Else

            'DONE: Kick by name
            CHARACTERs_Lock.AcquireReaderLock(DEFAULT_LOCK_TIMEOUT)
            For Each Character As KeyValuePair(Of ULong, CharacterObject) In CHARACTERs
                If UCase(CType(Character.Value, CharacterObject).Name) = Name Then
                    CHARACTERs_Lock.ReleaseReaderLock()
                    'DONE: Kick gracefully
                    Character.Value.Logout()
                    c.CommandResponse(String.Format("Character [{0}] kicked form server.", CType(Character.Value, CharacterObject).Name))
                    Log.WriteLine(LogType.INFORMATION, "[{0}:{1}] Character [{3}] kicked by [{2}].", c.Client.IP.ToString, c.Client.Port, c.Client.Character.Name, Name)
                    Return True
                End If
            Next
            CHARACTERs_Lock.ReleaseReaderLock()
            c.CommandResponse(String.Format("Character [{0:X}] not found.", Name))

        End If
        Return True
    End Function
    <ChatCommandAttribute("KickReason", "KICKREASON <TEXT> - Display message for 2 seconds and kick selected player.")> _
    Public Function cmdKickReason(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If c.TargetGUID = 0 Then
            c.CommandResponse("No target selected.")
        Else
            SystemMessage(String.Format("Character [{0}] kicked form server.{3}Reason: {1}{3}GameMaster: [{2}].", SetColor(CType(CHARACTERs(c.TargetGUID), CharacterObject).Name, 255, 0, 0), SetColor(Message, 255, 0, 0), SetColor(c.Name, 255, 0, 0), vbNewLine))
            Thread.Sleep(2000)

            cmdKick(c, "")
        End If

        Return True
    End Function
    <ChatCommandAttribute("Disconnect", "DISCONNECT <optional NAME> - Disconnects selected player or character with name specified if found.")> _
    Public Function cmdDisconnect(ByRef c As CharacterObject, ByVal Name As String) As Boolean
        If Name = "" Then

            'DONE: Kick by selection
            If c.TargetGUID = 0 Then
                c.CommandResponse("No target selected.")
            ElseIf CHARACTERs.ContainsKey(c.TargetGUID) Then
                c.CommandResponse(String.Format("Character [{0}] kicked form server.", CType(CHARACTERs(c.TargetGUID), CharacterObject).Name))
                Log.WriteLine(LogType.INFORMATION, "[{0}:{1}] Character [{3}] kicked by [{2}].", c.Client.IP.ToString, c.Client.Port, c.Client.Character.Name, CHARACTERs(c.TargetGUID).Name)
                CType(CHARACTERs(c.TargetGUID), CharacterObject).Client.Disconnect()
            Else
                c.CommandResponse(String.Format("Character GUID=[{0}] not found.", c.TargetGUID))
            End If

        Else

            'DONE: Kick by name
            CHARACTERs_Lock.AcquireReaderLock(DEFAULT_LOCK_TIMEOUT)
            For Each Character As KeyValuePair(Of ULong, CharacterObject) In CHARACTERs
                If UCase(CType(Character.Value, CharacterObject).Name) = Name Then
                    CHARACTERs_Lock.ReleaseReaderLock()
                    c.CommandResponse(String.Format("Character [{0}] kicked form server.", CType(Character.Value, CharacterObject).Name))
                    Log.WriteLine(LogType.INFORMATION, "[{0}:{1}] Character [{3}] kicked by [{2}].", c.Client.IP.ToString, c.Client.Port, c.Client.Character.Name, Name)
                    CType(Character.Value, CharacterObject).Client.Disconnect()
                    Return True
                End If
            Next
            CHARACTERs_Lock.ReleaseReaderLock()
            c.CommandResponse(String.Format("Character [{0:X}] not found.", Name))

        End If
        Return True
    End Function

    <ChatCommandAttribute("ForceRename", "FORCERENAME - Force selected player to change his name next time on char enum.")> _
    Public Function cmdForceRename(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If c.TargetGUID = 0 Then
            c.CommandResponse("No target selected.")
        ElseIf CHARACTERs.ContainsKey(c.TargetGUID) Then
            Database.Update(String.Format("UPDATE characters SET force_restrictions = 1 WHERE char_guid = {0};", c.TargetGUID))
            c.CommandResponse("Player will be asked to change his name on next logon.")
        Else
            c.CommandResponse(String.Format("Character GUID=[{0:X}] not found.", c.TargetGUID))
        End If

        Return True
    End Function
    <ChatCommandAttribute("BanChar", "BANCHAR - Selected player won't be able to login next time with this character.")> _
    Public Function cmdBanChar(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If c.TargetGUID = 0 Then
            c.CommandResponse("No target selected.")
        ElseIf CHARACTERs.ContainsKey(c.TargetGUID) Then
            Database.Update(String.Format("UPDATE characters SET force_restrictions = 2 WHERE char_guid = {0};", c.TargetGUID))
            c.CommandResponse("Character disabled.")
        Else
            c.CommandResponse(String.Format("Character GUID=[{0:X}] not found.", c.TargetGUID))
        End If

        Return True
    End Function

    <ChatCommandAttribute("Ban", "BAN <ACCOUNT> - Ban specified account from server.")> _
    Public Function cmdBan(ByRef c As CharacterObject, ByVal Name As String) As Boolean
        If Name = "" Then Return False

        Dim result As New DataTable
        Database.Query("SELECT banned FROM accounts WHERE account = """ & Name & """;", result)
        If result.Rows.Count > 0 Then
            If result.Rows(0).Item("banned") = 1 Then
                c.CommandResponse(String.Format("Account [{0}] already banned.", Name))
            Else
                Database.Update("UPDATE accounts SET banned = 1 WHERE account = """ & Name & """;")
                c.CommandResponse(String.Format("Account [{0}] banned.", Name))
                Log.WriteLine(LogType.INFORMATION, "[{0}:{1}] Account [{3}] banned by [{2}].", c.Client.IP.ToString, c.Client.Port, c.Name, Name)
            End If
        Else
            c.CommandResponse(String.Format("Account [{0}] not found.", Name))
        End If

        Return True
    End Function
    <ChatCommandAttribute("UnBan", "UNBAN <ACCOUNT> - Remove ban of specified account from server.")> _
    Public Function cmdUnBan(ByRef c As CharacterObject, ByVal Name As String) As Boolean
        If Name = "" Then Return False

        Dim result As New DataTable
        Database.Query("SELECT banned FROM accounts WHERE account = """ & Name & """;", result)
        If result.Rows.Count > 0 Then
            If result.Rows(0).Item("banned") = 0 Then
                c.CommandResponse(String.Format("Account [{0}] is not banned.", Name))
            Else
                Database.Update("UPDATE accounts SET banned = 0 WHERE account = """ & Name & """;")
                c.CommandResponse(String.Format("Account [{0}] unbanned.", Name))
                Log.WriteLine(LogType.INFORMATION, "[{0}:{1}] Account [{3}] unbanned by [{2}].", c.Client.IP.ToString, c.Client.Port, c.Name, Name)
            End If
        Else
            c.CommandResponse(String.Format("Account [{0}] not found.", Name))
        End If

        Return True
    End Function

    <ChatCommandAttribute("Info", "INFO <optional NAME> - Show account information for selected target or character with name specified if found.")> _
    Public Function cmdInfo(ByRef c As CharacterObject, ByVal Name As String) As Boolean
        If Name = "" Then

            Dim GUID As ULong = c.TargetGUID
            If GUID = 0 Then GUID = c.GUID

            'DONE: Info by selection
            If CHARACTERs.ContainsKey(GUID) Then
                c.CommandResponse(String.Format("Information for character [{0}]:{1}account = {2}{1}ip = {3}{1}guid = {4:X}{1}access = {5}", _
                CHARACTERs(GUID).Name, vbNewLine, _
                CHARACTERs(GUID).Client.Account, _
                CHARACTERs(GUID).Client.IP.ToString, _
                CHARACTERs(GUID).GUID, _
                CHARACTERs(GUID).Access))
            ElseIf WORLD_CREATUREs.ContainsKey(GUID) Then
                c.CommandResponse(String.Format("Information for creature [{0}]:{1}id = {2}{1}guid = {3:X}{1}model = {4}{1}ai = {5}{1}his reaction = {6}{1}guard = {7}{1}spawnID = {8}{1}Current Waypoint = {9}", _
                WORLD_CREATUREs(GUID).Name, vbNewLine, _
                WORLD_CREATUREs(GUID).ID, _
                GUID, _
                CREATURESDatabase(WORLD_CREATUREs(GUID).ID).Model, _
                WORLD_CREATUREs(GUID).aiScript.GetType().ToString, _
                c.GetReaction(WORLD_CREATUREs(GUID).Faction), _
                WORLD_CREATUREs(GUID).isGuard, _
                WORLD_CREATUREs(GUID).SpawnID, _
                WORLD_CREATUREs(GUID).CurrentWaypoint))
            ElseIf WORLD_GAMEOBJECTs.ContainsKey(GUID) Then
                c.CommandResponse(String.Format("Information for gameobject [{0}]:{1}id = {2}{1}guid = {3:X}{1}model = {4}", _
                WORLD_GAMEOBJECTs(GUID).Name, vbNewLine, _
                WORLD_GAMEOBJECTs(GUID).ID, _
                GUID, _
                GAMEOBJECTSDatabase(WORLD_GAMEOBJECTs(GUID).ID).Model))
            Else
                c.CommandResponse(String.Format("GUID=[{0:X}] not found or unsupported.", GUID))
            End If

        Else

            'DONE: Info by name
            CHARACTERs_Lock.AcquireReaderLock(DEFAULT_LOCK_TIMEOUT)
            For Each Character As KeyValuePair(Of ULong, CharacterObject) In CHARACTERs
                If UCase(CType(Character.Value, CharacterObject).Name) = Name Then
                    CHARACTERs_Lock.ReleaseReaderLock()
                    c.CommandResponse(String.Format("Information for character [{0}]:{1}account = {2}{1}ip = {3}{1}guid = {4}{1}access = {5}", _
                    CType(Character.Value, CharacterObject).Name, vbNewLine, _
                    CType(Character.Value, CharacterObject).Client.Account, _
                    CType(Character.Value, CharacterObject).Client.IP.ToString, _
                    CType(Character.Value, CharacterObject).GUID, _
                    CType(Character.Value, CharacterObject).Access))
                    Exit Function
                End If
            Next
            CHARACTERs_Lock.ReleaseReaderLock()
            c.CommandResponse(String.Format("Character [{0}] not found.", Name))

        End If

        Return True
    End Function
    <ChatCommandAttribute("Where", "WHERE - Display your position information.")> _
    Public Function cmdWhere(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        c.SystemMessage(String.Format("Coords: x={0}, y={1}, z={2}, or={3}, map={4}", c.positionX, c.positionY, c.positionZ, c.orientation, c.MapID))
        c.SystemMessage(String.Format("Cell: {0},{1} SubCell: {2},{3}", GetMapTileX(c.positionX), GetMapTileY(c.positionY), GetSubMapTileX(c.positionX), GetSubMapTileY(c.positionY)))
        c.SystemMessage(String.Format("ZCoords: {0} AreaFlag: {1} WaterLevel={2}", GetZCoord(c.positionX, c.positionY, c.MapID), GetAreaFlag(c.positionX, c.positionY, c.MapID), GetWaterLevel(c.positionX, c.positionY, c.MapID)))
        c.ZoneCheck()
        c.SystemMessage(String.Format("ZoneID: {0}", c.ZoneID))
#If ENABLE_PPOINTS Then
        c.SystemMessage(String.Format("ZCoords_PP: {0}", GetZCoord_PP(c.positionX, c.positionY, c.MapID)))
#End If

        Return True
    End Function

    '****************************************** MISC COMMANDs *******************************************************
    <ChatCommandAttribute("SetGM", "SETGM <FLAG> <INVISIBILITY> - Toggles gameMaster status. You can use values like On/Off/1/0.")> _
    Public Function cmdSetGM(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        Dim tmp() As String = Split(Message, " ", 2)
        Dim value1 As String = tmp(0)
        Dim value2 As String = tmp(1)

        'Commnad: .setgm <gmflag:0/1/off/on> <invisibility:0/1/off/on>
        If value1 = "0" Or UCase(value1) = "OFF" Then
            c.GM = False
            c.CommandResponse("GameMaster Flag turned off.")
        Else
            c.GM = True
            c.CommandResponse("GameMaster Flag turned on.")
        End If
        If value2 = "0" Or UCase(value2) = "OFF" Then
            c.Invisibility = InvisibilityLevel.VISIBLE
            c.CanSeeInvisibility = InvisibilityLevel.VISIBLE
            c.CommandResponse("GameMaster Invisibility turned off.")
        Else
            c.Invisibility = InvisibilityLevel.GM
            c.CanSeeInvisibility = InvisibilityLevel.GM
            c.CommandResponse("GameMaster Invisibility turned on.")
        End If
        c.SetUpdateFlag(EPlayerFields.PLAYER_FLAGS, c.cPlayerFlags)
        c.SendCharacterUpdate()
        UpdateCell(c)

        Return True
    End Function
    <ChatCommandAttribute("SetWeather", "SETWEATHER <TYPE> <INTENSITY> - Change weather in current zone. Intensity is float value!")> _
    Public Function cmdSetWeather(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        Dim tmp() As String = Split(Message, " ", 2)
        Dim Type As Integer = tmp(0)
        Dim Intensity As Single = tmp(1)

        Dim MySQLQuery As New DataTable
        Database.Query(String.Format("SELECT * FROM weather WHERE weather_zone = {0};", c.ZoneID), MySQLQuery)
        If MySQLQuery.Rows.Count = 0 Then
            Database.Update(String.Format("INSERT INTO weather (weather_zone, weather_type, weather_intensity) VALUES ({0}, {1}, {2});", c.ZoneID, Type, Trim(Str(Intensity))))
        Else
            Database.Update(String.Format("UPDATE weather SET weather_zone = {0}, weather_type = {1}, weather_intensity = {2};", c.ZoneID, Type, Trim(Str(Intensity))))
        End If
        SendWeather(Type, Intensity, c.Client)

        Return True
    End Function

    '****************************************** SPAWNING COMMANDs ***************************************************
    <ChatCommandAttribute("Del", "DEL <ID> - Delete selected creature or gameobject."), _
     ChatCommandAttribute("Delete", "DELETE <ID> - Delete selected creature or gameobject."), _
     ChatCommandAttribute("Remove", "REMOVE <ID> - Delete selected creature or gameobject.")> _
    Public Function cmdDeleteObject(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If c.TargetGUID = 0 Then
            c.CommandResponse("Select target first!")
            Return True
        End If

        If GuidIsCreature(c.TargetGUID) Then
            'DONE: Delete creature
            If Not WORLD_CREATUREs.ContainsKey(c.TargetGUID) Then
                c.CommandResponse("Selected target is not creature!")
                Return True
            End If

            WORLD_CREATUREs(c.TargetGUID).Destroy()
            c.CommandResponse("Creature deleted.")

        ElseIf GuidIsGameObject(c.TargetGUID) Then
            'DONE: Delete GO
            If Not WORLD_GAMEOBJECTs.ContainsKey(c.TargetGUID) Then
                c.CommandResponse("Selected target is not game object!")
                Return True
            End If

            WORLD_GAMEOBJECTs(c.TargetGUID).Destroy()
            c.CommandResponse("Game object deleted.")

        End If

        Return True
    End Function
    <ChatCommandAttribute("Turn", "TURN - Selected creature or game object will turn to your position.")> _
    Public Function cmdTurnObject(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If c.TargetGUID = 0 Then
            c.CommandResponse("Select target first!")
            Return True
        End If

        If GuidIsCreature(c.TargetGUID) Then
            'DONE: Turn creature
            If Not WORLD_CREATUREs.ContainsKey(c.TargetGUID) Then
                c.CommandResponse("Selected target is not creature!")
                Return True
            End If

            CType(WORLD_CREATUREs(c.TargetGUID), CreatureObject).TurnTo(c.positionX, c.positionY)

        ElseIf GuidIsGameObject(c.TargetGUID) Then
            'DONE: Turn GO
            If Not WORLD_GAMEOBJECTs.ContainsKey(c.TargetGUID) Then
                c.CommandResponse("Selected target is not game object!")
                Return True
            End If

            CType(WORLD_GAMEOBJECTs(c.TargetGUID), GameObjectObject).TurnTo(c.positionX, c.positionY)

            Dim q As New DataTable
            Dim GUID As ULong = c.TargetGUID - GUID_GAMEOBJECT

            c.CommandResponse("Object rotation will be visible when the object is reloaded!")

        End If

        Return True
    End Function

    <ChatCommandAttribute("AddNPC", "ADDNPC <ID> - Spawn creature at your position."), _
     ChatCommandAttribute("AddCreature", "ADDCREATURE <ID> - Spawn creature at your position.")> _
    Public Function cmdAddCreature(ByRef c As CharacterObject, ByVal Message As String) As Boolean

        Dim tmpCr As CreatureObject = New CreatureObject(CType(Message, Integer), c.positionX, c.positionY, c.positionZ, c.orientation, c.MapID)
        tmpCr.AddToWorld()
        c.CommandResponse("Creature [" & tmpCr.Name & "] spawned.")

        Return True
    End Function
    <ChatCommandAttribute("NPCFlood", "NPCFLOOD <Amount> - Spawn a number of creatures at your position.")> _
    Public Function cmdCreatureFlood(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If IsNumeric(Message) = False OrElse CInt(Message) <= 0 Then Return False
        For i As Integer = 1 To CInt(Message)
            Dim tmpCreature As New CreatureObject(20472, c.positionX, c.positionY, c.positionZ, c.orientation, c.MapID)
            tmpCreature.CreatedBy = c.GUID
            tmpCreature.CreatedBySpell = 35239
            tmpCreature.AddToWorld()
        Next

        Return True
    End Function
    <ChatCommandAttribute("Come", "COME - Selected creature will come to your position.")> _
    Public Function cmdComeCreature(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If c.TargetGUID = 0 Then
            c.CommandResponse("Select target first!")
            Return True
        End If
        If Not WORLD_CREATUREs.ContainsKey(c.TargetGUID) Then
            c.CommandResponse("Selected target is not creature!")
            Return True
        End If

        CType(WORLD_CREATUREs(c.TargetGUID), CreatureObject).MoveTo(c.positionX, c.positionY, c.positionZ)

        Return True
    End Function
    <ChatCommandAttribute("Kill", "KILL - Selected creature or character will die.")> _
    Public Function cmdKillCreature(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If c.TargetGUID = 0 Then
            c.CommandResponse("Select target first!")
            Return True
        End If

        If CHARACTERs.ContainsKey(c.TargetGUID) Then
            CHARACTERs(c.TargetGUID).Die(c)
            Return True
        End If
        If WORLD_CREATUREs.ContainsKey(c.TargetGUID) Then
            'CType(WORLD_CREATUREs(c.TargetGUID), CreatureObject).Die(CType(c, CharacterObject))

            CType(WORLD_CREATUREs(c.TargetGUID), CreatureObject).DealDamage(CType(WORLD_CREATUREs(c.TargetGUID), CreatureObject).Life.Maximum)
            Return True
        End If
        Return True
    End Function

    <ChatCommandAttribute("TargetGo", "TARGETGO - Nearest game object will be selected.")> _
    Public Function cmdTargetGameObject(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        Dim minDistance As Single = Single.MaxValue
        Dim tmpDistance As Single

        For Each GUID As ULong In c.gameObjectsNear
            tmpDistance = GetDistance(WORLD_GAMEOBJECTs(GUID), CType(c, CharacterObject))
            If tmpDistance < minDistance Then
                minDistance = tmpDistance
                c.TargetGUID = GUID
            End If
        Next

        If minDistance = Single.MaxValue Then
            c.CommandResponse("Could not find any near objects.")
        Else
            c.CommandResponse(String.Format("Selected [{0}] game object at distance {1}.", WORLD_GAMEOBJECTs(c.TargetGUID).Name, minDistance))
        End If

        Return True
    End Function
    <ChatCommandAttribute("AddGO", "ADDGO <ID> - Spawn game object at your position."), _
     ChatCommandAttribute("AddGameObject", "ADDGAMEOBJECT <ID> - Spawn game object at your position.")> _
    Public Function cmdAddGameObject(ByRef c As CharacterObject, ByVal Message As String) As Boolean

        Dim tmpGO As GameObjectObject = New GameObjectObject(CType(Message, Integer), c.MapID, c.positionX, c.positionY, c.positionZ, c.orientation)
        tmpGO.Rotations(2) = Math.Sin(tmpGO.orientation / 2)
        tmpGO.Rotations(3) = Math.Cos(tmpGO.orientation / 2)

        c.CommandResponse("GameObject [" & tmpGO.Name & "] spawned.")

        Return True
    End Function
    <ChatCommandAttribute("CreateAccount", "CreateAccount <Name> <Password> <Email> - Add a new account using Name, Password, and Email.")> _
    Public Function cmdCreateAccount(ByRef c As CharacterObject, ByVal Message As String) As Boolean
        If Message = "" Then Return False
        Dim result As New DataTable
        Dim acct() As String
        acct = Split(Trim(Message), " ")

        Dim aName As String = acct(0)
        Dim aPassword As String = acct(1)
        Dim aEmail As String = acct(2)
        Database.Query("SELECT account FROM accounts WHERE account = """ & aName & """;", result)
        If result.Rows.Count > 0 Then
        ElseIf result.Rows(0).Item("account") = aName Then
            c.CommandResponse(String.Format("Account [{0}] already exists.", aName))
        Else

            Database.Insert(String.Format("INSERT INTO accounts (account, password, email, joindate, last_ip) VALUES ('{0}', '{1}', '{2}', '{3}', '{4}')", aName, aPassword, aEmail, Format(Now, "yyyy-MM-dd"), "0.0.0.0"))
            c.CommandResponse(String.Format("Account [{0}] has been created.", aName))
        End If
        Return True
    End Function

#End Region
#Region "WS.Commands.InternalCommands.HelperSubs"

    Public Function GetGUID(ByVal Name As String) As ULong
        Dim MySQLQuery As New DataTable
        Database.Query(String.Format("SELECT char_guid FROM characters WHERE char_name = ""{0}"";", Name), MySQLQuery)

        If MySQLQuery.Rows.Count > 0 Then
            Return CType(MySQLQuery.Rows(0).Item("char_guid"), ULong)
        Else
            Return 0
        End If
    End Function
    Public Sub SystemMessage(ByVal Message As String)
        Dim packet As PacketClass = BuildChatMessage(0, "System Message: " & Message, ChatMsg.CHAT_MSG_SYSTEM, LANGUAGES.LANG_UNIVERSAL, 0, "")

        packet.UpdateLength()
        WS.Cluster.Broadcast(packet.Data)
        packet.Dispose()
    End Sub
    Public Function SetUpdateValue(ByVal GUID As ULong, ByVal Index As Integer, ByVal Value As Integer, ByVal Client As ClientClass) As Boolean
        Dim packet As New PacketClass(OPCODES.SMSG_UPDATE_OBJECT)
        packet.AddInt32(1)      'Operations.Count
        'packet.AddInt8(0)
        Dim UpdateData As New UpdateClass
        UpdateData.SetUpdateFlag(Index, Value)

        If GuidIsCreature(GUID) Then
            UpdateData.AddToPacket(packet, ObjectUpdateType.UPDATETYPE_VALUES, CType(WORLD_CREATUREs(GUID), CreatureObject))
        ElseIf GuidIsPlayer(GUID) Then
            If GUID = Client.Character.GUID Then
                UpdateData.AddToPacket(packet, ObjectUpdateType.UPDATETYPE_VALUES, CType(CHARACTERs(GUID), CharacterObject))
            Else
                UpdateData.AddToPacket(packet, ObjectUpdateType.UPDATETYPE_VALUES, CType(CHARACTERs(GUID), CharacterObject))
            End If
        End If

        Client.Send(packet)
        packet.Dispose()
        UpdateData.Dispose()
    End Function

#End Region

End Module
