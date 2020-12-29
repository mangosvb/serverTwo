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
Imports System.Collections.Generic
Imports mangosVB.Common.BaseWriter

Public Module WS_Group

    Public GROUPs As New Dictionary(Of Long, Group)
    Public Class Group
        Implements IDisposable

        Public ID As Long
        Public Type As GroupType = GroupType.PARTY
        Public DungeonDifficulty As GroupDungeonDifficulty = GroupDungeonDifficulty.DIFFICULTY_NORMAL
        Public LootMethod As GroupLootMethod = GroupLootMethod.LOOT_GROUP
        Public LootThreshold As GroupLootThreshold = GroupLootThreshold.Uncommon
        Public Leader As ULong

        Public LocalMembers As List(Of ULong)
        Public LocalLootMaster As CharacterObject

        Public Sub New(ByVal GroupID As Long)
            ID = GroupID
            GROUPs.Add(ID, Me)
        End Sub
        Public Sub Dispose() Implements IDisposable.Dispose
            GROUPs.Remove(ID)
        End Sub

        Public Sub Broadcast(ByVal p As PacketClass)
            p.UpdateLength()
            WS.Cluster.BroadcastGroup(ID, p.Data)
        End Sub

        Private LastLooter As ULong = 0
        Public Function GetNextLooter() As CharacterObject
            Dim nextIsLooter As Boolean = False
            Dim nextLooterFound As Boolean = False

            For Each Guid As ULong In LocalMembers
                If nextIsLooter Then
                    LastLooter = Guid
                    nextLooterFound = True
                    Exit For
                End If

                If Guid = LastLooter Then nextIsLooter = True
            Next

            If Not nextLooterFound Then
                LastLooter = LocalMembers.Item(0)
            End If

            Return CHARACTERs(LastLooter)
        End Function

        Public Function GetMembersCount() As Integer
            Return LocalMembers.Count
        End Function

    End Class

    Function BuildPartyMemberStats(ByRef c As CharacterObject, ByVal Flag As Integer) As PacketClass
        Dim OpCode As OPCODES = OPCODES.SMSG_PARTY_MEMBER_STATS
        If Flag = PartyMemberStatsFlag.GROUP_UPDATE_FULL OrElse Flag = PartyMemberStatsFlag.GROUP_UPDATE_FULL_PET Then
            OpCode = OPCODES.SMSG_PARTY_MEMBER_STATS_FULL
        End If
        Dim packet As New PacketClass(OpCode)
        If (Flag And PartyMemberStatsFlag.GROUP_UPDATE_FULL_REQUEST_REPLY) = PartyMemberStatsFlag.GROUP_UPDATE_FULL_REQUEST_REPLY Then
            packet.AddInt8(0)
        End If

        packet.AddPackGUID(c.GUID)
        packet.AddInt32(Flag)

        If (Flag And PartyMemberStatsFlag.GROUP_UPDATE_FLAG_STATUS) Then
            If c.isPvP Then
                packet.AddUInt16(PartyMemberStatsStatus.STATUS_ONLINE_PVP)
            Else
                packet.AddUInt16(PartyMemberStatsStatus.STATUS_ONLINE)
            End If
        End If
        If (Flag And PartyMemberStatsFlag.GROUP_UPDATE_FLAG_CUR_HP) Then packet.AddUInt32(c.Life.Current)
        If (Flag And PartyMemberStatsFlag.GROUP_UPDATE_FLAG_MAX_HP) Then packet.AddUInt32(c.Life.Maximum)
        If (Flag And PartyMemberStatsFlag.GROUP_UPDATE_FLAG_POWER_TYPE) Then packet.AddInt8(c.ManaType)
        If (Flag And PartyMemberStatsFlag.GROUP_UPDATE_FLAG_CUR_POWER) Then
            If c.ManaType = ManaTypes.TYPE_RAGE Then
                packet.AddUInt16(c.Rage.Current)
            ElseIf c.ManaType = ManaTypes.TYPE_ENERGY Then
                packet.AddUInt16(c.Energy.Current)
            Else
                packet.AddUInt16(c.Mana.Current)
            End If
        End If
        If (Flag And PartyMemberStatsFlag.GROUP_UPDATE_FLAG_MAX_POWER) Then
            If c.ManaType = ManaTypes.TYPE_RAGE Then
                packet.AddUInt16(c.Rage.Maximum)
            ElseIf c.ManaType = ManaTypes.TYPE_ENERGY Then
                packet.AddUInt16(c.Energy.Maximum)
            Else
                packet.AddUInt16(c.Mana.Maximum)
            End If
        End If
        If (Flag And PartyMemberStatsFlag.GROUP_UPDATE_FLAG_LEVEL) Then packet.AddUInt16(c.Level)
        If (Flag And PartyMemberStatsFlag.GROUP_UPDATE_FLAG_ZONE) Then packet.AddUInt16(c.ZoneID)
        If (Flag And PartyMemberStatsFlag.GROUP_UPDATE_FLAG_POSITION) Then
            packet.AddInt16(Fix(c.positionX))
            packet.AddInt16(Fix(c.positionY))
        End If
        If (Flag And PartyMemberStatsFlag.GROUP_UPDATE_FLAG_AURAS) Then
            Dim AuraMask As ULong = 0
            Dim AuraPos As Integer = packet.Data.Length
            packet.AddUInt64(0) 'AuraMask (is set after the loop)
            For i As Integer = 0 To MAX_AURA_EFFECTs_VISIBLE - 1
                If Not c.ActiveSpells(i) Is Nothing Then
                    AuraMask = AuraMask Or (CULng(1) << CULng(i))
                    packet.AddUInt16(c.ActiveSpells(i).SpellID)
                    packet.AddInt8(1) 'Stack Count?
                End If
            Next
            packet.AddUInt64(AuraMask, AuraPos) 'Reset the AuraMask
        End If
        If (Flag And PartyMemberStatsFlag.GROUP_UPDATE_FLAG_PET_GUID) Then
            packet.AddUInt64(0)
        End If
        If (Flag And PartyMemberStatsFlag.GROUP_UPDATE_FLAG_PET_NAME) Then
            packet.AddInt8(0)
        End If
        If (Flag And PartyMemberStatsFlag.GROUP_UPDATE_FLAG_PET_MODEL_ID) Then
            packet.AddUInt16(0)
        End If
        If (Flag And PartyMemberStatsFlag.GROUP_UPDATE_FLAG_PET_CUR_HP) Then
            packet.AddUInt16(0)
        End If
        If (Flag And PartyMemberStatsFlag.GROUP_UPDATE_FLAG_PET_MAX_HP) Then
            packet.AddUInt16(0)
        End If
        If (Flag And PartyMemberStatsFlag.GROUP_UPDATE_FLAG_PET_POWER_TYPE) Then
            packet.AddInt8(0)
        End If
        If (Flag And PartyMemberStatsFlag.GROUP_UPDATE_FLAG_PET_CUR_POWER) Then
            packet.AddUInt16(0)
        End If
        If (Flag And PartyMemberStatsFlag.GROUP_UPDATE_FLAG_PET_MAX_POWER) Then
            packet.AddUInt16(0)
        End If
        If (Flag And PartyMemberStatsFlag.GROUP_UPDATE_FLAG_PET_AURAS) Then
            packet.AddUInt64(0) 'AuraMask
        End If

        Return packet
    End Function

End Module