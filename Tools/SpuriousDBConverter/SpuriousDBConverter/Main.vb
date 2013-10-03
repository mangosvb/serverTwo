imports mangosVB.Common
Imports System.IO
Imports System.Reflection

Module Main
    Public Declare Function timeGetTime Lib "winmm.dll" () As Integer
    Public Declare Function timeBeginPeriod Lib "winmm.dll" (ByVal uPeriod As Integer) As Integer

    Public SourceDB As New SQL
    Public DestDB As New SQL
    Public FailMsg As String = ""
    Public FillEmptyOnly As Boolean = False
    Public SQLSourceDB As String = ""
    Public SQLDestDB As String = ""

    Public Sub SLQEventHandler(ByVal MessageID As SQL.EMessages, ByVal OutBuf As String)
        Select Case MessageID
            Case SQL.EMessages.ID_Error
                If FailMsg <> "" Then FailMsg &= vbNewLine
                FailMsg &= OutBuf
        End Select
    End Sub

    Sub Main()
        Try
            AddHandler SourceDB.SQLMessage, AddressOf SLQEventHandler
            AddHandler DestDB.SQLMessage, AddressOf SLQEventHandler

            timeBeginPeriod(1) 'Set period to 1ms

            Dim MysqlQuery As New DataTable

            Console.BackgroundColor = System.ConsoleColor.Black
            Console.Title = String.Format("{0} v{1}", CType([Assembly].GetExecutingAssembly().GetCustomAttributes(GetType(AssemblyTitleAttribute), False)(0), AssemblyTitleAttribute).Title, [Assembly].GetExecutingAssembly().GetName().Version)

            Console.ForegroundColor = System.ConsoleColor.Yellow
            Console.WriteLine("{0}", CType([Assembly].GetExecutingAssembly().GetCustomAttributes(GetType(AssemblyProductAttribute), False)(0), AssemblyProductAttribute).Product)
            Console.WriteLine(CType([Assembly].GetExecutingAssembly().GetCustomAttributes(GetType(AssemblyCopyrightAttribute), False)(0), AssemblyCopyrightAttribute).Copyright)
            Console.WriteLine()

            Console.ForegroundColor = System.ConsoleColor.Magenta
            Console.WriteLine("http://www.www.getmangos.co.uk")
            Console.WriteLine()

            'DONE: Get all the sql info
            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("SQL Host [localhost]: ")
            Console.ForegroundColor = ConsoleColor.Cyan
            Dim SQLHost As String = Console.ReadLine()
            If SQLHost = "" Then SQLHost = "localhost"

            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("SQL Port [3306]: ")
            Console.ForegroundColor = ConsoleColor.Cyan
            Dim SQLPortDefault As String = Console.ReadLine()
            If SQLPortDefault = "" Then SQLPortDefault = "3306"
            Dim SQLPort As Integer = CType(SQLPortDefault, Integer)

            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("SQL User [root]: ")
            Console.ForegroundColor = ConsoleColor.Cyan
            Dim SQLUser As String = Console.ReadLine()
            If SQLUser = "" Then SQLUser = "root"

            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("SQL Pass [root]: ")
            Console.ForegroundColor = ConsoleColor.Cyan
            Dim SQLPass As String = Console.ReadLine()
            If SQLPass = "" Then SQLPass = "root"

            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("SQL Source Database [whydb]: ")
            Console.ForegroundColor = ConsoleColor.Cyan
            SQLSourceDB = Console.ReadLine()
            If SQLSourceDB = "" Then SQLSourceDB = "whydb"

            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("SQL Destination Database [spurious]: ")
            Console.ForegroundColor = ConsoleColor.Cyan
            SQLDestDB = Console.ReadLine()
            If SQLDestDB = "" Then SQLDestDB = "spurious"

            Console.ForegroundColor = ConsoleColor.Green
            Console.Write("Only fill empty tables [false]: ")
            Console.ForegroundColor = ConsoleColor.Cyan
            Dim strFillEmptyOnly As String = Console.ReadLine()
            If strFillEmptyOnly = "" Then strFillEmptyOnly = "false"
            If strFillEmptyOnly = "1" OrElse LCase(strFillEmptyOnly) = "true" OrElse LCase(strFillEmptyOnly) = "yes" Then FillEmptyOnly = True

            'DONE: Setup the source DB
            SourceDB.SQLTypeServer = SQL.DB_Type.MySQL
            SourceDB.SQLHost = SQLHost
            SourceDB.SQLPort = SQLPort
            SourceDB.SQLUser = SQLUser
            SourceDB.SQLPass = SQLPass
            SourceDB.SQLDBName = SQLSourceDB

            'DONE: Setup the destination DB
            DestDB.SQLTypeServer = SQL.DB_Type.MySQL
            DestDB.SQLHost = SQLHost
            DestDB.SQLPort = SQLPort
            DestDB.SQLUser = SQLUser
            DestDB.SQLPass = SQLPass
            DestDB.SQLDBName = SQLDestDB

            'DONE: Connect to the DB
            Console.ForegroundColor = ConsoleColor.Yellow
            Console.Write("Connecting to databases... ")
            SourceDB.Connect()
            If FailMsg <> "" Then
                Console.WriteLine("Failed.")
                Console.ForegroundColor = ConsoleColor.Red
                Console.WriteLine(FailMsg)
                FailMsg = ""
                GoTo ExitNow
            End If
            DestDB.Connect()
            If FailMsg <> "" Then
                Console.WriteLine("Failed.")
                Console.ForegroundColor = ConsoleColor.Red
                Console.WriteLine(FailMsg)
                FailMsg = ""
                GoTo ExitNow
            End If
            Console.WriteLine("Done.")

            'DONE: Prepare the DB
            Console.Write("Preparing the database... ")
            SourceDB.Update("SET SESSION sql_mode='STRICT_ALL_TABLES';")
            DestDB.Update("SET SESSION sql_mode='STRICT_ALL_TABLES';")

            Dim SourceTables As New WhyDB
            Dim DestTables As New Spurious
            If FillEmptyOnly = False Then
                For Each DestTable As WhyDBBaseDB.TTable In DestTables.Tables
                    'DONE: Empty the table before we start inserting data into it
                    DestDB.Update("DELETE FROM " & DestTable.Name)
                Next
            End If
            Console.WriteLine("Done.")

            Console.ForegroundColor = ConsoleColor.White

            Dim StartedConvert As Integer = timeGetTime
            Dim AcceptedTypes As New List(Of String)
            For Each DestTable As WhyDBBaseDB.TTable In DestTables.Tables
                If AcceptedTypes.Contains(DestTable.Name) = False AndAlso FillEmptyOnly Then
                    'DONE: Skip table if it includes data and it's only supposed to fill empty tables
                    Dim tmpResult As New DataTable
                    DestDB.Query("SELECT * FROM " & DestTable.Name & " LIMIT 1", tmpResult)
                    If tmpResult.Rows.Count > 0 Then Continue For
                    AcceptedTypes.Add(DestTable.Name)
                End If
                Dim SqlResult As New DataTable
                Dim SqlLine As String = ""
                Dim SqlLine2 As String = ""
                Dim ColumnUse As New List(Of Byte)
                Dim HardcodedValues As New Dictionary(Of Byte, Integer)
                Dim TablesUsed As Integer = 0
                Dim CurrTable As Integer = 0
                Dim FirstTable As Integer = 0
                Dim LastTable As Integer = 0
                SqlLine = "SELECT "
                SqlLine2 = ""
                For Each SourceTable As WhyDBBaseDB.TTable In SourceTables.Tables
                    CurrTable += 1
                    If DestTable.Type = SourceTable.Type Then
                        If FirstTable = 0 Then FirstTable = CurrTable
                        LastTable = CurrTable
                        TablesUsed += 1
                        'DONE: Build the SQL Line for getting data
                        Console.Write("Creating query for {0}... ", SourceTable.Name)

                        For Each DestColumn As KeyValuePair(Of Byte, String) In DestTable.Columns
                            For Each SourceColumn As KeyValuePair(Of Byte, String) In SourceTable.Columns
                                If DestColumn.Key = SourceColumn.Key AndAlso DestColumn.Value <> "" AndAlso Left(SourceColumn.Value, 1) <> "-" AndAlso SourceColumn.Value <> "" AndAlso ColumnUse.Contains(DestColumn.Key) = False Then
                                    'DONE: If this column is in both DB's then we're going to use it for the transfering
                                    ColumnUse.Add(DestColumn.Key)
                                    If SqlLine <> "SELECT " Then SqlLine &= ","
                                    SqlLine &= SourceTable.Name & "." & SourceColumn.Value
                                ElseIf DestColumn.Key = SourceColumn.Key AndAlso Left(SourceColumn.Value, 1) = "-" Then
                                    'DONE: Hardcoded values
                                    ColumnUse.Add(DestColumn.Key)
                                    HardcodedValues.Add(DestColumn.Key, SourceColumn.Value.Replace("-", ""))
                                End If
                            Next
                        Next
                        If SqlLine2 <> "" Then
                            If TablesUsed = 2 Then
                                SqlLine2 &= " LEFT JOIN "
                            Else
                                SqlLine2 = "(" & SqlLine2 & ") LEFT JOIN "
                            End If
                        End If
                        SqlLine2 &= "`" & SourceTable.Name & "`"

                        If TablesUsed > 1 Then
                            If SourceTable.Columns.ContainsKey(0) Then
                                SqlLine2 &= " ON (" & SourceTables.Tables(FirstTable - 1).Name & "." & SourceTables.Tables(FirstTable - 1).Columns.Item(0) & " = " & SourceTable.Name & "." & SourceTable.Columns.Item(0) & ")"
                            ElseIf SourceTable.Columns.ContainsKey(1) Then
                                SqlLine2 &= " ON (" & SourceTables.Tables(FirstTable - 1).Name & "." & SourceTables.Tables(FirstTable - 1).Columns.Item(1) & " = " & SourceTable.Name & "." & SourceTable.Columns.Item(1) & ")"
                            End If
                        End If

                        Console.WriteLine("Done.")
                    End If
                Next

                SqlLine &= " FROM " & SqlLine2
                If Left(SourceTables.Tables(FirstTable - 1).Columns.Item(0), 1) <> "-" Then
                    SqlLine &= " ORDER BY " & SourceTables.Tables(FirstTable - 1).Name & "." & SourceTables.Tables(FirstTable - 1).Columns.Item(0)
                Else
                    SqlLine &= " ORDER BY " & SourceTables.Tables(FirstTable - 1).Name & "." & SourceTables.Tables(FirstTable - 1).Columns.Item(1)
                End If

                Console.Write("Collecting data... ")
                'DONE: Execute the SQL Line and save the results in a empty slot
                SourceDB.Query(SqlLine, SqlResult)
                If FailMsg <> "" Then
                    Console.WriteLine("Failed.")
                    Console.ForegroundColor = ConsoleColor.Red
                    Console.WriteLine(FailMsg)
                    FailMsg = ""
                    GoTo ExitNow
                End If
                Console.WriteLine("Done.")

                Console.Write("Transfer data to {0}", DestTable.Name)
                'DONE: Build the start of the SQL Line
                SqlLine = "INSERT INTO " & DestTable.Name & " ("
                For Each Column As Byte In ColumnUse
                    If SqlLine <> "INSERT INTO " & DestTable.Name & " (" Then SqlLine &= ","
                    SqlLine &= "`" & DestTable.Columns.Item(Column) & "`"
                Next

                'DONE: Build the SQL Line to insert data
                Dim CurrentRow As Integer = 0
                Dim NextPercent As Integer = 10
                For Each RowData As DataRow In SqlResult.Rows
                    CurrentRow += 1
                    SqlLine2 = SqlLine & ") VALUES ('"
                    Dim SkipRows As Integer = 0
                    For k As Integer = 0 To ColumnUse.Count - 1
                        If k > 0 Then SqlLine2 &= "','"
                        If HardcodedValues.ContainsKey(CByte(k)) Then
                            SqlLine2 &= HardcodedValues.Item(CByte(k))
                            SkipRows += 1
                        Else
                            SqlLine2 &= ReplaceValue(RowData.Item(k - SkipRows), SqlResult.Columns.Item(k - SkipRows).DataType)
                        End If
                    Next
                    SqlLine2 &= "');"

                    'DONE: Execute the SQL Line
                    DestDB.Update(SqlLine2)
                    If FailMsg <> "" Then
                        Console.WriteLine(" Failed.")
                        Console.ForegroundColor = ConsoleColor.Red
                        Console.WriteLine(FailMsg)
                        FailMsg = ""
                        GoTo ExitNow
                    End If

                    If Int(CurrentRow / SqlResult.Rows.Count * 100) >= NextPercent Then
                        Console.Write(".")
                        NextPercent += 10
                    End If
                Next

                'DONE: Clean up
                SqlResult.Clear()
                SqlResult.Dispose()
                Console.WriteLine(" Done.")
                ColumnUse.Clear()
            Next

            'DONE: Perform Any Special Table Handling
            SpecialTableHandling()

            Dim TimeTaken As Integer = timeGetTime - StartedConvert
            Dim TimeTakenMin As Integer = ((TimeTaken / 1000) / 60)
            Dim TimeTakenSec As Integer = (TimeTaken / 1000) Mod 60

            Console.ForegroundColor = ConsoleColor.DarkYellow
            Console.WriteLine(vbNewLine & "Time taken: {0}:{1} ({2} ms)", Format(TimeTakenMin, "00"), Format(TimeTakenSec, "00"), TimeTaken)
