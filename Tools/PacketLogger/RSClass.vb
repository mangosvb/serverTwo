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

Namespace PacketLogger
    Public Module RSClass
        Const CMD_AUTH_LOGON_CHALLENGE As Integer = &H0
        Const CMD_AUTH_LOGON_PROOF As Integer = &H1
        Const CMD_AUTH_RECONNECT_CHALLENGE As Integer = &H2
        Const CMD_AUTH_RECONNECT_PROOF As Integer = &H3
        Const CMD_AUTH_UPDATESRV As Integer = &H4
        Const CMD_AUTH_REALMLIST As Integer = &H10

        Public Class RSClientClass
            Implements IDisposable

            Public Socket As Socket
            Public connSocket As Socket = New Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP)
            Public IP As Net.IPAddress = Net.IPAddress.Parse("0.0.0.0")
            Public Port As Int32 = 0
            Public connIP As Net.IPAddress = Net.IPAddress.Parse("0.0.0.0")
            Public connPort As Int32 = 0
            Public AuthEngine As AuthEngineClass
            Private Account() As Byte
            Private Password() As Byte
            Public WSOnline As Byte = 2



            Public Sub OnServerData(ByVal data() As Byte)
                FileLogger.LogPacket(data, packetType.RECV_PACKET, packetSource.RS)
                Select Case data(0)
                    Case CMD_AUTH_LOGON_CHALLENGE, CMD_AUTH_RECONNECT_CHALLENGE
                        HandleLogonChallengeServer(data)
                        SendToClient(data)
                    Case CMD_AUTH_LOGON_PROOF, CMD_AUTH_RECONNECT_PROOF
                        SendToClient(data)
                    Case CMD_AUTH_REALMLIST
                        HandleRealmlist(data)
                    Case Else
                        'TODO: Write Opcode
                        Console.WriteLine("[{0}]RS: [{1}:{2}] Unknown Opcode 0x{3}", Format(TimeOfDay, "hh:mm:ss"), IP, Port, data(0))
                        SendToClient(data)
                End Select
            End Sub
            Public Sub OnClientData(ByVal data() As Byte)
                FileLogger.LogPacket(data, packetType.SENT_PACKET, packetSource.RS)
                Select Case data(0)
                    Case CMD_AUTH_LOGON_CHALLENGE, CMD_AUTH_RECONNECT_CHALLENGE
                        HandleLogonChallengeClient(data)

                        Dim Host As IPEndPoint = New IPEndPoint(Dns.GetHostEntry(Config.rsConnHost).AddressList(0), Config.rsConnPort)
                        connSocket.Connect(Host)
                        Dim NewThread As Thread
                        NewThread = New Thread(AddressOf ProcessServerConnection)
                        NewThread.Name = "Packet Logger, Server Connected"
                        NewThread.Start()

                        Send(data)
                    Case CMD_AUTH_LOGON_PROOF, CMD_AUTH_RECONNECT_PROOF
                        HandleLogonProof(data)
                        Send(data)
                    Case CMD_AUTH_REALMLIST
                        Send(data)
                    Case Else
                        'TODO: Write Opcode
                        Console.WriteLine("[{0}]RS: [{1}:{2}] Unknown Opcode 0x{3}", Format(TimeOfDay, "hh:mm:ss"), IP, Port, data(0))
                End Select
            End Sub
            Public Sub ProcessClientConnection()
                IP = CType(Socket.RemoteEndPoint, IPEndPoint).Address
                Port = CType(Socket.RemoteEndPoint, IPEndPoint).Port
                FileLogger.Log(String.Format("RealmServer: New connection from [{0}:{1}].", IP, Port))

                Dim Buffer() As Byte
                Dim bytes As Integer
                'Dim RecvMessage As String

                Dim oThread As Thread
                oThread = Thread.CurrentThread()

                ConsoleColor.SetConsoleColor(ConsoleColorClass.ForegroundColors.Gray)
                Console.WriteLine("[{0}]RS: Incoming connection from [{1}:{2}]", Format(TimeOfDay, "hh:mm:ss"), IP, Port)
                ConsoleColor.SetConsoleColor()

                Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 1000)
                Socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 1000)

                While Not PL._flagStopListen
                    Thread.Sleep(CONNETION_SLEEP_TIME)
                    If Socket.Available > 0 Then
                        ReDim Buffer(Socket.Available - 1)
                        bytes = Socket.Receive(Buffer, Buffer.Length, 0)
                        OnClientData(Buffer)
                        'RaiseEvent OnData(Buffer, Me)
                    End If
                    If Not Socket.Connected Then Exit While
                    If (Socket.Poll(100, SelectMode.SelectRead)) And (Socket.Available = 0) Then Exit While
                End While

                Socket.Close()

                ConsoleColor.SetConsoleColor(ConsoleColorClass.ForegroundColors.Gray)
                Console.WriteLine("[{0}]RS: Connection from [{1}:{2}] closed", Format(TimeOfDay, "hh:mm:ss"), IP, Port)
                ConsoleColor.SetConsoleColor()
                FileLogger.Log(String.Format("RealmServer: Connection from [{0}:{1}] closed.", IP, Port))

                Me.Dispose()
                oThread.Abort()
            End Sub
            Public Sub ProcessServerConnection()
                connIP = CType(connSocket.RemoteEndPoint, IPEndPoint).Address
                connPort = CType(connSocket.RemoteEndPoint, IPEndPoint).Port
                FileLogger.Log(String.Format("RealmServer: New connection to [{0}:{1}].", connIP, connPort))

                Dim Buffer() As Byte
                Dim bytes As Integer
                'Dim RecvMessage As String

                Dim oThread As Thread
                oThread = Thread.CurrentThread()

                ConsoleColor.SetConsoleColor(ConsoleColorClass.ForegroundColors.Gray)
                Console.WriteLine("[{0}]RS: Incoming connection from [{1}:{2}]", Format(TimeOfDay, "hh:mm:ss"), connIP, connPort)
                ConsoleColor.SetConsoleColor()

                connSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.SendTimeout, 1000)
                connSocket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReceiveTimeout, 1000)

                While Not PL._flagStopListen
                    Thread.Sleep(CONNETION_SLEEP_TIME)
                    If connSocket.Available > 0 Then
                        ReDim Buffer(connSocket.Available - 1)
                        bytes = connSocket.Receive(Buffer, Buffer.Length, 0)
                        OnServerData(Buffer)
                    End If
                    If Not connSocket.Connected Then Exit While
                    If (connSocket.Poll(100, SelectMode.SelectRead)) And (connSocket.Available = 0) Then Exit While
                End While

                connSocket.Close()

                ConsoleColor.SetConsoleColor(ConsoleColorClass.ForegroundColors.Gray)
                Console.WriteLine("[{0}]RS: Connection from [{1}:{2}] closed", Format(TimeOfDay, "hh:mm:ss"), connIP, connPort)
                ConsoleColor.SetConsoleColor()
                FileLogger.Log(String.Format("RealmServer: Connection to [{0}:{1}] closed.", connIP, connPort))

                Me.Dispose()
                oThread.Abort()
            End Sub
            Public Sub Send(ByRef packet As PacketClass)
                Send(packet.Data)
            End Sub
            Public Sub Send(ByVal data() As Byte)
                Try
                    Dim i As Integer = connSocket.Send(data, 0, data.Length, SocketFlags.None)

                    ConsoleColor.SetConsoleColor(ConsoleColorClass.ForegroundColors.Gray)
                    Console.WriteLine("[{0}] [{1}:{2}] Data sent, result code={3}", Format(TimeOfDay, "hh:mm:ss"), connIP, connPort, i)
                    ConsoleColor.SetConsoleColor()
                Catch Err As Exception
                    ConsoleColor.SetConsoleColor(ConsoleColorClass.ForegroundColors.LightRed)
                    Console.WriteLine("[{0}] Connection from [{1}:{2}] do not exist - ERROR!!!", Format(TimeOfDay, "hh:mm:ss"), connIP, connPort)
                    ConsoleColor.SetConsoleColor()
                    Socket.Close()
                End Try
            End Sub
            Public Sub SendToClient(ByRef packet As PacketClass)
                SendToClient(packet.Data)
            End Sub
            Public Sub SendToClient(ByVal data() As Byte)
                Try
                    Dim i As Integer = Socket.Send(data, 0, data.Length, SocketFlags.None)

                    ConsoleColor.SetConsoleColor(ConsoleColorClass.ForegroundColors.Gray)
                    Console.WriteLine("[{0}] [{1}:{2}] Data sent, result code={3}", Format(TimeOfDay, "hh:mm:ss"), IP, Port, i)
                    ConsoleColor.SetConsoleColor()
                Catch Err As Exception
                    ConsoleColor.SetConsoleColor(ConsoleColorClass.ForegroundColors.LightRed)
                    Console.WriteLine("[{0}] Connection from [{1}:{2}] do not exist - ERROR!!!", Format(TimeOfDay, "hh:mm:ss"), IP, Port)
                    ConsoleColor.SetConsoleColor()
                    Socket.Close()
                End Try
            End Sub
            Public Sub Dispose() Implements System.IDisposable.Dispose
                'ConsoleColor.SetConsoleColor(ConsoleColor.ForegroundColors.Gray)
                'Console.WriteLine("[{0}]RS: Connection from [{1}:{2}] deleted", Format(TimeOfDay, "hh:mm:ss"), IP, Port)
                'Console.WriteLine("[{0}]RS: Connection from [{1}:{2}] deleted", Format(TimeOfDay, "hh:mm:ss"), connIP, connPort)
                'ConsoleColor.SetConsoleColor()
                Try
                    connSocket.Shutdown(SocketShutdown.Both)
                Catch
                End Try
                Try
                    Socket.Shutdown(SocketShutdown.Both)
                Catch
                End Try
            End Sub

            Public Sub HandleLogonChallengeClient(ByRef Data() As Byte)
                'ReDim Account(Data(33) - 1)
                'Array.Copy(Data, 34, Account, 0, Data(33))

                'Dim i As Integer
                'ReDim Password(Config.Password.Length - 1)
                'For i = 0 To Config.Password.Length - 1
                '    Password(i) = Asc(Config.Password.Substring(i, 1))
                'Next
            End Sub
            Public Sub HandleLogonChallengeServer(ByRef Data() As Byte)
                'AuthEngine = New AuthEngineClass

                'AuthEngine.g(0) = Data(36)
                'Array.Copy(Data, 3, AuthEngine.PublicB, 0, 32)
                'Array.Copy(Data, 38, AuthEngine.N, 0, 32)
                'Array.Copy(Data, 70, AuthEngine.salt, 0, 32)
                'Array.Copy(Data, 102, AuthEngine.unk3, 0, 16)

                'AuthEngine.CalculateX(Account, Password)
            End Sub
            Public Sub HandleLogonProof(ByRef Data() As Byte)
                'Dim A(31) As Byte
                'Array.Copy(Data, 1, A, 0, 32)
                'Dim M1(19) As Byte
                'Array.Copy(Data, 33, M1, 0, 20)

                'AuthEngine.CalculateU(A)
                'AuthEngine.CalculateM1()

                'Dim wrong_pass = False
                'Dim i As Byte
                'For i = 0 To 19
                '    If M1(i) <> AuthEngine.M1(i) Then wrong_pass = True
                'Next

                'If wrong_pass Then
                '    ConsoleColor.SetConsoleColor(ConsoleColor.ForegroundColors.LightRed)
                '    Console.WriteLine("ERROR: Password is not matching, world packet decoding will not work correctly!")
                '    ConsoleColor.SetConsoleColor()
                'End If

                'AuthEngine.CalculateM2(M1)

                'SSHash = AuthEngine.SS_Hash
                'Console.WriteLine(String.Format("[{0}]RS: [{1}:{2}] SSHash calculated.", Format(TimeOfDay, "hh:mm:ss"), IP, Port))
            End Sub
            Public Sub HandleRealmlist(ByRef Data() As Byte)
                Dim packet_len As Integer = 0
                Dim wsName As String = "Redirected Realm"
                packet_len = Len(Config.wsHost) + Len(wsName) + 1 + Len(Format(Config.wsPort, "0")) + 12

                Dim data_response(packet_len + 10) As Byte
                data_response(3) = Data(1)
                data_response(4) = Data(2)
                data_response(5) = Data(3)
                data_response(6) = Data(4)
                data_response(0) = CMD_AUTH_REALMLIST

                '(uint16) Packet Length
                data_response(2) = (packet_len + 8) \ 256
                data_response(1) = (packet_len + 8) Mod 256

                '(uint16) Realms Count
                data_response(7) = 1
                data_response(8) = 0
                Dim tmp As Integer = 9

                '(uint8) Realm Icon
                '   0 -> Normal; 1 -> PvP; 6 -> RP; 8 -> RPPvP;
                Converter.ToBytes(CType(1, Byte), data_response, tmp)
                '(uint8) IsLocked
                '	0 -> none; 1 -> locked
                Converter.ToBytes(CType(0, Byte), data_response, tmp)
                '(uint8) Realm Color 
                '   0 -> Green; 1 -> Red; 2 -> Offline;
                Converter.ToBytes(CType(0, Byte), data_response, tmp)
                '(string) Realm Name (zero terminated)
                Converter.ToBytes(CType(wsName, String), data_response, tmp)
                Converter.ToBytes(CType(0, Byte), data_response, tmp) '\0
                '(string) Realm Address ("ip:port", zero terminated)
                Converter.ToBytes(CType(Config.wsHost & ":" & Config.wsPort, String), data_response, tmp)
                Converter.ToBytes(CType(0, Byte), data_response, tmp) '\0
                '(float) Population 
                '   400F -> Full; 5F -> Medium; 1.6F -> Low; 200F -> New; 2F -> High
                '   00 00 48 43 -> Recommended
                '   00 00 C8 43 -> Full
                '   9C C4 C0 3F -> Low
                '   BC 74 B3 3F -> Low
                Converter.ToBytes(CType(0, Single), data_response, tmp)
                '(byte) Number of character at this realm for this account
                Converter.ToBytes(CType(1, Byte), data_response, tmp)
                '(byte) Timezone 
                '	UnitedKingdom = 0x0, USA = 0x1, Germany = 0x2, France = 0x3, Other = 0x4, Oceania = 0x5, Spain = 0x5
                Converter.ToBytes(CType(0, Byte), data_response, tmp)
                '(byte) Unknown (may be 2 -> TestRealm)
                Converter.ToBytes(CType(0, Byte), data_response, tmp)

                'unknown1, 0x2A
                Converter.ToBytes(CType(&H2A, Byte), data_response, tmp) '2=list of realms 0=wizard
                'unknown2, 0x00
                Converter.ToBytes(CType(0, Byte), data_response, tmp)
                SendToClient(data_response)
            End Sub
        End Class


    End Module


End Namespace
