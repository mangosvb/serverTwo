' 
' Copyright (C) 2008-2010 Spurious <http://SpuriousEmu.com>
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

Imports Spurious.Common.BaseWriter


Public Module WS_Quests
#Region "WS.Quests.Enums"
    Const QUEST_OBJECTIVES_COUNT As Integer = 3
    Const QUEST_REWARD_CHOICES_COUNT As Integer = 5
    Const QUEST_REWARDS_COUNT As Integer = 3
    Const QUEST_DEPLINK_COUNT As Integer = 9

    Public Const QUEST_SLOTS As Integer = 24

    '-QUEST TYPE-
    '81 = Burn ?

    Public Enum QuestgiverStatus
        DIALOG_STATUS_NONE = 0                  ' There aren't any quests available. - No Mark
        DIALOG_STATUS_UNAVAILABLE = 1           ' Quest available and your leve isn't enough. - Gray Quotation ! Mark
        DIALOG_STATUS_CHAT = 2                  ' Quest available it shows a talk baloon. - No Mark
        DIALOG_STATUS_INCOMPLETE = 3            ' Quest isn't finished yet. - Gray Question ? Mark
        DIALOG_STATUS_REPEATABLE_FINISHED = 4
        DIALOG_STATUS_REPEATABLE = 5            ' Quest repeatable. - Blue Question ? Mark
        DIALOG_STATUS_AVAILABLE = 6             ' Quest available, and your level is enough. - Yellow Quotation ! Mark
        DIALOG_STATUS_REWARD2 = 7                ' Quest has been finished. - Yellow Question ? Mark (No yellow dot on the minimap?)
        DIALOG_STATUS_REWARD = 8                ' Quest has been finished. - Yellow Question ? Mark
    End Enum
    Public Enum QuestObjectiveFlag 'These flags are custom and are only used for Spurious
        QUEST_OBJECTIVE_KILL = 1 'You have to kill creatures
        QUEST_OBJECTIVE_EXPLORE = 2 'You have to explore an area
        QUEST_OBJECTIVE_ESCORT = 4 'You have to escort someone
        QUEST_OBJECTIVE_EVENT = 8 'Something is required to happen (escort may be included in this one)
        QUEST_OBJECTIVE_CAST = 16 'You will have to cast a spell on a creature or a gameobject (spells on gameobjects are f.ex opening)
        QUEST_OBJECTIVE_ITEM = 32 'You have to recieve some items to deliver
    End Enum
    Public Enum QuestFlag As Integer
        QUEST_FLAGS_STAY_ALIVE = 1 'Needs to stay alive or else it fails
        QUEST_FLAGS_EVENT = 2 'Something must happen
        QUEST_FLAGS_EXPLORATION = 4 'Explore an area
        QUEST_FLAGS_SHARABLE = 8 'Can be shared
        QUEST_FLAGS_EPIC = 32 'Unsure of content
        QUEST_FLAGS_RAID = 64 'Raid quest
        QUEST_FLAGS_TBC = 128 'Available if TBC expension enabled only
        QUEST_FLAGS_UNK2 = 256 '_DELIVER_MORE Quest needs more than normal _q-item_ drops from mobs
        QUEST_FLAGS_HIDDEN_REWARDS = 512 'Items and money rewarded only sent in SMSG_QUESTGIVER_OFFER_REWARD (not in SMSG_QUESTGIVER_QUEST_DETAILS or in client quest log(SMSG_QUEST_QUERY_RESPONSE))
        QUEST_FLAGS_UNK4 = 1024 'Unknown tbc flag
        QUEST_FLAGS_TBC_RACES = 2048 'Bloodelf/draenei starting zone quests
        QUEST_FLAGS_DAILY = 4096 'Daily quest
    End Enum
    Public Enum QuestSpecialFlag As Integer
        QUEST_SPECIALFLAGS_REPEATABLE = 1 'Quest can be repeated
        QUEST_SPECIALFLAGS_EXPLORATION_OR_EVENT = 2 'if required area explore, spell SPELL_EFFECT_QUEST_COMPLETE casting
    End Enum

    'Used in the queststarters and questfinishers tables to see what type of object it is
    Public Enum QuestGiverType As Byte
        QUEST_OBJECTTYPE_CREATURE = 1
        QUEST_OBJECTTYPE_GAMEOBJECT = 2
        QUEST_OBJECTTYPE_ITEM = 3
    End Enum

    Public Enum QuestInvalidError
        'SMSG_QUESTGIVER_QUEST_INVALID
        '   uint32 invalidReason

        INVALIDREASON_DONT_HAVE_REQ = 0                     'You don't meet the requirements for that quest
        INVALIDREASON_DONT_HAVE_LEVEL = 1                   'You are not high enough level for that quest.
        INVALIDREASON_DONT_HAVE_RACE = 6                    'That quest is not available to your race
        INVALIDREASON_COMPLETED_QUEST = 7                   'You have already completed this quest
        INVALIDREASON_HAVE_TIMED_QUEST = 12                 'You can only be on one timed quest at a time
        INVALIDREASON_HAVE_QUEST = 13                       'You are already on that quest
        INVALIDREASON_DONT_HAVE_EXP_ACCOUNT = 16            '??????
        INVALIDREASON_DONT_HAVE_REQ_ITEMS = 21  'Changed for 2.1.3  'You don't have the required items with you. Check storage.
        INVALIDREASON_DONT_HAVE_REQ_MONEY = 23              'You don't have enough money for that quest
        INVALIDREASON_REACHED_DAILY_LIMIT = 26              'You have completed xx daily quests today
        INVALIDREASON_UNKNOW27 = 27                         'You can not complete quests once you have reached tired time ???
    End Enum
    Public Enum QuestFailedReason
        'SMSG_QUESTGIVER_QUEST_FAILED
        '		uint32 questID
        '		uint32 failedReason

        FAILED_INVENTORY_FULL = 4       '0x04: '%s failed: Inventory is full.'
        FAILED_DUPE_ITEM = &H11         '0x11: '%s failed: Duplicate item found.'
        FAILED_INVENTORY_FULL2 = &H31   '0x31: '%s failed: Inventory is full.'
        FAILED_NOREASON = 0       '0x00: '%s failed.'
    End Enum
    Public Enum QuestPartyPushError As Byte
        QUEST_PARTY_MSG_SHARRING_QUEST = 0
        QUEST_PARTY_MSG_CANT_TAKE_QUEST = 1
        QUEST_PARTY_MSG_ACCEPT_QUEST = 2
        QUEST_PARTY_MSG_REFUSE_QUEST = 3
        QUEST_PARTY_MSG_TO_FAR = 4
        QUEST_PARTY_MSG_BUSY = 5
        QUEST_PARTY_MSG_LOG_FULL = 6
        QUEST_PARTY_MSG_HAVE_QUEST = 7
        QUEST_PARTY_MSG_FINISH_QUEST = 8
    End Enum
#End Region