ExitNow:
            Console.ForegroundColor = ConsoleColor.DarkMagenta
            Console.WriteLine("Press any key to close this window.")

        Catch e As Exception
            Console.ForegroundColor = ConsoleColor.DarkRed
            Console.WriteLine("Error.")
            Console.WriteLine(e.ToString)

            Console.ForegroundColor = ConsoleColor.DarkMagenta
            Console.WriteLine("Press any key to close this window.")
        End Try
        Console.ReadKey()
    End Sub

    Public Sub SpecialTableHandling()
        Try
            Dim SqlLine As String = ""
            Dim SqlLine2 As String = ""
            Dim SqlQuests As String = ""
            Dim SqlResult As New DataTable
            Dim SqlQuestsResult As New DataTable
            Dim SqlExists As String = ""
            Dim SqlExistsResults As New DataTable
            Dim SqlTrainer As String = ""
            Dim SqlTrainerResults As New DataTable
            Dim NextPercent As Integer = 0
            Dim CurrentRow As Integer = 0

            'PROCESS AREATRIGGERS!!!!
            '****Area Triggers Tavern****
            If FillEmptyOnly = False Then
                'DONE: Empty the table before we start inserting data into it
                DestDB.Update("DELETE FROM areatrigger_tavern")
            Else
                'DONE: Skip table if it includes data and it's only supposed to fill empty tables
                Dim tmpResult As New DataTable
                DestDB.Query("SELECT * FROM " & SQLDestDB & ".areatrigger_tavern LIMIT 1", tmpResult)
                If tmpResult.Rows.Count > 0 Then GoTo AreaTriggerTeleport
            End If

            Console.Write("Creating query for {0}... ", "areatriggers (tavern)")

            SqlLine = "SELECT * FROM " & SQLSourceDB & ".areatriggers WHERE type='3';"
            Console.WriteLine("Done.")

            Console.Write("Collecting data... ")

            SourceDB.Query(SqlLine, SqlResult)
            If FailMsg <> "" Then
                Console.WriteLine("Failed.")
                Console.ForegroundColor = ConsoleColor.Red
                Console.WriteLine(FailMsg)
                FailMsg = ""
                GoTo ExitHere
            End If
            Console.WriteLine("Done.")

            Console.Write("Transfer data to {0}", "areatrigger_tavern")

            For Each RowData As DataRow In SqlResult.Rows
                SqlLine2 = "INSERT INTO areatrigger_tavern (id, name) VALUES ('" & RowData.Item("entry") & "', '" & ReplaceValue(RowData.Item("name"), System.Type.GetType("System.String")) & "');"

                CurrentRow += 1

                'DONE: Execute the SQL Line
                DestDB.Update(SqlLine2)
                If FailMsg <> "" Then
                    Console.WriteLine(" Failed.")
                    Console.ForegroundColor = ConsoleColor.Red
                    Console.WriteLine(FailMsg)
                    FailMsg = ""
                    GoTo ExitHere
                End If

                If Int(CurrentRow / SqlResult.Rows.Count * 100) >= NextPercent Then
                    Console.Write(".")
                    NextPercent += 10
                End If

            Next

            Console.WriteLine("Done.")

            CurrentRow = 0

