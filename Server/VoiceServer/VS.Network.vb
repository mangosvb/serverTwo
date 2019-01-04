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
Imports System
Imports System.IO
Imports System.Threading
Imports System.Net
Imports System.Net.Sockets
Imports System.Runtime.Remoting
Imports System.Runtime.CompilerServices
Imports System.Security.Permissions
Imports mangosVB.Common.BaseWriter
Imports mangosVB.Common

Public Module WC_Network

#Region "WS.Sockets"

    Public VS As VoiceServerClass

    Public DataTransferIn As Long = 0
    Public DataTransferOut As Long = 0

    Class VoiceServerClass
        Inherits MarshalByRefObject
        Implements IVoice
        Implements IDisposable

        <CLSCompliant(False)> _
        Public m_flagStopListen As Boolean = False
        Private m_Socket As Socket
        Private m_RemoteChannel As Channels.IChannel = Nothing
        Private m_RemoteURI As String = ""
        Private m_LocalURI As String = ""
        Public Cluster As ICluster = Nothing

        Public Sub New()
            Try
                m_RemoteURI = String.Format("{0}://{1}:{2}/Cluster.rem", Config.ClusterMethod, Config.ClusterHost, Config.ClusterPort)
                m_LocalURI = String.Format("{0}://{1}:{2}/VoiceServer.rem", Config.ClusterMethod, Config.LocalHost, Config.LocalPort)
                Cluster = Nothing

                'Create sockets
                m_Socket = New Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp)
                m_Socket.Bind(New IPEndPoint(Net.IPAddress.Parse(Config.VSHost), Config.VSPort))

                'Start listening
                AsyncBeginReceive()

                Log.WriteLine(LogType.SUCCESS, "Listening on {0} on port {1}", Net.IPAddress.Parse(Config.VSHost), Config.VSPort)

                'Create Remoting Channel
                Select Case Config.ClusterMethod
                    Case "ipc"
                        m_RemoteChannel = New Channels.Ipc.IpcChannel(String.Format("{0}:{1}", Config.LocalHost, Config.LocalPort))
                    Case "tcp"
                        m_RemoteChannel = New Channels.Tcp.TcpChannel(Config.LocalPort)
                End Select

                Channels.ChannelServices.RegisterChannel(m_RemoteChannel, False)
                RemotingServices.Marshal(CType(Me, IVoice), "VoiceServer.rem")

                'Notify Cluster About Us
                ClusterConnect()

            Catch e As Exception
                Console.WriteLine()
                Log.WriteLine(LogType.FAILED, "Error in {1}: {0}.", e.Message, e.Source)
            End Try
        End Sub
        Public Sub Dispose() Implements IDisposable.Dispose
            m_flagStopListen = True
            m_Socket.Close()
        End Sub

        Public Sub AsyncBeginReceive()
            Dim p As New PacketClass

            Try
                m_Socket.BeginReceiveFrom(p.Data, 0, PacketClass.BUFFER_SIZE, SocketFlags.None, p.IP, AddressOf AsyncEndReceive, p)
            Catch ex As Exception
                Log.WriteLine(LogType.WARNING, "Connection from [{0}:{1}] cause error {2}{3}", p.IP.ToString, 0, ex.ToString, vbNewLine)
            End Try
        End Sub
        Public Sub AsyncEndReceive(ByVal ar As IAsyncResult)
            AsyncBeginReceive()

            Dim p As PacketClass = CType(ar.AsyncState, PacketClass)

            Try
                p.Length = m_Socket.EndReceiveFrom(ar, p.IP)
                Interlocked.Add(DataTransferOut, p.Length)

                OnPacket(p)

            Catch ex As Exception
                Log.WriteLine(LogType.WARNING, "Connection from [{0}:{1}] cause error {2}{3}", p.IP.ToString, 0, ex.ToString, vbNewLine)
            End Try
        End Sub
        Public Sub AsyncBeginSend(ByVal p As PacketClass)
            Try
                m_Socket.BeginSendTo(p.Data, 0, p.Length, SocketFlags.None, p.IP, AddressOf AsyncEndSend, p)
            Catch ex As Exception
                Log.WriteLine(LogType.WARNING, "Connection from [{0}:{1}] cause error {2}{3}", p.IP.ToString, 0, ex.ToString, vbNewLine)
            End Try
        End Sub
        Public Sub AsyncEndSend(ByVal ar As IAsyncResult)
            Dim p As PacketClass = CType(ar, PacketClass)

            Try
                Dim bytesSent As Integer = m_Socket.EndSendTo(ar)
                Interlocked.Add(DataTransferOut, bytesSent)

                Log.WriteLine(LogType.WARNING, "Sent {0} bytes to {1}", bytesSent, p.IP.ToString)
            Catch ex As Exception
                Log.WriteLine(LogType.WARNING, "Connection from [{0}:{1}] cause error {2}{3}", p.IP.ToString, 0, ex.ToString, vbNewLine)
            End Try
        End Sub

        Public Sub OnPacket(ByVal p As PacketClass)
            Log.WriteLine(LogType.DEBUG, "Unknown packet from {0}", p.IP.ToString)
            DumpPacket(p)
        End Sub

        Public Sub ClusterConnect()
            While Cluster Is Nothing
                Try
                    Cluster = RemotingServices.Connect(GetType(ICluster), m_RemoteURI)

                    Cluster.VoiceConnect(m_LocalURI, Config.GetVSHost, Config.VSPort, Config.EncryptionKey)
                    Exit While
                Catch e As Exception
                    Log.WriteLine(LogType.FAILED, "Unable to connect to cluster. [{0}]", e.Message)
                End Try
                Cluster = Nothing
                Thread.Sleep(3000)
            End While

            Log.WriteLine(LogType.SUCCESS, "Contacted cluster [{0}]", m_RemoteURI)
        End Sub
        Public Sub ClusterDisconnect()
            Try
                Cluster.VoiceDisconnect()
            Catch
            Finally
                Cluster = Nothing
            End Try
        End Sub

        Public Function ChannelCreate(ByVal Type As Byte, ByVal Name As String) As UShort Implements Common.IVoice.ChannelCreate
            Return 0
        End Function
        Public Sub ChannelDestroy(ByVal ChannelID As UShort) Implements Common.IVoice.ChannelDestroy
        End Sub
        Public Function ClientConnect(ByVal ChannelID As UShort) As Byte Implements Common.IVoice.ClientConnect
            Return 0
        End Function
        Public Sub ClientDisconnect(ByVal ChannelID As UShort, ByVal Slot As Byte) Implements Common.IVoice.ClientDisconnect
        End Sub

        Public Function Ping(ByVal Timestamp As Integer) As Integer Implements Common.IVoice.Ping
            'Log.WriteLine(LogType.DEBUG, "Cluster ping: [{0}ms]", timeGetTime - Timestamp)
            Return System.Environment.TickCount
        End Function
    End Class

    Public Class PacketClass
        Public Const BUFFER_SIZE As Integer = 4096

        Public Data() As Byte
        Public Length As Integer
        Public IP As EndPoint

        Public Sub New()
            ReDim Data(BUFFER_SIZE - 1)
            IP = New IPEndPoint(IPAddress.Any, 0)
        End Sub
    End Class

    Public Sub DumpPacket(ByVal p As PacketClass)
        Dim j As Integer
        Dim buffer As String = ""
        Try
            buffer = buffer + String.Format("DEBUG: Packet Dump{0}", vbNewLine)

            If p.Length Mod 16 = 0 Then
                For j = 0 To p.Length - 1 Step 16
                    buffer += "|  " & BitConverter.ToString(p.Data, j, 16).Replace("-", " ")
                    buffer += " |  " & System.Text.Encoding.ASCII.GetString(p.Data, j, 16).Replace(vbTab, "?").Replace(vbBack, "?").Replace(vbCr, "?").Replace(vbFormFeed, "?").Replace(vbLf, "?") & " |" & vbNewLine
                Next
            Else
                For j = 0 To p.Length - 1 - 16 Step 16
                    buffer += "|  " & BitConverter.ToString(p.Data, j, 16).Replace("-", " ")
                    buffer += " |  " & System.Text.Encoding.ASCII.GetString(p.Data, j, 16).Replace(vbTab, "?").Replace(vbBack, "?").Replace(vbCr, "?").Replace(vbFormFeed, "?").Replace(vbLf, "?") & " |" & vbNewLine
                Next

                buffer += "|  " & BitConverter.ToString(p.Data, j, p.Length Mod 16).Replace("-", " ")
                buffer += New String(" ", (16 - p.Length Mod 16) * 3)
                buffer += " |  " & System.Text.Encoding.ASCII.GetString(p.Data, j, p.Length Mod 16).Replace(vbTab, "?").Replace(vbBack, "?").Replace(vbCr, "?").Replace(vbFormFeed, "?").Replace(vbLf, "?")
                buffer += New String(" ", 16 - p.Length Mod 16)
                buffer += " |" & vbNewLine
            End If

            Log.WriteLine(LogType.DEBUG, buffer, Nothing)
        Catch e As Exception
            Log.WriteLine(LogType.FAILED, "Error dumping packet: {0}{1}", vbNewLine, e.ToString)
        End Try
    End Sub

#End Region

End Module
