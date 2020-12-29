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

Public Module WC_Handlers

    Public Sub IntializePacketHandlers()
        'NOTE: These opcodes are not used in any way
        '   none

        'NOTE: These opcodes below are handled by Voice Server
        '   none

        'NOTE: TODO Opcodes
        '   none

    End Sub

    'Public Sub OnUnhandledPacket(ByRef packet As PacketClass, ByRef Client As ClientClass)
    '    Log.WriteLine(LogType.WARNING, "[{0}:{1}] {2} [Unhandled Packet]", Client.IP, Client.Port, CType(packet.OpCode, OPCODES))
    'End Sub

End Module