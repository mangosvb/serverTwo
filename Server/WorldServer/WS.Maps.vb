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
Imports System.Threading
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Collections.Generic
Imports mangosVB.Common.BaseWriter
Imports mangosVB.Common

Public Module WS_Maps
#Region "Zones"
    Public AreaTable As New Dictionary(Of Integer, TArea)
    Public Class TArea
        Public ID As Integer
        Public Level As Byte
        Public Zone As Integer
        Public ZoneType As Integer
        Public Team As AreaTeam
        Public Name As String
        Public Enum AreaTeam As Integer
            AREATEAM_NONE = 0
            AREATEAM_ALLY = 2
            AREATEAM_HORDE = 4
        End Enum
        Public Enum AreaFlag As Integer
            AREA_FLAG_SNOW = &H1                ' snow (only Dun Morogh, Naxxramas, Razorfen Downs and Winterspring)
            AREA_FLAG_UNK1 = &H2                ' unknown, (only Naxxramas and Razorfen Downs)
            AREA_FLAG_UNK2 = &H4                ' Only used on development map
            AREA_FLAG_SLAVE_CAPITAL = &H8       ' slave capital city flag?
            AREA_FLAG_UNK3 = &H10               ' unknown
            AREA_FLAG_SLAVE_CAPITAL2 = &H20     ' slave capital city flag?
            AREA_FLAG_UNK4 = &H40               ' many zones have this flag
            AREA_FLAG_ARENA = &H80              ' arena, both instanced and world arenas
            AREA_FLAG_CAPITAL = &H100           ' main capital city flag
            AREA_FLAG_CITY = &H200              ' only for one zone named "City" (where it located?)
            AREA_FLAG_OUTLAND = &H400           ' outland zones? (only Eye of the Storm not have this flag, but have 0x00004000 flag)
            AREA_FLAG_SANCTUARY = &H800         ' sanctuary area (PvP disabled)
            AREA_FLAG_NEED_FLY = &H1000         ' only Netherwing Ledge, Socrethar's Seat, Tempest Keep, The Arcatraz, The Botanica, The Mechanar, Sorrow Wing Point, Dragonspine Ridge, Netherwing Mines, Dragonmaw Base Camp, Dragonmaw Skyway
            AREA_FLAG_UNUSED1 = &H2000          ' not used now (no area/zones with this flag set in 2.4.2)
            AREA_FLAG_OUTLAND2 = &H4000         ' outland zones? (only Circle of Blood Arena not have this flag, but have 0x00000400 flag)
            AREA_FLAG_PVP = &H8000              ' pvp objective area? (Death's Door also has this flag although it's no pvp object area)
            AREA_FLAG_ARENA_INSTANCE = &H10000  ' used by instanced arenas only
            AREA_FLAG_UNUSED2 = &H20000         ' not used now (no area/zones with this flag set in 2.4.2)
            AREA_FLAG_UNK5 = &H40000            ' just used for Amani Pass, Hatchet Hills
            AREA_FLAG_LOWLEVEL = &H100000       ' used for some starting areas with area_level <=15
        End Enum
        Public Function IsMyLand(ByRef c As CharacterObject) As Boolean
            If Team = AreaTeam.AREATEAM_NONE Then Return False
            If c.Side = False Then Return Team = AreaTeam.AREATEAM_ALLY
            If c.Side = True Then Return Team = AreaTeam.AREATEAM_HORDE
        End Function
        Public Function IsCity() As Boolean
            Return ZoneType = 312
        End Function
        Public Function NeedFlyingMount() As Boolean
            Return (ZoneType And AreaFlag.AREA_FLAG_NEED_FLY)
        End Function
        Public Function IsSanctuary() As Boolean
            Return (ZoneType And AreaFlag.AREA_FLAG_SANCTUARY)
        End Function
        Public Function IsArena() As Boolean
            Return (ZoneType And AreaFlag.AREA_FLAG_ARENA)
        End Function
    End Class
#End Region

#Region "Continents"

    'NOTE: Map resolution. The resolution of your map files in your maps folder.
    Public Const RESOLUTION_ZMAP As Integer = 64 - 1

    Public Class TMapTile
        Implements IDisposable

        Public Const SIZE As Single = 533.3333F
        Public Const RESOLUTION_WATER As Integer = 128 - 1
        Public Const RESOLUTION_FLAGS As Integer = 16 - 1
        Public Const RESOLUTION_TERRAIN As Integer = 16 - 1

        'TMap contains 64x64 TMapTile(s)
        Public AreaFlag(RESOLUTION_FLAGS, RESOLUTION_FLAGS) As UShort
        Public AreaTerrain(RESOLUTION_TERRAIN, RESOLUTION_TERRAIN) As Byte
        Public WaterLevel(RESOLUTION_WATER, RESOLUTION_WATER) As Single
        Public ZCoord(RESOLUTION_ZMAP, RESOLUTION_ZMAP) As Single