AreaTriggerTeleport:
            '****Area Triggers Teleport****
            If FillEmptyOnly = False Then
                'DONE: Empty the table before we start inserting data into it
                DestDB.Update("DELETE FROM areatrigger_teleport")
            Else
                'DONE: Skip table if it includes data and it's only supposed to fill empty tables
                Dim tmpResult As New DataTable
                DestDB.Query("SELECT * FROM " & SQLDestDB & ".areatrigger_teleport LIMIT 1", tmpResult)
                If tmpResult.Rows.Count > 0 Then GoTo AreaTriggerInvolvedRelation
            End If

            Console.Write("Creating query for {0}... ", "areatriggers (teleport)")

            SqlLine = "SELECT * FROM " & SQLSourceDB & ".areatriggers WHERE type='1';"
            Console.WriteLine("Done.")

            Console.Write("Collecting data... ")

            SourceDB.Query(SqlLine, SqlResult)
            If FailMsg <> "" Then
                Console.WriteLine("Failed.")
                Console.ForegroundColor = ConsoleColor.Red
                Console.WriteLine(FailMsg)
                FailMsg = ""
                GoTo ExitHere
            End If
            Console.WriteLine("Done.")

            Console.Write("Transfer data to {0}", "areatrigger_teleport")

            For Each RowData As DataRow In SqlResult.Rows
                SqlLine2 = "INSERT INTO areatrigger_teleport (id, name, required_level, required_item, required_item2, heroic_key, heroic_key2, required_quest_done, required_quest_done_heroic, required_failed_text, target_map, target_position_x, target_position_y, target_position_z, target_orientation) VALUES ('" & _
                RowData.Item("entry") & "', '" & ReplaceValue(RowData.Item("name"), System.Type.GetType("System.String")) & "', '" & RowData.Item("required_level") & "', '0', '0', '0', '0', '0', '0', '', '" & RowData.Item("map") & "', '" & _
                RowData.Item("position_x") & "', '" & RowData.Item("position_y") & "', '" & RowData.Item("position_z") & "', '" & RowData.Item("orientation") & "');"

                CurrentRow += 1

                'DONE: Execute the SQL Line
                DestDB.Update(SqlLine2)
                If FailMsg <> "" Then
                    Console.WriteLine(" Failed.")
                    Console.ForegroundColor = ConsoleColor.Red
                    Console.WriteLine(FailMsg)
                    FailMsg = ""
                    GoTo ExitHere
                End If

                If Int(CurrentRow / SqlResult.Rows.Count * 100) >= NextPercent Then
                    Console.Write(".")
                    NextPercent += 10
                End If

            Next

            Console.WriteLine("Done.")