#Region "Quests.DataTypes"

    'WARNING: These are used only for Quests packets
    Public Class QuestInfo
        Public ID As Integer
        Public NextQuest As Integer = 0
        Public Type As Integer
        Public Zone As Integer
        Public Flags As Integer = 0
        Public RewTitleId As Integer = 0
        Public rewtalents As Integer = 0
        Public SpecialFlags As Byte = 0
        Public Level_Start As Byte = 0
        Public Level_Normal As Short = 0

        Public Title As String
        Public TextObjectives As String
        Public TextDescription As String
        Public TextEnd As String
        Public TextIncomplete As String
        Public TextComplete As String

        Public RequiredRace As Integer
        Public RequiredClass As Integer
        Public RequiredTradeSkill As Integer
        Public RequiredTradeSkillValue As Integer
        Public RequiredReputation(1) As Integer
        Public RequiredReputation_Faction(1) As Integer

        Public RewardXP As Integer = 0
        Public RewardHonor As Integer = 0
        Public RewardGold As Integer = 0
        Public RewardSpell As Integer = 0
        Public RewardSpellCast As Integer = 0
        Public RewardItems(QUEST_REWARD_CHOICES_COUNT) As Integer
        Public RewardItems_Count(QUEST_REWARD_CHOICES_COUNT) As Integer
        Public RewardStaticItems(QUEST_REWARDS_COUNT) As Integer
        Public RewardStaticItems_Count(QUEST_REWARDS_COUNT) As Integer

        'Explore <place_name>
        Public ObjectivesTrigger(3) As Integer
        'Cast <x> spell on <mob_name> / <object_name>
        Public ObjectivesCastSpell(3) As Integer
        'Kill <x> of <mob_name>
        Public ObjectivesKill(3) As Integer
        Public ObjectivesKill_Count(3) As Integer
        'Gather <x> of <item_name>
        Public ObjectivesItem(3) As Integer
        Public ObjectivesItem_Count(3) As Integer
        'Deliver <item_name>
        Public ObjectivesDeliver As Integer
        Public ObjectivesDeliver_Count As Integer

        Public ObjectivesGold As Integer = 0
        Public ObjectivesText(3) As String

        Public TimeLimit As Integer = 0
        Public SuggestedPlayers As Byte = 0
        Public MoneyAtMaxLevel As Integer = 0

        Public PointMapID As Integer = 0
        Public PointX As Single = 0
        Public PointY As Single = 0
        Public PointOpt As Integer = 0

        Public IsActive As Byte = 2

        Public Sub New(ByVal QuestID As Integer)
            ID = QuestID

            Dim MySQLQuery As New DataTable
            'Database.Query(String.Format("SELECT * FROM quests WHERE id = {0};", QuestID), MySQLQuery)
            WorldDatabase.Query(String.Format("SELECT * FROM quests WHERE id = {0};", QuestID), MySQLQuery)

            If MySQLQuery.Rows.Count = 0 Then Throw New ApplicationException("Quest " & QuestID & " not found in database.")

            NextQuest = MySQLQuery.Rows(0).Item("NextQuest")
            Level_Start = MySQLQuery.Rows(0).Item("Level_Start")
            Level_Normal = MySQLQuery.Rows(0).Item("Level_Normal")
            Type = MySQLQuery.Rows(0).Item("Type")
            Zone = MySQLQuery.Rows(0).Item("Zone")
            Flags = MySQLQuery.Rows(0).Item("Flags")
            RewTitleId = MySQLQuery.Rows(0).Item("RewTitleId")
            rewtalents = MySQLQuery.Rows(0).Item("rewtalents")
            SpecialFlags = MySQLQuery.Rows(0).Item("SpecialFlags")

            RequiredRace = MySQLQuery.Rows(0).Item("Required_Race")
            RequiredClass = MySQLQuery.Rows(0).Item("Required_Class")
            RequiredTradeSkill = MySQLQuery.Rows(0).Item("Required_TradeSkill")
            RequiredTradeSkillValue = MySQLQuery.Rows(0).Item("Required_TradeSkillValue")
            RequiredReputation(0) = MySQLQuery.Rows(0).Item("Required_Reputation1")
            RequiredReputation(1) = MySQLQuery.Rows(0).Item("Required_Reputation2")
            RequiredReputation_Faction(0) = MySQLQuery.Rows(0).Item("Required_Reputation1_Faction")
            RequiredReputation_Faction(1) = MySQLQuery.Rows(0).Item("Required_Reputation2_Faction")

            Title = MySQLQuery.Rows(0).Item("Title")
            TextObjectives = MySQLQuery.Rows(0).Item("Text_Objectives")
            TextDescription = MySQLQuery.Rows(0).Item("Text_Description")
            TextEnd = MySQLQuery.Rows(0).Item("Text_End")
            TextIncomplete = MySQLQuery.Rows(0).Item("Text_Incomplete")
            TextComplete = MySQLQuery.Rows(0).Item("Text_Complete")

            'TODO: RewardHonor from MySQL?
            RewardXP = MySQLQuery.Rows(0).Item("Reward_XP")
            RewardGold = MySQLQuery.Rows(0).Item("Reward_Gold")
            RewardSpell = MySQLQuery.Rows(0).Item("Reward_Spell")
            RewardSpellCast = MySQLQuery.Rows(0).Item("Reward_SpellCast")

            RewardItems(0) = MySQLQuery.Rows(0).Item("Reward_Item1")
            RewardItems(1) = MySQLQuery.Rows(0).Item("Reward_Item2")
            RewardItems(2) = MySQLQuery.Rows(0).Item("Reward_Item3")
            RewardItems(3) = MySQLQuery.Rows(0).Item("Reward_Item4")
            RewardItems(4) = MySQLQuery.Rows(0).Item("Reward_Item5")
            RewardItems(5) = MySQLQuery.Rows(0).Item("Reward_Item6")
            RewardItems_Count(0) = MySQLQuery.Rows(0).Item("Reward_Item1_Count")
            RewardItems_Count(1) = MySQLQuery.Rows(0).Item("Reward_Item2_Count")
            RewardItems_Count(2) = MySQLQuery.Rows(0).Item("Reward_Item3_Count")
            RewardItems_Count(3) = MySQLQuery.Rows(0).Item("Reward_Item4_Count")
            RewardItems_Count(4) = MySQLQuery.Rows(0).Item("Reward_Item5_Count")
            RewardItems_Count(5) = MySQLQuery.Rows(0).Item("Reward_Item6_Count")

            RewardStaticItems(0) = MySQLQuery.Rows(0).Item("Reward_StaticItem1")
            RewardStaticItems(1) = MySQLQuery.Rows(0).Item("Reward_StaticItem2")
            RewardStaticItems(2) = MySQLQuery.Rows(0).Item("Reward_StaticItem3")
            RewardStaticItems(3) = MySQLQuery.Rows(0).Item("Reward_StaticItem4")
            RewardStaticItems_Count(0) = MySQLQuery.Rows(0).Item("Reward_StaticItem1_Count")
            RewardStaticItems_Count(1) = MySQLQuery.Rows(0).Item("Reward_StaticItem2_Count")
            RewardStaticItems_Count(2) = MySQLQuery.Rows(0).Item("Reward_StaticItem3_Count")
            RewardStaticItems_Count(3) = MySQLQuery.Rows(0).Item("Reward_StaticItem4_Count")

            ObjectivesTrigger(0) = MySQLQuery.Rows(0).Item("Objective_Trigger1")
            ObjectivesTrigger(1) = MySQLQuery.Rows(0).Item("Objective_Trigger2")
            ObjectivesTrigger(2) = MySQLQuery.Rows(0).Item("Objective_Trigger3")
            ObjectivesTrigger(3) = MySQLQuery.Rows(0).Item("Objective_Trigger4")

            ObjectivesCastSpell(0) = MySQLQuery.Rows(0).Item("Objective_Cast1")
            ObjectivesCastSpell(1) = MySQLQuery.Rows(0).Item("Objective_Cast2")
            ObjectivesCastSpell(2) = MySQLQuery.Rows(0).Item("Objective_Cast3")
            ObjectivesCastSpell(3) = MySQLQuery.Rows(0).Item("Objective_Cast4")

            ObjectivesKill(0) = MySQLQuery.Rows(0).Item("Objective_Kill1")
            ObjectivesKill(1) = MySQLQuery.Rows(0).Item("Objective_Kill2")
            ObjectivesKill(2) = MySQLQuery.Rows(0).Item("Objective_Kill3")
            ObjectivesKill(3) = MySQLQuery.Rows(0).Item("Objective_Kill4")
            ObjectivesKill_Count(0) = MySQLQuery.Rows(0).Item("Objective_Kill1_Count")
            ObjectivesKill_Count(1) = MySQLQuery.Rows(0).Item("Objective_Kill2_Count")
            ObjectivesKill_Count(2) = MySQLQuery.Rows(0).Item("Objective_Kill3_Count")
            ObjectivesKill_Count(3) = MySQLQuery.Rows(0).Item("Objective_Kill4_Count")

            ObjectivesItem(0) = MySQLQuery.Rows(0).Item("Objective_Item1")
            ObjectivesItem(1) = MySQLQuery.Rows(0).Item("Objective_Item2")
            ObjectivesItem(2) = MySQLQuery.Rows(0).Item("Objective_Item3")
            ObjectivesItem(3) = MySQLQuery.Rows(0).Item("Objective_Item4")
            ObjectivesItem_Count(0) = MySQLQuery.Rows(0).Item("Objective_Item1_Count")
            ObjectivesItem_Count(1) = MySQLQuery.Rows(0).Item("Objective_Item2_Count")
            ObjectivesItem_Count(2) = MySQLQuery.Rows(0).Item("Objective_Item3_Count")
            ObjectivesItem_Count(3) = MySQLQuery.Rows(0).Item("Objective_Item4_Count")

            ObjectivesDeliver = MySQLQuery.Rows(0).Item("Objective_Deliver1")

            ObjectivesText(0) = MySQLQuery.Rows(0).Item("Objective_Text1")
            ObjectivesText(1) = MySQLQuery.Rows(0).Item("Objective_Text2")
            ObjectivesText(2) = MySQLQuery.Rows(0).Item("Objective_Text3")
            ObjectivesText(3) = MySQLQuery.Rows(0).Item("Objective_Text4")

            TimeLimit = MySQLQuery.Rows(0).Item("Time_Limit")
            SuggestedPlayers = MySQLQuery.Rows(0).Item("SuggestedPlayers")
            MoneyAtMaxLevel = MySQLQuery.Rows(0).Item("MoneyAtMaxLevel")

            PointMapID = MySQLQuery.Rows(0).Item("PointMap")
            PointX = MySQLQuery.Rows(0).Item("PointX")
            PointY = MySQLQuery.Rows(0).Item("PointY")
            PointOpt = MySQLQuery.Rows(0).Item("PointOpt")

            IsActive = MySQLQuery.Rows(0).Item("IsActive")
        End Sub
    End Class



    'WARNING: These are used only for CharManagment
    Public Class BaseQuest
        Public ID As Integer = 0
        Public Title As String = ""
        Public Flags As Integer = 0
        Public RewTitleId As Integer = 0
        Public rewtalents As Integer = 0
        Public SpecialFlags As Byte = 0
        Public ObjectiveFlags As Integer = 0

        Public Slot As Byte = 0

        Public ObjectivesType() As Byte = {0, 0, 0, 0}
        Public ObjectivesDeliver As Integer
        Public ObjectivesExplore(3) As Integer
        Public ObjectivesSpell(3) As Integer
        Public ObjectivesItem(3) As Integer
        Public ObjectivesItemCount() As Byte = {0, 0, 0, 0}
        Public ObjectivesObject(3) As Integer
        Public ObjectivesCount() As Byte = {0, 0, 0, 0}
        Public Explored As Boolean = True
        Public Progress() As Byte = {0, 0, 0, 0}
        Public ProgressItem() As Byte = {0, 0, 0, 0}
        Public Complete As Boolean = False
        Public Failed As Boolean = False

        Public TimeEnd As Integer = 0

        Public Sub New()
            'Nothing? :/
        End Sub

        Public Sub New(ByVal Quest As QuestInfo)
            Dim i As Byte, j As Byte

            'Load Spell Casts
            For i = 0 To 3
                If Quest.ObjectivesCastSpell(i) > 0 Then
                    ObjectiveFlags = ObjectiveFlags Or QuestObjectiveFlag.QUEST_OBJECTIVE_CAST
                    ObjectivesSpell(i) = Quest.ObjectivesCastSpell(i)
                End If
            Next

            'Load Kills
            For i = 0 To 3
                If Quest.ObjectivesKill(i) > 0 Then
                    For j = 0 To 3
                        If ObjectivesType(j) = 0 Then
                            ObjectiveFlags = ObjectiveFlags Or QuestObjectiveFlag.QUEST_OBJECTIVE_KILL
                            ObjectivesType(j) = QuestObjectiveFlag.QUEST_OBJECTIVE_KILL
                            ObjectivesObject(j) = Quest.ObjectivesKill(i)
                            ObjectivesCount(j) = Quest.ObjectivesKill_Count(i)
                            Exit For
                        End If
                    Next
                End If
            Next

            'Load Items
            For i = 0 To 3
                If Quest.ObjectivesItem(i) > 0 Then
                    ObjectiveFlags = ObjectiveFlags Or QuestObjectiveFlag.QUEST_OBJECTIVE_ITEM
                    ObjectivesItem(i) = Quest.ObjectivesItem(i)
                    ObjectivesItemCount(i) = Quest.ObjectivesItem_Count(i)
                End If
            Next

            'Load Exploration loctions
            If (Quest.Flags And QuestFlag.QUEST_FLAGS_EXPLORATION) Then
                ObjectiveFlags = ObjectiveFlags Or QuestObjectiveFlag.QUEST_OBJECTIVE_EXPLORE
                For i = 0 To 3
                    ObjectivesExplore(i) = Quest.ObjectivesTrigger(i)
                Next
            End If
            'TODO: Fix this below
            If (Quest.Flags And QuestFlag.QUEST_FLAGS_EVENT) Then
                ObjectiveFlags = ObjectiveFlags Or QuestObjectiveFlag.QUEST_OBJECTIVE_EVENT
                For i = 0 To 3
                    If ObjectivesType(i) = 0 Then
                        ObjectivesType(i) = QuestObjectiveFlag.QUEST_OBJECTIVE_EVENT
                        ObjectivesCount(i) = 1
                    End If
                Next
            End If

            'No objective flags are set, complete it directly
            If ObjectiveFlags = 0 Then
                For i = 0 To 3
                    'Make sure these are zero
                    ObjectivesObject(i) = 0
                    ObjectivesCount(i) = 0
                    ObjectivesExplore(i) = 0
                    ObjectivesSpell(i) = 0
                    ObjectivesType(i) = 0
                Next
                IsCompleted()
            End If

            Title = Quest.Title
            ID = Quest.ID
            Flags = Quest.Flags
            RewTitleId = Quest.RewTitleId
            rewtalents = Quest.rewtalents
            SpecialFlags = Quest.SpecialFlags
            ObjectivesDeliver = Quest.ObjectivesDeliver
            'TODO: Fix a timer or something so that the quest really expires when it does
            If Quest.TimeLimit > 0 Then TimeEnd = GetTimestamp(Now) + Quest.TimeLimit 'The time the quest expires
        End Sub
        Public Sub UpdateItemCount(ByRef c As CharacterObject)
            'DONE: Update item count at login
            For i As Byte = 0 To 3
                If ObjectivesItem(i) <> 0 Then
                    ProgressItem(i) = c.ItemCOUNT(ObjectivesItem(i))
                    Log.WriteLine(LogType.DEBUG, "ITEM COUNT UPDATED TO: {0}", ProgressItem(i))
                End If
            Next

            'DONE: If the quest doesn't require any explore than set this as completed
            If (ObjectiveFlags And QuestObjectiveFlag.QUEST_OBJECTIVE_EXPLORE) = 0 Then Explored = True

            'DONE: Check if the quest is completed
            IsCompleted()
        End Sub
        Public Sub Initialize(ByRef c As CharacterObject)
            Dim i As Byte
            If ObjectivesDeliver > 0 Then
                Dim tmpItem As New ItemObject(ObjectivesDeliver, c.GUID)
                If Not c.ItemADD(tmpItem) Then
                    'DONE: Some error, unable to add item, quest is uncompletable
                    tmpItem.Delete()

                    Dim response As New PacketClass(OPCODES.SMSG_QUESTGIVER_QUEST_FAILED)
                    response.AddInt32(ID)
                    response.AddInt32(QuestFailedReason.FAILED_INVENTORY_FULL)
                    c.Client.Send(response)
                    response.Dispose()
                    Exit Sub
                Else
                    c.LogLootItem(tmpItem, 1, True, False)
                End If
            End If

            For i = 0 To 3
                If ObjectivesItem(i) <> 0 Then ProgressItem(i) = c.ItemCOUNT(ObjectivesItem(i))
            Next

            If (ObjectiveFlags And QuestObjectiveFlag.QUEST_OBJECTIVE_EXPLORE) Then Explored = False

            IsCompleted()
        End Sub
        Public Overridable Function IsCompleted() As Boolean
            Complete = (ObjectivesCount(0) <= Progress(0) AndAlso ObjectivesCount(1) <= Progress(1) AndAlso ObjectivesCount(2) <= Progress(2) AndAlso ObjectivesCount(3) <= Progress(3) AndAlso ObjectivesItemCount(0) <= ProgressItem(0) AndAlso ObjectivesItemCount(1) <= ProgressItem(1) AndAlso ObjectivesItemCount(2) <= ProgressItem(2) AndAlso ObjectivesItemCount(3) <= ProgressItem(3) AndAlso Explored AndAlso Failed = False)
            Return Complete
        End Function
        Public Overridable Function GetState(Optional ByVal ForSave As Boolean = False) As Integer
            Dim tmpProgress As Integer = 0
            If ForSave Then
                tmpProgress += CType(Progress(0), Integer)
                tmpProgress += CType(Progress(1), Integer) << 6
                tmpProgress += CType(Progress(2), Integer) << 12
                tmpProgress += CType(Progress(3), Integer) << 18
                If Explored Then tmpProgress += CType(1, Integer) << 24
                If Complete Then tmpProgress += CType(1, Integer) << 25
                If Failed Then tmpProgress += CType(1, Integer) << 26
            Else
                tmpProgress += CType(Progress(0), Integer)
                tmpProgress += CType(Progress(1), Integer) << 8
                tmpProgress += CType(Progress(2), Integer) << 16
                tmpProgress += CType(Progress(3), Integer) << 24
            End If
            Return tmpProgress
        End Function
        Public Overridable Sub LoadState(ByVal state As Integer)
            Progress(0) = state And &H3F
            Progress(1) = (state >> 6) And &H3F
            Progress(2) = (state >> 12) And &H3F
            Progress(3) = (state >> 18) And &H3F
            Explored = (((state >> 24) And &H1) = 1)
            Complete = (((state >> 25) And &H1) = 1)
            Failed = (((state >> 26) And &H1) = 1)
        End Sub
        Public Sub AddKill(ByVal c As CharacterObject, ByVal index As Byte, ByVal oGUID As ULong)
            Progress(index) += 1
            IsCompleted()
            c.TalkUpdateQuest(Slot)

            SendQuestMessageAddKill(c.Client, ID, oGUID, ObjectivesObject(index), Progress(index), ObjectivesCount(index))
        End Sub
        Public Sub AddCast(ByVal c As CharacterObject, ByVal index As Byte, ByVal oGUID As ULong)
            Progress(index) += 1
            IsCompleted()
            c.TalkUpdateQuest(Slot)

            SendQuestMessageAddKill(c.Client, ID, oGUID, ObjectivesObject(index), Progress(index), ObjectivesCount(index))
        End Sub
        Public Sub AddExplore(ByVal c As CharacterObject)
            Explored = True
            IsCompleted()
            c.TalkUpdateQuest(Slot)

            SendQuestMessageComplete(c.Client, ID)
        End Sub
        Public Sub AddItem(ByVal c As CharacterObject, ByVal index As Byte, ByVal Count As Byte)
            If ProgressItem(index) + Count > ObjectivesItemCount(index) Then Count = ObjectivesItemCount(index) - ProgressItem(index)
            ProgressItem(index) += Count
            IsCompleted()
            c.TalkUpdateQuest(Slot)

            SendQuestMessageAddItem(c.Client, ObjectivesItem(index), Count)
        End Sub
        Public Sub RemoveItem(ByVal c As CharacterObject, ByVal index As Byte, ByVal Count As Byte)
            If CInt(ProgressItem(index)) - CInt(Count) < 0 Then Count = ProgressItem(index)
            ProgressItem(index) -= Count
            IsCompleted()
            c.TalkUpdateQuest(Slot)
        End Sub
    End Class
    Public Class BaseQuestScripted
        Inherits BaseQuest
        Public Overridable Sub OnQuestStart(ByRef c As CharacterObject)
        End Sub
        Public Overridable Sub OnQuestComplete(ByRef c As CharacterObject)
        End Sub
        Public Overridable Sub OnQuestCancel(ByRef c As CharacterObject)
        End Sub

        Public Overridable Sub OnQuestItem(ByRef c As CharacterObject, ByVal ItemID As Integer, ByVal ItemCount As Integer)
        End Sub
        Public Overridable Sub OnQuestKill(ByRef c As CharacterObject, ByRef Creature As CreatureObject)
        End Sub
        Public Overridable Sub OnQuestCastSpell(ByRef c As CharacterObject, ByRef Creature As CreatureObject, ByVal SpellID As Integer)
        End Sub
        Public Overridable Sub OnQuestCastSpell(ByRef c As CharacterObject, ByRef GameObject As GameObjectObject, ByVal SpellID As Integer)
        End Sub
        Public Overridable Sub OnQuestExplore(ByRef c As CharacterObject, ByVal AreaID As Integer)
        End Sub
    End Class

