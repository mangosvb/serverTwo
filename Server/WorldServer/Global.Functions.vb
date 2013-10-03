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
Imports System.Reflection
Imports System.Text.RegularExpressions
Imports mangosVB.Common.BaseWriter
Imports System.Security.Cryptography

Public Module Functions

#Region "System"

    Public Function ToInteger(ByVal Value As Boolean) As Integer
        If Value Then
            Return 1
        Else
            Return 0
        End If
    End Function

    Public Function HaveFlag(ByVal value As UInteger, ByVal flagPos As Byte) As Boolean
        value = value >> flagPos
        value = value Mod 2

        If value = 1 Then
            Return True
        Else
            Return False
        End If
    End Function
    Public Function HaveFlags(ByVal value As Integer, ByVal flags As Integer) As Boolean
        Return ((value And flags) = flags)
    End Function
    Public Sub SetFlag(ByRef value As UInteger, ByVal flagPos As Byte, ByVal flagValue As Boolean)
        If flagValue Then
            value = (value Or (&H1UI << flagPos))
        Else
            value = (value And ((&H0UI << flagPos) And &HFFFFFFFFUI))
        End If
    End Sub

    Public Function GetTimestamp(ByVal fromDateTime As DateTime) As UInteger
        Dim startDate As DateTime = #1/1/1970#
        Dim timeSpan As System.TimeSpan

        timeSpan = fromDateTime.Subtract(startDate)
        Return CType(Math.Abs(timeSpan.TotalSeconds()), UInteger)
    End Function
    Public Function GetDateFromTimestamp(ByVal unixTimestamp As UInteger) As DateTime
        Dim timeSpan As System.TimeSpan
        Dim startDate As Date = #1/1/1970#

        If unixTimestamp = 0 Then Return startDate

        timeSpan = New System.TimeSpan(0, 0, unixTimestamp)
        Return startDate.Add(timeSpan)
    End Function

#If LINUX Then
    Public Function timeGetTime() As Integer
        Return System.Environment.TickCount()
    End Function
    Public Function timeBeginPeriod(ByVal uPeriod As Integer) As Integer
        Return 0
    End Function
#Else
    Public Declare Function timeGetTime Lib "winmm.dll" () As Integer
    Public Declare Function timeBeginPeriod Lib "winmm.dll" (ByVal uPeriod As Integer) As Integer