AreaTriggerInvolvedRelation:
            'Area Triggers Involved Relation
            If FillEmptyOnly = False Then
                'DONE: Empty the table before we start inserting data into it
                DestDB.Update("DELETE FROM areatrigger_involvedrelation")
            Else
                'DONE: Skip table if it includes data and it's only supposed to fill empty tables
                Dim tmpResult As New DataTable
                DestDB.Query("SELECT * FROM " & SQLDestDB & ".areatrigger_involvedrelation LIMIT 1", tmpResult)
                If tmpResult.Rows.Count > 0 Then GoTo TrainerSpells
            End If

            Console.Write("Creating query for {0}... ", "areatriggers (involvedrelation)")

            SqlLine = "SELECT * FROM " & SQLSourceDB & ".areatriggers WHERE type='2';"
            Console.WriteLine("Done.")

            Console.Write("Collecting data... ")

            SourceDB.Query(SqlLine, SqlResult)
            If FailMsg <> "" Then
                Console.WriteLine("Failed.")
                Console.ForegroundColor = ConsoleColor.Red
                Console.WriteLine(FailMsg)
                FailMsg = ""
                GoTo ExitHere
            End If
            Console.WriteLine("Done.")

            Console.Write("Transfer data to {0}", "areatrigger_involvedrelation")

            For Each RowData As DataRow In SqlResult.Rows
                SqlExists = "SELECT id FROM " & SQLDestDB & ".areatrigger_involvedrelation WHERE id = '" & RowData.Item("entry") & "';"
                DestDB.Query(SqlExists, SqlExistsResults)
                If SqlExistsResults.Rows.Count > 0 Then
                    'Duplicate Entry
                    Continue For
                Else
                    SqlQuests = "SELECT * FROM " & SQLSourceDB & ".quests WHERE ExploreTrigger1 = '" & RowData.Item("entry") & "' OR ExploreTrigger2 = '" & RowData.Item("entry") & "' OR ExploreTrigger3 = '" & RowData.Item("entry") & "' OR ExploreTrigger4 = '" & RowData.Item("entry") & "';"
                    SourceDB.Query(SqlQuests, SqlQuestsResult)
                    If SqlQuestsResult.Rows.Count > 0 Then
                        SqlLine2 = "INSERT INTO areatrigger_involvedrelation (id, quest) VALUES ('" & RowData.Item("entry") & "', '" & SqlQuestsResult.Rows(0).Item("entry") & "');"
                    Else
                        SqlLine2 = "INSERT INTO areatrigger_involvedrelation (id, quest) VALUES ('" & RowData.Item("entry") & "', '0');"
                    End If

                End If

                CurrentRow += 1

                'DONE: Execute the SQL Line
                DestDB.Update(SqlLine2)
                If FailMsg <> "" Then
                    Console.WriteLine(" Failed.")
                    Console.ForegroundColor = ConsoleColor.Red
                    Console.WriteLine(FailMsg)
                    FailMsg = ""
                    GoTo ExitHere
                End If

                If Int(CurrentRow / SqlResult.Rows.Count * 100) >= NextPercent Then
                    Console.Write(".")
                    NextPercent += 10
                End If

            Next

            Console.WriteLine(" Done.")

