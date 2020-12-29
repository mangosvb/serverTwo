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
Imports System.Net.Sockets
Imports System.Xml.Serialization
Imports System.IO
Imports System.Net
Imports System.Reflection
Imports System.Runtime.CompilerServices
Imports mangosVB.Common.BaseWriter
Imports mangosVB.Common

Public Module WorldCluster

#Region "Global.Variables"
    'Players' containers
    Public CLIETNIDs As Long = 0

    Public CLIENTs As New Dictionary(Of UInteger, ClientClass)

    Public CHARACTERs_Lock As New ReaderWriterLock
    Public CHARACTERs As New Dictionary(Of ULong, CharacterObject)
    'Public CHARACTER_NAMEs As New Hashtable

    'System Things...
    Public Log As New BaseWriter
    Public PacketHandlers As New Dictionary(Of OPCODES, HandlePacket)
    Public Rnd As New Random
    Delegate Sub HandlePacket(ByRef Packet As PacketClass, ByRef Client As ClientClass)

#End Region
#Region "Global.Config"
    Public Config As XMLConfigFile
    <XmlRoot(ElementName:="WorldCluster")> _
    Public Class XMLConfigFile
        <XmlElement(ElementName:="WSPort")> Public WSPort As Integer = 4720
        <XmlElement(ElementName:="WSHost")> Public WSHost As String = "127.0.0.1"
        <XmlElement(ElementName:="ServerLimit")> Public ServerLimit As Integer = 10
        <XmlElement(ElementName:="LogType")> Public LogType As String = "COLORCONSOLE"
        <XmlElement(ElementName:="LogLevel")> Public LogLevel As LogType = mangosVB.Common.BaseWriter.LogType.NETWORK
        <XmlElement(ElementName:="LogConfig")> Public LogConfig As String = ""
        <XmlElement(ElementName:="SQLUser")> Public SQLUser As String = "root"
        <XmlElement(ElementName:="SQLPass")> Public SQLPass As String = "Spurious"
        <XmlElement(ElementName:="SQLHost")> Public SQLHost As String = "localhost"
        <XmlElement(ElementName:="SQLPort")> Public SQLPort As String = "3306"
        <XmlElement(ElementName:="SQLDBName")> Public SQLDBName As String = "Spurious"
        <XmlElement(ElementName:="SQLDBType")> Public SQLDBType As SQL.DB_Type = SQL.DB_Type.MySQL
        <XmlElement(ElementName:="ClusterListenMethod")> Public ClusterMethod As String = "tcp"
        <XmlElement(ElementName:="ClusterListenHost")> Public ClusterHost As String = "localhost"
        <XmlElement(ElementName:="ClusterListenPort")> Public ClusterPort As Integer = 50001
        <XmlArray(ElementName:="ClusterFirewall"), XmlArrayItem(GetType(String), ElementName:="IP")> Public Firewall As New ArrayList

        <XmlElement(ElementName:="StatsEnabled")> Public StatsEnabled As Boolean = True
        <XmlElement(ElementName:="StatsTimer")> Public StatsTimer As Integer = 120000
        <XmlElement(ElementName:="StatsLocation")> Public StatsLocation As String = "stats.xml"
    End Class

    Public Sub LoadConfig()
        Try
            'Make sure WorldCluster.ini exists
            If System.IO.File.Exists("WorldCluster.ini") = False Then
                Console.ForegroundColor = ConsoleColor.Red
                Console.WriteLine("[{0}] Cannot Continue. {1} does not exist.", Format(TimeOfDay, "hh:mm:ss"), "WorldCluster.ini")
                Console.WriteLine("Please copy the ini files into the same directory as the mangosVB exe files.")
                Console.WriteLine("Press any key to exit server: ")
                Console.ReadKey()
                End
            End If

            Console.Write("[{0}] Loading Configuration...", Format(TimeOfDay, "hh:mm:ss"))

            Config = New XMLConfigFile
            Console.Write("...")

            Dim oXS As XmlSerializer = New XmlSerializer(GetType(XMLConfigFile))

            Console.Write("...")
            Dim oStmR As StreamReader
            oStmR = New StreamReader("WorldCluster.ini")
            Config = oXS.Deserialize(oStmR)
            oStmR.Close()

            Console.WriteLine(".[done]")

            'DONE: Setting SQL Connections
            Database.SQLDBName = Config.SQLDBName
            Database.SQLHost = Config.SQLHost
            Database.SQLPort = Config.SQLPort
            Database.SQLUser = Config.SQLUser
            Database.SQLPass = Config.SQLPass
            Database.SQLTypeServer = Config.SQLDBType

            'DONE: Creating logger
            Common.BaseWriter.CreateLog(Config.LogType, Config.LogConfig, Log)
            Log.LogLevel = Config.LogLevel

        Catch e As Exception
            Console.WriteLine(e.ToString)
        End Try
    End Sub
