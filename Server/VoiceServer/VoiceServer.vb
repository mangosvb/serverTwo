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

    'System Things...
    Public Log As New BaseWriter
    Public Rnd As New Random

    'Public PacketHandlers As New Dictionary(Of OPCODES, HandlePacket)
    'Delegate Sub HandlePacket(ByRef Packet As PacketClass, ByRef Client As ClientClass)

#End Region
#Region "Global.Config"
    Public Config As XMLConfigFile
    <XmlRoot(ElementName:="VoiceServer")> _
    Public Class XMLConfigFile
        <XmlElement(ElementName:="VSPort")> Public VSPort As Integer = 4720
        <XmlElement(ElementName:="VSHost")> Public VSHost As String = "127.0.0.1"
        <XmlElement(ElementName:="LogType")> Public LogType As String = "COLORCONSOLE"
        <XmlElement(ElementName:="LogLevel")> Public LogLevel As LogType = mangosVB.Common.BaseWriter.LogType.NETWORK
        <XmlElement(ElementName:="LogConfig")> Public LogConfig As String = ""
        <XmlElement(ElementName:="ClusterConnectMethod")> Public ClusterMethod As String = "tcp"
        <XmlElement(ElementName:="ClusterConnectHost")> Public ClusterHost As String = "127.0.0.1"
        <XmlElement(ElementName:="ClusterConnectPort")> Public ClusterPort As Integer = 50001
        <XmlElement(ElementName:="LocalConnectHost")> Public LocalHost As String = "127.0.0.1"
        <XmlElement(ElementName:="LocalConnectPort")> Public LocalPort As Integer = 50009

        Public Function GetVSHost() As UInteger
            Dim b As Byte() = IPAddress.Parse(VSHost).GetAddressBytes()
            Array.Reverse(b)
            Return BitConverter.ToUInt32(b, 0)
        End Function
        Public Function EncryptionKey() As Byte()
            Return New Byte() {0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0}
        End Function
    End Class

    Public Sub LoadConfig()
        Try
            'Make sure VoiceServer.ini exists
            If System.IO.File.Exists("VoiceServer.ini") = False Then
                Console.ForegroundColor = ConsoleColor.Red
                Console.WriteLine("[{0}] Cannot Continue. {1} does not exist.", Format(TimeOfDay, "hh:mm:ss"), "VoiceServer.ini")
                Console.WriteLine("Please copy the ini files into the same directory as the MaNGOSvb exe files.")
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
            oStmR = New StreamReader("VoiceServer.ini")
            Config = oXS.Deserialize(oStmR)
            oStmR.Close()

            Console.WriteLine(".[done]")

            'DONE: Creating logger
            Common.BaseWriter.CreateLog(Config.LogType, Config.LogConfig, Log)
            Log.LogLevel = Config.LogLevel

        Catch e As Exception
            Console.WriteLine(e.ToString)
        End Try
    End Sub
#End Region

    <System.MTAThreadAttribute()> _
    Sub Main()
        Console.BackgroundColor = System.ConsoleColor.Black
        Console.Title = String.Format("{0} v{1} r{2}", CType([Assembly].GetExecutingAssembly().GetCustomAttributes(GetType(AssemblyTitleAttribute), False)(0), AssemblyTitleAttribute).Title, [Assembly].GetExecutingAssembly().GetName().Version, Common.RevisionReader.GetBuildRevision())

        Console.ForegroundColor = System.ConsoleColor.Yellow
        Console.WriteLine("{0}", CType([Assembly].GetExecutingAssembly().GetCustomAttributes(GetType(AssemblyProductAttribute), False)(0), AssemblyProductAttribute).Product)
        Console.WriteLine(CType([Assembly].GetExecutingAssembly().GetCustomAttributes(GetType(AssemblyCopyrightAttribute), False)(0), AssemblyCopyrightAttribute).Copyright)
        Console.WriteLine()

        Console.ForegroundColor = System.ConsoleColor.Magenta
        Console.WriteLine("http://www.getMangos.co.uk")
        Console.WriteLine()

        Console.ForegroundColor = System.ConsoleColor.White
        Console.WriteLine(CType([Assembly].GetExecutingAssembly().GetCustomAttributes(GetType(System.Reflection.AssemblyTitleAttribute), False)(0), AssemblyTitleAttribute).Title)
        Console.Write("version {0}", [Assembly].GetExecutingAssembly().GetName().Version)
        Console.WriteLine("revision {0}", Common.RevisionReader.GetBuildRevision())
        Console.ForegroundColor = System.ConsoleColor.White

        Console.WriteLine("")
        Console.ForegroundColor = System.ConsoleColor.Gray

        Dim dateTimeStarted As Date = Now
        Log.WriteLine(LogType.INFORMATION, "[{0}] Voice Server Starting...", Format(TimeOfDay, "hh:mm:ss"))

        Dim currentDomain As AppDomain = AppDomain.CurrentDomain
        AddHandler currentDomain.UnhandledException, AddressOf GenericExceptionHandler

        LoadConfig()
        Console.ForegroundColor = System.ConsoleColor.Gray

        IntializePacketHandlers()
        VS = New VoiceServerClass
        GC.Collect()

        Log.WriteLine(LogType.INFORMATION, "Load Time: {0}", Format(DateDiff(DateInterval.Second, dateTimeStarted, Now), "0 seconds"))
        Log.WriteLine(LogType.INFORMATION, "Used memory: {0}", Format(GC.GetTotalMemory(False), "### ### ##0 bytes"))

        WaitConsoleCommand()
    End Sub

    Public Sub WaitConsoleCommand()
        Dim tmp As String = "", CommandList() As String, cmds() As String
        Dim cmd() As String = {}
        Dim varList As Integer
        While Not VS.m_flagStopListen
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
                                VS.m_flagStopListen = True
                            Case "gccollect"
                                GC.Collect()
                            Case "info", "/info"
                                Log.WriteLine(LogType.INFORMATION, "Used memory: {0}", Format(GC.GetTotalMemory(False), "### ### ##0 bytes"))
                            Case Else
                                Console.ForegroundColor = System.ConsoleColor.Red
                                Console.WriteLine("Error! Cannot find specified command. Please type 'help' for information on 'mangosVB.WorldServer' console commands.")
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
        Log.WriteLine(LogType.FAILED, "Unexpected error has occured. An 'Error-yyyy-mmm-d-h-mm.log' file has been created. Please post the file in the BUG SECTION at getMaNGOS.co.uk (http://www.getMangos.co.uk/community)!")

        Dim tw As TextWriter
        tw = New StreamWriter(New FileStream(String.Format("Error-{0}.log", Format(Now, "yyyy-MMM-d-H-mm")), FileMode.Create))
        tw.Write(EX.ToString)
        tw.Close()
    End Sub

End Module