TrainerSpells:
            'PROCESS TRAINER_SPELLS!!!!
            If FillEmptyOnly = False Then
                'DONE: Empty the table before we start inserting data into it
                DestDB.Update("DELETE FROM trainer_spells")
            Else
                'DONE: Skip table if it includes data and it's only supposed to fill empty tables
                Dim tmpResult As New DataTable
                DestDB.Query("SELECT * FROM " & SQLDestDB & ".trainer_spells LIMIT 1", tmpResult)
                If tmpResult.Rows.Count > 0 Then GoTo ExitHere
            End If

            Console.Write("Creating query for {0}... ", "trainer_spells")

            SqlLine = "SELECT * FROM " & SQLSourceDB & ".trainer_spells;"
            Console.WriteLine("Done.")

            Console.Write("Collecting data... ")

            SourceDB.Query(SqlLine, SqlTrainerResults)
            If FailMsg <> "" Then
                Console.WriteLine("Failed.")
                Console.ForegroundColor = ConsoleColor.Red
                Console.WriteLine(FailMsg)
                FailMsg = ""
                GoTo ExitHere
            End If
            Console.WriteLine("Done.")

            Console.Write("Transfer data to {0}", "trainer_spells")

            Dim isCast As Integer = 0
            Dim SpellID As Integer = 0

            CurrentRow = 0

            For Each RowData As DataRow In SqlTrainerResults.Rows
                'Is this a Cast Spell Record?
                If RowData.Item("cast_spell") <> 0 Then
                    isCast = 1
                    SpellID = RowData.Item("cast_spell")
                Else
                    isCast = 0
                    SpellID = RowData.Item("learn_spell")
                End If

                SqlLine2 = "INSERT INTO trainer_spells (entry, spellid, spellcost, reqspell, reqskill, reqskillvalue, reqlevel, deletespell, is_prof, is_cast) VALUES ('" & _
                RowData.Item("entry") & "', '" & SpellID & "', '" & RowData.Item("spellcost") & "', '" & RowData.Item("reqspell") & "', '" & RowData.Item("reqskill") & _
                "', '" & RowData.Item("reqskillvalue") & "', '" & RowData.Item("reqlevel") & "', '" & RowData.Item("deletespell") & "', '" & RowData.Item("is_prof") & _
                "', '" & isCast & "');"

                CurrentRow += 1

                'DONE: Execute the SQL Line
                DestDB.Update(SqlLine2)
                If FailMsg <> "" Then
                    Console.WriteLine(" Failed.")
                    Console.ForegroundColor = ConsoleColor.Red
                    Console.WriteLine(FailMsg)
                    FailMsg = ""
                    Continue For
                End If

                If Int(CurrentRow / SqlTrainerResults.Rows.Count * 100) >= NextPercent Then
                    Console.Write(".")
                    NextPercent += 10
                End If

            Next

            Console.WriteLine(" Done.")

ExitHere:

        Catch e As Exception
            Console.ForegroundColor = ConsoleColor.DarkRed
            Console.WriteLine("Error.")
            Console.WriteLine(e.ToString)

            Console.ForegroundColor = ConsoleColor.DarkMagenta
            Console.WriteLine("Press any key to close this window.")
            Console.ReadKey()
            End
        End Try

    End Sub

    Public Function ReplaceValue(ByVal Value As Object, ByVal VarType As Type) As String
        Dim sReplaced As String = ""

        'DONE: If the value is NULL then give it a 0 if it's a int/float.
        If TypeOf Value Is DBNull Then
            If VarType.Equals(GetType(String)) Then Return "" 'If it's a string
            Return "0"
        End If

        If TypeOf Value Is Single Then
            sReplaced = Value
            Return Replace(sReplaced, ",", ".")
        ElseIf TypeOf Value Is String Then
            sReplaced = Value
            Return Replace(sReplaced, "'", "\'")
        ElseIf TypeOf Value Is Boolean Then
            If CBool(Value) = True Then
                sReplaced = "1"
            Else
                sReplaced = "0"
            End If
            Return sReplaced
        End If
        Return Value
    End Function

