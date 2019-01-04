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
Namespace PacketLogger
    Public Module LogReader

        Public Sub StartReadingLog(ByVal LogFile As String)
            PL.wsClient = New WSClientClass
            Dim file As New System.IO.StreamReader(LogFile)
            Dim data() As Byte
            Dim dataList As New ArrayList
            Dim char1 As Byte = 0

            While char1 <> 255
                dataList.Clear()

                Dim tmp As String
                Dim charArray() As String
                Dim i As Integer
                char1 = file.Read
                While char1 = Asc("|")
                    file.Read()
                    file.Read()

                    tmp = Trim(file.ReadLine)
                    charArray = Split(tmp, "|", 2)
                    charArray = Split(charArray(0), " ")
                    For i = 0 To 15
                        If Trim(charArray(i)) = "" Then Exit For
                        dataList.Add(CType(Val("&H" & charArray(i)), Byte))
                    Next


                    char1 = file.Read
                End While

                If dataList.Count > 0 Then
                    ReDim data(dataList.Count - 1)
                    dataList.CopyTo(data)
                    PL.wsClient.OnServerData(data)
                End If
            End While

            file.Close()
            Console.WriteLine("Parsing of file <{0}> finished.", LogFile)
        End Sub

    End Module
End Namespace
