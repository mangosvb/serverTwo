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
Imports mangosVB.Common

Public Module WS_Pets

#Region "WS.Pets.Framework"

    Public Const CREATURE_MAX_SPELLS As Integer = 4
    Public Const MAX_OWNER_DIS As Integer = 100

    Public LevelUpLoyalty(6) As Integer
    Public LevelStartLoyalty(6) As Integer

    Public Sub InitializeLevelUpLoyalty()
        WS_Pets.LevelUpLoyalty(0) = 0
        WS_Pets.LevelUpLoyalty(1) = 5500
        WS_Pets.LevelUpLoyalty(2) = 11500
        WS_Pets.LevelUpLoyalty(3) = 17000
        WS_Pets.LevelUpLoyalty(4) = 23500
        WS_Pets.LevelUpLoyalty(5) = 31000
        WS_Pets.LevelUpLoyalty(6) = 39500
    End Sub

    Public Sub InitializeLevelStartLoyalty()
        WS_Pets.LevelStartLoyalty(0) = 0
        WS_Pets.LevelStartLoyalty(1) = 2000
        WS_Pets.LevelStartLoyalty(2) = 4500
        WS_Pets.LevelStartLoyalty(3) = 7000
        WS_Pets.LevelStartLoyalty(4) = 10000
        WS_Pets.LevelStartLoyalty(5) = 13500
        WS_Pets.LevelStartLoyalty(6) = 17500
    End Sub

    Public Enum PetType As Byte
        SUMMON_PET = 0
        HUNTER_PET = 1
        GUARDIAN_PET = 2
        MINI_PET = 3
    End Enum

    Public Enum PetSaveType As Integer
        PET_SAVE_DELETED = -1
        PET_SAVE_CURRENT = 0
        PET_SAVE_IN_STABLE_1 = 1
        PET_SAVE_IN_STABLE_2 = 2
        PET_SAVE_NO_SLOT = 3
    End Enum

    Public Enum HappinessState As Byte
        UNHAPPY = 1
        CONTENT = 2
        HAPPY = 3
    End Enum

    Public Enum LoyaltyState As Byte
        REBELLIOUS = 1
        UNRULY = 2
        SUBMISSIVE = 3
        DEPENDABLE = 4
        FAITHFUL = 5
        BEST_FRIEND = 6
    End Enum

    Public Enum PetSpellState As Byte
        SPELL_UNCHANGED = 0
        SPELL_CHANGED = 1
        SPELL_NEW = 2
        SPELL_REMOVED = 3
    End Enum

    Public Enum ActionFeedback As Byte
        FEEDBACK_NONE = 0
        FEEDBACK_PET_DEAD = 1
        FEEDBACK_NO_TARGET = 2
        FEEDBACK_CANT_ATT = 3
    End Enum

    Public Enum PetTalk As Byte
        PET_TALK_SPECIAL_SPELL = 0
        PET_TALK_ATTACK = 1
    End Enum

#End Region
#Region "WS.Pets.TypeDef"

    Public Class PetObject
        Inherits CreatureObject

        Public Command As Byte = 7
        Public State As Byte = 6
        Public Spells As ArrayList

        Public Sub New(ByVal CreatureID As Integer)
            MyBase.New(CreatureID)
            Dim MySQLQuery As New DataTable

        End Sub

    End Class

#End Region
#Region "WS.Pets.Handlers"

    Public Sub on_CMSG_PET_NAME_QUERY(ByRef packet As PacketClass, ByRef Client As ClientClass)
        packet.GetInt16()
        Dim PetNumber As Integer = packet.GetInt32()
        Dim PetGUID As ULong = packet.GetInt64()

        Dim response As New PacketClass(OPCODES.SMSG_PET_NAME_QUERY_RESPONSE)
        response.AddInt32(PetNumber)
        response.AddString("Pet")
        response.AddInt32(EUnitFields.UNIT_FIELD_PET_NAME_TIMESTAMP)
        Client.Send(response)

        response.Dispose()

    End Sub

    Public Sub HandlePetAction(ByRef packet As PacketClass, ByRef Client As ClientClass)

    End Sub

#End Region
#Region "WS.Pets.AI"

#End Region

End Module