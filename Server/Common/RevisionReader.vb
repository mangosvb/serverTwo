' 
' Copyright (C) 2009 Spurious <http://SpuriousEmu.com>
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
' build revision extractor by 0xR3v0luti0n

Imports System.IO

Public Class RevisionReader
    Shared Function GetBuildRevision() As Integer
        Try

            Dim fi As New FileInfo(CurDir().Replace("ServerFiles", "") + "\.svn\entries")
            If fi.Exists = False Then Return 0

            Dim Data() As String = System.IO.File.ReadAllLines(fi.FullName)

            Dim revision As Integer = Convert.ToInt32(Data(3))
            Return revision
        Catch ex As Exception
            Return 0
        End Try
    End Function
End Class