#End Region

#Region "WS.DataAccess"
    Public Database As New SQL
    Public Sub SLQEventHandler(ByVal MessageID As Sql.EMessages, ByVal OutBuf As String)
        Select Case MessageID
            Case SQL.EMessages.ID_Error
                Log.WriteLine(LogType.FAILED, "[WORLD] " & OutBuf)
            Case SQL.EMessages.ID_Message
                Log.WriteLine(LogType.SUCCESS, "[WORLD] " & OutBuf)
        End Select
    End Sub
#End Region

    <System.MTAThreadAttribute()> _
    Sub Main()
        timeBeginPeriod(1)  'Set timeGetTime to a accuracy of 1ms

        Console.BackgroundColor = System.ConsoleColor.Black
        Console.Title = String.Format("{0} v{1} r{2}", CType([Assembly].GetExecutingAssembly().GetCustomAttributes(GetType(AssemblyTitleAttribute), False)(0), AssemblyTitleAttribute).Title, [Assembly].GetExecutingAssembly().GetName().Version, GetBuildRevision())

        Console.ForegroundColor = System.ConsoleColor.Yellow
        Console.WriteLine("{0}", CType([Assembly].GetExecutingAssembly().GetCustomAttributes(GetType(AssemblyProductAttribute), False)(0), AssemblyProductAttribute).Product)
        Console.WriteLine(CType([Assembly].GetExecutingAssembly().GetCustomAttributes(GetType(AssemblyCopyrightAttribute), False)(0), AssemblyCopyrightAttribute).Copyright)
        Console.WriteLine()

        Console.ForegroundColor = System.ConsoleColor.Magenta
        Console.WriteLine("https://getMangos.eu")
        Console.WriteLine()

        Console.ForegroundColor = System.ConsoleColor.White
        Console.WriteLine(CType([Assembly].GetExecutingAssembly().GetCustomAttributes(GetType(System.Reflection.AssemblyTitleAttribute), False)(0), AssemblyTitleAttribute).Title)
        Console.Write("version {0}", [Assembly].GetExecutingAssembly().GetName().Version)
        Console.WriteLine(" revision {0}", GetBuildRevision())
        Console.ForegroundColor = System.ConsoleColor.White

        Console.WriteLine("")
        Console.ForegroundColor = System.ConsoleColor.Gray

        Dim dateTimeStarted As Date = Now
        Log.WriteLine(LogType.INFORMATION, "[{0}] World Cluster Starting...", Format(TimeOfDay, "hh:mm:ss"))

        Dim currentDomain As AppDomain = AppDomain.CurrentDomain
        AddHandler currentDomain.UnhandledException, AddressOf GenericExceptionHandler

        LoadConfig()
        Console.ForegroundColor = System.ConsoleColor.Gray
        AddHandler Database.SQLMessage, AddressOf SLQEventHandler
        Database.Connect()
        Database.Update("SET NAMES 'utf8';")

#If DEBUG Then
        Log.WriteLine(LogType.DEBUG, "Setting MySQL into debug mode..[done]")
        Database.Update("SET SESSION sql_mode='STRICT_ALL_TABLES';")
