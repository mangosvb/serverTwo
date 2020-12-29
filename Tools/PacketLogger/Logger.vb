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
Imports System.Xml.Serialization
Imports System.Threading
Imports System.Net.Sockets
Imports System.IO
Imports System.Net
Imports System.Reflection

Namespace PacketLogger

    Public Module Logger
        Public Const CONNETION_SLEEP_TIME As Integer = 10

        Public ConsoleColor As New ConsoleColorClass
        Public SSHash() As Byte
        Public FileLogger As New BaseFileLogger



#Region "Global.Config"
        Public Config As XMLConfigFile
        <XmlRoot(ElementName:="PacketLogger")> _
            Public Class XMLConfigFile
            <XmlElement(ElementName:="RSPort")> Public rsPort As Int32 = 0
            <XmlElement(ElementName:="RSHost")> Public rsHost As String = "localhost"

            <XmlElement(ElementName:="WSPort")> Public wsPort As Int32 = 0
            <XmlElement(ElementName:="WSHost")> Public wsHost As String = "localhost"

            <XmlElement(ElementName:="KeyPort")> Public keyPort As Int32 = 0
            <XmlElement(ElementName:="KeyHost")> Public keyHost As String = "localhost"

            <XmlElement(ElementName:="RSConnectPort")> Public rsConnPort As Int32 = 0
            <XmlElement(ElementName:="RSConnectHost")> Public rsConnHost As String = "localhost"

            <XmlElement(ElementName:="WSConnectPort")> Public wsConnPort As Int32 = 0
            <XmlElement(ElementName:="WSConnectHost")> Public wsConnHost As String = "localhost"
        End Class

        Public Sub LoadConfig()
            Try
                Console.Write("[{0}] Loading Configuration...", Format(TimeOfDay, "hh:mm:ss"))

                Config = New XMLConfigFile
                Console.Write("...")

                Dim oXS As XmlSerializer = New XmlSerializer(GetType(XMLConfigFile))
                Console.Write("...")
                Dim oStmR As StreamReader
                oStmR = New StreamReader(System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly.Location()) + "\\Spurious.PacketLogger.xml")
                Config = oXS.Deserialize(oStmR)
                oStmR.Close()

                Console.WriteLine(".[done]")
            Catch e As Exception
                Console.WriteLine(e.ToString)
            End Try
        End Sub
