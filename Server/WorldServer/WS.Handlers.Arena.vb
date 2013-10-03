Imports mangosVB.Common.BaseWriter

Public Module WS_Handlers_Arena

    Public Enum ArenaTeamCommandTypes As Byte
        ERR_ARENA_TEAM_CREATE_S = &H0
        ERR_ARENA_TEAM_INVITE_SS = &H1
        ERR_ARENA_TEAM_QUIT_S = &H3
        ERR_ARENA_TEAM_FOUNDER_S = &HC
    End Enum
    Public Enum ArenaTeamCommandErrors As Byte
        ERR_ARENA_TEAM_INTERNAL = &H1
        ERR_ALREADY_IN_ARENA_TEAM = &H2
        ERR_ALREADY_IN_ARENA_TEAM_S = &H3
        ERR_INVITED_TO_ARENA_TEAM = &H4
        ERR_ALREADY_INVITED_TO_ARENA_TEAM_S = &H5
        ERR_ARENA_TEAM_NAME_INVALID = &H6
        ERR_ARENA_TEAM_NAME_EXISTS_S = &H7
        ERR_ARENA_TEAM_LEADER_LEAVE_S = &H8
        ERR_ARENA_TEAM_PERMISSIONS = &H8
        ERR_ARENA_TEAM_PLAYER_NOT_IN_TEAM = &H9
        ERR_ARENA_TEAM_PLAYER_NOT_IN_TEAM_SS = &HA
        ERR_ARENA_TEAM_PLAYER_NOT_FOUND_S = &HB
        ERR_ARENA_TEAM_NOT_ALLIED = &HC
    End Enum

    Public Sub SendArenaCommandResult(ByRef Client As ClientClass, ByVal ErrorType As ArenaTeamCommandTypes, ByVal String1 As String, ByVal String2 As String, ByVal ErrorCode As ArenaTeamCommandErrors)
        Dim packet As New PacketClass(OPCODES.SMSG_ARENA_TEAM_COMMAND_RESULT)
        packet.AddInt32(ErrorType)
        packet.AddString(String1)
        packet.AddString(String1)
        packet.AddInt32(ErrorCode)
        Client.Send(packet)
        packet.Dispose()
    End Sub
    Public Sub SendArenaRoster(ByRef Client As ClientClass, ByVal ArenaTeamID As UInteger)
        Dim q As New DataTable
        Database.Query(String.Format("SELECT * FROM arena_members WHERE member_team = {0}", ArenaTeamID), q)
        If q.Rows.Count = 0 Then Exit Sub
        Dim q2 As New DataTable
        Database.Query(String.Format("SELECT * FROM arena_teams WHERE arena_id = {0}", ArenaTeamID), q2)
        If q2.Rows.Count = 0 Then Exit Sub

        Dim response As New PacketClass(OPCODES.SMSG_ARENA_TEAM_ROSTER)
        response.AddUInt32(ArenaTeamID)
        response.AddUInt32(q.Rows.Count)
        response.AddUInt32(q2.Rows(0).Item("arena_type"))

        For i As Byte = 0 To q.Rows.Count - 1
            If CHARACTERs.ContainsKey(CULng(q.Rows(i).Item("member_id"))) Then
                response.AddUInt64(q.Rows(i).Item("member_id"))
                response.AddInt8(1) 'Online
                response.AddString(CHARACTERs(CULng(q.Rows(i).Item("member_id"))).Name)
                If CULng(q2.Rows(0).Item("arena_captain")) = CULng(q.Rows(i).Item("member_id")) Then
                    response.AddInt32(1) 'Is leader
                Else
                    response.AddInt32(0) 'Normal member
                End If
                response.AddInt8(CHARACTERs(CULng(q.Rows(i).Item("member_id"))).Level) 'Level
                response.AddInt8(CHARACTERs(CULng(q.Rows(i).Item("member_id"))).Classe) 'Class
                response.AddUInt32(q.Rows(i).Item("member_playedweek")) 'Played this week
                response.AddUInt32(q.Rows(i).Item("member_wonsweek")) 'Wins this week
                response.AddUInt32(q.Rows(i).Item("member_playedseason")) 'Played season
                response.AddUInt32(q.Rows(i).Item("member_wonsseason")) 'Wins season
                response.AddUInt32(q.Rows(i).Item("member_personalrating")) 'Personal rating?
            Else
                Dim q3 As New DataTable
                Database.Query(String.Format("SELECT char_name, char_level, char_class FROM characters WHERE char_guid = {0}", q.Rows(i).Item("member_id")), q3)
                If q3.Rows.Count = 0 Then GoTo NextMember

                response.AddUInt64(q.Rows(i).Item("member_id"))
                response.AddInt8(0) 'Offline
                response.AddString(q3.Rows(0).Item("char_name"))
                If CULng(q2.Rows(0).Item("arena_captain")) = CULng(q.Rows(i).Item("member_id")) Then
                    response.AddInt32(1) 'Is leader
                Else
                    response.AddInt32(0) 'Normal member
                End If
                response.AddInt8(q3.Rows(0).Item("char_level")) 'Level
                response.AddInt8(q3.Rows(0).Item("char_class")) 'Class
                response.AddUInt32(q.Rows(i).Item("member_playedweek")) 'Played this week
                response.AddUInt32(q.Rows(i).Item("member_wonsweek")) 'Wins this week
                response.AddUInt32(q.Rows(i).Item("member_playedseason")) 'Played season
                response.AddUInt32(q.Rows(i).Item("member_wonsseason")) 'Wins season
                response.AddUInt32(q.Rows(i).Item("member_personalrating")) 'Personal rating?
            End If