#End If

    Public Function EscapeString(ByVal s As String) As String
        Return s.Replace("""", "").Replace("'", "")
    End Function

    Public Function CapitalizeName(ByRef Name As String) As String
        If Name.Length > 1 Then 'Why would a name be one letter, or even 0? :P
            Return UCase(Left(Name, 1)) & LCase(Right(Name, Name.Length - 1))
        Else
            Return UCase(Name)
        End If
    End Function

    Private Regex_AZ As Regex = New Regex("^[a-zA-Z]+$")
    Public Function ValidateName(ByVal strName As String) As Boolean
        If strName.Length < 2 OrElse strName.Length > 16 Then Return False
        Return Regex_AZ.IsMatch(strName)
    End Function

    Private Regex_Guild As Regex = New Regex("^[a-z A-Z]+$")
    Public Function ValidateGuildName(ByVal strName As String) As Boolean
        If strName.Length < 2 OrElse strName.Length > 16 Then Return False
        Return Regex_Guild.IsMatch(strName)
    End Function

#End Region
#Region "Database"

    Public Sub Ban_Account(ByVal Name As String, ByVal Reason As String)
        Database.Update("UPDATE accounts SET banned = 1 WHERE account = """ & Name & """;")

        Log.WriteLine(LogType.INFORMATION, "Account [{0}] banned by server. Reason: [{1}].", Name, Reason)
    End Sub

#End Region
#Region "Game"

    Public Function GetClassName(ByRef Classe As Integer) As String
        Select Case Classe
            Case Classes.CLASS_DEATH_KNIGHT
                GetClassName = "Death Knight"
            Case Classes.CLASS_DRUID
                GetClassName = "Druid"
            Case Classes.CLASS_HUNTER
                GetClassName = "Hunter"
            Case Classes.CLASS_MAGE
                GetClassName = "Mage"
            Case Classes.CLASS_PALADIN
                GetClassName = "Paladin"
            Case Classes.CLASS_PRIEST
                GetClassName = "Priest"
            Case Classes.CLASS_ROGUE
                GetClassName = "Rogue"
            Case Classes.CLASS_SHAMAN
                GetClassName = "Shaman"
            Case Classes.CLASS_WARLOCK
                GetClassName = "Warlock"
            Case Classes.CLASS_WARRIOR
                GetClassName = "Warrior"
            Case Else
                GetClassName = "Unknown Class"
        End Select
    End Function
    Public Function GetRaceName(ByRef Race As Integer) As String
        Select Case Race
            Case Races.RACE_BLOOD_ELF
                GetRaceName = "Blood Elf"
            Case Races.RACE_DRAENEI
                GetRaceName = "Draenei"
            Case Races.RACE_DWARF
                GetRaceName = "Dwarf"
            Case Races.RACE_GNOME
                GetRaceName = "Gnome"
            Case Races.RACE_GOBLIN
                GetRaceName = "Goblin"
            Case Races.RACE_HUMAN
                GetRaceName = "Human"
            Case Races.RACE_NIGHT_ELF
                GetRaceName = "Night Elf"
            Case Races.RACE_ORC
                GetRaceName = "Orc"
            Case Races.RACE_TAUREN
                GetRaceName = "Tauren"
            Case Races.RACE_TROLL
                GetRaceName = "Troll"
            Case Races.RACE_UNDEAD
                GetRaceName = "Undead"
            Case Else
                GetRaceName = "Unknown Race"
        End Select
    End Function
    Public Function GetRaceModel(ByVal Race As Races, ByVal Gender As Integer) As Integer
        Select Case Race
            Case Races.RACE_HUMAN
                Return 49 + Gender
            Case Races.RACE_ORC
                Return 51 + Gender
            Case Races.RACE_DWARF
                Return 53 + Gender
            Case Races.RACE_NIGHT_ELF
                Return 55 + Gender
            Case Races.RACE_UNDEAD
                Return 57 + Gender
            Case Races.RACE_TAUREN
                Return 59 + Gender
            Case Races.RACE_GNOME
                Return 1563 + Gender
            Case Races.RACE_TROLL
                Return 1478 + Gender
            Case Races.RACE_BLOOD_ELF
                Return 15476 - Gender           '15476 = m 15475 = f
            Case Races.RACE_DRAENEI
                Return 16125 + Gender           '16126 = f 16125 = m
            Case Else
                Return 16358                    'PinkPig
        End Select
    End Function
    Public Function GetCharacterSide(ByVal Race As Byte) As Boolean
        Select Case Race
            Case Races.RACE_DWARF, Races.RACE_GNOME, Races.RACE_HUMAN, Races.RACE_NIGHT_ELF, Races.RACE_DRAENEI
                Return False
            Case Else
                Return True
        End Select
    End Function
    Public Function IsContinentMap(ByVal Map As Integer) As Boolean
        Select Case Map
            Case 0, 1, 530
                Return True
            Case Else
                Return False
        End Select
    End Function

    Public Function SetColor(ByVal Message As String, ByVal Red As Byte, ByVal Green As Byte, ByVal Blue As Byte) As String
        SetColor = "|cFF"
        If Red < 16 Then
            SetColor = SetColor & "0" & Hex(Red)
        Else
            SetColor = SetColor & Hex(Red)
        End If
        If Green < 16 Then
            SetColor = SetColor & "0" & Hex(Green)
        Else
            SetColor = SetColor & Hex(Green)
        End If
        If Blue < 16 Then
            SetColor = SetColor & "0" & Hex(Blue)
        Else
            SetColor = SetColor & Hex(Blue)
        End If
        SetColor = SetColor & Message & "|r"

        'SetColor = String.Format("|cff{0:x}{1:x}{2:x}{3}|r", Red, Green, Blue, Message)
    End Function

    Public Function RollChance(ByVal Chance As Single) As Boolean
        Dim nChance As Integer = Chance * 100
        If Rnd.Next(1, 10001) <= nChance Then Return True
        Return False
    End Function

#End Region
#Region "Packets"

    Public Sub SendMessageMOTD(ByRef Client As ClientClass, ByVal Message As String)
        Dim packet As New PacketClass(OPCODES.SMSG_MOTD)
        packet.AddInt32(1)                  'Number of Lines (we use only 1)
        packet.AddString(Message)           'Line 1
        Client.Send(packet)
    End Sub
    Public Sub SendMessageNotification(ByRef Client As ClientClass, ByVal Message As String)
        Dim packet As New PacketClass(OPCODES.SMSG_NOTIFICATION)
        packet.AddString(Message)
        Client.Send(packet)
        packet.Dispose()
    End Sub
    Public Sub SendMessageSystem(ByVal c As ClientClass, ByVal Message As String)
        Dim packet As PacketClass = BuildChatMessage(0, Message, ChatMsg.CHAT_MSG_SYSTEM, LANGUAGES.LANG_UNIVERSAL, 0, "")
        c.Send(packet)
        packet.Dispose()
    End Sub

    Public Sub SendAccountMD5(ByRef Client As ClientClass, ByRef Character As CharacterObject)
        Dim FoundData As Boolean = False
        Dim AccData As New DataTable
        Database.Query(String.Format("SELECT account_id FROM accounts WHERE account = ""{0}"";", Client.Account), AccData)
        If AccData.Rows.Count > 0 Then
            Dim AccID As Integer = CType(AccData.Rows(0).Item("account_id"), Integer)

            AccData.Clear()
            Database.Query(String.Format("SELECT * FROM account_data WHERE account_id = {0}", AccID), AccData)
            If AccData.Rows.Count > 0 Then
                FoundData = True
            Else
                Database.Update(String.Format("INSERT INTO account_data VALUES({0}, 0, '', 0, '', 0, '', 0, '', 0, '', 0, '', 0, '', 0, '')", AccID))
            End If
        End If

        Dim SMSG_ACCOUNT_DATA_TIMES As New PacketClass(OPCODES.SMSG_ACCOUNT_DATA_TIMES)
        SMSG_ACCOUNT_DATA_TIMES.AddUInt32(GetTimestamp(Now)) 'unix time now
        SMSG_ACCOUNT_DATA_TIMES.AddInt8(1) 'Unk (1)

        For i As Integer = 0 To 7
            If FoundData Then
                SMSG_ACCOUNT_DATA_TIMES.AddUInt32(CType(AccData.Rows(0).Item("account_time" & i), UInteger))
            Else
                SMSG_ACCOUNT_DATA_TIMES.AddUInt32(0)
            End If
        Next

        Client.Send(SMSG_ACCOUNT_DATA_TIMES)
        SMSG_ACCOUNT_DATA_TIMES.Dispose()

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] SMSG_ACCOUNT_DATA_TIMES", Client.IP, Client.Port)
    End Sub
    Public Sub SendTrigerCinematic(ByRef Client As ClientClass, ByRef Character As CharacterObject)
        Dim packet As New PacketClass(OPCODES.SMSG_TRIGGER_CINEMATIC)
        If CharClasses.ContainsKey(Character.Classe) AndAlso CharClasses(Character.Classe).CinematicID Then
            packet.AddInt32(CharClasses(Character.Classe).CinematicID)
        Else
            If CharRaces.ContainsKey(Character.Race) Then
                packet.AddInt32(CharRaces(Character.Race).CinematicID)
            Else
                Log.WriteLine(LogType.WARNING, "[{0}:{1}] SMSG_TRIGGER_CINEMATIC [Error: RACE={2} CLASS={3}]", Client.IP, Client.Port, Character.Race, Character.Classe)
                Exit Sub
            End If
        End If

        Client.Send(packet)
        packet.Dispose()
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] SMSG_TRIGGER_CINEMATIC", Client.IP, Client.Port)
    End Sub
    Public Sub SendTimeSyncReq(ByRef Client As ClientClass)
        Dim packet As New PacketClass(OPCODES.SMSG_TIME_SYNC_REQ)
        packet.AddInt32(0)
        Client.Send(packet)
    End Sub
    Public Sub SendGameTime(ByRef Client As ClientClass, ByRef Character As CharacterObject)
        Dim SMSG_LOGIN_SETTIMESPEED As New PacketClass(OPCODES.SMSG_LOGIN_SETTIMESPEED)

        Dim time As DateTime = DateTime.Now
        Dim Year As Integer = time.Year - 2000
        Dim Month As Integer = time.Month - 1
        Dim Day As Integer = time.Day - 1
        Dim DayOfWeek As Integer = CType(time.DayOfWeek, Integer)
        Dim Hour As Integer = time.Hour
        Dim Minute As Integer = time.Minute

        'SMSG_LOGIN_SETTIMESPEED.AddInt32(CType((((((Minute + (Hour << 6)) + (DayOfWeek << 11)) + (Day << 14)) + (Year << 18)) + (Month << 20)), Integer))
        SMSG_LOGIN_SETTIMESPEED.AddInt32(CType((((((Minute + (Hour << 6)) + (DayOfWeek << 11)) + (Day << 14)) + (Month << 20)) + (Year << 24)), Integer))
        SMSG_LOGIN_SETTIMESPEED.AddSingle(0.01666667F)

        Client.Send(SMSG_LOGIN_SETTIMESPEED)
        SMSG_LOGIN_SETTIMESPEED.Dispose()

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] SMSG_LOGIN_SETTIMESPEED", Client.IP, Client.Port)
    End Sub
    Public Sub SendProficiency(ByRef Client As ClientClass, ByVal ProficiencyType As Byte, ByVal ProficiencyFlags As Integer)
        Dim packet As New PacketClass(OPCODES.SMSG_SET_PROFICIENCY)
        packet.AddInt8(ProficiencyType)
        packet.AddInt32(ProficiencyFlags)

        Client.Send(packet)
        packet.Dispose()
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] SMSG_SET_PROFICIENCY", Client.IP, Client.Port)
    End Sub
    Public Sub SendFlatSpellMod(ByRef Client As ClientClass, ByRef Character As CharacterObject)
        Dim packet As New PacketClass(OPCODES.SMSG_SET_FLAT_SPELL_MODIFIER)
        packet.AddByteArray(New Byte() {6, 10, 56, 255, 255, 255})
        Client.Send(packet)
        packet.Dispose()
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] SMSG_SET_FLAT_SPELL_MODIFIER", Client.IP, Client.Port)
    End Sub
    Public Sub SendCorpseReclaimDelay(ByRef Client As ClientClass, ByRef Character As CharacterObject, Optional ByVal Seconds As Integer = 30)
        Dim packet As New PacketClass(OPCODES.SMSG_CORPSE_RECLAIM_DELAY)
        packet.AddInt32(Seconds * 1000)
        Client.Send(packet)
        packet.Dispose()
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] SMSG_CORPSE_RECLAIM_DELAY [{2}s]", Client.IP, Client.Port, Seconds)
    End Sub

    Public Function BuildChatMessage(ByVal SenderGUID As ULong, ByVal Message As String, ByVal msgType As ChatMsg, ByVal msgLanguage As LANGUAGES, Optional ByVal Flag As Byte = 0, Optional ByVal msgChannel As String = "Global") As PacketClass
        Dim packet As New PacketClass(OPCODES.SMSG_MESSAGECHAT)

        packet.AddInt8(msgType)
        packet.AddUInt32(msgLanguage)

        Select Case msgType
            Case ChatMsg.CHAT_MSG_CHANNEL
                packet.AddUInt64(SenderGUID)
                packet.AddUInt32(0)
                packet.AddString(msgChannel)
                packet.AddUInt64(SenderGUID)
            Case ChatMsg.CHAT_MSG_SYSTEM, ChatMsg.CHAT_MSG_YELL, ChatMsg.CHAT_MSG_SAY, ChatMsg.CHAT_MSG_PARTY, ChatMsg.CHAT_MSG_EMOTE, ChatMsg.CHAT_MSG_IGNORED, ChatMsg.CHAT_MSG_SKILL, ChatMsg.CHAT_MSG_GUILD, ChatMsg.CHAT_MSG_OFFICER, ChatMsg.CHAT_MSG_RAID, ChatMsg.CHAT_MSG_RAID_LEADER, ChatMsg.CHAT_MSG_RAID_WARNING, ChatMsg.CHAT_MSG_WHISPER_INFORM, ChatMsg.CHAT_MSG_GUILD, ChatMsg.CHAT_MSG_WHISPER, ChatMsg.CHAT_MSG_REPLY, ChatMsg.CHAT_MSG_AFK, ChatMsg.CHAT_MSG_DND
                packet.AddUInt64(SenderGUID)
                packet.AddUInt32(msgLanguage)
                packet.AddUInt64(SenderGUID)
            Case ChatMsg.CHAT_MSG_MONSTER_SAY, ChatMsg.CHAT_MSG_MONSTER_EMOTE, ChatMsg.CHAT_MSG_MONSTER_YELL, ChatMsg.CHAT_MSG_MONSTER_WHISPER, ChatMsg.CHAT_MSG_MONSTER_PARTY, ChatMsg.CHAT_MSG_RAID_BOSS_WHISPER, ChatMsg.CHAT_MSG_RAID_BOSS_EMOTE
                Log.WriteLine(LogType.WARNING, "Use Creature.SendChatMessage() for this message type - {0}!", msgType)
            Case Else
                Log.WriteLine(LogType.WARNING, "Unknown chat message type - {0}!", msgType)
        End Select

        packet.AddUInt32(System.Text.Encoding.UTF8.GetByteCount(Message) + 1)
        packet.AddString(Message)

        packet.AddInt8(Flag)

        Return packet
    End Function

    Public Enum PartyMemberStatsStatus As Byte
        STATUS_OFFLINE = 0
        STATUS_ONLINE = 1
        STATUS_OFFLINE_PVP = 2
        STATUS_ONLINE_PVP = 3
    End Enum
    Public Enum PartyMemberStatsBits As Byte
        FIELD_STATUS = 0
        FIELD_LIFE_CURRENT = 1
        FIElD_LIFE_MAX = 2
        FIELD_MANA_TYPE = 3
        FIELD_MANA_CURRENT = 4
        FIELD_MANA_MAX = 5
        FIELD_LEVEL = 6
        FIELD_ZONEID = 7
        FIELD_POSXPOSY = 8
    End Enum
    Public Enum PartyMemberStatsFlag As Integer
        GROUP_UPDATE_FLAG_NONE = &H0 'nothing
        GROUP_UPDATE_FLAG_STATUS = &H1 'uint16, flags
        GROUP_UPDATE_FLAG_CUR_HP = &H2 'uint16
        GROUP_UPDATE_FLAG_MAX_HP = &H4 'uint16
        GROUP_UPDATE_FLAG_POWER_TYPE = &H8 'uint8
        GROUP_UPDATE_FLAG_CUR_POWER = &H10 'uint16
        GROUP_UPDATE_FLAG_MAX_POWER = &H20 'uint16
        GROUP_UPDATE_FLAG_LEVEL = &H40 'uint16
        GROUP_UPDATE_FLAG_ZONE = &H80 'uint16
        GROUP_UPDATE_FLAG_POSITION = &H100 'uint16, uint16
        GROUP_UPDATE_FLAG_AURAS = &H200 'uint64 mask, for each bit set uint16 spellid + uint8 unk
        GROUP_UPDATE_FLAG_PET_GUID = &H400 'uint64 pet guid
        GROUP_UPDATE_FLAG_PET_NAME = &H800 'pet name, NULL terminated string
        GROUP_UPDATE_FLAG_PET_MODEL_ID = &H1000 'uint16, model id
        GROUP_UPDATE_FLAG_PET_CUR_HP = &H2000 'uint16 pet cur health
        GROUP_UPDATE_FLAG_PET_MAX_HP = &H4000 'uint16 pet max health
        GROUP_UPDATE_FLAG_PET_POWER_TYPE = &H8000 'uint8 pet power type
        GROUP_UPDATE_FLAG_PET_CUR_POWER = &H10000 'uint16 pet cur power
        GROUP_UPDATE_FLAG_PET_MAX_POWER = &H20000 'uint16 pet max power
        GROUP_UPDATE_FLAG_PET_AURAS = &H40000 'uint64 mask, for each bit set uint16 spellid + uint8 unk, pet auras...

        GROUP_UPDATE_PET = &H7FC00 'all pet flags
        GROUP_UPDATE_FULL = &H40BFF
        GROUP_UPDATE_FULL_PET = &H7FFFF 'all known flags
        GROUP_UPDATE_FULL_REQUEST_REPLY = &H7FFC0BFF
    End Enum
    Function BuildPartyMemberStatsOffline(ByVal GUID As ULong) As PacketClass
        Dim packet As New PacketClass(OPCODES.SMSG_PARTY_MEMBER_STATS_FULL)
        packet.AddPackGUID(GUID)
        packet.AddUInt32(PartyMemberStatsFlag.GROUP_UPDATE_FLAG_STATUS)
        packet.AddUInt16(PartyMemberStatsStatus.STATUS_OFFLINE)
        Return packet
    End Function

#End Region

End Module