#End Region
#Region "Quests.HelpingSubs"


    Public Function GetQuestMenu(ByRef c As CharacterObject, ByVal GUID As ULong) As QuestMenu
        Dim QuestMenu As New QuestMenu

        'DONE: Avaible quests
        Dim MySQLQuery As New DataTable

        Dim CharacterDB As String = CharacterDatabase.SQLDBName

        'NOTE: This is more complex query with checking requirements (may do some slowdowns)
        'Database.Query(String.Format("SELECT * FROM quests q WHERE q.NPC_Start = {0} AND q.Level_Start <= {1} AND NOT EXISTS(SELECT * FROM characters_quests WHERE char_guid = {2} AND quest_id = q.id) " & _
        '"AND (q.Required_Quest = 0 OR EXISTS(SELECT * FROM characters_quests WHERE char_guid = {2} AND quest_status = -1 AND quest_id = q.Required_Quest));", _
        'WORLD_CREATUREs(GUID).ID, c.Level + 1, c.GUID), MySQLQuery)
        'Database.Query(String.Format("SELECT s.questid, q.Title, q.Level_Normal, q.specialflags FROM queststarters s LEFT JOIN quests q ON (s.questid=q.id) WHERE s.type = {0} AND s.typeid = {1} AND q.Level_Start <= {2} AND (q.Required_Race = 0 OR (Required_Race & {4}) > 0) AND (q.Required_Class = 0 OR (Required_Class & {5}) > 0) " & _
        '    "AND NOT EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status <> -2 AND quest_id = q.id) AND (q.Required_Quest1 = 0 OR EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest1)) " & _
        '    "AND (q.Required_Quest2 = 0 OR EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest2)) AND (q.Required_Quest3 = 0 OR EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest3)) " & _
        '    "AND (q.Required_Quest4 = 0 OR EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest4));", _
        '    CType(QuestGiverType.QUEST_OBJECTTYPE_CREATURE, Byte), CType(WORLD_CREATUREs(GUID), CreatureObject).ID, c.Level, c.GUID, 1 << (c.Race - 1), 1 << (c.Classe - 1)), MySQLQuery)
        WorldDatabase.Query(String.Format("SELECT s.questid, q.Title, q.Level_Normal, q.specialflags FROM queststarters s LEFT JOIN quests q ON (s.questid=q.id) WHERE s.type = {0} AND s.typeid = {1} AND q.Level_Start <= {2} AND (q.Required_Race = 0 OR (Required_Race & {4}) > 0) AND (q.Required_Class = 0 OR (Required_Class & {5}) > 0) " & _
            "AND NOT EXISTS(SELECT * FROM " & CharacterDB & ".characters_quests WHERE char_guid = {3} AND quest_status <> -2 AND quest_id = q.id) AND (q.Required_Quest1 = 0 OR EXISTS(SELECT * FROM " & CharacterDB & ".characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest1)) " & _
            "AND (q.Required_Quest2 = 0 OR EXISTS(SELECT * FROM " & CharacterDB & ".characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest2)) AND (q.Required_Quest3 = 0 OR EXISTS(SELECT * FROM " & CharacterDB & ".characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest3)) " & _
            "AND (q.Required_Quest4 = 0 OR EXISTS(SELECT * FROM " & CharacterDB & ".characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest4));", _
            CType(QuestGiverType.QUEST_OBJECTTYPE_CREATURE, Byte), CType(WORLD_CREATUREs(GUID), CreatureObject).ID, c.Level, c.GUID, 1 << (c.Race - 1), 1 << (c.Classe - 1)), MySQLQuery)

        For Each Row As DataRow In MySQLQuery.Rows
            If (CByte(Row.Item("specialflags")) And QuestSpecialFlag.QUEST_SPECIALFLAGS_REPEATABLE) Then
                QuestMenu.AddMenu(Row.Item("Title"), Row.Item("questid"), Row.Item("Level_Normal"), QuestgiverStatus.DIALOG_STATUS_REPEATABLE)
            Else
                QuestMenu.AddMenu(Row.Item("Title"), Row.Item("questid"), Row.Item("Level_Normal"), QuestgiverStatus.DIALOG_STATUS_AVAILABLE)
            End If
        Next

        'DONE: Quests for completing
        Dim i As Integer
        For i = 0 To QUEST_SLOTS
            If Not c.TalkQuests(i) Is Nothing Then
                'Database.Query(String.Format("SELECT s.questid, q.specialflags FROM questfinishers s LEFT JOIN quests q ON (s.questid=q.id) WHERE s.type = {0} AND s.typeid = {1} AND s.questid = {2};", CType(QuestGiverType.QUEST_OBJECTTYPE_CREATURE, Byte), CType(WORLD_CREATUREs(GUID), CreatureObject).ID, c.TalkQuests(i).ID), MySQLQuery)
                WorldDatabase.Query(String.Format("SELECT s.questid, q.specialflags FROM questfinishers s LEFT JOIN quests q ON (s.questid=q.id) WHERE s.type = {0} AND s.typeid = {1} AND s.questid = {2};", CType(QuestGiverType.QUEST_OBJECTTYPE_CREATURE, Byte), CType(WORLD_CREATUREs(GUID), CreatureObject).ID, c.TalkQuests(i).ID), MySQLQuery)
                For Each Row As DataRow In MySQLQuery.Rows
                    If (CByte(Row.Item("specialflags")) And QuestSpecialFlag.QUEST_SPECIALFLAGS_REPEATABLE) Then
                        QuestMenu.AddMenu(c.TalkQuests(i).Title, c.TalkQuests(i).ID, 0, QuestgiverStatus.DIALOG_STATUS_REPEATABLE_FINISHED)
                    Else
                        QuestMenu.AddMenu(c.TalkQuests(i).Title, c.TalkQuests(i).ID, 0, QuestgiverStatus.DIALOG_STATUS_INCOMPLETE)
                    End If
                Next
            End If
        Next

        Return QuestMenu
    End Function
    Public Function GetQuestMenuGO(ByRef c As CharacterObject, ByVal GUID As ULong) As QuestMenu
        Dim QuestMenu As New QuestMenu

        'DONE: Avaible quests
        Dim MySQLQuery As New DataTable

        Dim CharacterDB As String = CharacterDatabase.SQLDBName

        'Database.Query(String.Format("SELECT * FROM quests q WHERE q.NPC_Start = -{0} AND q.Level_Start <= {1} AND NOT EXISTS(SELECT * FROM characters_quests WHERE char_guid = {2} AND quest_id = q.id) " & _
        '"AND (q.Required_Quest = 0 OR EXISTS(SELECT * FROM characters_quests WHERE char_guid = {2} AND quest_status = -1 AND quest_id = q.Required_Quest));", _
        'WORLD_GAMEOBJECTs(GUID).ID, c.Level + 1, c.GUID), MySQLQuery)
        'Database.Query(String.Format("SELECT s.questid, q.Title, q.Level_Normal FROM queststarters s LEFT JOIN quests q ON (s.questid=q.id) WHERE s.type = {0} AND s.typeid = {1} AND q.Level_Start <= {2} AND (q.Required_Race = 0 OR (Required_Race & {4}) > 0) AND (q.Required_Class = 0 OR (Required_Class & {5}) > 0) " & _
        '    "AND NOT EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status <> -2 AND quest_id = q.id) AND (q.Required_Quest1 = 0 OR EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest1)) " & _
        '    "AND (q.Required_Quest2 = 0 OR EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest2)) AND (q.Required_Quest3 = 0 OR EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest3)) " & _
        '    "AND (q.Required_Quest4 = 0 OR EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest4));", _
        '    CType(QuestGiverType.QUEST_OBJECTTYPE_GAMEOBJECT, Byte), WORLD_GAMEOBJECTs(GUID).ID, c.Level, c.GUID, 1 << (c.Race - 1), 1 << (c.Classe - 1)), MySQLQuery)
        WorldDatabase.Query(String.Format("SELECT s.questid, q.Title, q.Level_Normal FROM queststarters s LEFT JOIN quests q ON (s.questid=q.id) WHERE s.type = {0} AND s.typeid = {1} AND q.Level_Start <= {2} AND (q.Required_Race = 0 OR (Required_Race & {4}) > 0) AND (q.Required_Class = 0 OR (Required_Class & {5}) > 0) " & _
            "AND NOT EXISTS(SELECT * FROM " & CharacterDB & ".characters_quests WHERE char_guid = {3} AND quest_status <> -2 AND quest_id = q.id) AND (q.Required_Quest1 = 0 OR EXISTS(SELECT * FROM " & CharacterDB & ".characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest1)) " & _
            "AND (q.Required_Quest2 = 0 OR EXISTS(SELECT * FROM " & CharacterDB & ".characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest2)) AND (q.Required_Quest3 = 0 OR EXISTS(SELECT * FROM " & CharacterDB & ".characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest3)) " & _
            "AND (q.Required_Quest4 = 0 OR EXISTS(SELECT * FROM " & CharacterDB & ".characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest4));", _
            CType(QuestGiverType.QUEST_OBJECTTYPE_GAMEOBJECT, Byte), WORLD_GAMEOBJECTs(GUID).ID, c.Level, c.GUID, 1 << (c.Race - 1), 1 << (c.Classe - 1)), MySQLQuery)

        For Each Row As DataRow In MySQLQuery.Rows
            QuestMenu.AddMenu(Row.Item("Title"), Row.Item("id"), 0, QuestgiverStatus.DIALOG_STATUS_AVAILABLE)
        Next

        'DONE: Quests for completing
        Dim i As Integer
        For i = 0 To QUEST_SLOTS
            If Not c.TalkQuests(i) Is Nothing Then
                'Database.Query(String.Format("SELECT questid FROM questfinishers WHERE type = {0} AND typeid = {1} AND questid = {2};", CType(QuestGiverType.QUEST_OBJECTTYPE_GAMEOBJECT, Byte), WORLD_GAMEOBJECTs(GUID).ID, c.TalkQuests(i).ID), MySQLQuery)
                WorldDatabase.Query(String.Format("SELECT questid FROM questfinishers WHERE type = {0} AND typeid = {1} AND questid = {2};", CType(QuestGiverType.QUEST_OBJECTTYPE_GAMEOBJECT, Byte), WORLD_GAMEOBJECTs(GUID).ID, c.TalkQuests(i).ID), MySQLQuery)
                For Each Row As DataRow In MySQLQuery.Rows
                    QuestMenu.AddMenu(c.TalkQuests(i).Title, c.TalkQuests(i).ID, 0, QuestgiverStatus.DIALOG_STATUS_INCOMPLETE)
                Next
            End If
        Next

        Return QuestMenu
    End Function
    Public Sub SendQuestMenu(ByRef c As CharacterObject, ByVal GUID As ULong, Optional ByVal Title As String = "Available quests", Optional ByVal QuestMenu As QuestMenu = Nothing)
        If QuestMenu Is Nothing Then
            QuestMenu = GetQuestMenu(c, GUID)
        End If

        Dim packet As New PacketClass(OPCODES.SMSG_QUESTGIVER_QUEST_LIST)
        packet.AddUInt64(GUID)
        packet.AddString(Title)
        packet.AddInt32(1)              'Delay
        packet.AddInt32(1)              'Emote
        packet.AddInt8(QuestMenu.IDs.Count) 'Count
        Dim i As Integer = 0
        For i = 0 To QuestMenu.IDs.Count - 1
            packet.AddInt32(QuestMenu.IDs(i))
            packet.AddInt32(QuestMenu.Icons(i))
            packet.AddInt32(QuestMenu.Levels(i))
            packet.AddString(QuestMenu.Names(i))
        Next
        c.Client.Send(packet)
        packet.Dispose()
    End Sub

    Public Sub SendQuestDetails(ByRef client As ClientClass, ByRef Quest As QuestInfo, ByVal GUID As ULong, ByVal AcceptActive As Boolean)
        Dim i As Integer
        Dim packet As New PacketClass(OPCODES.SMSG_QUESTGIVER_QUEST_DETAILS)
        packet.AddUInt64(GUID)
        packet.AddUInt64(0) 'Unk 3.0.2

        'QuestDetails
        packet.AddInt32(Quest.ID)
        packet.AddString(Quest.Title)
        packet.AddString(Quest.TextDescription)
        packet.AddString(Quest.TextObjectives)
        packet.AddInt32(AcceptActive)
        packet.AddInt32(Quest.SuggestedPlayers)
        packet.AddInt8(0) 'Unk 3.0.2

        'QuestRewards (Choosable)
        Dim questRewardsCount As Integer = 0
        If (Quest.Flags And QuestFlag.QUEST_FLAGS_HIDDEN_REWARDS) = QuestFlag.QUEST_FLAGS_HIDDEN_REWARDS Then
            packet.AddInt32(0) 'Rewarded chosen items hidden
            packet.AddInt32(0) 'Rewarded items hidden
            packet.AddInt32(0) 'Rewarded money hidden
        Else
            For i = 0 To QUEST_REWARD_CHOICES_COUNT
                If Quest.RewardItems(i) <> 0 Then questRewardsCount += 1
            Next
            packet.AddInt32(questRewardsCount)
            For i = 0 To QUEST_REWARD_CHOICES_COUNT
                If Quest.RewardItems(i) <> 0 Then
                    'Add item if not loaded into server
                    If Not ITEMDatabase.ContainsKey(Quest.RewardItems(i)) Then Dim tmpItem As New ItemInfo(Quest.RewardItems(i))
                    packet.AddInt32(Quest.RewardItems(i))
                    packet.AddInt32(Quest.RewardItems_Count(i))
                    packet.AddInt32(ITEMDatabase(Quest.RewardItems(i)).Model)
                End If
            Next
            'QuestRewards (Static)
            questRewardsCount = 0
            For i = 0 To QUEST_REWARDS_COUNT
                If Quest.RewardStaticItems(i) <> 0 Then questRewardsCount += 1
            Next
            packet.AddInt32(questRewardsCount)
            For i = 0 To QUEST_REWARDS_COUNT
                If Quest.RewardStaticItems(i) <> 0 Then
                    'Add item if not loaded into server
                    If Not ITEMDatabase.ContainsKey(Quest.RewardStaticItems(i)) Then Dim tmpItem As New ItemInfo(Quest.RewardStaticItems(i))
                    packet.AddInt32(Quest.RewardStaticItems(i))
                    packet.AddInt32(Quest.RewardStaticItems_Count(i))
                    packet.AddInt32(ITEMDatabase(Quest.RewardStaticItems(i)).Model)
                End If
            Next
            packet.AddInt32(Quest.RewardGold)
        End If
        packet.AddInt32(Quest.RewardHonor)
        packet.AddInt32(Quest.RewardSpell)
        packet.AddInt32(Quest.RewardSpellCast)
        packet.AddInt32(Quest.RewTitleId) 'CharTitleID
        packet.AddInt32(Quest.rewtalents) 'RewardTalents 3.0.2

        'Emotes List
        packet.AddInt32(4)              'EmoteCount
        For i = 0 To 3
            packet.AddInt32(1) 'Emote
            packet.AddInt32(0) 'Delay
        Next

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] SMSG_QUESTGIVER_QUEST_DETAILS [GUID={2:X} Quest={3}]", client.IP, client.Port, GUID, Quest.ID)

        'Finishing
        client.Send(packet)
        packet.Dispose()
    End Sub
    Public Sub SendQuest(ByRef client As ClientClass, ByRef Quest As QuestInfo)
        Dim packet As New PacketClass(OPCODES.SMSG_QUEST_QUERY_RESPONSE)
        packet.AddInt32(Quest.ID)

        'Basic Details
        packet.AddInt32(Quest.IsActive)
        packet.AddInt32(Quest.Level_Normal)
        packet.AddInt32(Quest.Zone)
        packet.AddInt32(Quest.Type)
        packet.AddInt32(Quest.SuggestedPlayers)
        packet.AddInt32(Quest.RequiredReputation_Faction(0))
        packet.AddInt32(Quest.RequiredReputation(0))
        packet.AddInt32(0)
        packet.AddInt32(0)
        packet.AddInt32(Quest.NextQuest)

        If (Quest.Flags And QuestFlag.QUEST_FLAGS_HIDDEN_REWARDS) Or Quest.RewardGold < 0 Then
            packet.AddInt32(0)
        Else
            packet.AddInt32(Quest.RewardGold) 'Negative is required money
        End If
        ''''packet.AddInt32(Quest.MoneyAtMaxLevel)
        packet.AddInt32(0) ' Required Money????
        ''''packet.AddInt32(Quest.RewardSpell) 'Spell rewarded
        ''''packet.AddInt32(Quest.RewardSpellCast) 'Spell Casted
        packet.AddInt32(Quest.RewardSpellCast) 'Spell Casted
        packet.AddInt32(Quest.RewardSpell) 'Spell rewarded
        packet.AddInt32(Quest.RewardHonor)
        packet.AddInt32(Quest.ObjectivesDeliver) ' Item given at the start of a quest (srcItem)
        packet.AddInt32(Quest.Flags)
        packet.AddInt32(Quest.RewTitleId) ' 2.4.0 Rewarded CharTitleID
        packet.AddInt32(0) ' 3.0.2 Players killed
        packet.AddInt32(Quest.rewtalents) ' 3.0.2 Rewarded talents

        Dim i As Integer
        If (Quest.Flags And QuestFlag.QUEST_FLAGS_HIDDEN_REWARDS) = 0 Then
            For i = 0 To QUEST_REWARDS_COUNT
                packet.AddInt32(0)
                packet.AddInt32(0)
            Next
            For i = 0 To QUEST_REWARD_CHOICES_COUNT
                packet.AddInt32(0)
                packet.AddInt32(0)
            Next
        Else
            For i = 0 To QUEST_REWARDS_COUNT
                packet.AddInt32(Quest.RewardStaticItems(i))
                packet.AddInt32(Quest.RewardStaticItems_Count(i))
            Next
            For i = 0 To QUEST_REWARD_CHOICES_COUNT
                packet.AddInt32(Quest.RewardItems(i))
                packet.AddInt32(Quest.RewardItems_Count(i))
            Next
        End If

        packet.AddInt32(Quest.PointMapID)       'Point MapID
        packet.AddSingle(Quest.PointX)          'Point X
        packet.AddSingle(Quest.PointY)          'Point Y
        packet.AddInt32(Quest.PointOpt)         'Point Opt

        'Texts
        packet.AddString(Quest.Title)
        packet.AddString(Quest.TextObjectives)
        packet.AddString(Quest.TextDescription)
        packet.AddString(Quest.TextEnd) 'Something in the objectives (NOT endtext)

        'Objectives
        For i = 0 To QUEST_OBJECTIVES_COUNT
            If Quest.ObjectivesKill(i) < 0 Then 'If it's a game object (client wants ID | 0x80000000)
                packet.AddInt32((-Quest.ObjectivesKill(i)) Or &H80000000)
            Else
                packet.AddInt32(Quest.ObjectivesKill(i))
            End If
            packet.AddInt32(Quest.ObjectivesKill_Count(i))
            packet.AddInt32(0) ' UNKNOWN
            packet.AddInt32(Quest.ObjectivesItem(i))
            packet.AddInt32(Quest.ObjectivesItem_Count(i))
            ''''packet.AddInt32(0) ' 3.0.2 Unk
            packet.AddInt32(0) ' 5th Collect Item Id
            packet.AddInt32(0) ' 5th Collect Item Count

            'HACK: Fix for not showing "Unknown Item" (sometimes client doesn't get items on time)
            If Quest.ObjectivesItem(i) <> 0 Then SendItemInfo(client, Quest.ObjectivesItem(i))
        Next

        For i = 0 To QUEST_OBJECTIVES_COUNT
            packet.AddString(Quest.ObjectivesText(i))
        Next

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] SMSG_QUEST_QUERY_RESPONSE [Quest={2}]", client.IP, client.Port, Quest.ID)

        'Finishing
        client.Send(packet)
        packet.Dispose()
    End Sub

    Public Sub SendQuestMessageAddItem(ByRef client As ClientClass, ByVal itemID As Integer, ByVal itemCount As Integer)
        Dim packet As New PacketClass(OPCODES.SMSG_QUESTUPDATE_ADD_ITEM)
        packet.AddInt32(itemID)
        packet.AddInt32(itemCount)
        client.Send(packet)
        packet.Dispose()
    End Sub
    Public Sub SendQuestMessageAddKill(ByRef client As ClientClass, ByVal questID As Integer, ByVal killGUID As ULong, ByVal killID As Integer, ByVal killCurrentCount As Integer, ByVal killCount As Integer)
        'Message: %s slain: %d/%d
        Dim packet As New PacketClass(OPCODES.SMSG_QUESTUPDATE_ADD_KILL)
        packet.AddInt32(questID)
        If killID < 0 Then killID = ((-killID) Or &H80000000) 'Gameobject
        packet.AddInt32(killID)
        packet.AddInt32(killCurrentCount)
        packet.AddInt32(killCount)
        packet.AddUInt64(killGUID)
        client.Send(packet)
        packet.Dispose()
    End Sub
    Public Sub SendQuestMessageFailed(ByRef client As ClientClass, ByVal QuestID As Integer)
        'Message: ?
        Dim packet As New PacketClass(OPCODES.SMSG_QUESTGIVER_QUEST_FAILED)
        packet.AddInt32(QuestID)
        ' TODO: Need to add failed reason to packet here
        client.Send(packet)
        packet.Dispose()
    End Sub
    Public Sub SendQuestMessageFailedTimer(ByRef client As ClientClass, ByVal QuestID As Integer)
        'Message: ?
        Dim packet As New PacketClass(OPCODES.SMSG_QUESTUPDATE_FAILEDTIMER)
        packet.AddInt32(QuestID)
        client.Send(packet)
        packet.Dispose()
    End Sub
    Public Sub SendQuestMessageComplete(ByRef client As ClientClass, ByVal QuestID As Integer)
        'Message: Objective Complete.
        Dim packet As New PacketClass(OPCODES.SMSG_QUESTUPDATE_COMPLETE)
        packet.AddInt32(QuestID)
        client.Send(packet)
        packet.Dispose()
    End Sub

    Public Sub SendQuestComplete(ByRef client As ClientClass, ByRef Quest As QuestInfo, ByVal XP As Integer, ByVal Gold As Integer)
        Dim packet As New PacketClass(OPCODES.SMSG_QUESTGIVER_QUEST_COMPLETE)

        packet.AddInt32(Quest.ID)
        ''''packet.AddInt32(3)
        packet.AddInt32(XP)
        packet.AddInt32(Gold)
        packet.AddInt32(0) ' ??????
        ''''packet.AddInt32(Quest.RewardHonor) ' bonus honor...used in BG quests
        packet.AddInt32(Quest.rewtalents)
        Log.WriteLine(LogType.DEBUG, "DEBUG: Reward Talents [{0}]", Quest.rewtalents)

        Dim i As Integer
        packet.AddInt32(QUEST_REWARDS_COUNT + 1)
        For i = 0 To QUEST_REWARDS_COUNT
            packet.AddInt32(Quest.RewardStaticItems(i))
            packet.AddInt32(Quest.RewardStaticItems_Count(i))

        Next
        client.Send(packet)
        packet.Dispose()
    End Sub
    Public Sub SendQuestReward(ByRef client As ClientClass, ByRef Quest As QuestInfo, ByVal GUID As ULong, ByRef q As BaseQuest)
        Dim packet As New PacketClass(OPCODES.SMSG_QUESTGIVER_OFFER_REWARD)

        packet.AddUInt64(GUID)
        packet.AddInt32(q.ID)
        packet.AddString(q.Title)
        packet.AddString(Quest.TextComplete)

        packet.AddInt32(CType(q.Complete, Integer))     'EnbleNext

        packet.AddInt32(0) ' Maybe Required Money????
        packet.AddInt32(1)          'EmotesCount
        ''''packet.AddInt32(0)          'EmoteDelay
        ''''packet.AddInt32(1)          'EmoteID (Type)????
        packet.AddInt32(1)          'EmoteID (Type)????
        packet.AddInt32(0)          'EmoteDelay

        Dim i As Integer

        Dim questRewardsCount As Integer = 0
        For i = 0 To QUEST_REWARD_CHOICES_COUNT
            If Quest.RewardItems(i) <> 0 Then questRewardsCount += 1
        Next
        packet.AddInt32(questRewardsCount)
        For i = 0 To QUEST_REWARD_CHOICES_COUNT
            If Quest.RewardItems(i) <> 0 Then
                packet.AddInt32(Quest.RewardItems(i))
                packet.AddInt32(Quest.RewardItems_Count(i))

                'Add item if not loaded into server
                If Not ITEMDatabase.ContainsKey(Quest.RewardItems(i)) Then Dim tmpItem As New ItemInfo(Quest.RewardItems(i))
                packet.AddInt32(ITEMDatabase(Quest.RewardItems(i)).Model)
            End If
        Next

        For i = 0 To QUEST_REWARDS_COUNT
            If Quest.RewardItems(i) <> 0 Then questRewardsCount += 1
        Next
        packet.AddInt32(questRewardsCount)
        For i = 0 To QUEST_REWARDS_COUNT
            If Quest.RewardStaticItems(i) <> 0 Then
                packet.AddInt32(Quest.RewardStaticItems(i))
                packet.AddInt32(Quest.RewardStaticItems_Count(i))

                'Add item if not loaded into server
                If Not ITEMDatabase.ContainsKey(Quest.RewardStaticItems(i)) Then Dim tmpItem As New ItemInfo(Quest.RewardStaticItems(i))
                packet.AddInt32(ITEMDatabase(Quest.RewardStaticItems(i)).Model)
            End If
        Next

        packet.AddInt32(Quest.RewardGold)
        packet.AddInt32(Quest.RewardHonor) ' Bonus Honor???
        packet.AddInt32(0)
        ''''packet.AddInt32(8)
        packet.AddInt32(Quest.RewardSpell)
        packet.AddInt32(Quest.RewardSpellCast)
        packet.AddInt32(q.RewTitleId) ' Reward Title ID???
        packet.AddInt32(q.rewtalents) ' Reward Talents???

        client.Send(packet)
        packet.Dispose()
    End Sub
    Public Sub SendQuestRequireItems(ByRef client As ClientClass, ByRef Quest As QuestInfo, ByVal GUID As ULong, ByRef q As BaseQuest)
        Dim packet As New PacketClass(OPCODES.SMSG_QUESTGIVER_REQUEST_ITEMS)

        packet.AddUInt64(GUID)
        packet.AddInt32(q.ID)
        packet.AddString(q.Title)
        packet.AddString(Quest.TextIncomplete)
        packet.AddInt32(0) 'Unknown

        If q.Complete Then
            packet.AddInt32(0) 'CompleteEmote?
        Else
            packet.AddInt32(0) 'IncompleteEmote?
        End If

        packet.AddInt32(0)                      'Close Window on Cancel (1 = true / 0 = false) (Emote Delay???)
        packet.AddInt32(1) ' Emote Type???
        If Quest.RewardGold < 0 Then
            packet.AddInt32(-Quest.RewardGold)   'Required Money
        Else
            packet.AddInt32(0)
        End If
        ''''packet.AddInt32(0)                      'Unknown

        'DONE: Count the required items
        Dim i As Integer = 0
        Dim requiredItemsCount As Byte = 0
        For i = 0 To QUEST_OBJECTIVES_COUNT
            If Quest.ObjectivesItem(i) <> 0 Then requiredItemsCount += 1
        Next
        packet.AddInt32(requiredItemsCount)

        'DONE: List items
        If requiredItemsCount > 0 Then
            For i = 0 To QUEST_OBJECTIVES_COUNT
                If Quest.ObjectivesItem(i) <> 0 Then
                    If ITEMDatabase.ContainsKey(Quest.ObjectivesItem(i)) = False Then Dim tmpItem As ItemInfo = New ItemInfo(Quest.ObjectivesItem(i))
                    packet.AddInt32(Quest.ObjectivesItem(i))
                    packet.AddInt32(Quest.ObjectivesItem_Count(i))
                    If ITEMDatabase.ContainsKey(Quest.ObjectivesItem(i)) Then
                        packet.AddInt32(ITEMDatabase(Quest.ObjectivesItem(i)).Model)
                    Else
                        packet.AddInt32(0)
                    End If
                End If
            Next
        End If

        If q.Complete Then
            ''''packet.AddInt32(3)
            packet.AddInt32(2)
        Else
            packet.AddInt32(0)
        End If

        ''''packet.AddInt32(4)
        packet.AddInt32(8)
        packet.AddInt32(10)

        client.Send(packet)
        packet.Dispose()
    End Sub


    Public Sub LoadQuests(ByRef c As CharacterObject)
        Dim cQuests As New DataTable
        'Database.Query(String.Format("SELECT * FROM characters_quests q WHERE q.char_guid = {0} AND q.quest_status > -1 LIMIT 25;", c.GUID), cQuests)
        CharacterDatabase.Query(String.Format("SELECT * FROM characters_quests q WHERE q.char_guid = {0} AND q.quest_status > -1 LIMIT 25;", c.GUID), cQuests)

        Dim i As Integer = 0
        For Each cRow As DataRow In cQuests.Rows
            Dim tmpQuest As New QuestInfo(cRow.Item("quest_id"))

            'DONE: Initialize quest info
            CreateQuest(c.TalkQuests(i), tmpQuest)

            c.TalkQuests(i).LoadState(cRow.Item("quest_status"))
            c.TalkQuests(i).Slot = i
            c.TalkQuests(i).UpdateItemCount(c)

            i += 1
        Next

    End Sub
    Public Sub CreateQuest(ByRef q As BaseQuest, ByRef tmpQuest As QuestInfo)
        'Initialize Quest
        q = New BaseQuest(tmpQuest)
    End Sub