NextMember:
        Next

        Client.Send(response)
        response.Dispose()
    End Sub
    Public Sub SendArenaQueryResponse(ByRef Client As ClientClass, ByVal ArenaTeamID As UInteger)
        Dim q As New DataTable
        Database.Query(String.Format("SELECT * FROM arena_teams WHERE arena_id = {0}", ArenaTeamID), q)
        If q.Rows.Count = 0 Then Exit Sub

        Dim response As New PacketClass(OPCODES.SMSG_ARENA_TEAM_QUERY_RESPONSE)
        response.AddUInt32(ArenaTeamID)
        response.AddString(q.Rows(0).Item("arena_name"))
        response.AddUInt32(q.Rows(0).Item("arena_type"))
        response.AddUInt32(q.Rows(0).Item("arena_emblemstyle"))
        response.AddUInt32(q.Rows(0).Item("arena_emblemcolor"))
        response.AddUInt32(q.Rows(0).Item("arena_borderstyle"))
        response.AddUInt32(q.Rows(0).Item("arena_bordercolor"))
        response.AddUInt32(q.Rows(0).Item("arena_background"))
        Client.Send(response)
        response.Dispose()
    End Sub
    Public Sub SendArenaStats(ByRef Client As ClientClass, ByVal ArenaTeamID As UInteger)
        Dim q As New DataTable
        Database.Query(String.Format("SELECT * FROM arena_teams WHERE arena_id = {0}", ArenaTeamID), q)
        If q.Rows.Count = 0 Then Exit Sub

        'DONE: Read it from the DB
        Dim response As New PacketClass(OPCODES.SMSG_ARENA_TEAM_STATS)
        response.AddUInt32(ArenaTeamID)
        response.AddUInt32(q.Rows(0).Item("arena_rating")) 'Rating
        response.AddUInt32(q.Rows(0).Item("arena_weekgames")) 'Games this week?
        response.AddUInt32(q.Rows(0).Item("arena_weekwins")) 'Week wins?
        response.AddUInt32(q.Rows(0).Item("arena_seasongames")) 'Games this season?
        response.AddUInt32(q.Rows(0).Item("arena_seasonwins")) 'Season wins?
        response.AddUInt32(q.Rows(0).Item("arena_rank")) 'Rank
        Client.Send(response)
        response.Dispose()
    End Sub

    Public Sub On_CMSG_ARENA_TEAM_QUERY(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 9 Then Exit Sub
        packet.GetInt16()
        Dim ArenaTeamID As UInteger = packet.GetUInt32

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_ARENA_TEAM_QUERY [{2}]", Client.IP, Client.Port, ArenaTeamID)

        SendArenaQueryResponse(Client, ArenaTeamID)
        SendArenaStats(Client, ArenaTeamID)
    End Sub
    Public Sub On_CMSG_ARENA_TEAM_ROSTER(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 9 Then Exit Sub
        packet.GetInt16()
        Dim ArenaTeamID As UInteger = packet.GetUInt32

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_ARENA_TEAM_ROSTER [{2}]", Client.IP, Client.Port, ArenaTeamID)

        SendArenaRoster(Client, ArenaTeamID)
    End Sub
    Public Sub On_MSG_INSPECT_ARENA_TEAMS(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 13 Then Exit Sub
        packet.GetInt16()
        Dim GUID As ULong = packet.GetUInt64

        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] MSG_INSPECT_ARENA_TEAMS [{2:X}]", Client.IP, Client.Port, GUID)

        If CHARACTERs.ContainsKey(GUID) = False Then Exit Sub

        'TODO: Read it from the DB
        For i As Byte = 0 To 2
            If CHARACTERs(GUID).ArenaTeamID(i) <> 0 Then
                Dim q As New DataTable
                Database.Query(String.Format("SELECT * FROM arena_teams WHERE arena_id = {0}", CHARACTERs(GUID).ArenaTeamID(i)), q)
                If q.Rows.Count = 0 Then Exit Sub

                Dim response As New PacketClass(OPCODES.MSG_INSPECT_ARENA_TEAMS)
                response.AddUInt64(GUID)
                response.AddInt8(i)
                response.AddUInt32(CHARACTERs(GUID).ArenaTeamID(i))
                response.AddUInt32(q.Rows(0).Item("arena_rating")) 'Rating
                response.AddUInt32(q.Rows(0).Item("arena_weekgames")) 'Games played this week
                response.AddUInt32(q.Rows(0).Item("arena_weekwins")) 'Wins
                response.AddUInt32(q.Rows(0).Item("arena_seasongames")) 'Games this season
                response.AddUInt32(0) 'Personal rating?
                Client.Send(response)
                response.Dispose()
            End If
        Next i
    End Sub

End Module