End Module

Public Class BaseDB
    Public Enum TableTypes As Byte
        CreatureInfo = 0
        CreatureSpawn
        GameobjectInfo
        GameobjectSpawn
        Items
        CreatureLoot
        GameobjectLoot
        Quests
        BattleGround_BattleMaster
        BattleGround_Template
        AreaTrigger_InvolvedRelation
        AreaTrigger_Tavern
        AreaTrigger_Template
        QuestStartersCreature
        QuestStartersGameobject
        QuestFinishersCreature
        QuestFinishersGameobject
        SpellTeleportCoords
        GameObjectQuestAssociation
        NpcText
        NpcVendor
        NpcGossipTextID
        NpcTrainerDefs
        NpcTrainerSpells
        ItemRandomPropGroups
        ItemSuffixGroups
        ItemPages
        ItemPetFood
        PlayerCreateInfo
        PlayerCreateInfo_Bars
        PlayerCreateInfo_Items
        PlayerCreateInfo_Skills
        PlayerCreateInfo_Spells
        MonsterSay
        LootsSkinning
        LootsItem
        CmdTeleports
        creature_movement
    End Enum

    Public Enum CreatureInfo_Columns As Byte
        Entry = 0
        Name
        SubName
        InfoStr
        Flags
        Type
        Family
        Rank
        Unk4
        SpellDataID
        MaleDisplayID
        FemaleDisplayID
        MaleDisplayID2
        FemaleDisplayID2
        UnknownFloat1
        UnknownFloat2
        Civilian
        Leader
        MinLevel
        MaxLevel
        Size
        Life
        StartLife
        Mana
        ManaType
        Faction
        MinDamage
        MaxDamage
        MinRangedDamage
        MaxRangedDamage
        Armor
        ResHoly
        ResFire
        ResNature
        ResFrost
        ResShadow
        ResArcane
        WalkSpeed
        RunSpeed
        FlySpeed
        RespawnTime
        BaseAttackSpeed
        BaseRangedAttackSpeed
        CombatReach
        BondingRadius
        NpcFlags
        Loot
        SkinLoot
        PickpocketLoot
        EquipmentEntry
    End Enum

    Public Enum CreatureSpawn_Columns As Byte
        ID = 0
        EquipmentEntry
        Entry
        PositionX
        PositionY
        PositionZ
        Orientation
        Range
        Map
        MoveType
        Model
        Faction
        Mount
        Flags
        Bytes0
        Bytes1
        Bytes2
        EmoteState
        StandState
        equipslot1
        equipslot2
        equipslot3
    End Enum

    Public Enum GameobjectInfo_Columns As Byte
        Entry = 0
        Model
        Name
        Type
        RespawnTime
        Field0
        Field1
        Field2
        Field3
        Field4
        Field5
        Field6
        Field7
        Field8
        Field9
        Field10
        Field11
        Field12
        Field13
        Field14
        Field15
        Field16
        Field17
        Field18
        Field19
        Field20
        Field21
        Field22
        Field23
        CastBarCaption
        Scale
        Flags
        Faction
        AnimProgress
    End Enum

    Public Enum GameobjectSpawn_Columns As Byte
        ID = 0
        Entry
        PositionX
        PositionY
        PositionZ
        Orientation
        Rotation1
        Rotation2
        Rotation3
        Rotation4
        Map
        State
        Flags
        Faction
        Scale
    End Enum

    Public Enum Item_Columns As Byte
        Entry = 0
        Classe
        SubClass
        Field4
        Name1
        Name2
        Name3
        Name4
        DisplayID
        Quality
        Flags
        Buyprice
        Sellprice
        InventoryType
        AllowableClass
        AllowableRace
        ItemLevel
        RequiredLevel
        RequiredSkill
        RequiredSkillRank
        RequiredSubRank
        RequiredPlayerRank1
        RequiredPlayerRank2
        RequiredFaction
        RequiredFactionStanding
        Unique
        MaxCount
        ContainerSlots
        Stat_Type1
        Stat_Value1
        Stat_Type2
        Stat_Value2
        Stat_Type3
        Stat_Value3
        Stat_Type4
        Stat_Value4
        Stat_Type5
        Stat_Value5
        Stat_Type6
        Stat_Value6
        Stat_Type7
        Stat_Value7
        Stat_Type8
        Stat_Value8
        Stat_Type9
        Stat_Value9
        Stat_Type10
        Stat_Value10
        Dmg_Min1
        Dmg_Max1
        Dmg_Type1
        Dmg_Min2
        Dmg_Max2
        Dmg_Type2
        Dmg_Min3
        Dmg_Max3
        Dmg_Type3
        Dmg_Min4
        Dmg_Max4
        Dmg_Type4
        Dmg_Min5
        Dmg_Max5
        Dmg_Type5
        Armor
        HolyRes
        FireRes
        NatureRes
        FrostRes
        ShadowRes
        ArcaneRes
        Delay
        AmmoType
        Range
        SpellID_1
        SpellTrigger_1
        SpellCharges_1
        SpellCooldown_1
        SpellCategory_1
        SpellCategoryCooldown_1
        SpellID_2
        SpellTrigger_2
        SpellCharges_2
        SpellCooldown_2
        SpellCategory_2
        SpellCategoryCooldown_2
        SpellID_3
        SpellTrigger_3
        SpellCharges_3
        SpellCooldown_3
        SpellCategory_3
        SpellCategoryCooldown_3
        SpellID_4
        SpellTrigger_4
        SpellCharges_4
        SpellCooldown_4
        SpellCategory_4
        SpellCategoryCooldown_4
        SpellID_5
        SpellTrigger_5
        SpellCharges_5
        SpellCooldown_5
        SpellCategory_5
        SpellCategoryCooldown_5
        Bonding
        Description
        PageID
        PageLanguage
        PageMaterial
        QuestID
        LockID
        LockMaterial
        SheathID
        RandomProp
        RandomSuffix
        Block
        ItemSet
        MaxDurability
        ZoneNameID
        MapID
        BagFamily
        TotemCategory
        SocketColor_1
        SocketContent_1
        SocketColor_2
        SocketContent_2
        SocketColor_3
        SocketContent_3
        SocketBonus
        GemProperties
        ReqDisenchantSkill
        ArmorDamageModifier
        ExistingDuration
    End Enum

    Public Enum ItemsAmounts_Columns As Byte
        Item_Entry = 0
        SellAmount
        Stock
        StockRefill
    End Enum

    Public Enum CreatureLoot_Columns As Byte
        Entry = 0
        Item
        Group
        Chance
        HeroicChance
        Min
        Max
        FFA
        Condition
        ConditionValue1
        ConditionValue2
    End Enum

    Public Enum GameobjectLoot_Columns As Byte
        Entry = 0
        Item
        Group
        Chance
        HeroicChance
        Min
        Max
        FFA
        Condition
        ConditionValue1
        ConditionValue2
    End Enum

    Public Enum Quest_Columns As Byte
        ID = 0
        PrevQuest
        NextQuest
        Title
        Zone
        Type
        Flags
        SpecialFlags
        LevelStart
        LevelNormal
        ReqQuest1
        ReqQuest2
        ReqQuest3
        ReqQuest4
        ReqRace
        ReqClass
        ReqTradeSkill
        ReqTradeSkillValue
        ReqReputation1
        ReqReputation1Faction
        ReqReputation2
        ReqReputation2Faction
        TextObjectives
        TextDescription
        TextEnd
        TextIncomplete
        TextPrecomplete
        TextComplete
        RewardXP
        RewardGold
        RewardSpell
        RewardSpellCast
        RewardReputation1
        RewardReputation1Faction
        RewardReputation2
        RewardReputation2Faction
        RewardReputation3
        RewardReputation3Faction
        RewardItem1
        RewardItem1Count
        RewardItem2
        RewardItem2Count
        RewardItem3
        RewardItem3Count
        RewardItem4
        RewardItem4Count
        RewardItem5
        RewardItem5Count
        RewardItem6
        RewardItem6Count
        RewardStaticItem1
        RewardStaticItem1Count
        RewardStaticItem2
        RewardStaticItem2Count
        RewardStaticItem3
        RewardStaticItem3Count
        RewardStaticItem4
        RewardStaticItem4Count
        TimeLimit
        ObjectiveTrigger1
        ObjectiveTrigger2
        ObjectiveTrigger3
        ObjectiveTrigger4
        ObjectiveCast1
        ObjectiveCast2
        ObjectiveCast3
        ObjectiveCast4
        ObjectiveKill1
        ObjectiveKill1Count
        ObjectiveKill2
        ObjectiveKill2Count
        ObjectiveKill3
        ObjectiveKill3Count
        ObjectiveKill4
        ObjectiveKill4Count
        ObjectiveItem1
        ObjectiveItem1Count
        ObjectiveItem2
        ObjectiveItem2Count
        ObjectiveItem3
        ObjectiveItem3Count
        ObjectiveItem4
        ObjectiveItem4Count
        ObjectiveDeliver
        ObjectiveText1
        ObjectiveText2
        ObjectiveText3
        ObjectiveText4
        SuggestedPlayers
        PointMap
        PointX
        PointY
        PointZ
        PointOpt
        MoneyAtMaxLevel
        IsActive
        RewTitleId
        RewardTalents
        Repeatable
    End Enum

    Public Enum BattleGround_Columns
        battleground = 0
        creature
        Name
        Type
        Map1
        Map2
        Map3
        MinPlayersPerTeam
        MaxPlayersPerTeam
        MinLvl
        MaxLvl
        Band
        AllianceStartLoc
        AllianceStartO
        HordeStartLoc
        HordeStartO
        IsActive
    End Enum

    Public Enum AreaTrigger_Columns
        TriggerID = 0
        quest
        type
        name
        position_map
        position_x
        position_y
        position_z
        target_map
        target_screen
        target_position_x
        target_position_y
        target_position_z
        target_orientation
        required_item
        required_item2
        heroic_key
        heroic_key2
        required_quest_done
        required_failed_text
        required_level
    End Enum

    Public Enum QuestStarter_Columns
        Type = 0
        TypeID
        QuestID
    End Enum

    Public Enum QuestFinisher_Columns
        Type = 0
        TypeID
        QuestID
    End Enum

    Public Enum SpellTeleportCoords_Columns
        ID = 0
        Name
        Map
        PosX
        PosY
        PosZ
        ToTrigger
    End Enum

    Public Enum GameObjectQuestAssociation_Columns
        Entry = 0
        Quest
        Item
        ItemCount
    End Enum

    Public Enum NpcText_Columns
        Entry = 0
        Prob0
        Text0_0
        Text0_1
        Lang0
        em0_0
        em0_1
        em0_2
        em0_3
        em0_4
        em0_5
        Prob1
        Text1_0
        Text1_1
        Lang1
        em1_0
        em1_1
        em1_2
        em1_3
        em1_4
        em1_5
        Prob2
        Text2_0
        Text2_1
        Lang2
        em2_0
        em2_1
        em2_2
        em2_3
        em2_4
        em2_5
        Prob3
        Text3_0
        Text3_1
        Lang3
        em3_0
        em3_1
        em3_2
        em3_3
        em3_4
        em3_5
        Prob4
        Text4_0
        Text4_1
        Lang4
        em4_0
        em4_1
        em4_2
        em4_3
        em4_4
        em4_5
        Prob5
        Text5_0
        Text5_1
        Lang5
        em5_0
        em5_1
        em5_2
        em5_3
        em5_4
        em5_5
        Prob6
        Text6_0
        Text6_1
        Lang6
        em6_0
        em6_1
        em6_2
        em6_3
        em6_4
        em6_5
        Prob7
        Text7_0
        Text7_1
        Lang7
        em7_0
        em7_1
        em7_2
        em7_3
        em7_4
        em7_5
    End Enum

    Public Enum NpcVendor_Columns
        Entry = 0
        Item
        Amount
        Stock
        Refill
        ExtendedCost
    End Enum

    Public Enum NpcGossipTextID_Columns
        Entry = 0
        TextID
    End Enum

    Public Enum NpcTrainerDef_Columns
        Entry = 0
        ReqSkill
        ReqSkillValue
        ReqRace
        ReqClass
        ReqSpell
        TrainerType
        WindowMessage
        CanTrainMessage
        CanNotTrainMessage
    End Enum

    Public Enum NpcTrainerSpell_Columns
        Spell = 0
        Entry
        SpellCost
        ReqSpell
        ReqSkill
        ReqSkillValue
        ReqLevel
        DeleteSpell
        IsProf
        IsCast
    End Enum

    Public Enum ItemRandom_Columns
        ItemEntry = 0
        RandomPropsEntryID
        RandomSuffixEntryID
        Chance
    End Enum

    Public Enum PetFood_Columns
        ItemEntry = 0
        FoodType
    End Enum

    Public Enum ItemPages_Columns
        ItemEntry = 0
        Text
        NextPage
    End Enum

    Public Enum PlayerCreateInfo_Columns
        Race = 0
        Classe
        BaseStrength
        BaseAgility
        BaseStamina
        BaseIntellect
        BaseSpirit
        BaseHealth
        BasePower
        Faction
        MaleModelOffset
        FemaleModelOffset
        Scale
        StartMap
        StartZone
        StartX
        StartY
        StartZ
        StartO
        Intro
        IsBC
        MinDamage
        MaxDamage
        PowerType
    End Enum

    Public Enum PlayerCreateInfoBar_Columns
        Race = 0
        Classe
        Button
        Action
        Type
        Misc
    End Enum

    Public Enum PlayerCreateInfoItem_Columns
        Race = 0
        Classe
        ItemID
        Slot
        Amount
    End Enum

    Public Enum PlayerCreateInfoSkill_Columns
        Race = 0
        Classe
        SkillID
        Level
        MaxLevel
    End Enum

    Public Enum PlayerCreateInfoSpell_Columns
        Race = 0
        Classe
        SpellID
    End Enum

    Public Enum MonsterSay_Columns
        CreatureEntry = 0
        Evente
        Chance
        Language
        Type
        MonsterName
        Text0
        Text1
        Text2
        Text3
        Text4
    End Enum

    Public Enum LootsSkinning_Columns
        CreatureEntry = 0
        ItemID
        GroupID
        PercentChance
        HeroicPercentChance
        MinCount
        MaxCount
        FFA_Loot
        LootCondition
        ConditionValue1
        ConditionValue2
    End Enum

    Public Enum LootsItem_Columns
        ItemEntry = 0
        ItemID
        GroupID
        PercentChance
        HeroicPercentChance
        MinCount
        MaxCount
        FFA_Loot
        LootCondition
        ConditionValue1
        ConditionValue2
    End Enum

    Public Enum CmdTeleports_Columns
        Id = 0
        Name
        MapID
        PositionX
        PositionY
        PositionZ
    End Enum

    Public Enum Creature_Movement_Columns As Byte
        spawnid = 0
        waypointid
        position_x
        position_y
        position_z
        waittime
        flags
        emote
        orientation
    End Enum

    Public Class TTable
        Public Name As String
        Public Type As TableTypes
        Public Columns As New Dictionary(Of Byte, String)

        Public Sub New(ByVal Name_ As String, ByVal Type_ As TableTypes)
            Name = Name_
            Type = Type_
        End Sub
    End Class

    Public Tables As New List(Of TTable)
End Class
