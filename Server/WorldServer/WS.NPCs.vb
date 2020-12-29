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
Imports mangosVB.Common.BaseWriter

Public Module WS_NPCs

#Region "Constants"
    Enum SELL_ERROR As Byte
        SELL_ERR_CANT_FIND_ITEM = 1
        SELL_ERR_CANT_SELL_ITEM = 2
        SELL_ERR_CANT_FIND_VENDOR = 3
    End Enum
    Enum BUY_ERROR As Byte
        'SMSG_BUY_FAILED error
        '0: cant find item
        '1: item already selled
        '2: not enought money
        '4: seller(dont Like u)
        '5: distance too far
        '8: cant carry more
        '11: level(require)
        '12: reputation(require)

        BUY_ERR_CANT_FIND_ITEM = 0
        BUY_ERR_ITEM_ALREADY_SOLD = 1
        BUY_ERR_NOT_ENOUGHT_MONEY = 2
        BUY_ERR_SELLER_DONT_LIKE_YOU = 4
        BUY_ERR_DISTANCE_TOO_FAR = 5
        BUY_ERR_CANT_CARRY_MORE = 8
        BUY_ERR_LEVEL_REQUIRE = 11
        BUY_ERR_REPUTATION_REQUIRE = 12
    End Enum
#End Region

    'TODO: MSG_LIST_STABLED_PETS