#If ENABLE_PPOINTS Then
        Public ZCoord_PP(RESOLUTION_ZMAP, RESOLUTION_ZMAP) As Single
        Public ZCoord_PP_ModTimes As Integer = 0

        Public Sub ZCoord_PP_Save()
            ZCoord_PP_ModTimes = 0

            Dim fileName As String = String.Format("maps\{0}{1}{2}.pp", Format(CellMap, "000"), Format(CellX, "00"), Format(CellY, "00"))

            Log.WriteLine(LogType.INFORMATION, "Saving PP file [{0}] version [{1}]", fileName, PPOINT_VERSION)

            Dim f As New IO.FileStream(fileName, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read, 16392, FileOptions.WriteThrough)
            Dim w As New BinaryWriter(f)
            w.Write(System.Text.Encoding.ASCII.GetBytes(PPOINT_VERSION))
            For x As Integer = 0 To RESOLUTION_ZMAP
                For y As Integer = 0 To RESOLUTION_ZMAP
                    w.Write(ZCoord_PP(x, y))
                Next y
            Next x
            w.Close()
            f.Close()
        End Sub
#End If

        Public PlayersHere As New List(Of ULong)
        Public CreaturesHere As New List(Of ULong)
        Public GameObjectsHere As New List(Of ULong)
        Public CorpseObjectsHere As New List(Of ULong)
        Public DynamicObjectsHere As New List(Of ULong)

        Private CellX As Byte
        Private CellY As Byte
        Private CellMap As Integer

        Public Sub New(ByVal tileX As Byte, ByVal tileY As Byte, ByVal tileMap As Integer)
            ReDim ZCoord(RESOLUTION_ZMAP, RESOLUTION_ZMAP)

            CellX = tileX
            CellY = tileY
            CellMap = tileMap

            Dim fileName As String
            Dim fileVersion As String
            Dim f As IO.FileStream
            Dim b As BinaryReader
            Dim x, y As Integer

            'DONE: Loading MAP file
            fileName = String.Format("{0}{1}{2}.map", Format(tileMap, "000"), Format(tileX, "00"), Format(tileY, "00"))
            If Dir("maps\" & fileName, 63) = "" Then
                Log.WriteLine(LogType.WARNING, "Map file [{0}] not found", fileName)
            Else
                f = New IO.FileStream("maps\" & fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 82704, FileOptions.SequentialScan)
                b = New BinaryReader(f)

                fileVersion = System.Text.Encoding.ASCII.GetString(b.ReadBytes(8), 0, 8)
                Log.WriteLine(LogType.INFORMATION, "Loading map file [{0}] version [{1}]", fileName, fileVersion)

                For x = 0 To RESOLUTION_FLAGS
                    For y = 0 To RESOLUTION_FLAGS
                        AreaFlag(x, y) = b.ReadUInt16()
                    Next y
                Next x
                For x = 0 To RESOLUTION_TERRAIN
                    For y = 0 To RESOLUTION_TERRAIN
                        AreaTerrain(x, y) = b.ReadByte
                    Next y
                Next x
                For x = 0 To RESOLUTION_WATER
                    For y = 0 To RESOLUTION_WATER
                        WaterLevel(x, y) = b.ReadSingle
                    Next y
                Next x
                For x = 0 To RESOLUTION_ZMAP
                    For y = 0 To RESOLUTION_ZMAP
                        ZCoord(x, y) = b.ReadSingle
                    Next y
                Next x
                b.Close()
                f.Close()
            End If

#If ENABLE_PPOINTS Then
            'DONE: Initializing PPoints to unused values
            For x = 0 To RESOLUTION_ZMAP
                For y = 0 To RESOLUTION_ZMAP
                    ZCoord_PP(x, y) = PPOINT_BAD
                Next y
            Next x

            'DONE: Loading PPoints file
            fileName = String.Format("{0}{1}{2}.pp", Format(tileMap, "000"), Format(tileX, "00"), Format(tileY, "00"))

            If Dir("maps\" & fileName, 63) = "" Then
                'DONE: We are loading it only for instance maps
                If Not IsContinentMap(tileMap) Then ZCoord_PP_Save()
            Else
                f = New IO.FileStream("maps\" & fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 16392, FileOptions.SequentialScan)
                b = New BinaryReader(f)

                fileVersion = System.Text.Encoding.ASCII.GetString(b.ReadBytes(8), 0, 8)
                Log.WriteLine(LogType.INFORMATION, "Loading PP file [{0}] version [{1}]", fileName, fileVersion)
                For x = 0 To RESOLUTION_ZMAP
                    For y = 0 To RESOLUTION_ZMAP
                        ZCoord_PP(x, y) = b.ReadSingle
                    Next y
                Next x
                b.Close()
                f.Close()
            End If

#End If

        End Sub
        Public Sub Dispose() Implements System.IDisposable.Dispose
            'Done: Remove spawns
            UnloadSpawns(CellX, CellY, CellMap)
        End Sub
    End Class
    Public Class TMap
        Implements IDisposable

        Public ID As Integer
        Public Type As MapTypes = MapTypes.MAP_COMMON
        Public Name As String = ""
        Public Expansion As ExpansionLevel = 0
        Public ResetTime_Raid As Integer = 0
        Public ResetTime_Heroic As Integer = 0

        Public Tiles(63, 63) As TMapTile

        Public ReadOnly Property IsDungeon() As Boolean
            Get
                Return Type = MapTypes.MAP_INSTANCE OrElse Type = MapTypes.MAP_RAID
            End Get
        End Property
        Public ReadOnly Property IsRaid() As Boolean
            Get
                Return Type = MapTypes.MAP_RAID
            End Get
        End Property
        Public ReadOnly Property IsBattleGround() As Boolean
            Get
                Return Type = MapTypes.MAP_BATTLEGROUND
            End Get
        End Property
        Public ReadOnly Property IsBattleArena() As Boolean
            Get
                Return Type = MapTypes.MAP_ARENA
            End Get
        End Property
        Public ReadOnly Property SupportsHeroicMode() As Boolean
            Get
                Return (ResetTime_Heroic <> 0)
            End Get
        End Property

        Public ReadOnly Property ResetTime(ByVal Heroic As Boolean) As Integer
            Get
                Select Case Type
                    Case MapTypes.MAP_BATTLEGROUND, MapTypes.MAP_ARENA
                        Return DEFAULT_BATTLEFIELD_EXPIRE_TIME

                    Case MapTypes.MAP_RAID, MapTypes.MAP_INSTANCE
                        If Heroic Then
                            If ResetTime_Heroic = 0 Then
                                Return DEFAULT_INSTANCE_EXPIRE_TIME
                            Else
                                Return ResetTime_Heroic
                            End If
                        Else
                            If ResetTime_Raid = 0 Then
                                Return DEFAULT_INSTANCE_EXPIRE_TIME
                            Else
                                Return ResetTime_Raid
                            End If
                        End If
                End Select
            End Get
        End Property

        Public Sub New(ByVal Map As UInteger)
            Maps.Add(Map, Me)

            Try
                Dim tmpDBC As DBC.BufferedDBC = New DBC.BufferedDBC("dbc\Map.dbc")
                Dim tmpMap As Integer

                Dim i As Integer = 0
                For i = 0 To tmpDBC.Rows - 1
                    tmpMap = tmpDBC.Item(i, 0)

                    If tmpMap = Map Then
                        ID = Map
                        Type = tmpDBC.Item(i, 2, DBC.DBCValueType.DBC_INTEGER)
                        Name = tmpDBC.Item(i, 4, DBC.DBCValueType.DBC_STRING)
                        ResetTime_Raid = tmpDBC.Item(i, 112, DBC.DBCValueType.DBC_INTEGER)
                        ResetTime_Heroic = tmpDBC.Item(i, 113, DBC.DBCValueType.DBC_INTEGER)
                        Expansion = tmpDBC.Item(i, 116, DBC.DBCValueType.DBC_INTEGER)
                        Exit For
                    End If
                Next i

                tmpDBC.Dispose()

                InitializedMaps = InitializedMaps & "[ " & Name & "]"

                Log.WriteLine(LogType.INFORMATION, "DBC: 1 Map initialized. [ {1}]", i, Name)
            Catch e As System.IO.DirectoryNotFoundException
                Console.ForegroundColor = System.ConsoleColor.DarkRed
                Console.WriteLine("DBC File : Map missing.")
                Console.ForegroundColor = System.ConsoleColor.Gray
            End Try
        End Sub
        Public Sub Dispose() Implements System.IDisposable.Dispose
            For i As Integer = 0 To 63
                For j As Integer = 0 To 63
                    If Not Tiles(i, j) Is Nothing Then Tiles(i, j).Dispose()
                Next
            Next

            Maps.Remove(ID)
        End Sub
    End Class

    Public Maps As New Collections.Generic.Dictionary(Of UInteger, TMap)
    Public MapList As String

    Public Sub InitializeMaps()
        'DONE: Creating map list for queries
        Dim e As IEnumerator = Config.Maps.GetEnumerator
        e.Reset()
        If e.MoveNext() Then
            MapList = e.Current
            While e.MoveNext
                MapList += ", " & e.Current
            End While
        End If

        'DONE: Loading maps
        For Each ID As UInteger In Config.Maps
            Dim Map As New TMap(ID)
        Next

        Log.WriteLine(LogType.INFORMATION, "Initalizing: {0} Maps initialized.", Maps.Count)
    End Sub

    Public Sub GetMapTile(ByVal x As Single, ByVal y As Single, ByRef MapTileX As Byte, ByRef MapTileY As Byte)
        'How to calculate where is X,Y:
        MapTileX = Fix(32 - (x / TMapTile.SIZE))
        MapTileY = Fix(32 - (y / TMapTile.SIZE))
    End Sub
    Public Function GetMapTileX(ByVal x As Single) As Byte
        Return Fix(32 - (x / TMapTile.SIZE))
    End Function
    Public Function GetMapTileY(ByVal y As Single) As Byte
        Return Fix(32 - (y / TMapTile.SIZE))
    End Function
    Public Function GetSubMapTileX(ByVal x As Single) As Byte
        Return Fix(RESOLUTION_ZMAP * (32 - (x / TMapTile.SIZE) - Fix(32 - (x / TMapTile.SIZE))))
    End Function
    Public Function GetSubMapTileY(ByVal y As Single) As Byte
        Return Fix(RESOLUTION_ZMAP * (32 - (y / TMapTile.SIZE) - Fix(32 - (y / TMapTile.SIZE))))
    End Function
    Public Function GetZCoord(ByVal x As Single, ByVal y As Single, ByVal Map As Integer) As Single
        Try
            Dim MapTileX As Byte = Fix(32 - (x / TMapTile.SIZE))
            Dim MapTileY As Byte = Fix(32 - (y / TMapTile.SIZE))
            Dim MapTile_LocalX As Byte = CType(RESOLUTION_ZMAP * (32 - (x / TMapTile.SIZE) - MapTileX), Byte)
            Dim MapTile_LocalY As Byte = CType(RESOLUTION_ZMAP * (32 - (y / TMapTile.SIZE) - MapTileY), Byte)

            If Maps(Map).Tiles(MapTileX, MapTileY) Is Nothing Then Return 0
            Return Maps(Map).Tiles(MapTileX, MapTileY).ZCoord(MapTile_LocalX, MapTile_LocalY)
        Catch e As Exception
            Return 0
        End Try
    End Function
    Public Function GetWaterLevel(ByVal x As Single, ByVal y As Single, ByVal Map As Integer) As Single
        Dim MapTileX As Byte = Fix(32 - (x / TMapTile.SIZE))
        Dim MapTileY As Byte = Fix(32 - (y / TMapTile.SIZE))
        Dim MapTile_LocalX As Byte = CType(TMapTile.RESOLUTION_WATER * (32 - (x / TMapTile.SIZE) - MapTileX), Byte)
        Dim MapTile_LocalY As Byte = CType(TMapTile.RESOLUTION_WATER * (32 - (y / TMapTile.SIZE) - MapTileY), Byte)

        If Maps(Map).Tiles(MapTileX, MapTileY) Is Nothing Then Return 0
        Return Maps(Map).Tiles(MapTileX, MapTileY).WaterLevel(MapTile_LocalX, MapTile_LocalY)
    End Function
    Public Function GetTerrainType(ByVal x As Single, ByVal y As Single, ByVal Map As Integer) As Byte
        Dim MapTileX As Byte = Fix(32 - (x / TMapTile.SIZE))
        Dim MapTileY As Byte = Fix(32 - (y / TMapTile.SIZE))
        Dim MapTile_LocalX As Byte = CType(TMapTile.RESOLUTION_TERRAIN * (32 - (x / TMapTile.SIZE) - MapTileX), Byte)
        Dim MapTile_LocalY As Byte = CType(TMapTile.RESOLUTION_TERRAIN * (32 - (y / TMapTile.SIZE) - MapTileY), Byte)

        If Maps(Map).Tiles(MapTileX, MapTileY) Is Nothing Then Return 0
        Return Maps(Map).Tiles(MapTileX, MapTileY).AreaTerrain(MapTile_LocalX, MapTile_LocalY)
    End Function
    Public Function GetAreaFlag(ByVal x As Single, ByVal y As Single, ByVal Map As Integer) As Integer
        Dim MapTileX As Byte = Fix(32 - (x / TMapTile.SIZE))
        Dim MapTileY As Byte = Fix(32 - (y / TMapTile.SIZE))
        Dim MapTile_LocalX As Byte = CType(TMapTile.RESOLUTION_FLAGS * (32 - (x / TMapTile.SIZE) - MapTileX), Byte)
        Dim MapTile_LocalY As Byte = CType(TMapTile.RESOLUTION_FLAGS * (32 - (y / TMapTile.SIZE) - MapTileY), Byte)

        If Maps(Map).Tiles(MapTileX, MapTileY) Is Nothing Then Return 0
        Return Maps(Map).Tiles(MapTileX, MapTileY).AreaFlag(MapTile_LocalX, MapTile_LocalY)
    End Function

    Public Function IsOutsideOfMap(ByRef c As BaseObject) As Boolean
        'NOTE: Disabled these checks because DBC data contains too big X/Y coords to be usefull
        Return False

        'Dim x As Single = c.positionX
        'Dim y As Single = c.positionY
        'Dim m As UInteger = c.MapID

        ''Check transform data
        'For Each i As WorldMapTransformsDimension In WorldMapTransforms
        '    If i.Map = m Then
        '        With i
        '            If x < .X_Maximum And x > .X_Minimum And _
        '               y < .Y_Maximum And y > .Y_Minimum Then

        '                Log.WriteLine(LogType.USER, "Applying map transform {0},{1},{2} -> {3},{4},{5}", x, y, m, .Dest_X, .Dest_Y, .Dest_Map)
        '                'x += .Dest_X
        '                'y += .Dest_Y
        '                'm = .Dest_Map
        '                'Exit For
        '                Return False
        '            End If
        '        End With
        '    End If
        'Next

        ''Check Map data
        'If WorldMapContinent.ContainsKey(m) Then
        '    With WorldMapContinent(m)
        '        If x > .X_Maximum Or x < .X_Minimum Or _
        '           y > .Y_Maximum Or y < .Y_Minimum Then
        '            Log.WriteLine(LogType.USER, "Outside map: {0:X}", c.GUID)
        '            Return True
        '        Else
        '            Return False
        '        End If
        '    End With
        'End If

        'Log.WriteLine(LogType.USER, "WorldMapContinent not found for map {0}.", c.MapID)
        'Return False
    End Function

#If ENABLE_PPOINTS Then
    Public Const PPOINT_BAD As Single = Single.MinValue
    Public Const PPOINT_LIMIT As Single = 5.0F
    Public Const PPOINT_SAVE As Integer = 5
    Public Const PPOINT_VERSION As String = "PP__1.00"

    Public Function GetZCoord_PP(ByVal x As Single, ByVal y As Single, ByVal Map As Integer) As Single
        Try
            Dim MapTileX As Byte = Fix(32 - (x / TMapTile.SIZE))
            Dim MapTileY As Byte = Fix(32 - (y / TMapTile.SIZE))
            Dim MapTile_LocalX As Byte = CType(RESOLUTION_ZMAP * (32 - (x / TMapTile.SIZE) - MapTileX), Byte)
            Dim MapTile_LocalY As Byte = CType(RESOLUTION_ZMAP * (32 - (y / TMapTile.SIZE) - MapTileY), Byte)

            If Maps(Map).Tiles(MapTileX, MapTileY) Is Nothing Then Return 0

            Return Maps(Map).Tiles(MapTileX, MapTileY).ZCoord_PP(MapTile_LocalX, MapTile_LocalY)
        Catch e As Exception
            Return 0
        End Try
    End Function
    Public Sub SetZCoord_PP(ByVal x As Single, ByVal y As Single, ByVal Map As Integer, ByVal z As Single)
        Try
            Dim MapTileX As Byte = Fix(32 - (x / TMapTile.SIZE))
            Dim MapTileY As Byte = Fix(32 - (y / TMapTile.SIZE))
            Dim MapTile_LocalX As Byte = CType(RESOLUTION_ZMAP * (32 - (x / TMapTile.SIZE) - MapTileX), Byte)
            Dim MapTile_LocalY As Byte = CType(RESOLUTION_ZMAP * (32 - (y / TMapTile.SIZE) - MapTileY), Byte)

            If Maps(Map).Tiles(MapTileX, MapTileY) Is Nothing Then Return
            Maps(Map).Tiles(MapTileX, MapTileY).ZCoord_PP(MapTile_LocalX, MapTile_LocalY) = z

            'Notify PPoints changes
            Maps(Map).Tiles(MapTileX, MapTileY).ZCoord_PP_ModTimes += 1

            If Maps(Map).Tiles(MapTileX, MapTileY).ZCoord_PP_ModTimes > PPOINT_SAVE Then
                Maps(Map).Tiles(MapTileX, MapTileY).ZCoord_PP_Save()
            End If

        Catch e As Exception
            Return
        End Try
    End Sub

    Public Function GetZCoord(ByVal x As Single, ByVal y As Single, ByVal z As Single, ByVal Map As Integer) As Single
        Try
            Dim MapTileX As Byte = Fix(32 - (x / TMapTile.SIZE))
            Dim MapTileY As Byte = Fix(32 - (y / TMapTile.SIZE))
            Dim MapTile_LocalX As Byte = CType(RESOLUTION_ZMAP * (32 - (x / TMapTile.SIZE) - MapTileX), Byte)
            Dim MapTile_LocalY As Byte = CType(RESOLUTION_ZMAP * (32 - (y / TMapTile.SIZE) - MapTileY), Byte)

            If Maps(Map).Tiles(MapTileX, MapTileY) Is Nothing Then Return 0

            'Return map info if we are near the ground
            If Math.Abs(Maps(Map).Tiles(MapTileX, MapTileY).ZCoord(MapTile_LocalX, MapTile_LocalY) - z) < PPOINT_LIMIT Then
                Return Maps(Map).Tiles(MapTileX, MapTileY).ZCoord(MapTile_LocalX, MapTile_LocalY)
            End If

            'Return map info if we don't have this PPoint
            If Maps(Map).Tiles(MapTileX, MapTileY).ZCoord_PP(MapTile_LocalX, MapTile_LocalY) = PPOINT_BAD Then
                Return Maps(Map).Tiles(MapTileX, MapTileY).ZCoord(MapTile_LocalX, MapTile_LocalY)
            End If

            'Return pp info if we are too far from ground
            Return Maps(Map).Tiles(MapTileX, MapTileY).ZCoord_PP(MapTile_LocalX, MapTile_LocalY)
        Catch ex As Exception
            Log.WriteLine(LogType.FAILED, ex.ToString)
            Return z
        End Try
    End Function
#Else
    Public Function GetZCoord(ByVal x As Single, ByVal y As Single, ByVal z As Single, ByVal Map As Integer) As Single
        Try
            Dim MapTileX As Byte = Fix(32 - (x / TMapTile.SIZE))
            Dim MapTileY As Byte = Fix(32 - (y / TMapTile.SIZE))
            Dim MapTile_LocalX As Byte = CType(RESOLUTION_ZMAP * (32 - (x / TMapTile.SIZE) - MapTileX), Byte)
            Dim MapTile_LocalY As Byte = CType(RESOLUTION_ZMAP * (32 - (y / TMapTile.SIZE) - MapTileY), Byte)

            If Maps(Map).Tiles(MapTileX, MapTileY) Is Nothing Then Return 0
            Return Maps(Map).Tiles(MapTileX, MapTileY).ZCoord(MapTile_LocalX, MapTile_LocalY)
        Catch ex As Exception
            Log.WriteLine(LogType.FAILED, ex.ToString)
            Return z
        End Try
    End Function
#End If

    Public Sub LoadSpawns(ByVal TileX As Byte, ByVal TileY As Byte, ByVal TileMap As UInteger, ByVal TileInstance As UInteger)
        'Caluclate (x1, y1) and (x2, y2)
        Dim MinX As Single = ((32 - TileX) * TMapTile.SIZE)
        Dim MaxX As Single = ((32 - (TileX + 1)) * TMapTile.SIZE)
        Dim MinY As Single = ((32 - TileY) * TMapTile.SIZE)
        Dim MaxY As Single = ((32 - (TileY + 1)) * TMapTile.SIZE)
        'We need the maximum value to be the largest value
        If MinX > MaxX Then
            Dim tmpSng As Single = MinX
            MinX = MaxX
            MaxX = tmpSng
        End If
        If MinY > MaxY Then
            Dim tmpSng As Single = MinY
            MinY = MaxY
            MaxY = tmpSng
        End If

        Dim MysqlQuery As New DataTable
        Database.Query(String.Format("SELECT * FROM spawns_creatures WHERE spawn_map = {0} AND spawn_positionX BETWEEN '{1}' AND '{2}' AND spawn_positionY BETWEEN '{3}' AND '{4}';", TileMap, MinX, MaxX, MinY, MaxY), MysqlQuery)
        For Each InfoRow As DataRow In MysqlQuery.Rows
            If Not WORLD_CREATUREs.ContainsKey(CType(InfoRow.Item("spawn_id"), Long) + GUID_UNIT) Then
                Dim tmpCr As CreatureObject = New CreatureObject(CType(InfoRow.Item("spawn_id"), Long), InfoRow)
                tmpCr.instance = TileInstance
                tmpCr.AddToWorld()
            End If
        Next

        MysqlQuery.Clear()
        Database.Query(String.Format("SELECT * FROM spawns_gameobjects WHERE spawn_map = {0} AND spawn_positionX BETWEEN '{1}' AND '{2}' AND spawn_positionY BETWEEN '{3}' AND '{4}';", TileMap, MinX, MaxX, MinY, MaxY), MysqlQuery)
        For Each InfoRow As DataRow In MysqlQuery.Rows
            If Not WORLD_GAMEOBJECTs.ContainsKey(CType(InfoRow.Item("spawn_id"), ULong) + GUID_GAMEOBJECT) Then
                Dim tmpGo As GameObjectObject = New GameObjectObject(CType(InfoRow.Item("spawn_id"), Long), InfoRow)
                tmpGo.instance = TileInstance
                tmpGo.AddToWorld()
            End If
        Next

        MysqlQuery.Clear()
        Database.Query(String.Format("SELECT * FROM tmpspawnedcorpses WHERE corpse_mapId IN ({0}) AND corpse_positionX BETWEEN '{1}' AND '{2}' AND corpse_positionY BETWEEN '{3}' AND '{4}';", TileMap, MinX, MaxX, MinY, MaxY), MysqlQuery)
        For Each InfoRow As DataRow In MysqlQuery.Rows
            If Not WORLD_CORPSEOBJECTs.ContainsKey(CType(InfoRow.Item("corpse_guid"), ULong) + GUID_CORPSE) Then
                Dim tmpCorpse As CorpseObject = New CorpseObject(CType(InfoRow.Item("corpse_guid"), Long), InfoRow)
                tmpCorpse.instance = TileInstance
                tmpCorpse.AddToWorld()
            End If
        Next
    End Sub
    Public Sub UnloadSpawns(ByVal TileX As Byte, ByVal TileY As Byte, ByVal TileMap As UInteger)
        'Caluclate (x1, y1) and (x2, y2)
        Dim MinX As Single = ((32 - TileX) * TMapTile.SIZE)
        Dim MaxX As Single = ((32 - (TileX + 1)) * TMapTile.SIZE)
        Dim MinY As Single = ((32 - TileY) * TMapTile.SIZE)
        Dim MaxY As Single = ((32 - (TileY + 1)) * TMapTile.SIZE)
        'We need the maximum value to be the largest value
        If MinX > MaxX Then
            Dim tmpSng As Single = MinX
            MinX = MaxX
            MaxX = tmpSng
        End If
        If MinY > MaxY Then
            Dim tmpSng As Single = MinY
            MinY = MaxY
            MaxY = tmpSng
        End If

        Try
            WORLD_CREATUREs_Lock.AcquireReaderLock(DEFAULT_LOCK_TIMEOUT)
            For Each Creature As KeyValuePair(Of ULong, CreatureObject) In WORLD_CREATUREs
                If CType(Creature.Value, CreatureObject).MapID = TileMap AndAlso CType(Creature.Value, CreatureObject).SpawnX >= MinX AndAlso CType(Creature.Value, CreatureObject).SpawnX <= MaxX AndAlso CType(Creature.Value, CreatureObject).SpawnY >= MinY AndAlso CType(Creature.Value, CreatureObject).SpawnY <= MaxY Then
                    CType(Creature.Value, CreatureObject).Destroy()
                End If
            Next
        Catch ex As Exception
            Log.WriteLine(LogType.CRITICAL, ex.ToString, Nothing)
        Finally
            WORLD_CREATUREs_Lock.ReleaseReaderLock()
        End Try

        For Each Gameobject As KeyValuePair(Of ULong, GameObjectObject) In WORLD_GAMEOBJECTs
            If CType(Gameobject.Value, GameObjectObject).MapID = TileMap AndAlso CType(Gameobject.Value, GameObjectObject).positionX >= MinX AndAlso CType(Gameobject.Value, GameObjectObject).positionX <= MaxX AndAlso CType(Gameobject.Value, GameObjectObject).positionY >= MinY AndAlso CType(Gameobject.Value, GameObjectObject).positionY <= MaxY Then
                CType(Gameobject.Value, GameObjectObject).Destroy()
            End If
        Next

        For Each Corpseobject As KeyValuePair(Of ULong, CorpseObject) In WORLD_CORPSEOBJECTs
            If CType(Corpseobject.Value, CorpseObject).MapID = TileMap AndAlso CType(Corpseobject.Value, CorpseObject).positionX >= MinX AndAlso CType(Corpseobject.Value, CorpseObject).positionX <= MaxX AndAlso CType(Corpseobject.Value, CorpseObject).positionY >= MinY AndAlso CType(Corpseobject.Value, CorpseObject).positionY <= MaxY Then
                CType(Corpseobject.Value, CorpseObject).Destroy()
            End If
        Next

    End Sub

#End Region
#Region "Instances"

    Public Enum TransferAbortReason As Short
        TRANSFER_ABORT_MAX_PLAYERS = &H1                ' Transfer Aborted: instance is full
        TRANSFER_ABORT_NOT_FOUND = &H2                  ' Transfer Aborted: instance not found
        TRANSFER_ABORT_TOO_MANY_INSTANCES = &H3         ' You have entered too many instances recently.
        TRANSFER_ABORT_ZONE_IN_COMBAT = &H5             ' Unable to zone in while an encounter is in progress.
        TRANSFER_ABORT_INSUF_EXPAN_LVL1 = &H106         ' You must have TBC expansion installed to access this area.
        TRANSFER_ABORT_DIFFICULTY1 = &H7                ' Normal difficulty mode is not available for %s.
        TRANSFER_ABORT_DIFFICULTY2 = &H107              ' Heroic difficulty mode is not available for %s.
        TRANSFER_ABORT_DIFFICULTY3 = &H207              ' Epic difficulty mode is not available for %s.
    End Enum
    Public Sub SendTransferAborted(ByRef Client As ClientClass, ByVal Map As Integer, ByVal Reason As TransferAbortReason)
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] SMSG_TRANSFER_ABORTED [{2}:{3}]", Client.IP, Client.Port, Map, Reason)

        Dim p As New PacketClass(OPCODES.SMSG_TRANSFER_ABORTED)
        p.AddInt32(Map)
        p.AddInt16(Reason)
        Client.Send(p)
        p.Dispose()
    End Sub

#End Region

#Region "Graveyards"
    Public Graveyards As New List(Of TGraveyard)
    Public Class TGraveyard
        Public x As Single
        Public y As Single
        Public z As Single
        Public Map As Integer

        Public Sub New(ByVal px As Single, ByVal py As Single, ByVal pz As Single, ByVal pMap As Integer)
            x = px
            y = py
            z = pz
            Map = pMap
        End Sub
    End Class
    Public Sub GoToNearestGraveyard(ByRef Character As CharacterObject, Optional ByVal Alive As Boolean = False, Optional ByVal Teleport As Boolean = True)
        Dim minDistance As Single = 9999999.0F
        Dim tmp As Single
        Dim selectedGraveyard As TGraveyard = Nothing
        Dim FoundGraveYard As Boolean = False

        For Each Graveyard As TGraveyard In Graveyards
            If Graveyard.Map = Character.MapID Then
                'Formula: d^2 = (x1-x2)^2+(y1-y2)^2
                tmp = Math.Sqrt(((Character.positionX - Graveyard.x) ^ 2) + ((Character.positionY - Graveyard.y) ^ 2))
                If tmp < minDistance Then
                    minDistance = tmp
                    selectedGraveyard = Graveyard
                    FoundGraveYard = True
                End If
            End If
        Next

        If Teleport Then
            If Alive And Character.DEAD Then
                CharacterResurrect(Character)
                Character.Life.Current = Character.Life.Maximum
                If Character.ManaType = ManaTypes.TYPE_MANA Then Character.Mana.Current = Character.Mana.Maximum
                If selectedGraveyard.Map = Character.MapID Then
                    Character.SetUpdateFlag(EUnitFields.UNIT_FIELD_HEALTH, Character.Life.Current)
                    If Character.ManaType = ManaTypes.TYPE_MANA Then Character.SetUpdateFlag(EUnitFields.UNIT_FIELD_POWER1, Character.Mana.Current)
                    Character.SendCharacterUpdate()
                End If
            End If

            ' Only teleport if a graveyard is found
            If FoundGraveYard Then
                Log.WriteLine(LogType.INFORMATION, "GraveYards: GraveYard.Map[{0}], GraveYard.X[{1}], GraveYard.Y[{2}], GraveYard.Z[{3}]", selectedGraveyard.Map, selectedGraveyard.x, selectedGraveyard.y, selectedGraveyard.z)
                Character.Teleport(selectedGraveyard.x, selectedGraveyard.y, selectedGraveyard.z, 0, selectedGraveyard.Map)
                Character.SendDeathReleaseLoc(selectedGraveyard.x, selectedGraveyard.y, selectedGraveyard.z, selectedGraveyard.Map)
            Else
                Log.WriteLine(LogType.INFORMATION, "GraveYards: No near graveyards for map [{0}]", Character.MapID)
            End If
        Else
            Character.positionX = selectedGraveyard.x
            Character.positionY = selectedGraveyard.y
            Character.positionZ = selectedGraveyard.z
            Character.MapID = selectedGraveyard.Map
        End If
    End Sub

#End Region

End Module