#End If
        InitializeInternalDatabase()
        IntializePacketHandlers()
        WS = New WorldServerClass
        GC.Collect()

        If Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.High Then
            Log.WriteLine(LogType.WARNING, "Setting Process Priority to HIGH..[done]")
        Else
            Log.WriteLine(LogType.WARNING, "Setting Process Priority to NORMAL..[done]")
        End If

        Log.WriteLine(LogType.INFORMATION, "Load Time: {0}", Format(DateDiff(DateInterval.Second, dateTimeStarted, Now), "0 seconds"))
        Log.WriteLine(LogType.INFORMATION, "Used memory: {0}", Format(GC.GetTotalMemory(False), "### ### ##0 bytes"))

        WaitConsoleCommand()
    End Sub

    Public Sub WaitConsoleCommand()
        Dim tmp As String = "", CommandList() As String, cmds() As String
        Dim cmd() As String = {}
        Dim varList As Integer
        While Not WS.m_flagStopListen
            Try
                tmp = Log.ReadLine()
                CommandList = tmp.Split(";")

                For varList = LBound(CommandList) To UBound(CommandList)
                    cmds = Split(CommandList(varList), " ", 2)
                    If CommandList(varList).Length > 0 Then
                        '<<<<<<<<<<<COMMAND STRUCTURE>>>>>>>>>>
                        Select Case cmds(0).ToLower
                            Case "quit", "shutdown", "off", "kill", "/quit", "/shutdown", "/off", "/kill"
                                Log.WriteLine(LogType.WARNING, "Server shutting down...")
                                WS.m_flagStopListen = True
                            Case "createaccount", "/createaccount"
                                Database.InsertSQL([String].Format("INSERT INTO accounts (account, password, email, joindate, last_ip) VALUES ('{0}', '{1}', '{2}', '{3}', '{4}')", cmd(1), cmd(2), cmd(3), Format(Now, "yyyy-MM-dd"), "0.0.0.0"))
                                If Database.QuerySQL("SELECT * FROM accounts WHERE account = "" & packet_account & "";") Then
                                    Console.ForegroundColor = System.ConsoleColor.Green
                                    Console.WriteLine("[Account: " & cmd(1) & " Password: " & cmd(2) & " Email: " & cmd(3) & "] has been created.")
                                    Console.ForegroundColor = System.ConsoleColor.Gray
                                Else
                                    Console.ForegroundColor = System.ConsoleColor.Red
                                    Console.WriteLine("[Account: " & cmd(1) & " Password: " & cmd(2) & " Email: " & cmd(3) & "] could not be created.")
                                    Console.ForegroundColor = System.ConsoleColor.Gray
                                End If
                            Case "gccollect"
                                GC.Collect()
                            Case "ban", "Ban", "Ban Account", "acct ban"
                                Console.ForegroundColor = System.ConsoleColor.Yellow
                                Console.WriteLine("[{0}] Specify the Account Name :", Format(TimeOfDay, "hh:mm:ss"))
                                Dim aName As String
                                aName = Console.ReadLine
                                Dim result As New DataTable
                                Dim result1 As New DataTable
                                Database.Query("SELECT banned FROM accounts WHERE account = """ & aName & """;", result)
                                Database.Query("SELECT last_ip FROM accounts WHERE account = """ & aName & """;", result1)

                                Dim IP As String
                                IP = result1.Rows(0).Item("last_ip")
                                If result.Rows.Count > 0 Then
                                    If CInt(result.Rows(0).Item("banned")) = 1 Then
                                        Console.ForegroundColor = System.ConsoleColor.Green
                                        Console.WriteLine(String.Format("[{1}] Account [{0}] is already banned.", aName, Format(TimeOfDay, "hh:mm:ss")))

                                        Console.ForegroundColor = System.ConsoleColor.Yellow

                                    ElseIf IP = "127.0.0.1" Then
                                        Console.WriteLine("[{1}] Account [{0}] has the same IP Adress as the host.", aName, Format(TimeOfDay, "hh:mm:ss"))
                                    ElseIf IP = "0.0.0.0" Then
                                        Console.WriteLine("[{1}] Account [{0}] does not have an IP Address.", aName, Format(TimeOfDay, "hh:mm:ss"))
                                    Else
                                        Database.Query("SELECT last_ip FROM accounts WHERE account = """ & aName & """;", result)
                                        Dim IP1 As String
                                        IP1 = result.Rows(0).Item("last_ip")
                                        Database.Update("UPDATE accounts SET banned = 1 WHERE account = """ & aName & """;")
                                        Console.WriteLine(String.Format("[{1}] IP Address [{0}] is now banned.", IP, Format(TimeOfDay, "hh:mm:ss")))
                                        Database.Update(String.Format("INSERT INTO `bans` VALUES ('{0}', '{1}', '{2}', '{3}');", IP1, Format(Now, "yyyy-MM-dd"), "No Reason Specified.", aName))
                                        Console.WriteLine(String.Format("[{1}] Account [{0}] is now banned.", aName, Format(TimeOfDay, "hh:mm:ss")))
                                    End If
                                Else
                                    Console.ForegroundColor = System.ConsoleColor.Gray
                                    Console.WriteLine(String.Format("[{1}] Account [{0}] is not found.", aName, Format(TimeOfDay, "hh:mm:ss")))
                                End If
                            Case "unban", "Unban", "UnBan", "acct unban", "Unban Account"
                                Console.ForegroundColor = System.ConsoleColor.Cyan
                                Console.WriteLine("[{0}] Account Name?", Format(TimeOfDay, "hh:mm:ss"))
                                Dim aName As String
                                Console.ForegroundColor = System.ConsoleColor.Magenta
                                aName = Console.ReadLine
                                Dim result As New DataTable
                                Database.Query("SELECT banned FROM accounts WHERE account = """ & aName & """;", result)

                                If result.Rows.Count > 0 Then
                                    If CInt(result.Rows(0).Item("banned")) = 0 Then
                                        Console.ForegroundColor = System.ConsoleColor.Green
                                        Console.WriteLine(String.Format("[{1}] Account [{0}] is not banned.", aName, Format(TimeOfDay, "hh:mm:ss")))
                                    Else
                                        Console.ForegroundColor = System.ConsoleColor.Red
                                        Database.Update("UPDATE accounts SET banned = 0 WHERE account = """ & aName & """;")
                                        Database.Query("SELECT last_ip FROM accounts WHERE account = """ & aName & """;", result)
                                        Dim IP As String
                                        IP = result.Rows(0).Item("last_ip")
                                        Database.Update([String].Format("DELETE FROM `bans` WHERE `ip` = '{0}';", IP))
                                        Console.WriteLine(String.Format("[{1}] Account [{0}] has been unbanned.", aName, Format(TimeOfDay, "hh:mm:ss")))

                                    End If
                                Else
                                    Console.WriteLine(String.Format("[{1}] Account [{0}] is not found.", aName, Format(TimeOfDay, "hh:mm:ss")))
                                End If
                            Case "info", "/info"
                                Log.WriteLine(LogType.INFORMATION, "Used memory: {0}", Format(GC.GetTotalMemory(False), "### ### ##0 bytes"))
                            Case "db.restart", "/db.restart"
                                Database.Restart()
                            Case "db.run", "/db.run"
                                Database.Update(cmds(1))
                            Case "help", "/help"
                                Console.ForegroundColor = System.ConsoleColor.Blue
                                Console.WriteLine("'WorldServer' Command list:")
                                Console.ForegroundColor = System.ConsoleColor.White
                                Console.WriteLine("---------------------------------")
                                Console.WriteLine("")
                                Console.WriteLine("")
                                Console.WriteLine("'help' or '/help' - Brings up the 'WorldServer' Command list (this).")
                                Console.WriteLine("")
                                Console.WriteLine("'createaccount <user> <password> <email>' or '/createaccount <user> <password> <email>' - Creates an account with the specified username <user>, password <password>, and email <email>.")
                                Console.WriteLine("")
                                Console.WriteLine("'debug' or '/debug' - Creates a 'test' character.")
                                Console.WriteLine("")
                                Console.WriteLine("'info' or '/info' - Brings up a context menu showing server information (such as memory used).")
                                Console.WriteLine("")
                                Console.WriteLine("'exec' or '/exec' - Executes script files.")
                                Console.WriteLine("")
                                Console.WriteLine("'db.restart' or '/db.restart' - Reloads the database.")
                                Console.WriteLine("")
                                Console.WriteLine("'db.run' or '/db.run' - Runs and updates database.")
                                Console.WriteLine("")
                                Console.WriteLine("'quit' or 'shutdown' or 'off' or 'kill' or 'exit' - Shutsdown 'WorldServer'.")
                                Console.WriteLine("")
                                Console.WriteLine("'ban' or 'Ban'- Adds a Ban and IP Ban on an account.")
                                Console.WriteLine("")
                                Console.WriteLine("'unban' or 'Unban'- Removes a Ban and IP Ban on an account.")
                            Case Else
                                Console.ForegroundColor = System.ConsoleColor.Red
                                Console.WriteLine("Error! Cannot find specified command. Please type 'help' for information on 'WorldServer' console commands.")
                                Console.ForegroundColor = System.ConsoleColor.White
                        End Select
                        '<<<<<<<<<<</END COMMAND STRUCTURE>>>>>>>>>>>>
                    End If
                Next
            Catch e As Exception
                Log.WriteLine(LogType.FAILED, "Error executing command [{0}]. {2}{1}", Format(TimeOfDay, "hh:mm:ss"), tmp, e.ToString, vbNewLine)
            End Try
        End While
    End Sub

    Private Sub GenericExceptionHandler(ByVal sender As Object, ByVal e As UnhandledExceptionEventArgs)
        Dim EX As Exception
        EX = e.ExceptionObject

        Log.WriteLine(LogType.CRITICAL, EX.ToString & vbNewLine)
        Log.WriteLine(LogType.FAILED, "Unexpected error has occured. An 'Error-yyyy-mmm-d-h-mm.log' file has been created. Please post the file in the BUG SECTION at getMaNGOS.eu")

        Dim tw As TextWriter
        tw = New StreamWriter(New FileStream(String.Format("Error-{0}.log", Format(Now, "yyyy-MMM-d-H-mm")), FileMode.Create))
        tw.Write(EX.ToString)
        tw.Close()
    End Sub

End Module