#End Region
#Region "Global.Listener"
        Public PL As PacketRedirector
        Class PacketRedirector
            Public rsClient As RSClientClass
            Public wsClient As WSClientClass

            Public _flagStopListen As Boolean = False
            Private rsHost As Net.IPAddress = Net.IPAddress.Parse(Config.rsHost)
            Private rsConnection As TcpListener
            Private wsHost As Net.IPAddress = Net.IPAddress.Parse(Config.wsHost)
            Private wsConnection As TcpListener
            Private keyHost As Net.IPAddress = Net.IPAddress.Parse(Config.keyHost)
            Private keyConnection As TcpListener

            Public Sub New()
                Try
                    rsConnection = New TcpListener(rsHost, Config.rsPort)
                    rsConnection.Start()
                    Dim PLListenThread As Thread
                    PLListenThread = New Thread(AddressOf rsAcceptConnection)
                    PLListenThread.Name = "Packet Logger, Listening - RS"
                    PLListenThread.Start()
                    Console.WriteLine("[{0}]RS: Listening on {1} on port {2}", Format(TimeOfDay, "hh:mm:ss"), rsHost, Config.rsPort)

                    wsConnection = New TcpListener(wsHost, Config.wsPort)
                    wsConnection.Start()
                    Dim wsPLListenThread As Thread
                    wsPLListenThread = New Thread(AddressOf wsAcceptConnection)
                    wsPLListenThread.Name = "Packet Logger, Listening - WS"
                    wsPLListenThread.Start()
                    Console.WriteLine("[{0}]WS: Listening on {1} on port {2}", Format(TimeOfDay, "hh:mm:ss"), wsHost, Config.wsPort)

                    keyConnection = New TcpListener(keyHost, Config.keyPort)
                    keyConnection.Start()
                    Dim keyPLListenThread As Thread
                    keyPLListenThread = New Thread(AddressOf keyAcceptConnection)
                    keyPLListenThread.Name = "Packet Logger, Listening - Key"
                    keyPLListenThread.Start()
                    Console.WriteLine("[{0}]Key: Listening on {1} on port {2}", Format(TimeOfDay, "hh:mm:ss"), keyHost, Config.keyPort)
                Catch e As Exception
                    Console.WriteLine()
                    Console.WriteLine("[{0}] Error in {2}: {1}.", Format(TimeOfDay, "hh:mm:ss"), e.Message, e.Source)
                End Try
            End Sub
            Protected Sub rsAcceptConnection()
                Do While Not _flagStopListen
                    Thread.Sleep(CONNETION_SLEEP_TIME)
                    If rsConnection.Pending() Then
                        Dim NewThread As Thread
                        NewThread = New Thread(AddressOf rsProcessRequest)
                        NewThread.Name = "Packet Logger, Client Connected"
                        NewThread.Start()
                    End If
                Loop
            End Sub
            Protected Sub wsAcceptConnection()
                Do While Not _flagStopListen
                    Thread.Sleep(CONNETION_SLEEP_TIME)
                    If wsConnection.Pending() Then
                        Dim NewThread As Thread
                        NewThread = New Thread(AddressOf wsProcessRequest)
                        NewThread.Name = "Packet Logger, Client Connected"
                        NewThread.Start()
                    End If
                Loop
            End Sub
            Protected Sub keyAcceptConnection()
                Do While Not _flagStopListen
                    Thread.Sleep(CONNETION_SLEEP_TIME)
                    If keyConnection.Pending() Then
                        Dim Socket As Socket = keyConnection.AcceptSocket
                        While Socket.Available = 0
                            Thread.Sleep(CONNETION_SLEEP_TIME)
                        End While
                        ReDim SSHash(39)
                        Socket.Receive(SSHash)
                        Socket.Close()
                        Console.WriteLine(String.Format("[{0}]Key: Got SSHash, connecting to World Server.", Format(TimeOfDay, "hh:mm:ss")))
                        Dim temp As String = ""
                        Dim i As Integer = 0
                        For i = 0 To 39
                            temp = temp & Hex(SSHash(i))
                        Next
                        FileLogger.Log(String.Format("Got SSHash: [{0}]", temp))
                        rsClient.WSOnline = 1
                    End If
                Loop
            End Sub
            Protected Sub rsProcessRequest()
                rsClient = New RSClientClass

                'AddHandler Client.OnData, AddressOf ServerPart.IncomingData
                rsClient.Socket = rsConnection.AcceptSocket
                rsClient.ProcessClientConnection()
            End Sub
            Protected Sub wsProcessRequest()
                wsClient = New WSClientClass

                'AddHandler Client.OnData, AddressOf ServerPart.IncomingData
                wsClient.Socket = wsConnection.AcceptSocket
                wsClient.ProcessClientConnection()
            End Sub
            Protected Overloads Sub Dispose(ByVal disposing As Boolean)
                _flagStopListen = True
                rsConnection.Stop()
                wsConnection.Stop()
                FileLogger.Dispose()
            End Sub
        End Class