#End Region
#Region "Quests.Events"


    'DONE: Kill quest events
    Public Sub OnQuestKill(ByRef c As CharacterObject, ByRef Creature As CreatureObject)
        'HANDLERS: Added to DealDamage sub

        'DONE: Do not count killed from guards
        If c Is Nothing Then Exit Sub
        Dim i As Integer, j As Byte

        'DONE: Count kills
        For i = 0 To QUEST_SLOTS
            If (Not c.TalkQuests(i) Is Nothing) AndAlso (c.TalkQuests(i).ObjectiveFlags And QuestObjectiveFlag.QUEST_OBJECTIVE_KILL) AndAlso (c.TalkQuests(i).ObjectiveFlags And QuestObjectiveFlag.QUEST_OBJECTIVE_CAST) = 0 Then
                If TypeOf c.TalkQuests(i) Is BaseQuestScripted Then
                    CType(c.TalkQuests(i), BaseQuestScripted).OnQuestKill(c, Creature)
                Else
                    With c.TalkQuests(i)
                        For j = 0 To 3
                            If .ObjectivesType(j) = QuestObjectiveFlag.QUEST_OBJECTIVE_KILL AndAlso .ObjectivesObject(j) = Creature.ID Then
                                If .Progress(j) < .ObjectivesCount(j) Then
                                    .AddKill(c, j, Creature.GUID)
                                    Exit Sub
                                End If
                            End If
                        Next
                    End With
                End If
            End If
        Next i


        Exit Sub  'For now next is disabled

        'DONE: Check all in c's party for that quest
        For Each GUID As ULong In c.Group.LocalMembers
            If GUID = c.GUID Then Continue For

            With CHARACTERs(GUID)
                For i = 0 To QUEST_SLOTS
                    If (Not .TalkQuests(i) Is Nothing) AndAlso (.TalkQuests(i).ObjectiveFlags And QuestObjectiveFlag.QUEST_OBJECTIVE_KILL) AndAlso (.TalkQuests(i).ObjectiveFlags And QuestObjectiveFlag.QUEST_OBJECTIVE_CAST) = 0 Then
                        With .TalkQuests(i)
                            For j = 0 To 3
                                If .ObjectivesType(j) = QuestObjectiveFlag.QUEST_OBJECTIVE_KILL AndAlso .ObjectivesObject(j) = Creature.ID Then
                                    If .Progress(j) < .ObjectivesCount(j) Then
                                        .AddKill(c, j, Creature.GUID)
                                        Exit Sub
                                    End If
                                End If
                            Next
                        End With
                    End If
                Next i
            End With
        Next

    End Sub

    Public Sub OnQuestCastSpell(ByRef c As CharacterObject, ByRef Creature As CreatureObject, ByRef SpellID As Integer)
        Dim i As Integer, j As Byte

        'DONE: Count spell casts
        'DONE: Check if we're casting it on the correct creature
        For i = 0 To QUEST_SLOTS
            If (Not c.TalkQuests(i) Is Nothing) AndAlso (c.TalkQuests(i).ObjectiveFlags And QuestObjectiveFlag.QUEST_OBJECTIVE_CAST) Then
                If TypeOf c.TalkQuests(i) Is BaseQuestScripted Then
                    CType(c.TalkQuests(i), BaseQuestScripted).OnQuestCastSpell(c, Creature, SpellID)
                Else
                    With c.TalkQuests(i)
                        For j = 0 To 3
                            If .ObjectivesType(j) = QuestObjectiveFlag.QUEST_OBJECTIVE_KILL AndAlso .ObjectivesSpell(j) = SpellID Then
                                If .ObjectivesObject(j) = 0 OrElse .ObjectivesObject(j) = Creature.ID Then
                                    If .Progress(j) < .ObjectivesCount(j) Then
                                        .AddCast(c, j, Creature.GUID)
                                        Exit Sub
                                    End If
                                End If
                            End If
                        Next
                    End With
                End If
            End If
        Next i
    End Sub

    Public Sub OnQuestCastSpell(ByRef c As CharacterObject, ByRef GameObject As GameObjectObject, ByRef SpellID As Integer)
        Dim i As Integer, j As Byte

        'DONE: Count spell casts
        'DONE: Check if we're casting it on the correct gameobject
        For i = 0 To QUEST_SLOTS
            If (Not c.TalkQuests(i) Is Nothing) AndAlso (c.TalkQuests(i).ObjectiveFlags And QuestObjectiveFlag.QUEST_OBJECTIVE_CAST) Then
                If TypeOf c.TalkQuests(i) Is BaseQuestScripted Then
                    CType(c.TalkQuests(i), BaseQuestScripted).OnQuestCastSpell(c, GameObject, SpellID)
                Else
                    With c.TalkQuests(i)
                        For j = 0 To 3
                            If .ObjectivesType(j) = QuestObjectiveFlag.QUEST_OBJECTIVE_KILL AndAlso .ObjectivesSpell(j) = SpellID Then
                                'NOTE: GameObjects are negative here!
                                If .ObjectivesObject(j) = 0 OrElse .ObjectivesObject(j) = -(GameObject.ID) Then
                                    If .Progress(j) < .ObjectivesCount(j) Then
                                        .AddCast(c, j, GameObject.GUID)
                                        Exit Sub
                                    End If
                                End If
                            End If
                        Next
                    End With
                End If
            End If
        Next i
    End Sub

    Public Function IsItemNeededForQuest(ByRef c As CharacterObject, ByRef ItemEntry As Integer) As Boolean
        Dim j As Integer, k As Byte, IsRaid As Boolean

        'DONE: Check if anyone in the group has the quest that requires this item
        'DONE: If the quest isn't a raid quest then you can't loot quest items
        IsRaid = c.IsInRaid
        If c.IsInGroup Then
            For Each GUID As ULong In c.Group.LocalMembers
                With CHARACTERs(GUID)

                    For j = 0 To QUEST_SLOTS
                        If (Not .TalkQuests(j) Is Nothing) AndAlso (.TalkQuests(j).ObjectiveFlags And QuestObjectiveFlag.QUEST_OBJECTIVE_ITEM) AndAlso (IsRaid = False OrElse (.TalkQuests(j).Flags And QuestFlag.QUEST_FLAGS_RAID)) Then
                            With .TalkQuests(j)
                                For k = 0 To 3
                                    If .ObjectivesItem(k) = ItemEntry Then
                                        If .ProgressItem(k) < .ObjectivesItemCount(k) Then Return True
                                    End If
                                Next
                            End With
                        End If
                    Next
                End With
            Next

        Else
            For j = 0 To QUEST_SLOTS
                If (Not c.TalkQuests(j) Is Nothing) AndAlso (c.TalkQuests(j).ObjectiveFlags And QuestObjectiveFlag.QUEST_OBJECTIVE_ITEM) Then
                    With c.TalkQuests(j)
                        For k = 0 To 3
                            If .ObjectivesItem(k) = ItemEntry Then
                                If .ProgressItem(k) < .ObjectivesItemCount(k) Then Return True
                            End If
                        Next
                    End With
                End If
            Next
        End If

        Return False
    End Function

    Public Function IsGameObjectUsedForQuest(ByRef Gameobject As GameObjectObject, ByRef c As CharacterObject) As Byte
        'If the item got 100% this gameobject is only used for quests.
        Dim MysqlQuery As New DataTable
        'Database.Query(String.Format("SELECT quest, item, item_count FROM gameobject_quest_association WHERE entry = {0}", Gameobject.ID), MysqlQuery)
        WorldDatabase.Query(String.Format("SELECT quest, item, item_count FROM gameobject_quest_association WHERE entry = {0}", Gameobject.ID), MysqlQuery)
        If MysqlQuery.Rows.Count = 0 Then Return 0

        For Each LootRow As DataRow In MysqlQuery.Rows
            Dim i As Integer

            'DONE: Check quests needing that item
            For i = 0 To QUEST_SLOTS
                If (Not c.TalkQuests(i) Is Nothing) AndAlso (c.TalkQuests(i).ObjectiveFlags And QuestObjectiveFlag.QUEST_OBJECTIVE_ITEM) Then
                    If c.TalkQuests(i).ID = CInt(LootRow.Item("quest")) Then
                        If c.ItemCOUNT(CInt(LootRow.Item("item"))) < CByte(LootRow.Item("item_count")) Then Return 2
                    End If
                End If
            Next i
        Next

        Return 1
    End Function

    'DONE: Quest's loot generation
    Public Sub OnQuestAddQuestLoot(ByRef c As CharacterObject, ByRef Creature As CreatureObject, ByRef Loot As LootObject)
        'HANDLERS: Added in loot generation sub

        'TODO: Check for quest loots for adding to looted creature
    End Sub
    Public Sub OnQuestAddQuestLoot(ByRef c As CharacterObject, ByRef GameObject As GameObjectObject, ByRef Loot As LootObject)
        'HANDLERS: None
        'TODO: Check for quest loots for adding to looted gameObject
    End Sub
    Public Sub OnQuestAddQuestLoot(ByRef c As CharacterObject, ByRef Character As CharacterObject, ByRef Loot As LootObject)
        'HANDLERS: None
        'TODO: Check for quest loots for adding to looted player (used only in battleground?)
    End Sub

    'DONE: Item quest events
    Public Sub OnQuestItemAdd(ByRef c As CharacterObject, ByVal ItemID As Integer, ByVal Count As Byte)
        'HANDLERS: Added to looting sub

        If Count = 0 Then Count = 1
        Dim i As Integer, j As Byte


        'DONE: Check quests needing that item
        For i = 0 To QUEST_SLOTS
            If (Not c.TalkQuests(i) Is Nothing) AndAlso (c.TalkQuests(i).ObjectiveFlags And QuestObjectiveFlag.QUEST_OBJECTIVE_ITEM) Then
                If TypeOf c.TalkQuests(i) Is BaseQuestScripted Then
                    CType(c.TalkQuests(i), BaseQuestScripted).OnQuestItem(c, ItemID, Count)
                Else
                    With c.TalkQuests(i)
                        For j = 0 To 3
                            If .ObjectivesItem(j) = ItemID Then
                                If .ProgressItem(j) < .ObjectivesItemCount(j) Then
                                    .AddItem(c, j, Count)
                                    Exit Sub
                                End If
                            End If
                        Next
                    End With
                End If
            End If
        Next i
    End Sub
    Public Sub OnQuestItemRemove(ByRef c As CharacterObject, ByVal ItemID As Integer, ByVal Count As Byte)
        'HANDLERS: Added to delete sub

        If Count = 0 Then Count = 1
        Dim i As Integer, j As Byte


        'DONE: Check quests needing that item
        For i = 0 To QUEST_SLOTS
            If (Not c.TalkQuests(i) Is Nothing) AndAlso (c.TalkQuests(i).ObjectiveFlags And QuestObjectiveFlag.QUEST_OBJECTIVE_ITEM) Then
                If TypeOf c.TalkQuests(i) Is BaseQuestScripted Then
                    CType(c.TalkQuests(i), BaseQuestScripted).OnQuestItem(c, ItemID, -Count)
                Else
                    With c.TalkQuests(i)
                        For j = 0 To 3
                            If .ObjectivesItem(j) = ItemID Then
                                If .ProgressItem(j) > 0 Then
                                    .RemoveItem(c, j, Count)
                                    Exit Sub
                                End If
                            End If
                        Next
                    End With
                End If
            End If
        Next i
    End Sub

    'DONE: Exploration quest events
    Public Sub OnQuestExplore(ByRef c As CharacterObject, ByVal AreaID As Integer)
        Dim i As Integer, j As Byte
        For i = 0 To QUEST_SLOTS
            If (Not c.TalkQuests(i) Is Nothing) AndAlso (c.TalkQuests(i).ObjectiveFlags And QuestObjectiveFlag.QUEST_OBJECTIVE_EXPLORE) Then
                If TypeOf c.TalkQuests(i) Is BaseQuestScripted Then
                    CType(c.TalkQuests(i), BaseQuestScripted).OnQuestExplore(c, AreaID)
                Else
                    With c.TalkQuests(i)
                        For j = 0 To 3
                            If .ObjectivesExplore(j) = AreaID Then
                                If .Explored = False Then
                                    .AddExplore(c)
                                    Exit Sub
                                End If
                            End If
                        Next
                    End With
                End If
            End If
        Next i
    End Sub


