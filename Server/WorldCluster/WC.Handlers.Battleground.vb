Imports mangosVB.Common.BaseWriter

Public Module WC_Handlers_Battleground

    Public Sub On_CMSG_BATTLEFIELD_PORT(ByRef packet As PacketClass, ByRef Client As ClientClass)
        packet.GetInt16()

        Dim Unk1 As Byte = packet.GetInt8
        Dim Unk2 As Byte = packet.GetInt8                   'unk, can be 0x0 (may be if was invited?) and 0x1
        Dim MapType As UInteger = packet.GetInt32           'type id from dbc
        Dim ID As UInteger = packet.GetUInt16               'ID
        Dim Action As Byte = packet.GetInt8                 'enter battle 0x1, leave queue 0x0

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_BATTLEFIELD_PORT [MapType: {2}, Action: {3}, Unk1: {4}, Unk2: {5}, ID: {6}]", Client.IP, Client.Port, MapType, Action, Unk1, Unk2, ID)

        If Action = 0 Then
            BATTLEFIELDs(ID).Leave(Client.Character)
        Else
            BATTLEFIELDs(ID).Join(Client.Character)
        End If
    End Sub
    Public Sub On_CMSG_LEAVE_BATTLEFIELD(ByRef packet As PacketClass, ByRef Client As ClientClass)
        packet.GetInt16()

        Dim Unk1 As Byte = packet.GetInt8
        Dim Unk2 As Byte = packet.GetInt8
        Dim MapType As UInteger = packet.GetInt32
        Dim ID As UInteger = packet.GetUInt16

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_LEAVE_BATTLEFIELD [MapType: {2}, Unk1: {3}, Unk2: {4}, ID: {5}]", Client.IP, Client.Port, MapType, Unk1, Unk2, ID)

        BATTLEFIELDs(ID).Leave(Client.Character)
    End Sub
    Public Sub On_CMSG_BATTLEMASTER_JOIN(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 16 Then Exit Sub
        packet.GetInt16()
        Dim GUID As ULong = packet.GetUInt64
        Dim MapType As UInteger = packet.GetInt32
        Dim Intance As UInteger = packet.GetInt32
        Dim AsGroup As Byte = packet.GetInt8

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_BATTLEMASTER_JOIN [MapType: {2}, Instance: {3}, Group: {4}]", Client.IP, Client.Port, MapType, Intance, AsGroup)

        GetBattlefield(MapType, Client.Character.Level).Enqueue(Client.Character)
    End Sub
    Public Sub On_CMSG_BATTLEMASTER_JOIN_ARENA(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 16 Then Exit Sub
        packet.GetInt16()
        Dim GUID As ULong = packet.GetUInt64
        Dim Type As Byte = packet.GetInt8
        Dim AsGroup As Byte = packet.GetInt8
        Dim IsRated As Byte = packet.GetInt8

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_BATTLEMASTER_JOIN_ARENA [Type: {2}, Group: {3}, Rated: {4}]", Client.IP, Client.Port, Type, AsGroup, IsRated)

        'NOTE: There is no way to check if the NPC exist and is a battlemaster since this is located in the cluser.

        'TODO: Check if we are already in the queue
        'TODO: Ignore if we are already have too many queue's (max = 3)

        Dim ArenaType As BattlefieldArenaType = BattlefieldArenaType.ARENA_TYPE_NONE
        Select Case Type
            Case 0
                ArenaType = BattlefieldArenaType.ARENA_TYPE_2v2
            Case 1
                ArenaType = BattlefieldArenaType.ARENA_TYPE_3v3
            Case 2
                ArenaType = BattlefieldArenaType.ARENA_TYPE_5v5
            Case Else
                Exit Sub
        End Select

        'TODO: Check if in arena team for rated game

        If AsGroup AndAlso Client.Character.IsInGroup = False Then Exit Sub

        'TODO: Save entry position

        Dim response As New PacketClass(OPCODES.SMSG_BATTLEFIELD_STATUS)
        response.AddInt32(0)                    'SlotID
        response.AddUInt64(CULng(ArenaType) Or (CULng(&HD) << CULng(8)) Or (CULng(6) << CULng(16)) Or (CULng(&H1F90) << CULng(48)))
        response.AddInt32(0)                    'Unk
        response.AddInt8(IsRated)               '1 = Rated / 0 = Unrated
        response.AddInt32(1)                    '1 = Wait queue
        response.AddUInt32(10000)               'Average wait time
        response.AddUInt32(0)                   'Time in queue
        Client.Send(response)
        response.Dispose()
    End Sub

End Module