#Region "Trainers"

    Public Sub On_CMSG_TRAINER_LIST(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 13 Then Exit Sub
        packet.GetInt16()
        Dim GUID As ULong = packet.GetUInt64

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_TRAINER_LIST [GUID={2}]", Client.IP, Client.Port, GUID)

        SendTrainerList(Client.Character, GUID)
    End Sub
    Public Sub On_CMSG_TRAINER_BUY_SPELL(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 17 Then Exit Sub
        packet.GetInt16()
        Dim cGUID As ULong = packet.GetUInt64
        Dim SpellID As Integer = packet.GetInt32

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_TRAINER_BUY_SPELL [GUID={2} Spell={3}]", Client.IP, Client.Port, cGUID, SpellID)

        Dim MySQLQuery As New DataTable
        Database.Query(String.Format("SELECT * FROM trainer_spells WHERE entry = {0} AND spellid = {1};", WORLD_CREATUREs(cGUID).ID, SpellID), MySQLQuery)
        If MySQLQuery.Rows.Count = 0 Then Exit Sub

        'DONE: Check requirements
        If Client.Character.Copper < CUInt(MySQLQuery.Rows(0).Item("spellcost")) Then Exit Sub
        If Client.Character.Level < CType(SPELLs(SpellID), SpellInfo).spellLevel Then Exit Sub
        If CInt(MySQLQuery.Rows(0).Item("reqspell")) <> 0 AndAlso Client.Character.HaveSpell(CInt(MySQLQuery.Rows(0).Item("reqspell"))) = False Then Exit Sub
        If CInt(MySQLQuery.Rows(0).Item("reqskill")) <> 0 AndAlso Client.Character.HaveSkill(CInt(MySQLQuery.Rows(0).Item("reqskill")), CInt(MySQLQuery.Rows(0).Item("reqskillvalue"))) = False Then Exit Sub

        'TODO: Check proffessions - only alowed to learn 2!

        Try
            'DONE: Get the money
            Client.Character.Copper -= MySQLQuery.Rows(0).Item("spellcost")
            Client.Character.SetUpdateFlag(EPlayerFields.PLAYER_FIELD_COINAGE, Client.Character.Copper)
            Client.Character.SendCharacterUpdate(False)

            ' Send Visual Effect For NPC
            Dim VisualNPC As New PacketClass(OPCODES.SMSG_PLAY_SPELL_VISUAL)
            VisualNPC.AddUInt64(cGUID)
            VisualNPC.AddInt32(&HB3)
            Client.Send(VisualNPC)
            VisualNPC.Dispose()

            ' Send Visual Effect For Character
            Dim VisualCharacter As New PacketClass(OPCODES.SMSG_PLAY_SPELL_IMPACT)
            VisualCharacter.AddUInt64(Client.Character.GUID)
            VisualCharacter.AddInt32(&H16A)
            Client.SendMultiplyPackets(VisualCharacter)
            Client.Character.SendToNearPlayers(VisualCharacter)
            VisualCharacter.Dispose()

            'TODO: May Have To Send Visuals To Nearby Players???

            ' Learn the spell
            Client.Character.LearnSpell(SpellID)
            'TODO: Add any spell effect or skills needed, this is still to be done.
            'DONE: Just cast the spell
            Dim tmpTargets As New SpellTargets
            Dim IsCast As Boolean = CType(MySQLQuery.Rows(0).Item("is_cast"), Boolean)
            If IsCast Then
                tmpTargets.unitTarget = Client.Character
                SPELLs(SpellID).Cast(1, CType(WORLD_CREATUREs(cGUID), CreatureObject), tmpTargets)
            End If

            'TODO: Add The Code To Delete Lower Ranks Of Spells When a Higher Rank Is Learned.

        Catch e As Exception
            Log.WriteLine(LogType.FAILED, "Training Spell Error: Unable to cast spell. [{0}:{1}]", vbNewLine, e.ToString)

            'TODO: Fix this opcode
            'Dim errorPacket As New PacketClass(OPCODES.SMSG_TRAINER_BUY_FAILED)
            'errorPacket.AddUInt64(cGUID)
            'errorPacket.AddInt32(SpellID)
            'Client.Send(errorPacket)
            'errorPacket.Dispose()
        End Try

        'DONE: Send response
        Dim response As New PacketClass(OPCODES.SMSG_TRAINER_BUY_SUCCEEDED)
        response.AddUInt64(cGUID)
        response.AddInt32(SpellID)
        Client.Send(response)
        response.Dispose()

    End Sub

    Public Sub SendTrainerList(ByRef c As CharacterObject, ByVal cGUID As ULong)

        'DONE: Query the database and sort spells
        Dim NeedToLearn As Boolean = False
        Dim noTrainID As Integer = 0
        Dim TrainerSQLQuery As New DataTable
        Dim SpellSQLQuery As New DataTable
        Dim npcTextSQLQuery As New DataTable
        Database.Query(String.Format("SELECT req_class, trainer_type, trainer_ui_window_message, cannot_train_gossip_textid FROM trainer_defs WHERE entry = {0}", WORLD_CREATUREs(cGUID).ID) & " LIMIT 1;", TrainerSQLQuery)
        Database.Query(String.Format("SELECT * FROM trainer_spells WHERE entry = {0};", WORLD_CREATUREs(cGUID).ID), SpellSQLQuery)
        noTrainID = CType(TrainerSQLQuery.Rows(0).Item("cannot_train_gossip_textid"), Integer)
        Database.Query(String.Format("SELECT * FROM npctext WHERE entry = {0};", noTrainID), npcTextSQLQuery)

        Dim SpellsList As New ArrayList
        For Each SellRow As DataRow In SpellSQLQuery.Rows
            'TODO: Add code for the Required Race.
            ''If (CType(SellRow.Item("requiredRace"), Integer) And (1 << (c.Race - 1)) <> 0) _
            If (CType(TrainerSQLQuery.Rows(0).Item("req_class"), Integer) = CType(c.Classe, Integer)) Or (CType(TrainerSQLQuery.Rows(0).Item("req_class"), Integer) = 0) Then
                ''If (CType(TrainerSQLQuery.Rows(0).Item("req_class"), Integer) And (1 << (c.Classe - 1)) <> 0) Then
                SpellsList.Add(SellRow)
            End If
        Next

        'DONE: No spells available -> wrong trainer type.
        'TODO: Need to fix this so it works right.
        If SpellsList.Count = 0 Then
            Dim npcText As New NPCText
            If npcTextSQLQuery.Rows.Count > 0 Then
                npcText.Count = 1
                npcText.TextID = noTrainID
                npcText.Probability(0) = npcTextSQLQuery.Rows(0).Item("prob0")
                npcText.TextLine1(0) = npcTextSQLQuery.Rows(0).Item("text0_0")
                npcText.TextLine2(0) = npcTextSQLQuery.Rows(0).Item("text0_1")
                npcText.Language(0) = npcTextSQLQuery.Rows(0).Item("lang0")
                npcText.EmoteDelay1(0) = npcTextSQLQuery.Rows(0).Item("em0_0")
                npcText.Emote1(0) = npcTextSQLQuery.Rows(0).Item("em0_1")
                npcText.EmoteDelay2(0) = npcTextSQLQuery.Rows(0).Item("em0_2")
                npcText.Emote2(0) = npcTextSQLQuery.Rows(0).Item("em0_3")
                npcText.EmoteDelay3(0) = npcTextSQLQuery.Rows(0).Item("em0_4")
                npcText.Emote3(0) = npcTextSQLQuery.Rows(0).Item("em0_5")
            Else
                npcText.Count = 1
                npcText.TextID = 0
                npcText.Probability(0) = 1
                npcText.TextLine1(0) = "Hey there, $N. How can I help you?"
                npcText.TextLine2(0) = "Hey there, $N. How can I help you?"
            End If

            SendNPCText(c.Client, npcText)

            c.SendGossip(cGUID, noTrainID)
            Exit Sub
        End If

        'DONE: Build the packet
        Dim packet As New PacketClass(OPCODES.SMSG_TRAINER_LIST)

        packet.AddUInt64(cGUID)
        packet.AddInt32(CType(TrainerSQLQuery.Rows(0).Item("trainer_type"), Integer))
        packet.AddInt32(SpellsList.Count)              'Trains Length

        For Each SellRow As DataRow In SpellsList
            Dim SpellInfo As SpellInfo = CType(WS_Spells.SPELLs(CType(SellRow.Item("spellid"), Integer)), SpellInfo)

            packet.AddInt32(CType(SellRow.Item("spellid"), Integer)) 'SpellID

            'CanLearn (0):Green (1):Red (2):Gray
            If c.HaveSpell(CType(SellRow.Item("spellid"), Integer)) Then
                'NOTE: Already have that spell
                packet.AddInt8(2)
            ElseIf _
            c.Level >= SellRow.Item("reqlevel") Then
                NeedToLearn = True
                ''If (SellRow.Item("reqspell") <> 0) And (c.HaveSpell(SellRow.Item("reqspell"))) Then
                ''    canLearn = True
                ''Else
                ''    canLearn = False
                ''End If
                If (CType(SellRow.Item("reqskill"), Integer) <> 0) Then
                    If (CType(SellRow.Item("reqskillvalue"), Integer) <> 0) Then
                        If (c.HaveSkill(SellRow.Item("reqskill"), SellRow.Item("reqskillvalue"))) Then
                            packet.AddInt8(0)
                            NeedToLearn = False
                        Else
                            packet.AddInt8(1)
                            NeedToLearn = False
                        End If
                    End If
                End If
                ' ''NOTE: Can learn that spell
                ''If canLearn = True Then packet.AddInt8(0) Else packet.AddInt8(1)
                If NeedToLearn Then packet.AddInt8(0)
            Else
                'NOTE: Doesn't meet requirements, cannot learn that spell
                packet.AddInt8(1)
            End If
            ''packet.AddInt8(0) not sure what this was here for???

            packet.AddInt32(CType(SellRow.Item("spellcost"), Integer))              'SpellCost
            packet.AddInt32(0)
            packet.AddInt32(CType(SellRow.Item("is_prof"), Byte)) 'Primary Profession
            packet.AddInt8(CType(SpellInfo.spellLevel, Byte))
            packet.AddInt32(CType(SellRow.Item("reqskill"), Integer))          'Required Skill
            packet.AddInt32(CType(SellRow.Item("reqskillvalue"), Integer))    'Required Skill Value
            packet.AddInt32(CType(SellRow.Item("reqspell"), Integer))          'Required Spell
            packet.AddInt32(0)
            packet.AddInt32(0)
        Next

        packet.AddString(CType(TrainerSQLQuery.Rows(0).Item("trainer_ui_window_message"), String))

        c.Client.Send(packet)
        packet.Dispose()
    End Sub

#End Region
#Region "Merchants"
    Public Sub On_CMSG_LIST_INVENTORY(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 13 Then Exit Sub
        packet.GetInt16()
        Dim GUID As ULong = packet.GetUInt64
        If WORLD_CREATUREs.ContainsKey(GUID) = False OrElse ((CType(WORLD_CREATUREs(GUID), CreatureObject).CreatureInfo.cNpcFlags And NPCFlags.UNIT_NPC_FLAG_REPAIR) = 0 AndAlso (CType(WORLD_CREATUREs(GUID), CreatureObject).CreatureInfo.cNpcFlags And NPCFlags.UNIT_NPC_FLAG_VENDOR) = 0) Then Exit Sub
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_LIST_INVENTORY [GUID={2:X}]", Client.IP, Client.Port, GUID)

        SendListInventory(Client.Character, GUID)
    End Sub
    Public Sub On_CMSG_SELL_ITEM(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 22 Then Exit Sub
        packet.GetInt16()
        Dim vendorGUID As ULong = packet.GetUInt64
        Dim itemGUID As ULong = packet.GetUInt64
        Dim count As Byte = packet.GetInt8
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_SELL_ITEM [vendorGUID={2:X} itemGUID={3:X} Count={4}]", Client.IP, Client.Port, vendorGUID, itemGUID, count)

        Try
            If itemGUID = 0 OrElse WORLD_ITEMs.ContainsKey(itemGUID) = False Then
                Dim okPckt As New PacketClass(OPCODES.SMSG_SELL_ITEM)
                okPckt.AddUInt64(vendorGUID)
                okPckt.AddUInt64(itemGUID)
                okPckt.AddInt8(SELL_ERROR.SELL_ERR_CANT_FIND_ITEM)
                Client.Send(okPckt)
                okPckt.Dispose()
                Exit Sub
            End If
            'DONE: You can't sell someone else's items
            If CType(WORLD_ITEMs(itemGUID), ItemObject).OwnerGUID <> Client.Character.GUID Then
                Dim okPckt As New PacketClass(OPCODES.SMSG_SELL_ITEM)
                okPckt.AddUInt64(vendorGUID)
                okPckt.AddUInt64(itemGUID)
                okPckt.AddInt8(SELL_ERROR.SELL_ERR_CANT_FIND_ITEM)
                Client.Send(okPckt)
                okPckt.Dispose()
                Exit Sub
            End If
            If Not WORLD_CREATUREs.ContainsKey(vendorGUID) Then
                Dim okPckt As New PacketClass(OPCODES.SMSG_SELL_ITEM)
                okPckt.AddUInt64(vendorGUID)
                okPckt.AddUInt64(itemGUID)
                okPckt.AddInt8(SELL_ERROR.SELL_ERR_CANT_FIND_VENDOR)
                Client.Send(okPckt)
                okPckt.Dispose()
                Exit Sub
            End If
            'DONE: Can't sell quest items
            If (ITEMDatabase(WORLD_ITEMs(itemGUID).ItemEntry).SellPrice = 0) Or (CType(ITEMDatabase(WORLD_ITEMs(itemGUID).ItemEntry), ItemInfo).ObjectClass = ITEM_CLASS.ITEM_CLASS_QUEST) Then
                Dim okPckt As New PacketClass(OPCODES.SMSG_SELL_ITEM)
                okPckt.AddUInt64(vendorGUID)
                okPckt.AddUInt64(itemGUID)
                okPckt.AddInt8(SELL_ERROR.SELL_ERR_CANT_SELL_ITEM)
                Client.Send(okPckt)
                okPckt.Dispose()
                Exit Sub
            End If
            'DONE: Can't cheat and sell items that are located in the buyback
            Dim i As Byte
            For i = BUYBACK_SLOT_START To BUYBACK_SLOT_END - 1
                If Client.Character.Items.ContainsKey(i) AndAlso CType(Client.Character.Items(i), ItemObject).GUID = itemGUID Then
                    Dim okPckt As New PacketClass(OPCODES.SMSG_SELL_ITEM)
                    okPckt.AddUInt64(vendorGUID)
                    okPckt.AddUInt64(itemGUID)
                    okPckt.AddInt8(SELL_ERROR.SELL_ERR_CANT_FIND_ITEM)
                    Client.Send(okPckt)
                    okPckt.Dispose()
                    Exit Sub
                End If
            Next

            If count < 1 Then count = CType(WORLD_ITEMs(itemGUID), ItemObject).StackCount
            If CType(WORLD_ITEMs(itemGUID), ItemObject).StackCount > count Then
                CType(WORLD_ITEMs(itemGUID), ItemObject).StackCount -= count
                Dim tmpItem As ItemObject = LoadItemByGUID(itemGUID) 'Lets create a new stack to place in the buyback
                ItemGUIDCounter += 1 'Get a new GUID for our new stack
                tmpItem.GUID = ItemGUIDCounter
                tmpItem.StackCount = count
                Client.Character.ItemADD_BuyBack(tmpItem)

                Client.Character.Copper += (ITEMDatabase(WORLD_ITEMs(itemGUID).ItemEntry).SellPrice * count)
                Client.Character.SetUpdateFlag(EPlayerFields.PLAYER_FIELD_COINAGE, Client.Character.Copper)
                Client.Character.SendItemUpdate(WORLD_ITEMs(itemGUID))
                WORLD_ITEMs(itemGUID).Save(False)
            Else
                'DONE: Move item to buyback
                'TODO: Remove items that expire in the buyback, in mangos it seems like they use 30 hours until it's removed.

                For Each Item As KeyValuePair(Of Byte, ItemObject) In Client.Character.Items
                    If Item.Value.GUID = itemGUID Then
                        Client.Character.Copper += (ITEMDatabase(Item.Value.ItemEntry).SellPrice * Item.Value.StackCount)
                        Client.Character.SetUpdateFlag(EPlayerFields.PLAYER_FIELD_COINAGE, Client.Character.Copper)

                        If Item.Key < EQUIPMENT_SLOT_END Then Client.Character.UpdateRemoveItemStats(Item.Value, Item.Key)

                        Client.Character.ItemREMOVE(Item.Value.GUID, False, True)
                        Client.Character.ItemADD_BuyBack(CType(Item.Value, ItemObject))

                        Dim okPckt As New PacketClass(OPCODES.SMSG_SELL_ITEM)
                        okPckt.AddUInt64(vendorGUID)
                        okPckt.AddUInt64(itemGUID)
                        okPckt.AddInt8(0)
                        Client.Send(okPckt)
                        okPckt.Dispose()
                        Exit Sub
                    End If
                Next

                For i = INVENTORY_SLOT_BAG_1 To INVENTORY_SLOT_BAG_4
                    For Each Item As KeyValuePair(Of Byte, ItemObject) In Client.Character.Items
                        If Item.Value.GUID = itemGUID Then
                            Client.Character.Copper += (ITEMDatabase(Item.Value.ItemEntry).SellPrice * Item.Value.StackCount)
                            Client.Character.SetUpdateFlag(EPlayerFields.PLAYER_FIELD_COINAGE, Client.Character.Copper)

                            Client.Character.ItemREMOVE(Item.Value.GUID, False, True)
                            Client.Character.ItemADD_BuyBack(CType(Item.Value, ItemObject))

                            Dim okPckt As New PacketClass(OPCODES.SMSG_SELL_ITEM)
                            okPckt.AddUInt64(vendorGUID)
                            okPckt.AddUInt64(itemGUID)
                            okPckt.AddInt8(0)
                            Client.Send(okPckt)
                            okPckt.Dispose()
                            Exit Sub
                        End If
                    Next

                Next
            End If

        Catch e As Exception
            Log.WriteLine(LogType.FAILED, "Error selling item: {0}{1}", vbNewLine, e.ToString)
        End Try
    End Sub
    Public Sub On_CMSG_BUY_ITEM(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 19 Then Exit Sub
        packet.GetInt16()
        Dim vendorGUID As ULong = packet.GetUInt64
        Dim itemID As Integer = packet.GetInt32
        Dim count As Byte = packet.GetInt8
        Dim slot As Byte = packet.GetInt8       '??
        If WORLD_CREATUREs.ContainsKey(vendorGUID) = False OrElse ((CType(WORLD_CREATUREs(vendorGUID), CreatureObject).CreatureInfo.cNpcFlags And NPCFlags.UNIT_NPC_FLAG_REPAIR) = 0 AndAlso (CType(WORLD_CREATUREs(vendorGUID), CreatureObject).CreatureInfo.cNpcFlags And NPCFlags.UNIT_NPC_FLAG_VENDOR) = 0) Then Exit Sub
        If ITEMDatabase.ContainsKey(itemID) = False Then Exit Sub
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_BUY_ITEM [vendorGUID={2:X} ItemID={3} Count={4} Slot={5}]", Client.IP, Client.Port, vendorGUID, itemID, count, slot)

        'DONE: No count cheating
        If count > ITEMDatabase(itemID).Stackable Then count = ITEMDatabase(itemID).Stackable
        If count = 0 Then count = 1

        'DONE: Can't buy quest items
        If CType(ITEMDatabase(itemID), ItemInfo).ObjectClass = ITEM_CLASS.ITEM_CLASS_QUEST Then
            Dim errorPckt As New PacketClass(OPCODES.SMSG_BUY_FAILED)
            errorPckt.AddUInt64(vendorGUID)
            errorPckt.AddInt32(itemID)
            errorPckt.AddInt8(BUY_ERROR.BUY_ERR_SELLER_DONT_LIKE_YOU)
            Client.Send(errorPckt)
            errorPckt.Dispose()
            Exit Sub
        End If

        Dim itemPrice As Integer = 0
        Dim MySQLQuery As New DataTable
        Database.Query(String.Format("SELECT ExtendedCost, sellamount, currentcount FROM npc_vendor WHERE entry = {0} AND item = {1} AND (incrtime = 0 OR currentcount > (sellamount * {2}));", WORLD_CREATUREs(vendorGUID).ID, itemID, count), MySQLQuery)
        If MySQLQuery.Rows.Count = 0 Then
            'DONE: This is hack preventation. Used with WPE Pro packet editing of itemID value
            Dim errorPckt As New PacketClass(OPCODES.SMSG_BUY_FAILED)
            errorPckt.AddUInt64(vendorGUID)
            errorPckt.AddInt32(itemID)
            errorPckt.AddInt8(BUY_ERROR.BUY_ERR_CANT_FIND_ITEM)
            Client.Send(errorPckt)
            errorPckt.Dispose()
            Exit Sub
        End If
        If count * CByte(MySQLQuery.Rows(0).Item("sellamount")) > ITEMDatabase(itemID).Stackable Then count = (ITEMDatabase(itemID).Stackable / CByte(MySQLQuery.Rows(0).Item("sellamount")))

        'DONE: Reputation discount
        Dim DiscountMod As Single = Client.Character.GetDiscountMod(WORLD_CREATUREs(vendorGUID).Faction)
        itemPrice = ITEMDatabase(itemID).BuyPrice * DiscountMod
        If Client.Character.Copper < (itemPrice * count) Then
            Dim errorPckt As New PacketClass(OPCODES.SMSG_BUY_FAILED)
            errorPckt.AddUInt64(vendorGUID)
            errorPckt.AddInt32(itemID)
            errorPckt.AddInt8(BUY_ERROR.BUY_ERR_NOT_ENOUGHT_MONEY)
            Client.Send(errorPckt)
            errorPckt.Dispose()
            Exit Sub
        End If

        'DONE: Check for extended costs
        Dim ExtendedCost As Integer = CInt(MySQLQuery.Rows(0).Item("ExtendedCost"))
        If ItemExtendedCosts.ContainsKey(ExtendedCost) Then
            'DONE: Check for enough arena points
            If (ItemExtendedCosts(ExtendedCost).ArenaPoints * count) > Client.Character.ArenaCurrency Then
                SendInventoryChangeFailure(Client.Character, InventoryChangeFailure.EQUIP_ERR_NOT_ENOUGH_ARENA_POINTS, 0, 0)
                Exit Sub
            End If
            'DONE: Check for enough honor points
            If (ItemExtendedCosts(ExtendedCost).HonorPoints * count) > Client.Character.HonorCurrency Then
                SendInventoryChangeFailure(Client.Character, InventoryChangeFailure.EQUIP_ERR_NOT_ENOUGH_HONOR_POINTS, 0, 0)
                Exit Sub
            End If
            'DONE: Check if you don't have the required items
            For j As Byte = 0 To 4
                If ItemExtendedCosts(ExtendedCost).ItemRequired(j) > 0 AndAlso ItemExtendedCosts(ExtendedCost).ItemCount(j) > 0 Then
                    If Client.Character.ItemCOUNT(ItemExtendedCosts(ExtendedCost).ItemRequired(j)) < (ItemExtendedCosts(ExtendedCost).ItemCount(j) * count) Then
                        SendInventoryChangeFailure(Client.Character, InventoryChangeFailure.EQUIP_ERR_VENDOR_MISSING_TURNINS, 0, 0)
                        Exit Sub
                    End If
                End If
            Next
        End If

        Client.Character.Copper -= (itemPrice * count)
        Client.Character.SetUpdateFlag(EPlayerFields.PLAYER_FIELD_COINAGE, Client.Character.Copper)

        If ItemExtendedCosts.ContainsKey(ExtendedCost) Then
            'DONE: Remove Arena points
            If ItemExtendedCosts(ExtendedCost).ArenaPoints > 0 Then
                Client.Character.ArenaCurrency -= ItemExtendedCosts(ExtendedCost).ArenaPoints * count
                Client.Character.SetUpdateFlag(EPlayerFields.PLAYER_FIELD_ARENA_CURRENCY, Client.Character.ArenaCurrency)
            End If
            'DONE: Remove Honor points
            If ItemExtendedCosts(ExtendedCost).HonorPoints > 0 Then
                Client.Character.HonorCurrency -= ItemExtendedCosts(ExtendedCost).HonorPoints * count
                Client.Character.SetUpdateFlag(EPlayerFields.PLAYER_FIELD_HONOR_CURRENCY, Client.Character.HonorCurrency)
            End If
            'DONE: Remove Items
            For j As Byte = 0 To 4
                If ItemExtendedCosts(ExtendedCost).ItemRequired(j) > 0 AndAlso ItemExtendedCosts(ExtendedCost).ItemCount(j) > 0 Then
                    Client.Character.ItemCONSUME(ItemExtendedCosts(ExtendedCost).ItemRequired(j), ItemExtendedCosts(ExtendedCost).ItemCount(j))
                End If
            Next
        End If
        Client.Character.SendCharacterUpdate(False)

        Dim tmpItem As New ItemObject(itemID, Client.Character.GUID)
        tmpItem.StackCount = count * CByte(MySQLQuery.Rows(0).Item("sellamount"))

        'TODO: Remove one count of the item from the vendor if it's not unlimited

        If Not Client.Character.ItemADD(tmpItem) Then
            tmpItem.Delete()
            Client.Character.Copper += itemPrice
            Client.Character.SetUpdateFlag(EPlayerFields.PLAYER_FIELD_COINAGE, Client.Character.Copper)
        Else
            Dim CurCount As Short = CByte(MySQLQuery.Rows(0).Item("currentcount"))
            CurCount -= count * CByte(MySQLQuery.Rows(0).Item("sellamount"))
            If CurCount < 0 Then CurCount = 0
            Database.Update(String.Format("UPDATE npc_vendor SET currentcount = {0} WHERE entry = {1} AND item = {2}", CurCount, WORLD_CREATUREs(vendorGUID).ID, itemID))
            Dim okPckt As New PacketClass(OPCODES.SMSG_BUY_ITEM)
            okPckt.AddUInt64(vendorGUID)
            okPckt.AddInt32(itemID)
            okPckt.AddInt32(count)
            Client.Send(okPckt)
            okPckt.Dispose()
        End If
    End Sub
    Public Sub On_CMSG_BUY_ITEM_IN_SLOT(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 27 Then Exit Sub
        packet.GetInt16()
        Dim vendorGUID As ULong = packet.GetUInt64
        Dim itemID As Integer = packet.GetInt32
        Dim clientGUID As ULong = packet.GetUInt64
        Dim slot As Byte = packet.GetInt8
        Dim count As Byte = packet.GetInt8
        If WORLD_CREATUREs.ContainsKey(vendorGUID) = False OrElse ((CType(WORLD_CREATUREs(vendorGUID), CreatureObject).CreatureInfo.cNpcFlags And NPCFlags.UNIT_NPC_FLAG_REPAIR) = 0 AndAlso (CType(WORLD_CREATUREs(vendorGUID), CreatureObject).CreatureInfo.cNpcFlags And NPCFlags.UNIT_NPC_FLAG_VENDOR) = 0) Then Exit Sub
        If ITEMDatabase.ContainsKey(itemID) = False Then Exit Sub
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_BUY_ITEM_IN_SLOT [vendorGUID={2:X} ItemID={3} Count={4} Slot={5}]", Client.IP, Client.Port, vendorGUID, itemID, count, slot)

        'DONE: No count cheating
        If count > ITEMDatabase(itemID).Stackable Then count = ITEMDatabase(itemID).Stackable

        'DONE: Can't buy quest items
        If CType(ITEMDatabase(itemID), ItemInfo).ObjectClass = ITEM_CLASS.ITEM_CLASS_QUEST Then
            Dim errorPckt As New PacketClass(OPCODES.SMSG_BUY_FAILED)
            errorPckt.AddUInt64(vendorGUID)
            errorPckt.AddInt32(itemID)
            errorPckt.AddInt8(BUY_ERROR.BUY_ERR_SELLER_DONT_LIKE_YOU)
            Client.Send(errorPckt)
            errorPckt.Dispose()
            Exit Sub
        End If

        Dim itemPrice As Integer = 0
        Dim MySQLQuery As New DataTable
        Database.Query(String.Format("SELECT ExtendedCost FROM npc_vendor WHERE entry = {0} AND item = {1};", WORLD_CREATUREs(vendorGUID).ID, itemID), MySQLQuery)

        'DONE: Reputation discount
        Dim DiscountMod As Single = Client.Character.GetDiscountMod(WORLD_CREATUREs(vendorGUID).Faction)
        itemPrice = ITEMDatabase(itemID).BuyPrice * DiscountMod

        If Client.Character.Copper < (itemPrice * count) Then
            Dim errorPckt As New PacketClass(OPCODES.SMSG_BUY_FAILED)
            errorPckt.AddUInt64(vendorGUID)
            errorPckt.AddInt32(itemID)
            errorPckt.AddInt8(BUY_ERROR.BUY_ERR_NOT_ENOUGHT_MONEY)
            Client.Send(errorPckt)
            errorPckt.Dispose()
            Exit Sub
        End If

        'DONE: Check for extended costs
        Dim ExtendedCost As Integer = CLng(MySQLQuery.Rows(0).Item("ExtendedCost"))
        If ItemExtendedCosts.ContainsKey(ExtendedCost) Then
            'DONE: Check for enough arena points
            If (ItemExtendedCosts(ExtendedCost).ArenaPoints * count) > Client.Character.ArenaCurrency Then
                SendInventoryChangeFailure(Client.Character, InventoryChangeFailure.EQUIP_ERR_NOT_ENOUGH_ARENA_POINTS, 0, 0)
                Exit Sub
            End If
            'DONE: Check for enough honor points
            If (ItemExtendedCosts(ExtendedCost).HonorPoints * count) > Client.Character.HonorCurrency Then
                SendInventoryChangeFailure(Client.Character, InventoryChangeFailure.EQUIP_ERR_NOT_ENOUGH_HONOR_POINTS, 0, 0)
                Exit Sub
            End If
            'DONE: Check if you don't have the required items
            For j As Byte = 0 To 4
                If ItemExtendedCosts(ExtendedCost).ItemRequired(j) > 0 AndAlso ItemExtendedCosts(ExtendedCost).ItemCount(j) > 0 Then
                    If Client.Character.ItemCOUNT(ItemExtendedCosts(ExtendedCost).ItemRequired(j)) < (ItemExtendedCosts(ExtendedCost).ItemCount(j) * count) Then
                        SendInventoryChangeFailure(Client.Character, InventoryChangeFailure.EQUIP_ERR_VENDOR_MISSING_TURNINS, 0, 0)
                        Exit Sub
                    End If
                End If
            Next
        End If

        Dim errCode As Byte = 0
        Dim bag As Byte = 0

        If clientGUID = Client.Character.GUID Then
            'Store in inventory
            bag = 0
            If Client.Character.Items.ContainsKey(slot) Then
                Dim errorPckt As New PacketClass(OPCODES.SMSG_BUY_FAILED)
                errorPckt.AddUInt64(vendorGUID)
                errorPckt.AddInt32(itemID)
                errorPckt.AddInt8(BUY_ERROR.BUY_ERR_CANT_CARRY_MORE)
                Client.Send(errorPckt)
                errorPckt.Dispose()
                Exit Sub
            End If
        Else
            'Store in bag
            Dim i As Byte
            For i = INVENTORY_SLOT_BAG_1 To INVENTORY_SLOT_BAG_4
                If Client.Character.Items(i).GUID = clientGUID Then
                    bag = i
                    Exit For
                End If
            Next
            If bag = 0 Then
                Dim okPckt As New PacketClass(OPCODES.SMSG_BUY_FAILED)
                okPckt.AddUInt64(vendorGUID)
                okPckt.AddInt32(itemID)
                okPckt.AddInt8(BUY_ERROR.BUY_ERR_CANT_FIND_ITEM)
                Client.Send(okPckt)
                okPckt.Dispose()
                Exit Sub
            End If
            If Client.Character.Items(bag).Items.ContainsKey(slot) Then
                Dim errorPckt As New PacketClass(OPCODES.SMSG_BUY_FAILED)
                errorPckt.AddUInt64(vendorGUID)
                errorPckt.AddInt32(itemID)
                errorPckt.AddInt8(BUY_ERROR.BUY_ERR_CANT_CARRY_MORE)
                Client.Send(errorPckt)
                errorPckt.Dispose()
                Exit Sub
            End If
        End If

        Dim tmpItem As New ItemObject(itemID, Client.Character.GUID)
        tmpItem.StackCount = count

        errCode = Client.Character.ItemCANEQUIP(tmpItem, bag, slot)
        If errCode <> InventoryChangeFailure.EQUIP_ERR_OK Then
            If errCode <> InventoryChangeFailure.EQUIP_ERR_CANT_EQUIP_LEVEL_I Then
                Dim errorPckt As New PacketClass(OPCODES.SMSG_INVENTORY_CHANGE_FAILURE)
                errorPckt.AddInt8(errCode)
                errorPckt.AddUInt64(0)
                errorPckt.AddUInt64(0)
                errorPckt.AddInt8(0)
                Client.Send(errorPckt)
                errorPckt.Dispose()
            End If
            tmpItem.Delete()
            Exit Sub
        Else
            Client.Character.Copper -= (itemPrice * count)
            Client.Character.SetUpdateFlag(EPlayerFields.PLAYER_FIELD_COINAGE, Client.Character.Copper)

            If ItemExtendedCosts.ContainsKey(ExtendedCost) Then
                'DONE: Remove Arena points
                If ItemExtendedCosts(ExtendedCost).ArenaPoints > 0 Then
                    Client.Character.ArenaCurrency -= ItemExtendedCosts(ExtendedCost).ArenaPoints * count
                    Client.Character.SetUpdateFlag(EPlayerFields.PLAYER_FIELD_ARENA_CURRENCY, Client.Character.ArenaCurrency)
                End If
                'DONE: Remove Honor points
                If ItemExtendedCosts(ExtendedCost).HonorPoints > 0 Then
                    Client.Character.HonorCurrency -= ItemExtendedCosts(ExtendedCost).HonorPoints * count
                    Client.Character.SetUpdateFlag(EPlayerFields.PLAYER_FIELD_HONOR_CURRENCY, Client.Character.HonorCurrency)
                End If
                'DONE: Remove Items
                For j As Byte = 0 To 4
                    If ItemExtendedCosts(ExtendedCost).ItemRequired(j) > 0 AndAlso ItemExtendedCosts(ExtendedCost).ItemCount(j) > 0 Then
                        Client.Character.ItemCONSUME(ItemExtendedCosts(ExtendedCost).ItemRequired(j), ItemExtendedCosts(ExtendedCost).ItemCount(j))
                    End If
                Next
            End If

            If Not Client.Character.ItemSETSLOT(tmpItem, bag, slot) Then
                tmpItem.Delete()
                Client.Character.Copper += itemPrice
                Client.Character.SetUpdateFlag(EPlayerFields.PLAYER_FIELD_COINAGE, Client.Character.Copper)
            Else
                Dim okPckt As New PacketClass(OPCODES.SMSG_BUY_ITEM)
                okPckt.AddUInt64(vendorGUID)
                okPckt.AddInt32(itemID)
                okPckt.AddInt32(count)
                Client.Send(okPckt)
                okPckt.Dispose()
            End If
            Client.Character.SendCharacterUpdate(False)
        End If
    End Sub

    Public Sub On_CMSG_BUYBACK_ITEM(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 17 Then Exit Sub
        packet.GetInt16()
        Dim vendorGUID As ULong = packet.GetUInt64
        Dim Slot As Integer = packet.GetInt32
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_BUYBACK_ITEM [vendorGUID={2:X} Slot={3}]", Client.IP, Client.Port, vendorGUID, Slot)

        'TODO: If item is not located in your buyback you can't buy it back (this checking below doesn't work)
        If Slot < BUYBACK_SLOT_START OrElse Slot >= BUYBACK_SLOT_END OrElse Client.Character.Items.ContainsKey(Slot) = False Then
            Dim errorPckt As New PacketClass(OPCODES.SMSG_BUY_FAILED)
            errorPckt.AddUInt64(vendorGUID)
            errorPckt.AddInt32(0)
            errorPckt.AddInt8(BUY_ERROR.BUY_ERR_CANT_FIND_ITEM)
            Client.Send(errorPckt)
            errorPckt.Dispose()
            Exit Sub
        End If
        'DONE: Check if you can afford it
        Dim tmpItem As ItemObject = Client.Character.Items(Slot)
        If Client.Character.Copper < (tmpItem.ItemInfo.SellPrice * tmpItem.StackCount) Then
            Dim errorPckt As New PacketClass(OPCODES.SMSG_BUY_FAILED)
            errorPckt.AddUInt64(vendorGUID)
            errorPckt.AddInt32(tmpItem.ItemEntry)
            errorPckt.AddInt8(BUY_ERROR.BUY_ERR_NOT_ENOUGHT_MONEY)
            Client.Send(errorPckt)
            errorPckt.Dispose()
            Exit Sub
        End If

        'DONE: Move item to the inventory, if it's unable to do that tell the client that the bags are full
        Client.Character.ItemREMOVE(tmpItem.GUID, False, True)
        If Client.Character.ItemADD_AutoSlot(tmpItem) Then
            Dim eSlot As Byte = Slot - BUYBACK_SLOT_START
            Client.Character.Copper -= (tmpItem.ItemInfo.SellPrice * tmpItem.StackCount)
            Client.Character.BuyBackTimeStamp(eSlot) = 0
            Client.Character.SetUpdateFlag(EPlayerFields.PLAYER_FIELD_BUYBACK_TIMESTAMP_1 + eSlot, 0)
            Client.Character.SetUpdateFlag(EPlayerFields.PLAYER_FIELD_BUYBACK_PRICE_1 + eSlot, 0)
            Client.Character.SetUpdateFlag(EPlayerFields.PLAYER_FIELD_COINAGE, Client.Character.Copper)

            Client.Character.SendCharacterUpdate()
        Else
            SendInventoryChangeFailure(Client.Character, InventoryChangeFailure.EQUIP_ERR_INVENTORY_FULL, 0, 0)
            Client.Character.ItemSETSLOT(tmpItem, 0, Slot)
        End If
    End Sub

    Public Sub On_CMSG_REPAIR_ITEM(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 21 Then Exit Sub
        packet.GetInt16()
        Dim vendorGUID As ULong = packet.GetUInt64
        Dim itemGUID As ULong = packet.GetUInt64
        If WORLD_CREATUREs.ContainsKey(vendorGUID) = False OrElse (WORLD_CREATUREs(vendorGUID).CreatureInfo.cNpcFlags And NPCFlags.UNIT_NPC_FLAG_REPAIR) = 0 Then Exit Sub
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_REPAIR_ITEM [vendorGUID={2:X} itemGUID={3:X}]", Client.IP, Client.Port, vendorGUID, itemGUID)

        'DONE: Reputation discount
        Dim DiscountMod As Single = Client.Character.GetDiscountMod(WORLD_CREATUREs(vendorGUID).Faction)

        If itemGUID <> 0 Then
            If Client.Character.Copper >= (WORLD_ITEMs(itemGUID).GetDurabulityCost * DiscountMod) Then
                Client.Character.Copper -= WORLD_ITEMs(itemGUID).GetDurabulityCost * DiscountMod
                Client.Character.SetUpdateFlag(EPlayerFields.PLAYER_FIELD_COINAGE, Client.Character.Copper)
                Client.Character.SendCharacterUpdate()

                WORLD_ITEMs(itemGUID).ModifyDurability(-100.0F, Client)
            End If
        Else
            Dim i As Byte
            For i = 0 To EQUIPMENT_SLOT_END - 1
                If Client.Character.Items.ContainsKey(i) Then
                    If Client.Character.Copper >= (Client.Character.Items(i).GetDurabulityCost * DiscountMod) Then

                        Client.Character.Copper -= Client.Character.Items(i).GetDurabulityCost * DiscountMod
                        Client.Character.SetUpdateFlag(EPlayerFields.PLAYER_FIELD_COINAGE, Client.Character.Copper)
                        Client.Character.SendCharacterUpdate()

                        Client.Character.Items(i).ModifyDurability(-100.0F, Client)
                    Else
                        Exit For
                    End If

                End If
            Next
        End If
    End Sub

    Public Sub SendListInventory(ByVal Character As CharacterObject, ByVal GUID As ULong)
        Try
            If WORLD_CREATUREs.ContainsKey(GUID) = False OrElse ((CType(WORLD_CREATUREs(GUID), CreatureObject).CreatureInfo.cNpcFlags And NPCFlags.UNIT_NPC_FLAG_REPAIR) = 0 AndAlso (CType(WORLD_CREATUREs(GUID), CreatureObject).CreatureInfo.cNpcFlags And NPCFlags.UNIT_NPC_FLAG_VENDOR) = 0) Then Exit Sub
            Dim packet As New PacketClass(OPCODES.SMSG_LIST_INVENTORY)
            packet.AddUInt64(GUID)

            Dim MySQLQuery As New DataTable
            Database.Query(String.Format("SELECT * FROM npc_vendor WHERE entry = {0};", CType(WORLD_CREATUREs(GUID), CreatureObject).ID), MySQLQuery)
            Dim DataPos As Integer = packet.Data.Length
            packet.AddInt8(0) 'Will be updated later

            Dim i As Byte = 0
            For Each SellRow As DataRow In MySQLQuery.Rows
                'DONE: You will now only see items for your class
                If ITEMDatabase.ContainsKey(CType(SellRow.Item("item"), Integer)) = False Then Dim tmpItem As ItemInfo = New ItemInfo(CType(SellRow.Item("item"), Integer))
                If ITEMDatabase.ContainsKey(CType(SellRow.Item("item"), Integer)) AndAlso (ITEMDatabase(CType(SellRow.Item("item"), Integer)).AvailableClasses = 0 OrElse (ITEMDatabase(CType(SellRow.Item("item"), Integer)).AvailableClasses And Character.ClassMask)) Then
                    Dim CurCount As Byte = CByte(SellRow.Item("currentcount"))
                    'DONE: Refill
                    If CInt(SellRow.Item("incrtime")) > 0 AndAlso CurCount < CByte(SellRow.Item("maxcount")) Then
                        Dim diff As Integer = Int(GetTimestamp(Now) - CInt(SellRow.Item("lastrefill")) / CInt(SellRow.Item("incrtime")))
                        If diff > 0 Then
                            If (CurCount + diff * CByte(SellRow.Item("sellamount"))) <= CByte(SellRow.Item("maxcount")) Then
                                CurCount += diff * CByte(SellRow.Item("sellamount"))
                            Else
                                CurCount = CByte(SellRow.Item("maxcount"))
                            End If
                            Database.Update(String.Format("UPDATE npc_vendor SET lastrefill = {0}, currentcount = {1} WHERE entry = {2} AND item = {3};", GetTimestamp(Now), CurCount, SellRow.Item("entry"), SellRow.Item("item")))
                        End If
                    ElseIf CInt(SellRow.Item("incrtime")) <= 0 Then
                        CurCount = 1
                    End If

                    'DONE: Don't show items that is outsold.
                    If CurCount > 0 Then
                        i += 1
                        packet.AddInt32(i)
                        packet.AddInt32(SellRow.Item("item"))
                        packet.AddInt32(ITEMDatabase(CType(SellRow.Item("item"), Integer)).Model)

                        'AviableCount
                        If SellRow.Item("maxcount") <= 0 Then
                            packet.AddInt32(-1)
                        Else
                            packet.AddInt32(CurCount)
                        End If

                        'DONE: Discount on reputation
                        Dim DiscountMod As Single = Character.GetDiscountMod(WORLD_CREATUREs(GUID).Faction)
                        packet.AddInt32(CInt(ITEMDatabase(CType(SellRow.Item("item"), Integer)).BuyPrice * DiscountMod))
                        packet.AddInt32(ITEMDatabase(CType(SellRow.Item("item"), Integer)).Durability)
                        packet.AddInt32(SellRow.Item("sellamount"))
                        packet.AddInt32(SellRow.Item("extendedcost"))
                    End If
                End If
            Next

            packet.AddInt8(i, DataPos)
            Character.Client.Send(packet)
            packet.Dispose()
        Catch e As Exception
            Log.WriteLine(LogType.DEBUG, "Error while listing inventory.{0}", vbNewLine & e.ToString)
        End Try
    End Sub

#End Region
#Region "Banker"

    Public Const dbcBankBagSlotsMax As Integer = 12
    Public dbcBankBagSlotPrices(dbcBankBagSlotsMax) As Integer

    Public Sub On_CMSG_AUTOBANK_ITEM(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 7 Then Exit Sub
        packet.GetInt16()
        Dim srcBag As Byte = packet.GetInt8
        Dim srcSlot As Byte = packet.GetInt8
        If srcBag = 0 Then srcBag = 0

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_AUTOBANK_ITEM [srcSlot={2}:{3}]", Client.IP, Client.Port, srcBag, srcSlot)

        'TODO: Do real moving
    End Sub
    Public Sub On_CMSG_AUTOSTORE_BANK_ITEM(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 7 Then Exit Sub
        packet.GetInt16()
        Dim srcBag As Byte = packet.GetInt8
        Dim srcSlot As Byte = packet.GetInt8
        If srcBag = 0 Then srcBag = 0

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_AUTOSTORE_BANK_ITEM [srcSlot={2}:{3}]", Client.IP, Client.Port, srcBag, srcSlot)

        'TODO: Do real moving
    End Sub
    Public Sub On_CMSG_BUY_BANK_SLOT(ByRef packet As PacketClass, ByRef Client As ClientClass)
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_BUY_BANK_SLOT", Client.IP, Client.Port)

        If Client.Character.Items_AvailableBankSlots < dbcBankBagSlotsMax AndAlso _
           Client.Character.Copper >= dbcBankBagSlotPrices(Client.Character.Items_AvailableBankSlots) Then
            Client.Character.Copper -= dbcBankBagSlotPrices(Client.Character.Items_AvailableBankSlots)
            Client.Character.Items_AvailableBankSlots += 1

            Database.Update(String.Format("UPDATE characters SET char_bankSlots = {0}, char_copper = {1};", Client.Character.Items_AvailableBankSlots, Client.Character.Copper))

            Client.Character.SetUpdateFlag(EPlayerFields.PLAYER_FIELD_COINAGE, Client.Character.Copper)
            Client.Character.SetUpdateFlag(EPlayerFields.PLAYER_BYTES_2, (Client.Character.FacialHair + (&HEE << 8) + (CType(Client.Character.Items_AvailableBankSlots, Integer) << 16) + (CType(Client.Character.RestState, Integer) << 24)))
            Client.Character.SendCharacterUpdate(False)
        Else
            Dim errorPckt As New PacketClass(OPCODES.SMSG_BUY_FAILED)
            errorPckt.AddUInt64(0)
            errorPckt.AddInt32(0)
            errorPckt.AddInt8(BUY_ERROR.BUY_ERR_NOT_ENOUGHT_MONEY)
            Client.Send(errorPckt)
            errorPckt.Dispose()
        End If
    End Sub
    Public Sub On_CMSG_BANKER_ACTIVATE(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 13 Then Exit Sub
        packet.GetInt16()
        Dim GUID As ULong = packet.GetUInt64

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_BANKER_ACTIVATE [GUID={2:X}]", Client.IP, Client.Port, GUID)

        SendShowBank(Client.Character, GUID)
    End Sub
    Public Sub SendShowBank(ByVal c As CharacterObject, ByVal GUID As ULong)
        Dim packet As New PacketClass(OPCODES.SMSG_SHOW_BANK)
        packet.AddUInt64(GUID)
        c.Client.Send(packet)
        packet.Dispose()
    End Sub

#End Region
#Region "Other"

    Public Sub SendBindPointConfirm(ByVal c As CharacterObject, ByVal GUID As ULong)
        c.SendGossipComplete()
        c.ZoneCheck()
        Dim packet As New PacketClass(OPCODES.SMSG_BINDER_CONFIRM)
        packet.AddUInt64(GUID)
        packet.AddInt32(c.ZoneID)
        c.Client.Send(packet)
        packet.Dispose()
    End Sub
    Public Sub On_CMSG_BINDER_ACTIVATE(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 13 Then Exit Sub
        packet.GetInt16()
        Dim GUID As ULong = packet.GetUInt64

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_BINDER_ACTIVATE [binderGUID={2:X}]", Client.IP, Client.Port, GUID)

        Client.Character.BindPlayer(GUID)
        Client.Character.SendGossipComplete()

        Dim response As New PacketClass(OPCODES.SMSG_SPELL_START)
        response.AddPackGUID(GUID) 'Caster
        response.AddPackGUID(GUID) 'Caster
        response.AddInt8(1) 'Cast Count
        response.AddInt32(3286) 'Spell ID (Bind)
        response.AddInt32(0) 'Cast Flags
        response.AddInt32(0) 'Cast Time
        response.AddInt16(2) 'Target Mask (Unit)
        response.AddInt16(0) 'Target Mask Extended
        Client.SendMultiplyPackets(response)
        Client.Character.SendToNearPlayers(response)
        response.Dispose()

        response = New PacketClass(OPCODES.SMSG_SPELL_GO)
        response.AddPackGUID(GUID) 'Caster
        response.AddPackGUID(GUID) 'Caster
        response.AddInt8(1) 'Cast Count
        response.AddUInt32(3286) 'Spell ID (Bind)
        response.AddInt32(0) 'Cast Flags
        response.AddInt32(timeGetTime) 'MS Time
        response.AddInt8(1) 'Target Count
        response.AddUInt64(Client.Character.GUID) 'Targer
        response.AddInt8(0) 'Miss Count
        response.AddInt16(2) 'Target Mask (Unit)
        response.AddInt16(0) 'Target Mask Extended
        Client.SendMultiplyPackets(response)
        Client.Character.SendToNearPlayers(response)
        response.Dispose()
    End Sub
    Public Sub SendTalentWipeConfirm(ByVal c As CharacterObject, ByVal Cost As Integer)
        Dim packet As New PacketClass(OPCODES.MSG_TALENT_WIPE_CONFIRM)
        packet.AddUInt64(c.GUID)
        packet.AddInt32(Cost)
        c.Client.Send(packet)
        packet.Dispose()
    End Sub
    Public Sub On_MSG_TALENT_WIPE_CONFIRM(ByRef packet As PacketClass, ByRef Client As ClientClass)
        Try
            packet.GetInt16()
            Dim GUID As ULong = packet.GetPackGUID
            Dim i As Integer

            Log.WriteLine(LogType.DEBUG, "[{0}:{1}] MSG_TALENT_WIPE_CONFIRM [GUID={2:X}]", Client.IP, Client.Port, GUID)

            'DONE: Removing all talents
            For Each TalentInfo As KeyValuePair(Of Integer, TalentInfo) In Talents
                For i = 0 To 4
                    If CType(TalentInfo.Value, TalentInfo).RankID(i) <> 0 Then
                        If Client.Character.HaveSpell(CType(TalentInfo.Value, TalentInfo).RankID(i)) Then
                            Client.Character.UnLearnSpell(CType(TalentInfo.Value, TalentInfo).RankID(i))
                        End If
                    End If
                Next i
            Next

            'DONE: Reset Talentpoints to Level - 9
            Client.Character.TalentPoints = Client.Character.Level - 9
            Client.Character.SetUpdateFlag(EPlayerFields.PLAYER_CHARACTER_POINTS1, CType(Client.Character.TalentPoints, Integer))
            Client.Character.SendCharacterUpdate(True)

            'DONE: Use spell 14867
            Dim SMSG_SPELL_START As New PacketClass(OPCODES.SMSG_SPELL_START)
            SMSG_SPELL_START.AddPackGUID(Client.Character.GUID)
            SMSG_SPELL_START.AddPackGUID(GUID)
            SMSG_SPELL_START.AddInt16(14867)
            SMSG_SPELL_START.AddInt16(0)
            SMSG_SPELL_START.AddInt16(&HF)
            SMSG_SPELL_START.AddInt32(0)
            SMSG_SPELL_START.AddInt16(0)
            Client.Send(SMSG_SPELL_START)
            SMSG_SPELL_START.Dispose()

            Dim SMSG_SPELL_GO As New PacketClass(OPCODES.SMSG_SPELL_GO)
            SMSG_SPELL_GO.AddPackGUID(Client.Character.GUID)
            SMSG_SPELL_GO.AddPackGUID(GUID)
            SMSG_SPELL_GO.AddInt16(14867)
            SMSG_SPELL_GO.AddInt16(0)
            SMSG_SPELL_GO.AddInt8(&HD)
            SMSG_SPELL_GO.AddInt8(&H1)
            SMSG_SPELL_GO.AddInt8(&H1)
            SMSG_SPELL_GO.AddUInt64(Client.Character.GUID)
            SMSG_SPELL_GO.AddInt32(0)
            SMSG_SPELL_GO.AddInt16(&H200)
            SMSG_SPELL_GO.AddInt16(0)
            Client.Send(SMSG_SPELL_GO)
            SMSG_SPELL_GO.Dispose()

        Catch e As Exception
            Log.WriteLine(LogType.FAILED, "Error unlearning talents: {0}{1}", vbNewLine, e.ToString)
        End Try
    End Sub
#End Region
#Region "Default Menu"
    Enum Gossip_Option
        GOSSIP_OPTION_NONE = 0                                 'UNIT_NPC_FLAG_NONE              = 0
        GOSSIP_OPTION_GOSSIP = 1                               'UNIT_NPC_FLAG_GOSSIP            = 1
        GOSSIP_OPTION_QUESTGIVER = 2                           'UNIT_NPC_FLAG_QUESTGIVER        = 2
        GOSSIP_OPTION_VENDOR = 3                               'UNIT_NPC_FLAG_VENDOR            = 4
        GOSSIP_OPTION_TAXIVENDOR = 4                           'UNIT_NPC_FLAG_FLIGHTMASTER        = 8
        GOSSIP_OPTION_TRAINER = 5                              'UNIT_NPC_FLAG_TRAINER           = 16
        GOSSIP_OPTION_SPIRITHEALER = 6                         'UNIT_NPC_FLAG_SPIRITHEALER      = 32
        GOSSIP_OPTION_GUARD = 7                                'UNIT_NPC_FLAG_GUARD		        = 64
        GOSSIP_OPTION_INNKEEPER = 8                            'UNIT_NPC_FLAG_INNKEEPER         = 128
        GOSSIP_OPTION_BANKER = 9                               'UNIT_NPC_FLAG_BANKER            = 256
        GOSSIP_OPTION_ARENACHARTER = 10                         'UNIT_NPC_FLAG_ARENACHARTER     = 262144
        GOSSIP_OPTION_TABARDVENDOR = 11                        'UNIT_NPC_FLAG_TABARDVENDOR      = 1024
        GOSSIP_OPTION_BATTLEFIELD = 12                         'UNIT_NPC_FLAG_BATTLEFIELDPERSON = 2048
        GOSSIP_OPTION_AUCTIONEER = 13                          'UNIT_NPC_FLAG_AUCTIONEER        = 4096
        GOSSIP_OPTION_STABLEPET = 14                           'UNIT_NPC_FLAG_STABLE            = 8192
        GOSSIP_OPTION_ARMORER = 15                             'UNIT_NPC_FLAG_REPAIR           = 16384
        GOSSIP_OPTION_TALENTWIPE = 16
    End Enum
    Enum Gossip_Guard
        GOSSIP_GUARD_BANK = 32
        GOSSIP_GUARD_RIDE = 33
        GOSSIP_GUARD_GUILD = 34
        GOSSIP_GUARD_INN = 35
        GOSSIP_GUARD_MAIL = 36
        GOSSIP_GUARD_AUCTION = 37
        GOSSIP_GUARD_WEAPON = 38
        GOSSIP_GUARD_STABLE = 39
        GOSSIP_GUARD_BATTLE = 40
        GOSSIP_GUARD_SPELLTRAINER = 41
        GOSSIP_GUARD_SKILLTRAINER = 42
    End Enum
    Enum Gossip_Guard_Spell
        GOSSIP_GUARD_SPELL_WARRIOR = 64
        GOSSIP_GUARD_SPELL_PALADIN = 65
        GOSSIP_GUARD_SPELL_HUNTER = 66
        GOSSIP_GUARD_SPELL_ROGUE = 67
        GOSSIP_GUARD_SPELL_PRIEST = 68
        GOSSIP_GUARD_SPELL_UNKNOWN1 = 69
        GOSSIP_GUARD_SPELL_SHAMAN = 70
        GOSSIP_GUARD_SPELL_MAGE = 71
        GOSSIP_GUARD_SPELL_WARLOCK = 72
        GOSSIP_GUARD_SPELL_UNKNOWN2 = 73
        GOSSIP_GUARD_SPELL_DRUID = 74
    End Enum
    Enum Gossip_Guard_Skill
        GOSSIP_GUARD_SKILL_ALCHEMY = 80
        GOSSIP_GUARD_SKILL_BLACKSMITH = 81
        GOSSIP_GUARD_SKILL_COOKING = 82
        GOSSIP_GUARD_SKILL_ENCHANT = 83
        GOSSIP_GUARD_SKILL_FIRSTAID = 84
        GOSSIP_GUARD_SKILL_FISHING = 85
        GOSSIP_GUARD_SKILL_HERBALISM = 86
        GOSSIP_GUARD_SKILL_LEATHER = 87
        GOSSIP_GUARD_SKILL_MINING = 88
        GOSSIP_GUARD_SKILL_SKINNING = 89
        GOSSIP_GUARD_SKILL_TAILORING = 90
        GOSSIP_GUARD_SKILL_ENGINERING = 91
    End Enum
    Public Class TDefaultTalk
        Inherits TBaseTalk
        Public Overrides Sub OnGossipHello(ByRef c As CharacterObject, ByVal cGUID As ULong)
            Dim TextID As Integer = 99999999
            Dim npcText As New NPCText
            npcText.Count = 1
            npcText.TextID = TextID
            npcText.TextLine1(0) = "Greetings $N."
            SendNPCText(c.Client, npcText)

            Dim npcMenu As New GossipMenu

            c.TalkMenuTypes.Clear()

            If CREATURESDatabase(WORLD_CREATUREs(cGUID).ID).cNpcFlags = NPCFlags.UNIT_NPC_FLAG_GOSSIP Then
                StartGuardGossip(c, cGUID)
                c.TalkMenuTypes.Add(Gossip_Option.GOSSIP_OPTION_GUARD)
                Exit Sub
            End If

            'TODO: List quests
            If (CREATURESDatabase(WORLD_CREATUREs(cGUID).ID).cNpcFlags And NPCFlags.UNIT_NPC_FLAG_VENDOR) Then
                npcMenu.AddMenu("Let me browse your goods.", MenuIcon.MENUICON_VENDOR)
                c.TalkMenuTypes.Add(Gossip_Option.GOSSIP_OPTION_VENDOR)
            End If
            If (CREATURESDatabase(WORLD_CREATUREs(cGUID).ID).cNpcFlags And NPCFlags.UNIT_NPC_FLAG_FLIGHTMASTER) Then
                npcMenu.AddMenu("I want to continue my journey.", MenuIcon.MENUICON_TAXI)
                c.TalkMenuTypes.Add(Gossip_Option.GOSSIP_OPTION_TAXIVENDOR)
            End If
            If (CREATURESDatabase(WORLD_CREATUREs(cGUID).ID).cNpcFlags And NPCFlags.UNIT_NPC_FLAG_TRAINER) Then
                Dim TrainerSQLQuery As New DataTable
                Database.Query(String.Format("SELECT req_class, cannot_train_gossip_textid FROM trainer_defs WHERE entry = {0}", WORLD_CREATUREs(cGUID).ID) & " LIMIT 1;", TrainerSQLQuery)
                If (CType(TrainerSQLQuery.Rows(0).Item("req_class"), Integer) = CType(c.Classe, Integer)) Or (CType(TrainerSQLQuery.Rows(0).Item("req_class"), Integer) = 0) Then
                    If (CType(TrainerSQLQuery.Rows(0).Item("req_class"), Integer) = 0) Then
                        npcMenu.AddMenu("I am interested in professions training.", MenuIcon.MENUICON_TRAINER)
                    Else
                        npcMenu.AddMenu("I am interested in " & GetClassName(c.Classe) & " training.", MenuIcon.MENUICON_TRAINER)
                    End If
                    c.TalkMenuTypes.Add(Gossip_Option.GOSSIP_OPTION_TRAINER)
                    npcMenu.AddMenu("I want to unlearn all my talents.", MenuIcon.MENUICON_GOSSIP)
                    c.TalkMenuTypes.Add(Gossip_Option.GOSSIP_OPTION_TALENTWIPE)
                Else
                    SendTrainerList(c, cGUID)
                End If
            End If
            If (CREATURESDatabase(WORLD_CREATUREs(cGUID).ID).cNpcFlags And NPCFlags.UNIT_NPC_FLAG_SPIRITHEALER) Then
                npcMenu.AddMenu("Return me to life", MenuIcon.MENUICON_GOSSIP)
                c.TalkMenuTypes.Add(Gossip_Option.GOSSIP_OPTION_SPIRITHEALER)
            End If
            'UNIT_NPC_FLAG_GUARD
            If (CREATURESDatabase(WORLD_CREATUREs(cGUID).ID).cNpcFlags And NPCFlags.UNIT_NPC_FLAG_INNKEEPER) Then
                npcMenu.AddMenu("Make this inn your home.", MenuIcon.MENUICON_BINDER)
                c.TalkMenuTypes.Add(Gossip_Option.GOSSIP_OPTION_INNKEEPER)
            End If
            If (CREATURESDatabase(WORLD_CREATUREs(cGUID).ID).cNpcFlags And NPCFlags.UNIT_NPC_FLAG_BANKER) Then
                c.TalkMenuTypes.Add(Gossip_Option.GOSSIP_OPTION_BANKER)
            End If
            If (CREATURESDatabase(WORLD_CREATUREs(cGUID).ID).cNpcFlags And NPCFlags.UNIT_NPC_FLAG_PETITIONER) Then
                npcMenu.AddMenu("I am interested in guilds.", MenuIcon.MENUICON_PETITION)
                c.TalkMenuTypes.Add(Gossip_Option.GOSSIP_OPTION_ARENACHARTER)
            End If
            If (CREATURESDatabase(WORLD_CREATUREs(cGUID).ID).cNpcFlags And NPCFlags.UNIT_NPC_FLAG_TABARDDESIGNER) Then
                npcMenu.AddMenu("I want to purchase a tabard.", MenuIcon.MENUICON_TABARD)
                c.TalkMenuTypes.Add(Gossip_Option.GOSSIP_OPTION_TABARDVENDOR)
            End If
            If (CREATURESDatabase(WORLD_CREATUREs(cGUID).ID).cNpcFlags And NPCFlags.UNIT_NPC_FLAG_BATTLEMASTER) Then
                npcMenu.AddMenu("My blood hungers for battle.", MenuIcon.MENUICON_BATTLEMASTER)
                c.TalkMenuTypes.Add(Gossip_Option.GOSSIP_OPTION_BATTLEFIELD)
            End If
            If (CREATURESDatabase(WORLD_CREATUREs(cGUID).ID).cNpcFlags And NPCFlags.UNIT_NPC_FLAG_AUCTIONEER) Then
                npcMenu.AddMenu("Wanna auction something?", MenuIcon.MENUICON_AUCTIONER)
                c.TalkMenuTypes.Add(Gossip_Option.GOSSIP_OPTION_AUCTIONEER)
            End If
            If (CREATURESDatabase(WORLD_CREATUREs(cGUID).ID).cNpcFlags And NPCFlags.UNIT_NPC_FLAG_STABLEMASTER) Then
                npcMenu.AddMenu("Let me check my pet.", MenuIcon.MENUICON_VENDOR)
                c.TalkMenuTypes.Add(Gossip_Option.GOSSIP_OPTION_STABLEPET)
            End If
            If (CREATURESDatabase(WORLD_CREATUREs(cGUID).ID).cNpcFlags And NPCFlags.UNIT_NPC_FLAG_REPAIR) Then
                npcMenu.AddMenu("I want to browse your goods", MenuIcon.MENUICON_VENDOR)
                c.TalkMenuTypes.Add(Gossip_Option.GOSSIP_OPTION_ARMORER)
            End If
            If (CREATURESDatabase(WORLD_CREATUREs(cGUID).ID).cNpcFlags And NPCFlags.UNIT_NPC_FLAG_QUESTGIVER) = NPCFlags.UNIT_NPC_FLAG_QUESTGIVER Then
                Dim qMenu As QuestMenu = GetQuestMenu(c, cGUID)
                If (qMenu.IDs.Count = 0) And (npcMenu.Menus.Count = 0) Then Exit Sub
                c.SendGossip(cGUID, TextID, npcMenu, qMenu)
            Else
                c.SendGossip(cGUID, TextID, npcMenu)
            End If
        End Sub
        Public Overrides Sub OnGossipSelect(ByRef c As CharacterObject, ByVal cGUID As ULong, ByVal Selected As Integer)
            Select Case CType(c.TalkMenuTypes(Selected), Gossip_Option)
                Case Gossip_Option.GOSSIP_OPTION_SPIRITHEALER
                    If c.DEAD = True Then
                        Dim response As New WorldServer.PacketClass(OPCODES.SMSG_SPIRIT_HEALER_CONFIRM)
                        response.AddUInt64(cGUID)
                        c.Client.Send(response)
                        response.Dispose()
                    End If

                Case Gossip_Option.GOSSIP_OPTION_VENDOR, Gossip_Option.GOSSIP_OPTION_ARMORER, Gossip_Option.GOSSIP_OPTION_STABLEPET
                    SendListInventory(c, cGUID)
                Case Gossip_Option.GOSSIP_OPTION_TRAINER
                    SendTrainerList(c, cGUID)
                Case Gossip_Option.GOSSIP_OPTION_TAXIVENDOR
                    SendTaxiStatus(c, cGUID)
                Case Gossip_Option.GOSSIP_OPTION_INNKEEPER
                    SendBindPointConfirm(c, cGUID)
                Case Gossip_Option.GOSSIP_OPTION_BANKER
                    SendShowBank(c, cGUID)
                Case Gossip_Option.GOSSIP_OPTION_ARENACHARTER
                    SendPetitionActivate(c, cGUID)
                Case Gossip_Option.GOSSIP_OPTION_TABARDVENDOR
                    SendTabardActivate(c, cGUID)
                Case Gossip_Option.GOSSIP_OPTION_AUCTIONEER
                    SendShowAuction(c, cGUID)
                Case Gossip_Option.GOSSIP_OPTION_TALENTWIPE
                    SendTalentWipeConfirm(c, 0)
                Case Gossip_Option.GOSSIP_OPTION_GUARD
                    SendGuardGossip(c, cGUID, Selected)
                Case Gossip_Option.GOSSIP_OPTION_GOSSIP
                    'TODO: Unhandled gossip action
                Case Gossip_Option.GOSSIP_OPTION_QUESTGIVER
                    'NOTE: This may stay unused
                    Dim qMenu As QuestMenu = GetQuestMenu(c, cGUID)
                    SendQuestMenu(c, cGUID, "I have some tasks for you, $N.", qMenu)
            End Select
            ''c.SendGossipComplete()
        End Sub
    End Class
#End Region
End Module