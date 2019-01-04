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

Public Module WS_GuardGossip

    Public Class TMenuData
        Public OptionNumber As Integer = 0
        Public Icon As Integer = 0
        Public MenuItem_ID As Integer = 0
        Public Text_ID As Integer = 0
        Public POI_ID As Integer = 0
        Public SubMenuNumber As Integer = 0
        Public Sub New(ByVal Option_ As Integer, ByVal Icon_ As Integer, ByVal MenuItem_ID_ As Integer, ByVal Text_ID_ As Integer, ByVal POI_ID_ As Integer, ByVal SubMenuNumber_ As Integer)
            OptionNumber = Option_
            Icon = Icon_
            MenuItem_ID = MenuItem_ID_
            Text_ID = Text_ID_
            POI_ID = POI_ID_
            SubMenuNumber = SubMenuNumber_
        End Sub
    End Class

    Public MenuData As New Dictionary(Of Integer, TMenuData)

    Public Sub StartGuardGossip(ByRef c As CharacterObject, ByVal cGUID As ULong)
        Dim GuardGUID As Integer

        MenuData.Clear()
        GuardGUID = CType(WORLD_CREATUREs(cGUID).ID, Integer)

        Dim GuardMenusSQLQuery As New DataTable
        Database.Query(String.Format("SELECT * FROM guard_gossip_menus WHERE entry = {0} AND Menu_Number = 0;", GuardGUID), GuardMenusSQLQuery)
        Log.WriteLine(LogType.DEBUG, "Reading Guard Gossip Menus For [GUID={0}]", GuardGUID)

        If GuardMenusSQLQuery.Rows.Count > 0 Then
            Dim tmp() As String
            Dim i As Integer
            tmp = Split(CType(GuardMenusSQLQuery.Rows(0).Item("Menu_Data"), String), " ")
            If tmp.Length > 0 Then
                For i = 0 To tmp.Length - 1
                    If Trim(tmp(i)) <> "" Then
                        Dim tmp2() As String
                        tmp2 = Split(tmp(i), ":")
                        MenuData(CType(tmp2(0), Integer)) = New TMenuData(tmp2(0), tmp2(1), tmp2(2), tmp2(3), tmp2(4), tmp2(5))
                    End If
                Next i
            End If

            Dim npcText As New NPCText
            Dim npcTextIDSQLQuery As New DataTable
            Dim npcTextSQLQuery As New DataTable
            Database.Query(String.Format("SELECT * FROM npc_gossip_textid WHERE creatureid = {0};", GuardGUID), npcTextIDSQLQuery)
            Database.Query(String.Format("SELECT * FROM npctext WHERE entry = {0};", npcTextIDSQLQuery.Rows(0).Item("textid")), npcTextSQLQuery)

            npcText.Count = 1
            npcText.TextID = 99999999
            npcText.Probability(0) = 1
            npcText.TextLine1(0) = CType(npcTextSQLQuery.Rows(0).Item("text0_0"), String)
            npcText.TextLine2(0) = CType(npcTextSQLQuery.Rows(0).Item("text0_1"), String)
            SendNPCText(c.Client, npcText)

            c.TalkMenuTypes.Clear()
            Dim npcMenu As New GossipMenu
            For i = 0 To MenuData.Count - 1
                Dim MenuItemsSQLQuery As New DataTable
                Database.Query(String.Format("SELECT * FROM guard_gossip_menuitems WHERE MenuItem_ID = {0} LIMIT 1;", CType(MenuData(i).MenuItem_ID, Integer)), MenuItemsSQLQuery)
                npcMenu.AddMenu(CType(MenuItemsSQLQuery.Rows(0).Item("MenuItem_Text"), String), CType(MenuData(i).Icon, Integer))
                c.TalkMenuTypes.Add(Gossip_Option.GOSSIP_OPTION_GUARD)
            Next

            c.SendGossip(cGUID, 99999999, npcMenu)
            c.MenuNumber = 0
        Else
            Dim npcText As New NPCText
            npcText.Count = 1
            npcText.TextID = 99999999
            npcText.Probability(0) = 1
            npcText.TextLine1(0) = "Hi $N, I'm not yet set up to talk with you."
            npcText.TextLine2(0) = "Hi $N, I'm not yet set up to talk with you."
            SendNPCText(c.Client, npcText)

        End If
    End Sub

    Public Sub SendGuardGossip(ByRef c As CharacterObject, ByVal cGUID As ULong, ByVal Selected As Integer)
        Dim GuardGUID As Integer
        Dim i As Integer
        Dim SubNumber As Integer = CType(MenuData(Selected).SubMenuNumber, Integer)

        GuardGUID = CType(WORLD_CREATUREs(cGUID).ID, Integer)

        If CType(MenuData(Selected).SubMenuNumber, Integer) <> 0 Then

            Dim GuardMenusSQLQuery As New DataTable
            Database.Query(String.Format("SELECT * FROM guard_gossip_menus WHERE entry = {0} AND Menu_Number = {1};", GuardGUID, MenuData(Selected).SubMenuNumber), GuardMenusSQLQuery)
            If GuardMenusSQLQuery.Rows.Count > 0 Then
                MenuData.Clear()
                Dim tmp() As String
                tmp = Split(CType(GuardMenusSQLQuery.Rows(0).Item("Menu_Data"), String), " ")
                If tmp.Length > 0 Then
                    For i = 0 To tmp.Length - 1
                        If Trim(tmp(i)) <> "" Then
                            Dim tmp2() As String
                            tmp2 = Split(tmp(i), ":")
                            MenuData(CType(tmp2(0), Integer)) = New TMenuData(tmp2(0), tmp2(1), tmp2(2), tmp2(3), tmp2(4), tmp2(5))
                        End If
                    Next i
                End If
            End If

            Dim npcTextSQLQuery As New DataTable
            Database.Query(String.Format("SELECT * FROM npctext WHERE entry = {0};", GuardMenusSQLQuery.Rows(0).Item("Menu_Header_TextID")), npcTextSQLQuery)

            Dim npcText As New NPCText
            npcText.Count = 1
            npcText.TextID = 99999999
            npcText.Probability(0) = 1
            npcText.TextLine1(0) = CType(npcTextSQLQuery.Rows(0).Item("text0_0"), String)
            npcText.TextLine2(0) = CType(npcTextSQLQuery.Rows(0).Item("text0_1"), String)
            SendNPCText(c.Client, npcText)

            c.TalkMenuTypes.Clear()
            Dim npcMenu As New GossipMenu
            For i = 0 To MenuData.Count - 1
                Dim MenuItemsSQLQuery As New DataTable
                Database.Query(String.Format("SELECT * FROM guard_gossip_menuitems WHERE MenuItem_ID = {0} LIMIT 1;", CType(MenuData(i).MenuItem_ID, Integer)), MenuItemsSQLQuery)
                npcMenu.AddMenu(CType(MenuItemsSQLQuery.Rows(0).Item("MenuItem_Text"), String), CType(MenuData(i).Icon, Integer))
                c.TalkMenuTypes.Add(Gossip_Option.GOSSIP_OPTION_GUARD)
            Next

            c.SendGossip(cGUID, 99999999, npcMenu)
            c.MenuNumber = SubNumber
        Else
            If CType(MenuData(Selected).Text_ID, Integer) <> -1 Then
                c.SendGossip(cGUID, CType(MenuData(Selected).Text_ID, Integer))
            End If
            If CType(MenuData(Selected).POI_ID, Integer) <> -1 Then
                Dim PoI_X As Single
                Dim PoI_Y As Single
                Dim PoI_Icon As Integer
                Dim PoI_Flags As Integer
                Dim PoI_Data As Integer
                Dim PoI_Name As String = ""

                Dim POISQLQuery As New DataTable
                Database.Query(String.Format("SELECT * FROM guard_gossip_poi WHERE PoI_ID = {0} LIMIT 1;", MenuData(Selected).POI_ID), POISQLQuery)
                PoI_X = CType(POISQLQuery.Rows(0).Item("PoI_X"), Single)
                PoI_Y = CType(POISQLQuery.Rows(0).Item("PoI_Y"), Single)
                PoI_Icon = CType(POISQLQuery.Rows(0).Item("PoI_Icon"), Integer)
                PoI_Flags = CType(POISQLQuery.Rows(0).Item("PoI_Flags"), Integer)
                PoI_Data = CType(POISQLQuery.Rows(0).Item("PoI_Data"), Integer)
                PoI_Name = CType(POISQLQuery.Rows(0).Item("PoI_Name"), String)
                c.SendPointOfInterest(PoI_X, PoI_Y, PoI_Icon, PoI_Flags, PoI_Data, PoI_Name)
            End If
            c.MenuNumber = SubNumber
        End If
        Log.WriteLine(LogType.DEBUG, "Sending Guard Gossip For GUID={0} With Selection = {1}", WORLD_CREATUREs(cGUID).ID, Selected)
    End Sub

End Module