#End Region
#Region "Quests.OpcodeHandlers"


    Public Function GetQuestgiverStatus(ByVal c As CharacterObject, ByVal cGUID As ULong) As QuestgiverStatus
        'DONE: Invoke scripted quest status
        If WORLD_CREATUREs.ContainsKey(cGUID) = False Then Exit Function
        Dim Status As QuestgiverStatus = CType(WORLD_CREATUREs(cGUID), CreatureObject).CreatureInfo.TalkScript.OnQuestStatus(c, cGUID)
        Dim MySQLQuery As New DataTable
        If GuidIsCreature(cGUID) = False AndAlso GuidIsGameObject(cGUID) = False Then Return QuestgiverStatus.DIALOG_STATUS_NONE

        'DONE: Do search for completed quests or in progress
        Dim i As Integer
        For i = 0 To QUEST_SLOTS
            If Not c.TalkQuests(i) Is Nothing Then
                If GuidIsCreature(cGUID) Then
                    'Database.Query(String.Format("SELECT questid FROM questfinishers WHERE type = {0} AND typeid = {1} AND questid = {2} LIMIT 1;", CType(QuestGiverType.QUEST_OBJECTTYPE_CREATURE, Byte), CType(WORLD_CREATUREs(cGUID), CreatureObject).ID, c.TalkQuests(i).ID), MySQLQuery)
                    WorldDatabase.Query(String.Format("SELECT questid FROM questfinishers WHERE type = {0} AND typeid = {1} AND questid = {2} LIMIT 1;", CType(QuestGiverType.QUEST_OBJECTTYPE_CREATURE, Byte), CType(WORLD_CREATUREs(cGUID), CreatureObject).ID, c.TalkQuests(i).ID), MySQLQuery)
                Else
                    'Database.Query(String.Format("SELECT questid FROM questfinishers WHERE type = {0} AND typeid = {1} AND questid = {2} LIMIT 1;", CType(QuestGiverType.QUEST_OBJECTTYPE_GAMEOBJECT, Byte), CType(WORLD_GAMEOBJECTs(cGUID), GameObjectObject).ID, c.TalkQuests(i).ID), MySQLQuery)
                    WorldDatabase.Query(String.Format("SELECT questid FROM questfinishers WHERE type = {0} AND typeid = {1} AND questid = {2} LIMIT 1;", CType(QuestGiverType.QUEST_OBJECTTYPE_GAMEOBJECT, Byte), CType(WORLD_GAMEOBJECTs(cGUID), GameObjectObject).ID, c.TalkQuests(i).ID), MySQLQuery)
                End If
                If MySQLQuery.Rows.Count > 0 Then
                    If c.TalkQuests(i).Complete Then
                        If (c.TalkQuests(i).SpecialFlags And QuestSpecialFlag.QUEST_SPECIALFLAGS_REPEATABLE) Then
                            Return QuestgiverStatus.DIALOG_STATUS_REPEATABLE_FINISHED
                        Else
                            Return QuestgiverStatus.DIALOG_STATUS_REWARD
                        End If
                    End If
                    Status = QuestgiverStatus.DIALOG_STATUS_INCOMPLETE
                End If
            End If
        Next

        'DONE: Queries are last for performance
        Dim characterQuestDB As String = CharacterDatabase.SQLDBName

        If Status = QuestgiverStatus.DIALOG_STATUS_NONE OrElse Status = QuestgiverStatus.DIALOG_STATUS_INCOMPLETE Then
            'DONE: Do SQL query for available quests
            If GuidIsCreature(cGUID) Then
                'Database.Query(String.Format("SELECT questid, specialflags FROM queststarters s LEFT JOIN quests q ON (s.questid=q.id) WHERE s.type = {0} AND s.typeid = {1} AND q.Level_Start <= {2} AND (q.Level_Normal = -1 OR q.Level_Normal > {6}) AND (q.Required_Race = 0 OR (Required_Race & {4}) > 0) AND (q.Required_Class = 0 OR (Required_Class & {5}) > 0) " & _
                '"AND NOT EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status <> -2 AND quest_id = q.id) AND (q.Required_Quest1 = 0 OR EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest1)) " & _
                '"AND (q.Required_Quest2 = 0 OR EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest2)) AND (q.Required_Quest3 = 0 OR EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest3)) " & _
                '"AND (q.Required_Quest4 = 0 OR EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest4));", _
                'CType(QuestGiverType.QUEST_OBJECTTYPE_CREATURE, Byte), CType(WORLD_CREATUREs(cGUID), CreatureObject).ID, c.Level, c.GUID, c.RaceMask, c.ClassMask, c.Level - 6), MySQLQuery)
                WorldDatabase.Query(String.Format("SELECT questid, specialflags FROM queststarters s LEFT JOIN quests q ON (s.questid=q.id) WHERE s.type = {0} AND s.typeid = {1} AND q.Level_Start <= {2} AND (q.Level_Normal = -1 OR q.Level_Normal > {6}) AND (q.Required_Race = 0 OR (Required_Race & {4}) > 0) AND (q.Required_Class = 0 OR (Required_Class & {5}) > 0) " & _
                "AND NOT EXISTS(SELECT * FROM " & characterQuestDB & ".characters_quests WHERE char_guid = {3} AND quest_status <> -2 AND quest_id = q.id) AND (q.Required_Quest1 = 0 OR EXISTS(SELECT * FROM " & characterQuestDB & ".characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest1)) " & _
                "AND (q.Required_Quest2 = 0 OR EXISTS(SELECT * FROM " & characterQuestDB & ".characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest2)) AND (q.Required_Quest3 = 0 OR EXISTS(SELECT * FROM " & characterQuestDB & ".characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest3)) " & _
                "AND (q.Required_Quest4 = 0 OR EXISTS(SELECT * FROM " & characterQuestDB & ".characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest4));", _
                CType(QuestGiverType.QUEST_OBJECTTYPE_CREATURE, Byte), CType(WORLD_CREATUREs(cGUID), CreatureObject).ID, c.Level, c.GUID, c.RaceMask, c.ClassMask, c.Level - 6), MySQLQuery)
            Else
                'Database.Query(String.Format("SELECT questid, specialflags FROM queststarters s LEFT JOIN quests q ON (s.questid=q.id) WHERE s.type = {0} AND s.typeid = {1} AND q.Level_Start <= {2} AND (q.Level_Normal = -1 OR q.Level_Normal > {6}) AND (q.Required_Race = 0 OR (Required_Race & {4}) > 0) AND (q.Required_Class = 0 OR (Required_Class & {5}) > 0) " & _
                '"AND NOT EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status <> -2 AND quest_id = q.id) AND (q.Required_Quest1 = 0 OR EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest1)) " & _
                '"AND (q.Required_Quest2 = 0 OR EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest2)) AND (q.Required_Quest3 = 0 OR EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest3)) " & _
                '"AND (q.Required_Quest4 = 0 OR EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest4));", _
                'CType(QuestGiverType.QUEST_OBJECTTYPE_GAMEOBJECT, Byte), CType(WORLD_GAMEOBJECTs(cGUID), GameObjectObject).ID, c.Level, c.GUID, c.RaceMask, c.ClassMask, c.Level - 6), MySQLQuery)
                WorldDatabase.Query(String.Format("SELECT questid, specialflags FROM queststarters s LEFT JOIN quests q ON (s.questid=q.id) WHERE s.type = {0} AND s.typeid = {1} AND q.Level_Start <= {2} AND (q.Level_Normal = -1 OR q.Level_Normal > {6}) AND (q.Required_Race = 0 OR (Required_Race & {4}) > 0) AND (q.Required_Class = 0 OR (Required_Class & {5}) > 0) " & _
                "AND NOT EXISTS(SELECT * FROM " & characterQuestDB & ".characters_quests WHERE char_guid = {3} AND quest_status <> -2 AND quest_id = q.id) AND (q.Required_Quest1 = 0 OR EXISTS(SELECT * FROM " & characterQuestDB & ".characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest1)) " & _
                "AND (q.Required_Quest2 = 0 OR EXISTS(SELECT * FROM " & characterQuestDB & ".characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest2)) AND (q.Required_Quest3 = 0 OR EXISTS(SELECT * FROM " & characterQuestDB & ".characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest3)) " & _
                "AND (q.Required_Quest4 = 0 OR EXISTS(SELECT * FROM " & characterQuestDB & ".characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest4));", _
                CType(QuestGiverType.QUEST_OBJECTTYPE_GAMEOBJECT, Byte), CType(WORLD_GAMEOBJECTs(cGUID), GameObjectObject).ID, c.Level, c.GUID, c.RaceMask, c.ClassMask, c.Level - 6), MySQLQuery)
            End If
            If MySQLQuery.Rows.Count = 0 Then
                If Status = QuestgiverStatus.DIALOG_STATUS_NONE Then
                    'DONE: Do SQL query for gray quests
                    Dim MySQLQueryForGray As New DataTable
                    If GuidIsCreature(cGUID) Then
                        'Database.Query(String.Format("SELECT questid FROM queststarters s LEFT JOIN quests q ON (s.questid=q.id) WHERE s.type = {0} AND s.typeid = {1} AND q.Level_Start < {2} AND (q.Level_Normal = -1 OR q.Level_Normal > {6}) AND (q.Required_Race = 0 OR (Required_Race & {4}) > 0) AND (q.Required_Class = 0 OR (Required_Class & {5}) > 0) " & _
                        '"AND NOT EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_id = q.id) AND (q.Required_Quest1 = 0 OR EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest1)) " & _
                        '"AND (q.Required_Quest2 = 0 OR EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest2)) AND (q.Required_Quest3 = 0 OR EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest3)) " & _
                        '"AND (q.Required_Quest4 = 0 OR EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest4));", _
                        'CType(QuestGiverType.QUEST_OBJECTTYPE_CREATURE, Byte), CType(WORLD_CREATUREs(cGUID), CreatureObject).ID, c.Level + 6, c.GUID, c.RaceMask, c.ClassMask, c.Level - 6), MySQLQueryForGray)
                        WorldDatabase.Query(String.Format("SELECT questid FROM queststarters s LEFT JOIN quests q ON (s.questid=q.id) WHERE s.type = {0} AND s.typeid = {1} AND q.Level_Start < {2} AND (q.Level_Normal = -1 OR q.Level_Normal > {6}) AND (q.Required_Race = 0 OR (Required_Race & {4}) > 0) AND (q.Required_Class = 0 OR (Required_Class & {5}) > 0) " & _
                        "AND NOT EXISTS(SELECT * FROM " & characterQuestDB & ".characters_quests WHERE char_guid = {3} AND quest_id = q.id) AND (q.Required_Quest1 = 0 OR EXISTS(SELECT * FROM " & characterQuestDB & ".characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest1)) " & _
                        "AND (q.Required_Quest2 = 0 OR EXISTS(SELECT * FROM " & characterQuestDB & ".characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest2)) AND (q.Required_Quest3 = 0 OR EXISTS(SELECT * FROM " & characterQuestDB & ".characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest3)) " & _
                        "AND (q.Required_Quest4 = 0 OR EXISTS(SELECT * FROM " & characterQuestDB & ".characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest4));", _
                        CType(QuestGiverType.QUEST_OBJECTTYPE_CREATURE, Byte), CType(WORLD_CREATUREs(cGUID), CreatureObject).ID, c.Level + 6, c.GUID, c.RaceMask, c.ClassMask, c.Level - 6), MySQLQueryForGray)
                    Else
                        'Database.Query(String.Format("SELECT questid FROM queststarters s LEFT JOIN quests q ON (s.questid=q.id) WHERE s.type = {0} AND s.typeid = {1} AND q.Level_Start < {2} AND (q.Level_Normal = -1 OR q.Level_Normal > {6}) AND (q.Required_Race = 0 OR (Required_Race & {4}) > 0) AND (q.Required_Class = 0 OR (Required_Class & {5}) > 0) " & _
                        '"AND NOT EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_id = q.id) AND (q.Required_Quest1 = 0 OR EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest1)) " & _
                        '"AND (q.Required_Quest2 = 0 OR EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest2)) AND (q.Required_Quest3 = 0 OR EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest3)) " & _
                        '"AND (q.Required_Quest4 = 0 OR EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest4));", _
                        'CType(QuestGiverType.QUEST_OBJECTTYPE_GAMEOBJECT, Byte), CType(WORLD_GAMEOBJECTs(cGUID), GameObjectObject).ID, c.Level + 6, c.GUID, c.RaceMask, c.ClassMask, c.Level - 6), MySQLQueryForGray)
                        WorldDatabase.Query(String.Format("SELECT questid FROM queststarters s LEFT JOIN quests q ON (s.questid=q.id) WHERE s.type = {0} AND s.typeid = {1} AND q.Level_Start < {2} AND (q.Level_Normal = -1 OR q.Level_Normal > {6}) AND (q.Required_Race = 0 OR (Required_Race & {4}) > 0) AND (q.Required_Class = 0 OR (Required_Class & {5}) > 0) " & _
                        "AND NOT EXISTS(SELECT * FROM " & characterQuestDB & ".characters_quests WHERE char_guid = {3} AND quest_id = q.id) AND (q.Required_Quest1 = 0 OR EXISTS(SELECT * FROM " & characterQuestDB & ".characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest1)) " & _
                        "AND (q.Required_Quest2 = 0 OR EXISTS(SELECT * FROM " & characterQuestDB & ".characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest2)) AND (q.Required_Quest3 = 0 OR EXISTS(SELECT * FROM " & characterQuestDB & ".characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest3)) " & _
                        "AND (q.Required_Quest4 = 0 OR EXISTS(SELECT * FROM " & characterQuestDB & ".characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest4));", _
                        CType(QuestGiverType.QUEST_OBJECTTYPE_GAMEOBJECT, Byte), CType(WORLD_GAMEOBJECTs(cGUID), GameObjectObject).ID, c.Level + 6, c.GUID, c.RaceMask, c.ClassMask, c.Level - 6), MySQLQueryForGray)
                    End If

                    If MySQLQueryForGray.Rows.Count > 0 Then
                        Status = QuestgiverStatus.DIALOG_STATUS_UNAVAILABLE
                    Else
                        'DONE: Do SQL query for low level quests
                        Dim MySQLQueryForLowLevel As New DataTable
                        If GuidIsCreature(cGUID) Then
                            'Database.Query(String.Format("SELECT questid, specialflags FROM queststarters s LEFT JOIN quests q ON (s.questid=q.id) WHERE s.type = {0} AND s.typeid = {1} AND q.Level_Start < {2} AND (q.Required_Race = 0 OR (Required_Race & {4}) > 0) AND (q.Required_Class = 0 OR (Required_Class & {5}) > 0) " & _
                            '"AND NOT EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status <> -2 AND quest_id = q.id) AND (q.Required_Quest1 = 0 OR EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest1)) " & _
                            '"AND (q.Required_Quest2 = 0 OR EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest2)) AND (q.Required_Quest3 = 0 OR EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest3)) " & _
                            '"AND (q.Required_Quest4 = 0 OR EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest4));", _
                            'CType(QuestGiverType.QUEST_OBJECTTYPE_CREATURE, Byte), CType(WORLD_CREATUREs(cGUID), CreatureObject).ID, c.Level + 6, c.GUID, c.RaceMask, c.ClassMask), MySQLQueryForLowLevel)
                            WorldDatabase.Query(String.Format("SELECT questid, specialflags FROM queststarters s LEFT JOIN quests q ON (s.questid=q.id) WHERE s.type = {0} AND s.typeid = {1} AND q.Level_Start < {2} AND (q.Required_Race = 0 OR (Required_Race & {4}) > 0) AND (q.Required_Class = 0 OR (Required_Class & {5}) > 0) " & _
                            "AND NOT EXISTS(SELECT * FROM " & characterQuestDB & ".characters_quests WHERE char_guid = {3} AND quest_status <> -2 AND quest_id = q.id) AND (q.Required_Quest1 = 0 OR EXISTS(SELECT * FROM " & characterQuestDB & ".characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest1)) " & _
                            "AND (q.Required_Quest2 = 0 OR EXISTS(SELECT * FROM " & characterQuestDB & ".characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest2)) AND (q.Required_Quest3 = 0 OR EXISTS(SELECT * FROM " & characterQuestDB & ".characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest3)) " & _
                            "AND (q.Required_Quest4 = 0 OR EXISTS(SELECT * FROM " & characterQuestDB & ".characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest4));", _
                            CType(QuestGiverType.QUEST_OBJECTTYPE_CREATURE, Byte), CType(WORLD_CREATUREs(cGUID), CreatureObject).ID, c.Level + 6, c.GUID, c.RaceMask, c.ClassMask), MySQLQueryForLowLevel)
                        Else
                            'Database.Query(String.Format("SELECT questid, specialflags FROM queststarters s LEFT JOIN quests q ON (s.questid=q.id) WHERE s.type = {0} AND s.typeid = {1} AND q.Level_Start < {2} AND (q.Required_Race = 0 OR (Required_Race & {4}) > 0) AND (q.Required_Class = 0 OR (Required_Class & {5}) > 0) " & _
                            '"AND NOT EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status <> -2 AND quest_id = q.id) AND (q.Required_Quest1 = 0 OR EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest1)) " & _
                            '"AND (q.Required_Quest2 = 0 OR EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest2)) AND (q.Required_Quest3 = 0 OR EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest3)) " & _
                            '"AND (q.Required_Quest4 = 0 OR EXISTS(SELECT * FROM characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest4));", _
                            'CType(QuestGiverType.QUEST_OBJECTTYPE_GAMEOBJECT, Byte), CType(WORLD_GAMEOBJECTs(cGUID), GameObjectObject).ID, c.Level + 6, c.GUID, c.RaceMask, c.ClassMask), MySQLQueryForLowLevel)
                            WorldDatabase.Query(String.Format("SELECT questid, specialflags FROM queststarters s LEFT JOIN quests q ON (s.questid=q.id) WHERE s.type = {0} AND s.typeid = {1} AND q.Level_Start < {2} AND (q.Required_Race = 0 OR (Required_Race & {4}) > 0) AND (q.Required_Class = 0 OR (Required_Class & {5}) > 0) " & _
                            "AND NOT EXISTS(SELECT * FROM " & characterQuestDB & ".characters_quests WHERE char_guid = {3} AND quest_status <> -2 AND quest_id = q.id) AND (q.Required_Quest1 = 0 OR EXISTS(SELECT * FROM " & characterQuestDB & ".characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest1)) " & _
                            "AND (q.Required_Quest2 = 0 OR EXISTS(SELECT * FROM " & characterQuestDB & ".characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest2)) AND (q.Required_Quest3 = 0 OR EXISTS(SELECT * FROM " & characterQuestDB & ".characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest3)) " & _
                            "AND (q.Required_Quest4 = 0 OR EXISTS(SELECT * FROM " & characterQuestDB & ".characters_quests WHERE char_guid = {3} AND quest_status = -1 AND quest_id = q.Required_Quest4));", _
                            CType(QuestGiverType.QUEST_OBJECTTYPE_GAMEOBJECT, Byte), CType(WORLD_GAMEOBJECTs(cGUID), GameObjectObject).ID, c.Level + 6, c.GUID, c.RaceMask, c.ClassMask), MySQLQueryForLowLevel)
                        End If

                        If MySQLQueryForLowLevel.Rows.Count > 0 Then
                            If CByte(MySQLQueryForLowLevel.Rows(0).Item("specialflags")) And QuestSpecialFlag.QUEST_SPECIALFLAGS_REPEATABLE Then
                                Status = QuestgiverStatus.DIALOG_STATUS_REPEATABLE_FINISHED
                            Else
                                Status = QuestgiverStatus.DIALOG_STATUS_CHAT
                            End If
                        End If
                        MySQLQueryForLowLevel.Clear()
                    End If
                    MySQLQueryForGray.Clear()
                End If
            Else
                If CByte(MySQLQuery.Rows(0).Item("specialflags")) And QuestSpecialFlag.QUEST_SPECIALFLAGS_REPEATABLE Then
                    Status = QuestgiverStatus.DIALOG_STATUS_REPEATABLE
                Else
                    Status = QuestgiverStatus.DIALOG_STATUS_AVAILABLE
                End If
            End If
            MySQLQuery.Clear()
        End If

        Return Status
    End Function
    Public Sub On_CMSG_QUESTGIVER_STATUS_QUERY(ByRef packet As PacketClass, ByRef Client As ClientClass)
        Try
            'If (packet.Data.Length - 1) < 7 Then Exit Sub
            packet.GetInt16()
            Dim GUID As ULong = packet.GetUInt64
            Dim status As QuestgiverStatus = GetQuestgiverStatus(Client.Character, GUID)

            Dim response As New PacketClass(OPCODES.SMSG_QUESTGIVER_STATUS)
            response.AddUInt64(GUID)
            response.AddInt8(status)
            Client.Send(response)
            response.Dispose()
        Catch e As Exception
            Log.WriteLine(LogType.CRITICAL, "Error in questgiver status query.{0}", vbNewLine & e.ToString)
        End Try
    End Sub

    Public Sub On_CMSG_QUESTGIVER_STATUS_MULTIPLE_QUERY(ByRef packet As PacketClass, ByRef Client As ClientClass)
        Try
            Dim Count As Integer = 0, status As QuestgiverStatus
            Dim response As New PacketClass(OPCODES.SMSG_QUESTGIVER_STATUS_MULTIPLE)
            response.AddInt32(Count) 'Count updated later

            Dim List() As ULong = Client.Character.creaturesNear.ToArray
            For Each cGUID As ULong In List
                If WORLD_CREATUREs.ContainsKey(cGUID) AndAlso (CType(WORLD_CREATUREs(cGUID), CreatureObject).CreatureInfo.cNpcFlags And NPCFlags.UNIT_NPC_FLAG_QUESTGIVER) Then
                    'DONE: Send creature questgivers
                    If Client.Character.GetReaction(WORLD_CREATUREs(cGUID).Faction) >= TReaction.NEUTRAL Then
                        status = GetQuestgiverStatus(Client.Character, cGUID)
                        response.AddUInt64(cGUID)
                        response.AddInt8(status)
                        Count += 1
                    End If
                End If
            Next

            List = Client.Character.gameObjectsNear.ToArray
            For Each gGUID As ULong In List
                If WORLD_GAMEOBJECTs.ContainsKey(gGUID) Then
                    'DONE: Send gameobject questgivers
                    If WORLD_GAMEOBJECTs(gGUID).Type = GameObjectType.GAMEOBJECT_TYPE_QUESTGIVER AndAlso Client.Character.GetReaction(WORLD_GAMEOBJECTs(gGUID).Faction) >= TReaction.NEUTRAL Then
                        status = GetQuestgiverStatus(Client.Character, gGUID)
                        response.AddUInt64(gGUID)
                        response.AddInt8(status)
                        Count += 1
                    End If
                    'DONE: Activate/Deactivate chests used for quests
                    If CType(WORLD_GAMEOBJECTs(gGUID), GameObjectObject).Type = GameObjectType.GAMEOBJECT_TYPE_CHEST Then
                        Dim UsedForQuest As Byte = IsGameObjectUsedForQuest(WORLD_GAMEOBJECTs(gGUID), Client.Character)
                        If UsedForQuest > 0 Then
                            Dim ChestActivate As New PacketClass(OPCODES.SMSG_UPDATE_OBJECT)
                            ChestActivate.AddInt32(1)
                            'ChestActivate.AddInt8(0)
                            Dim UpdateData As New UpdateClass
                            WORLD_GAMEOBJECTs(gGUID).Flags = WORLD_GAMEOBJECTs(gGUID).Flags Or 4
                            If UsedForQuest = 2 Then
                                UpdateData.SetUpdateFlag(EGameObjectFields.GAMEOBJECT_DYNAMIC, 9)
                            Else
                                UpdateData.SetUpdateFlag(EGameObjectFields.GAMEOBJECT_DYNAMIC, 0)
                            End If
                            UpdateData.SetUpdateFlag(EGameObjectFields.GAMEOBJECT_FLAGS, WORLD_GAMEOBJECTs(gGUID).Flags)
                            UpdateData.AddToPacket(ChestActivate, ObjectUpdateType.UPDATETYPE_VALUES, CType(WORLD_GAMEOBJECTs(gGUID), GameObjectObject))
                            Client.Send(ChestActivate)
                            UpdateData.Dispose()
                            ChestActivate.Dispose()
                        End If
                    End If
                End If
            Next

            If Count > 0 Then
                response.AddInt32(Count, 4) 'Update the count
                Client.Send(response)
            End If

            response.Dispose()
        Catch e As Exception
            Log.WriteLine(LogType.CRITICAL, "Error in questgiver multiple status query.{0}", vbNewLine & e.ToString)
        End Try
    End Sub

    Public Sub On_CMSG_QUESTGIVER_HELLO(ByRef packet As PacketClass, ByRef Client As ClientClass)
        Try
            If (packet.Data.Length - 1) < 13 Then Exit Sub
            packet.GetInt16()
            Dim GUID As ULong = packet.GetUInt64

            Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_QUESTGIVER_HELLO [GUID={2:X}]", Client.IP, Client.Port, GUID)

            If CType(CREATURESDatabase(CType(WORLD_CREATUREs(GUID), CreatureObject).ID), CreatureInfo).TalkScript.OnQuestHello(Client.Character, GUID) Then
                SendQuestMenu(Client.Character, GUID, "I have some tasks for you, $N.")
            End If
        Catch e As Exception
            Log.WriteLine(LogType.CRITICAL, "Error when sending quest menu.{0}", vbNewLine & e.ToString)
        End Try
    End Sub
    Public Sub On_CMSG_QUESTGIVER_QUERY_QUEST(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 17 Then Exit Sub
        packet.GetInt16()
        Dim GUID As ULong = packet.GetUInt64
        Dim QuestID As Integer = packet.GetInt32

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_QUESTGIVER_QUERY_QUEST [GUID={2:X} QuestID={3}]", Client.IP, Client.Port, GUID, QuestID)

        Client.Character.TalkCurrentQuest = New QuestInfo(QuestID)
        SendQuestDetails(Client, Client.Character.TalkCurrentQuest, GUID, True)
    End Sub
    Public Sub On_CMSG_QUESTGIVER_ACCEPT_QUEST(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 17 Then Exit Sub
        packet.GetInt16()
        Dim GUID As ULong = packet.GetUInt64
        Dim QuestID As Integer = packet.GetInt32

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_QUESTGIVER_ACCEPT_QUEST [GUID={2:X} QuestID={3}]", Client.IP, Client.Port, GUID, QuestID)

        'Load quest data
        If Client.Character.TalkCurrentQuest.ID <> QuestID Then Client.Character.TalkCurrentQuest = New QuestInfo(QuestID)

        If Client.Character.TalkCanAccept(Client.Character.TalkCurrentQuest) Then
            If Client.Character.TalkAddQuest(Client.Character.TalkCurrentQuest) Then
                If GuidIsPlayer(GUID) Then
                    Dim response As New PacketClass(OPCODES.MSG_QUEST_PUSH_RESULT)
                    response.AddUInt64(Client.Character.GUID)
                    response.AddInt8(QuestPartyPushError.QUEST_PARTY_MSG_ACCEPT_QUEST)
                    response.AddInt32(0)
                    CHARACTERs(GUID).Client.Send(response)
                    response.Dispose()
                Else
                    Dim status As QuestgiverStatus = GetQuestgiverStatus(Client.Character, GUID)
                    Dim response As New PacketClass(OPCODES.SMSG_QUESTGIVER_STATUS)
                    response.AddUInt64(GUID)
                    response.AddInt32(status)
                    Client.Send(response)
                    response.Dispose()
                End If
            Else
                Dim response As New PacketClass(OPCODES.SMSG_QUESTLOG_FULL)
                Client.Send(response)
                response.Dispose()
            End If
        End If
    End Sub
    Public Sub On_CMSG_QUESTLOG_REMOVE_QUEST(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 6 Then Exit Sub
        packet.GetInt16()
        Dim Slot As Byte = packet.GetInt8

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_QUESTLOG_REMOVE_QUEST [Slot={2}]", Client.IP, Client.Port, Slot)

        Client.Character.TalkDeleteQuest(Slot)
    End Sub

    Public Sub On_CMSG_QUEST_QUERY(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 9 Then Exit Sub
        packet.GetInt16()
        Dim QuestID As Integer = packet.GetInt32

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_QUEST_QUERY [QuestID={2}]", Client.IP, Client.Port, QuestID)

        If Client.Character.TalkCurrentQuest Is Nothing Then
            Dim tmpQuest As New QuestInfo(QuestID)
            SendQuest(Client, tmpQuest)
            Exit Sub
        End If

        If Client.Character.TalkCurrentQuest.ID = QuestID Then
            SendQuest(Client, Client.Character.TalkCurrentQuest)
        Else
            Dim tmpQuest As New QuestInfo(QuestID)
            SendQuest(Client, tmpQuest)
        End If
    End Sub

    Public Sub CompleteQuest(ByVal c As CharacterObject, ByVal QuestID As Integer, ByVal QuestGiverGUID As ULong)
        Dim i As Integer
        For i = 0 To QUEST_SLOTS
            If Not c.TalkQuests(i) Is Nothing Then
                If c.TalkQuests(i).ID = QuestID Then

                    'Load quest data
                    If c.TalkCurrentQuest Is Nothing Then c.TalkCurrentQuest = New QuestInfo(QuestID)
                    If c.TalkCurrentQuest.ID <> QuestID Then c.TalkCurrentQuest = New QuestInfo(QuestID)


                    If c.TalkQuests(i).Complete Then
                        'DONE: Show completion dialog
                        If (c.TalkQuests(i).ObjectiveFlags And QuestObjectiveFlag.QUEST_OBJECTIVE_ITEM) Then
                            'Request items
                            SendQuestRequireItems(c.Client, c.TalkCurrentQuest, QuestGiverGUID, c.TalkQuests(i))
                        Else
                            SendQuestReward(c.Client, c.TalkCurrentQuest, QuestGiverGUID, c.TalkQuests(i))
                        End If
                    Else
                        'DONE: Just show incomplete text with disabled complete button
                        SendQuestRequireItems(c.Client, c.TalkCurrentQuest, QuestGiverGUID, c.TalkQuests(i))
                    End If


                    Exit For
                End If
            End If
        Next
    End Sub
    Public Sub On_CMSG_QUESTGIVER_COMPLETE_QUEST(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 17 Then Exit Sub
        packet.GetInt16()
        Dim GUID As ULong = packet.GetUInt64
        Dim QuestID As Integer = packet.GetInt32

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_QUESTGIVER_COMPLETE_QUEST [GUID={2:X} Quest={3}]", Client.IP, Client.Port, GUID, QuestID)

        CompleteQuest(Client.Character, QuestID, GUID)
    End Sub
    Public Sub On_CMSG_QUESTGIVER_REQUEST_REWARD(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 17 Then Exit Sub
        packet.GetInt16()
        Dim GUID As ULong = packet.GetUInt64
        Dim QuestID As Integer = packet.GetInt32

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_QUESTGIVER_REQUEST_REWARD [GUID={2:X} Quest={3}]", Client.IP, Client.Port, GUID, QuestID)

        Dim i As Integer
        For i = 0 To QUEST_SLOTS
            If Client.Character.TalkQuests(i).ID = QuestID AndAlso Client.Character.TalkQuests(i).Complete Then

                'Load quest data
                If Client.Character.TalkCurrentQuest.ID <> QuestID Then Client.Character.TalkCurrentQuest = New QuestInfo(QuestID)
                SendQuestReward(Client, Client.Character.TalkCurrentQuest, GUID, Client.Character.TalkQuests(i))

                Exit For
            End If
        Next

    End Sub
    Public Sub On_CMSG_QUESTGIVER_CHOOSE_REWARD(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 21 Then Exit Sub
        packet.GetInt16()
        Dim GUID As ULong = packet.GetUInt64
        Dim QuestID As Integer = packet.GetInt32
        Dim RewardIndex As Integer = packet.GetInt32
        Dim i As Integer

        Try
            Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_QUESTGIVER_CHOOSE_REWARD [GUID={2:X} Quest={3} Reward={4}]", Client.IP, Client.Port, GUID, QuestID, RewardIndex)

            'Load quest data
            If Client.Character.TalkCurrentQuest Is Nothing Then Client.Character.TalkCurrentQuest = New QuestInfo(QuestID)
            If Client.Character.TalkCurrentQuest.ID <> QuestID Then Client.Character.TalkCurrentQuest = New QuestInfo(QuestID)

            'DONE: Removing required gold
            If (Client.Character.Copper - Client.Character.TalkCurrentQuest.ObjectivesGold) >= 0 Then
                'NOTE: Update flag set below
                Client.Character.Copper -= Client.Character.TalkCurrentQuest.ObjectivesGold
            Else
                Dim errorPacket As New PacketClass(OPCODES.SMSG_QUESTGIVER_QUEST_INVALID)
                errorPacket.AddInt32(QuestInvalidError.INVALIDREASON_DONT_HAVE_REQ_MONEY)
                Client.Send(errorPacket)
                errorPacket.Dispose()
                Exit Sub
            End If

            'DONE: Removing required items
            For i = 0 To QUEST_OBJECTIVES_COUNT
                If Client.Character.TalkCurrentQuest.ObjectivesItem(i) <> 0 Then
                    If Not Client.Character.ItemCONSUME(Client.Character.TalkCurrentQuest.ObjectivesItem(i), Client.Character.TalkCurrentQuest.ObjectivesItem_Count(i)) Then
                        'DONE: Restore gold
                        Client.Character.Copper += Client.Character.TalkCurrentQuest.ObjectivesGold
                        'TODO: Restore items (not needed?)
                        Dim errorPacket As New PacketClass(OPCODES.SMSG_QUESTGIVER_QUEST_INVALID)
                        errorPacket.AddInt32(QuestInvalidError.INVALIDREASON_DONT_HAVE_REQ_ITEMS)
                        Client.Send(errorPacket)
                        errorPacket.Dispose()
                        Exit Sub
                    End If
                Else
                    Exit For
                End If
            Next


            'DONE: Adding reward choice
            If Client.Character.TalkCurrentQuest.RewardItems(RewardIndex) <> 0 Then
                Dim tmpItem As New ItemObject(Client.Character.TalkCurrentQuest.RewardItems(RewardIndex), Client.Character.GUID)
                tmpItem.StackCount = Client.Character.TalkCurrentQuest.RewardItems_Count(RewardIndex)
                If Not Client.Character.ItemADD(tmpItem) Then
                    tmpItem.Delete()
                    'DONE: Inventory full sent form SetItemSlot
                    Exit Sub
                Else
                    Client.Character.LogLootItem(tmpItem, 1, True, False)
                End If
            End If

            'DONE: Adding gold
            Client.Character.Copper += Client.Character.TalkCurrentQuest.RewardGold
            Client.Character.SetUpdateFlag(EPlayerFields.PLAYER_FIELD_COINAGE, Client.Character.Copper)

            'DONE: Add honor
            If Client.Character.TalkCurrentQuest.RewardHonor <> 0 Then
                Client.Character.HonorCurrency += Client.Character.TalkCurrentQuest.RewardHonor
                Client.Character.SetUpdateFlag(EPlayerFields.PLAYER_FIELD_HONOR_CURRENCY, Client.Character.HonorCurrency)
            End If

            'DONE: Learning reward spell
            If Client.Character.TalkCurrentQuest.RewardSpell <> 0 Then Client.Character.LearnSpell(Client.Character.TalkCurrentQuest.RewardSpell)

            'DONE: Cast spell
            If Client.Character.TalkCurrentQuest.RewardSpellCast <> 0 Then
                Dim t As New SpellTargets
                t.SetTarget_UNIT(Client.Character)
                CType(SPELLs(Client.Character.TalkCurrentQuest.RewardSpellCast), SpellInfo).Cast(1, WORLD_CREATUREs(GUID), t)
            End If

            'DONE: Remove quest
            For i = 0 To QUEST_SLOTS
                If Not Client.Character.TalkQuests Is Nothing Then
                    If Client.Character.TalkQuests(i).ID = Client.Character.TalkCurrentQuest.ID Then
                        Client.Character.TalkCompleteQuest(i)
                        Exit For
                    End If
                End If
            Next

            'DONE: XP Calculations
            Dim xp As Integer = Client.Character.TalkCurrentQuest.RewardXP
            Dim gold As Integer = Client.Character.TalkCurrentQuest.RewardGold
            If Client.Character.Level >= MAX_LEVEL Then
                gold += xp
                xp = 0
            Else
                Select Case Client.Character.Level
                    Case Client.Character.TalkCurrentQuest.Level_Normal + 6
                        xp = Fix(xp * 0.8 / 5) * 5
                    Case Client.Character.TalkCurrentQuest.Level_Normal + 7
                        xp = Fix(xp * 0.6 / 5) * 5
                    Case Client.Character.TalkCurrentQuest.Level_Normal + 8
                        xp = Fix(xp * 0.4 / 5) * 5
                    Case Client.Character.TalkCurrentQuest.Level_Normal + 9
                        xp = Fix(xp * 0.2 / 5) * 5
                    Case Is > Client.Character.TalkCurrentQuest.Level_Normal + 10
                        xp = Fix(xp * 0.1 / 5) * 5
                End Select
            End If

            'DONE: Adding XP
            Client.Character.AddXP(Client.Character.TalkCurrentQuest.RewardXP)

            SendQuestComplete(Client, Client.Character.TalkCurrentQuest, xp, gold)

            'DONE: Follow-up quests (no requirements checked?)
            If Client.Character.TalkCurrentQuest.NextQuest <> 0 Then
                Client.Character.TalkCurrentQuest = New QuestInfo(Client.Character.TalkCurrentQuest.NextQuest)
                SendQuestDetails(Client, Client.Character.TalkCurrentQuest, GUID, True)
            End If

        Catch e As Exception
            Log.WriteLine(LogType.CRITICAL, "Error while choosing reward.{0}", vbNewLine & e.ToString)
        End Try
    End Sub



    Const QUEST_SHARING_DISTANCE As Integer = 10
    Public Sub On_CMSG_PUSHQUESTTOPARTY(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 9 Then Exit Sub
        packet.GetInt16()
        Dim questID As Integer = packet.GetInt32

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_PUSHQUESTTOPARTY [{2}]", Client.IP, Client.Port, questID)

        If Client.Character.IsInGroup Then
            For Each GUID As ULong In Client.Character.Group.LocalMembers
                If GUID = Client.Character.GUID Then Continue For

                With CHARACTERs(GUID)

                    Dim response As New PacketClass(OPCODES.MSG_QUEST_PUSH_RESULT)
                    response.AddUInt64(GUID)
                    response.AddInt32(QuestPartyPushError.QUEST_PARTY_MSG_SHARRING_QUEST)
                    response.AddInt8(0)
                    Client.Send(response)
                    response.Dispose()

                    Dim message As QuestPartyPushError = QuestPartyPushError.QUEST_PARTY_MSG_SHARRING_QUEST

                    'DONE: Check distance and ...
                    If (Math.Sqrt((.positionX - Client.Character.positionX) ^ 2 + (.positionY - Client.Character.positionY) ^ 2) > QUEST_SHARING_DISTANCE) Then
                        message = QuestPartyPushError.QUEST_PARTY_MSG_TO_FAR
                    ElseIf .IsQuestInProgress(questID) Then
                        message = QuestPartyPushError.QUEST_PARTY_MSG_HAVE_QUEST
                    ElseIf .IsQuestCompleted(questID) Then
                        message = QuestPartyPushError.QUEST_PARTY_MSG_FINISH_QUEST
                    Else
                        If (.TalkCurrentQuest Is Nothing) OrElse (.TalkCurrentQuest.ID <> questID) Then .TalkCurrentQuest = New QuestInfo(questID)
                        If .TalkCanAccept(.TalkCurrentQuest) Then
                            SendQuestDetails(.Client, .TalkCurrentQuest, Client.Character.GUID, True)
                        Else
                            message = QuestPartyPushError.QUEST_PARTY_MSG_CANT_TAKE_QUEST
                        End If
                    End If


                    'DONE: Send error if present
                    If message <> QuestPartyPushError.QUEST_PARTY_MSG_SHARRING_QUEST Then
                        Dim errorPacket As New PacketClass(OPCODES.MSG_QUEST_PUSH_RESULT)
                        errorPacket.AddUInt64(.GUID)
                        errorPacket.AddInt32(message)
                        errorPacket.AddInt8(0)
                        Client.Send(errorPacket)
                        errorPacket.Dispose()
                    End If

                End With
            Next

        End If
    End Sub
    Public Sub On_MSG_QUEST_PUSH_RESULT(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 14 Then Exit Sub
        packet.GetInt16()
        Dim GUID As ULong = packet.GetUInt64
        Dim Message As QuestPartyPushError = packet.GetInt8

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] MSG_QUEST_PUSH_RESULT [{2:X} {3}]", Client.IP, Client.Port, GUID, Message)

        'Dim response As New PacketClass(OPCODES.MSG_QUEST_PUSH_RESULT)
        'response.AddUInt64(GUID)
        'response.AddInt8(QuestPartyPushError.QUEST_PARTY_MSG_ACCEPT_QUEST)
        'response.AddInt32(0)
        'Client.Send(response)
        'response.Dispose()
    End Sub


#End Region



End Module
