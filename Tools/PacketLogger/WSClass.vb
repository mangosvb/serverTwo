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


'PacketHandlers(<OPCODE>).Invoke(<PACKET>)
Namespace PacketLogger
    Public Module WSClass
        Class WSClientClass
            Implements IDisposable

            Public Socket As Socket
            Public connSocket As Socket = New Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.IP)
            Public IP As Net.IPAddress = Net.IPAddress.Parse("0.0.0.0")
            Public Port As Int32 = 0
            Public connIP As Net.IPAddress = Net.IPAddress.Parse("0.0.0.0")
            Public connPort As Int32 = 0


            Private servEncryption As Boolean = False
            Private clientEncryption As Boolean = False
            Private Key_Client() As Byte = {0, 0, 0, 0}
            Private Key_Server() As Byte = {0, 0, 0, 0}

            Private servBuffer As Byte() = Nothing
            Private undecodedBuffer As Byte() = Nothing

            Public Sub OnServerData(ByVal data() As Byte)
                SendToClient(data)

                Dim PacketBuffer As New PacketClass(data)

                'DONE: Restore undecoded data
                If Not undecodedBuffer Is Nothing Then
                    Dim oldDataLength As Byte = data.Length
                    ReDim Preserve data(data.Length + undecodedBuffer.Length - 1)
                    Array.Copy(data, 0, data, undecodedBuffer.Length, oldDataLength)
                    Array.Copy(undecodedBuffer, 0, data, 0, undecodedBuffer.Length)
                    undecodedBuffer = Nothing

                    PacketBuffer = New PacketClass(data)
                    If servEncryption Then DecodeServer(PacketBuffer.Data, PacketBuffer.Length)
                End If



                'Try
                If servBuffer Is Nothing Then
                    'DONE: No buffered data
                    If servEncryption Then DecodeServer(PacketBuffer.Data, PacketBuffer.Length)
                Else
                    'DONE: Restore decoded buffered data
                    Dim oldDataLength As Byte = data.Length
                    ReDim Preserve data(data.Length + servBuffer.Length - 1)
                    Array.Copy(data, 0, data, servBuffer.Length, oldDataLength)
                    Array.Copy(servBuffer, 0, data, 0, servBuffer.Length)
                    servBuffer = Nothing

                    PacketBuffer = New PacketClass(data)
                End If


                If PacketBuffer.OpCode = OPCODES.SMSG_AUTH_CHALLENGE Then
                    servEncryption = True
                    Console.WriteLine("[{0}]WS: [{1}:{2}] Server Encryption ON.", Format(TimeOfDay, "hh:mm:ss"), IP, Port)
                End If

                Dim packets As New Queue
                While PacketBuffer.Length < PacketBuffer.Data.Length - 2
                    'DONE: 2+ in 1 packet
                    Dim tmpbuff() As Byte
                    ReDim Preserve tmpbuff(PacketBuffer.Data.Length - 1 - PacketBuffer.Length - 2)
                    Array.Copy(PacketBuffer.Data, PacketBuffer.Length + 2, tmpbuff, 0, PacketBuffer.Data.Length - PacketBuffer.Length - 2)
                    ReDim Preserve PacketBuffer.Data(PacketBuffer.Length + 1)
                    Dim packet As New PacketClass(PacketBuffer.Data)
                    packets.Enqueue(packet)
                    PacketBuffer.Data = tmpbuff

                    If servEncryption Then
                        If PacketBuffer.Data.Length > 3 Then
                            'DONE: Got header, rest is unknown size
                            DecodeServer(PacketBuffer.Data, PacketBuffer.Length)
                        Else
                            'DONE: Don't have full header, copy it in undecodedBuffer
                            ReDim undecodedBuffer(PacketBuffer.Data.Length - 1)
                            Array.Copy(PacketBuffer.Data, undecodedBuffer, PacketBuffer.Data.Length)
                            Exit While
                        End If
                    End If
                End While

                'DONE: If packet is too big for sending on one time
                If PacketBuffer.Length = PacketBuffer.Data.Length - 2 Then
                    packets.Enqueue(PacketBuffer)
                    servBuffer = Nothing
                ElseIf PacketBuffer.Data.Length > 3 Then
                    'DONE: Leave the rest of partial packet's data into buffer
                    Console.WriteLine("DEBUG: Left {0} bytes of decoded data in buffer!", PacketBuffer.Data.Length)
                    ReDim servBuffer(PacketBuffer.Data.Length - 1)
                    Array.Copy(PacketBuffer.Data, servBuffer, PacketBuffer.Data.Length)
                End If

                For Each packet As PacketClass In packets
                    FileLogger.LogPacket(packet, packetType.RECV_PACKET)
                    If PacketHandlers.ContainsKey(packet.OpCode) = True Then
                        PacketHandlers(packet.OpCode).Invoke(packet)
                    End If
                Next

                'Catch Err As Exception
                '    ConsoleColor.SetConsoleColor(ConsoleColorClass.ForegroundColors.LightRed)
                '    Console.WriteLine("[{0}] Connection from [{1}:{2}] cause error {4}{3}", Format(TimeOfDay, "hh:mm:ss"), IP, Port, Err.ToString, vbNewLine)
                '    ConsoleColor.SetConsoleColor()
                '    FileLogger.Log(Err.ToString)
                'End Try
                PacketBuffer.Dispose()
            End Sub
            Public Sub OnClientData(ByVal data() As Byte)
                Send(data)

                Dim PacketBuffer As New PacketClass(data)
                Try
                    If clientEncryption Then DecodeClient(PacketBuffer.Data, PacketBuffer.Length)
                    If PacketBuffer.OpCode = OPCODES.CMSG_AUTH_SESSION Then
                        clientEncryption = True
                        Console.WriteLine("[{0}]WS: [{1}:{2}] Client Encryption ON.", Format(TimeOfDay, "hh:mm:ss"), IP, Port)
                    End If

                    Dim packets As New Queue
                    While PacketBuffer.Length < PacketBuffer.Data.Length - 2
                        '2+ in 1 packet
                        Dim tmpbuff() As Byte
                        ReDim Preserve tmpbuff(PacketBuffer.Data.Length - 1 - PacketBuffer.Length - 2)
                        Array.Copy(PacketBuffer.Data, PacketBuffer.Length + 2, tmpbuff, 0, PacketBuffer.Data.Length - PacketBuffer.Length - 2)
                        ReDim Preserve PacketBuffer.Data(PacketBuffer.Length + 1)
                        Dim packet As New PacketClass(PacketBuffer.Data)
                        packets.Enqueue(packet)
                        PacketBuffer.Data = tmpbuff
                        If clientEncryption Then DecodeClient(PacketBuffer.Data, PacketBuffer.Length)
                    End While
                    packets.Enqueue(PacketBuffer)

                    For Each packet As PacketClass In packets
                        FileLogger.LogPacket(packet, packetType.SENT_PACKET)
                        'If packet.OpCode = OPCODES.CMSG_AUTH_SESSION Then
                        '    Encryption = True
                        '    Console.WriteLine("[{0}]WS: [{1}:{2}] Encryption ON.", Format(TimeOfDay, "hh:mm:ss"), IP, Port)
                        'End If
                        If PacketHandlers.ContainsKey(packet.OpCode) = True Then
                            PacketHandlers(packet.OpCode).Invoke(packet)
                        End If
                    Next

                Catch Err As Exception
                    ConsoleColor.SetConsoleColor(ConsoleColorClass.ForegroundColors.LightRed)
                    Console.WriteLine("[{0}] Connection from [{1}:{2}] cause error {4}{3}", Format(TimeOfDay, "hh:mm:ss"), IP, Port, Err.ToString, vbNewLine)
                    ConsoleColor.SetConsoleColor()
                    FileLogger.Log(Err.ToString)
                End Try
                PacketBuffer.Dispose()
            End Sub
            Public Sub ProcessClientConnection()
                IP = CType(Socket.RemoteEndPoint, IPEndPoint).Address
                Port = CType(Socket.RemoteEndPoint, IPEndPoint).Port
                FileLogger.Log(String.Format("WorldServer: New connection from [{0}:{1}].", IP, Port))

                While Logger.PL.rsClient.WSOnline = 2
                    Thread.Sleep(CONNETION_SLEEP_TIME)
                End While
                ConnectToServer()

                Dim Buffer() As Byte
                Dim bytes As Integer
                'Dim RecvMessage As String

                Dim oThread As Thread
                oThread = Thread.CurrentThread()

                ConsoleColor.SetConsoleColor(ConsoleColorClass.ForegroundColors.Gray)
                Console.WriteLine("[{0}]WS: Incoming connection from [{1}:{2}]", Format(TimeOfDay, "hh:mm:ss"), IP, Port)
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
                Console.WriteLine("[{0}]WS: Connection from [{1}:{2}] closed", Format(TimeOfDay, "hh:mm:ss"), IP, Port)
                ConsoleColor.SetConsoleColor()
                FileLogger.Log(String.Format("WorldServer: Connection from [{0}:{1}] closed.", IP, Port))

                Me.Dispose()
                oThread.Abort()
            End Sub
            Public Sub ProcessServerConnection()
                connIP = CType(connSocket.RemoteEndPoint, IPEndPoint).Address
                connPort = CType(connSocket.RemoteEndPoint, IPEndPoint).Port
                FileLogger.Log(String.Format("WorldServer: New connection to [{0}:{1}].", connIP, connPort))

                Dim Buffer() As Byte
                Dim bytes As Integer
                'Dim RecvMessage As String

                Dim oThread As Thread
                oThread = Thread.CurrentThread()

                ConsoleColor.SetConsoleColor(ConsoleColorClass.ForegroundColors.Gray)
                Console.WriteLine("[{0}]WS: Incoming connection from [{1}:{2}]", Format(TimeOfDay, "hh:mm:ss"), connIP, connPort)
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
                Console.WriteLine("[{0}]WS: Connection to [{1}:{2}] closed", Format(TimeOfDay, "hh:mm:ss"), connIP, connPort)
                ConsoleColor.SetConsoleColor()
                FileLogger.Log(String.Format("WorldServer: Connection from [{0}:{1}] closed.", connIP, connPort))


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
                'Console.WriteLine("[{0}]WS: Connection from [{1}:{2}] deleted", Format(TimeOfDay, "hh:mm:ss"), IP, Port)
                'Console.WriteLine("[{0}]WS: Connection from [{1}:{2}] deleted", Format(TimeOfDay, "hh:mm:ss"), connIP, connPort)
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

            Public Sub ConnectToServer()
                'Dim Host As IPEndPoint = New IPEndPoint(Dns.Resolve(Config.wsConnHost).AddressList(0), Config.wsConnPort)
                Dim Host As IPEndPoint = New IPEndPoint(IPAddress.Parse(Config.wsConnHost), Config.wsConnPort)
                connSocket.Connect(Host)
                Dim NewThread As Thread
                NewThread = New Thread(AddressOf ProcessServerConnection)
                NewThread.Name = "Packet Logger, Server Connected"
                NewThread.Start()
            End Sub



            Public Sub DecodeClient(ByRef data() As Byte, ByRef length As Integer)
                'Dim buffer1 As Byte() = Me.SS_Hash

                Dim num1 As Integer
                For num1 = 0 To 6 - 1
                    Dim num2 As Byte = Me.Key_Client(0)
                    Me.Key_Client(0) = data(num1)
                    Dim num3 As Byte = data(num1)
                    num3 = CType((num3 - num2), Byte)
                    Dim num4 As Byte = Me.Key_Client(1)
                    num2 = SSHash(num4)
                    num2 = CType((num2 Xor num3), Byte)
                    data(num1) = num2
                    num2 = Me.Key_Client(1)
                    num2 = CType((num2 + 1), Byte)
                    Me.Key_Client(1) = CType((num2 Mod 40), Byte)
                Next num1
            End Sub
            Public Sub DecodeServer(ByRef data() As Byte, ByRef length As Integer)
                'Dim buffer1 As Byte() = Me.SS_Hash

                Dim num1 As Integer
                For num1 = 0 To 4 - 1
                    Dim num2 As Byte = Me.Key_Server(0)
                    Me.Key_Server(0) = data(num1)
                    Dim num3 As Byte = data(num1)
                    num3 = CType((num3 - num2), Byte)
                    Dim num4 As Byte = Me.Key_Server(1)
                    num2 = SSHash(num4)
                    num2 = CType((num2 Xor num3), Byte)
                    data(num1) = num2
                    num2 = Me.Key_Server(1)
                    num2 = CType((num2 + 1), Byte)
                    Me.Key_Server(1) = CType((num2 Mod 40), Byte)
                Next num1
            End Sub
        End Class
    End Module
End Namespace