#End Region




        Public Sub Main()
            ConsoleColorClass.SetConsoleTitle(String.Format("{0} v{1}", [Assembly].GetExecutingAssembly().GetCustomAttributes(GetType(System.Reflection.AssemblyTitleAttribute), False)(0).Title, [Assembly].GetExecutingAssembly().GetName().Version))

            ConsoleColor.SetConsoleColor(ConsoleColorClass.ForegroundColors.LightYellow)
            Console.WriteLine([Assembly].GetExecutingAssembly().GetCustomAttributes(GetType(System.Reflection.AssemblyProductAttribute), False)(0).Product)
            Console.WriteLine([Assembly].GetExecutingAssembly().GetCustomAttributes(GetType(System.Reflection.AssemblyCopyrightAttribute), False)(0).Copyright)
            Console.WriteLine()

            ConsoleColor.SetConsoleColor(ConsoleColorClass.ForegroundColors.BrightWhite)
            Console.WriteLine([Assembly].GetExecutingAssembly().GetCustomAttributes(GetType(System.Reflection.AssemblyTitleAttribute), False)(0).Title)
            Console.WriteLine("version {0}", [Assembly].GetExecutingAssembly().GetName().Version)
            Console.WriteLine("")
            ConsoleColor.SetConsoleColor()

            Console.WriteLine("[{0}] Packet Logger Starting...", Format(TimeOfDay, "hh:mm:ss"))

            LoadConfig()

            PL = New PacketRedirector
            FileLogger.Log(String.Format("{0} v{1}", [Assembly].GetExecutingAssembly().GetCustomAttributes(GetType(System.Reflection.AssemblyTitleAttribute), False)(0).Title, [Assembly].GetExecutingAssembly().GetName().Version))
            FileLogger.Log()

            InitializeOpcodes()
            Dim PacketHandlersInitialize As New ScriptedObject("scripts\PacketHandlersInitialize.vb", "", True)
            PacketHandlersInitialize.Invoke("ScriptedHandlers", "Initialize")
            PacketHandlersInitialize.Dispose()


            Dim tmp As String = "", CommandList() As String, cmds() As String
            Dim varList As Integer
            While Not PL._flagStopListen
                Try
                    tmp = Console.ReadLine()
                    CommandList = tmp.Split(";")

                    For varList = LBound(CommandList) To UBound(CommandList)
                        cmds = Split(CommandList(varList), " ", 2)
                        If CommandList(varList).Length > 0 Then
                            '<<<<<<<<<<<COMMAND STRUCTURE>>>>>>>>>>
                            Select Case cmds(0).ToLower
                                Case "/quit", "/shutdown", "/off", "/kill"
                                    ConsoleColor.SetConsoleColor(ConsoleColorClass.ForegroundColors.Green)
                                    Console.WriteLine("PacketLogger shutting down...")
                                    FileLogger.Log("PacketLogger shutting down...")
                                    ConsoleColor.SetConsoleColor()
                                    PL._flagStopListen = True
                                    Exit While
                                Case "/parse"
                                    StartReadingLog(cmds(1))
                                Case Else
                                    Console.WriteLine("Unknown command.")
                            End Select
                            '<<<<<<<<<<</END COMMAND STRUCTURE>>>>>>>>>>>>
                        End If
                    Next
                Catch e As Exception
                    ConsoleColor.SetConsoleColor(ConsoleColorClass.ForegroundColors.LightRed)
                    Console.WriteLine("[{0}] Error executing command [{1}]. {3}{2}", Format(TimeOfDay, "hh:mm:ss"), tmp, e.ToString, vbNewLine)
                    ConsoleColor.SetConsoleColor()
                End Try
            End While
        End Sub

        Public ASCII As New Text.ASCIIEncoding
        Public Function DumpPacket(ByVal data() As Byte) As String
            Dim j As Integer
            Dim buffer As String = ""

            'If (data.Length Mod 16) <> 0 Then ReDim Preserve data(data.Length - (data.Length Mod 16) + 16 - 1)
            If data.Length Mod 16 = 0 Then
                For j = 0 To data.Length - 1 Step 16
                    buffer += "|  " & BitConverter.ToString(data, j, 16).Replace("-", " ")
                    buffer += " |  " & ASCII.GetString(data, j, 16).Replace(vbTab, "?").Replace(vbBack, "?").Replace(vbCr, "?").Replace(vbFormFeed, "?").Replace(vbLf, "?") & " |" & vbNewLine
                Next
            Else
                For j = 0 To data.Length - 1 - 16 Step 16
                    buffer += "|  " & BitConverter.ToString(data, j, 16).Replace("-", " ")
                    buffer += " |  " & ASCII.GetString(data, j, 16).Replace(vbTab, "?").Replace(vbBack, "?").Replace(vbCr, "?").Replace(vbFormFeed, "?").Replace(vbLf, "?") & " |" & vbNewLine
                Next

                buffer += "|  " & BitConverter.ToString(data, j, data.Length Mod 16).Replace("-", " ")
                buffer += New String(" ", (16 - data.Length Mod 16) * 3)
                buffer += " |  " & ASCII.GetString(data, j, data.Length Mod 16).Replace(vbTab, "?").Replace(vbBack, "?").Replace(vbCr, "?").Replace(vbFormFeed, "?").Replace(vbLf, "?")
                buffer += New String(" ", 16 - data.Length Mod 16)
                buffer += " |" & vbNewLine
            End If



            Return buffer
        End Function

    End Module
End Namespace