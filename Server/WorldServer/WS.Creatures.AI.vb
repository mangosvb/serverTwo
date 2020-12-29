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
Imports System.Runtime.CompilerServices

Public Module WS_Creatures_AI

#Region "WS.Creatures.AI.Framework"
    Public Class TBaseAI
        Implements IDisposable
        Public Enum AIState
            AI_DO_NOTHING
            AI_DEAD
            AI_EVADE
            AI_ATTACKING
            AI_MOVE_FOR_ATTACK
            AI_MOVING
            AI_FLYING
            AI_WANDERING
            AI_RESPAWN
        End Enum

        Public State As AIState = AIState.AI_DO_NOTHING
        Public aiTarget As BaseUnit = Nothing
        Public aiHateTable As New Dictionary(Of BaseUnit, Integer)
        Public aiHateTableRemove As New Dictionary(Of BaseUnit, Integer)

        Public Overridable Function InCombat() As Boolean
            Return (aiHateTable.Count > 0)
        End Function
        Public Overridable Sub ChangeTimer(ByVal NewValue As Integer)
        End Sub
        Public Overridable Function IsMoving() As Boolean
            Select Case Me.State
                Case AIState.AI_MOVE_FOR_ATTACK, AIState.AI_MOVING, AIState.AI_WANDERING, AIState.AI_FLYING
                    Return True
                Case Else
                    Return False
            End Select
        End Function
        Public Overridable Function IsRunning() As Boolean
            Return Me.State = AIState.AI_MOVE_FOR_ATTACK
        End Function
        Public Overridable Sub Reset()
            State = AIState.AI_DO_NOTHING
        End Sub
        Public Overridable Sub OnEnterCombat()
        End Sub
        Public Overridable Sub OnLeaveCombat(Optional ByVal Reset As Boolean = True)
        End Sub
        Public Overridable Sub OnAttack(ByRef Attacker As BaseUnit)
        End Sub
        Public Overridable Sub OnGetHit(ByRef Attacker As BaseUnit, ByVal DamageCaused As Integer)
        End Sub
        Public Overridable Sub OnGenerateHate(ByRef Attacker As BaseUnit, ByVal HateValue As Integer)
        End Sub

        Public Overridable Sub DoThink()
        End Sub

        Public Overridable Sub Dispose() Implements System.IDisposable.Dispose
        End Sub
        Public Sub New()
        End Sub
    End Class

#End Region
#Region "WS.Creatures.AI.TestAIs"

    'NOTE: These are timer based AIs
    Public Class TestDefensiveAI
        Inherits TBaseAI
        Protected Creature As CreatureObject
        Protected NextAttackTimer As Timer = Nothing

        Public Sub New(ByRef Creature_ As CreatureObject)
            Creature = Creature_
        End Sub
        Public Overrides Sub Reset()
            aiHateTable.Clear()
            If Not NextAttackTimer Is Nothing Then NextAttackTimer.Dispose()
            NextAttackTimer = Nothing
            aiTarget = Nothing

            If State <> AIState.AI_DEAD Then State = AIState.AI_DO_NOTHING
        End Sub

        Public Overrides Sub OnAttack(ByRef Attacker As BaseUnit)
            aiHateTable(Attacker) += 0
            InitializeAttack()
        End Sub
        Public Overrides Sub OnGetHit(ByRef Attacker As BaseUnit, ByVal DamageCaused As Integer)
            aiHateTable(Attacker) += DamageCaused
            If State <> AIState.AI_ATTACKING Then
                InitializeAttack()
            End If
        End Sub
        Public Overrides Sub OnGenerateHate(ByRef Attacker As BaseUnit, ByVal HateValue As Integer)
            aiHateTable(Attacker) += HateValue
        End Sub

        Public Sub SelectTarget()
            Try
                Dim max As Integer = -1
                Dim tmpTarget As BaseObject = Nothing

                For Each Victim As KeyValuePair(Of BaseUnit, Integer) In aiHateTable
                    If Victim.Value > max Then
                        tmpTarget = Victim.Key
                    End If
                Next

                If Not aiTarget Is tmpTarget Then
                    aiTarget = tmpTarget
                    Creature.TurnTo(aiTarget.positionX, aiTarget.positionY)
                    'SendAttackStart(Creature.GUID, Target.GUID, CType(Target, CharacterObject).Client)
                End If
            Catch
                Reset()
            End Try
            If aiTarget Is Nothing Then Reset()
        End Sub
        Public Sub InitializeAttack()
            If Not NextAttackTimer Is Nothing Then Exit Sub
            SelectTarget()
            NextAttackTimer = New Threading.Timer(AddressOf DoAttack, Nothing, 1000, Timeout.Infinite)
        End Sub
        Public Sub DoAttack(ByVal Status As Object)
            If State = AIState.AI_DEAD Or (aiTarget Is Nothing) Then
                Reset()
            Else
                Try
                    If ((TypeOf aiTarget Is CharacterObject) AndAlso (CType(aiTarget, CharacterObject).DEAD = True)) OrElse _
                       ((TypeOf aiTarget Is CreatureObject) AndAlso (CType(aiTarget, CreatureObject).AIScript.State = AIState.AI_DEAD)) Then
                        aiHateTable.Remove(aiTarget)
                        SelectTarget()
                    End If

                    Dim distance As Single = GetDistance(CType(Creature, CreatureObject), CType(aiTarget, BaseUnit))

                    'DONE: Very far objects handling
                    If distance > Creature.MaxDistance Then
                        Creature.SendChatMessage("Arrgh... you won't get away!", ChatMsg.CHAT_MSG_MONSTER_SAY, LANGUAGES.LANG_UNIVERSAL, aiTarget.GUID)
                        aiHateTable.Remove(aiTarget)
                        SelectTarget()
                        If aiTarget Is Nothing Then
                            Reset()
                            Exit Sub
                        End If
                        distance = GetDistance(CType(Creature, CreatureObject), CType(aiTarget, BaseUnit))
                    End If

                    'DONE: Far objects handling
                    If distance > CREATURESDatabase(Creature.ID).CombatReach * Creature.Size + BaseUnit.CombatReach_Base Then
                        If Rnd.NextDouble > 0.3F Then
                            'DONE: Move closer
                            Dim NearX As Single = aiTarget.positionX
                            If aiTarget.positionX > Creature.positionX Then NearX -= Creature.Size Else NearX += Creature.Size
                            Dim NearY As Single = aiTarget.positionY
                            If aiTarget.positionY > Creature.positionY Then NearY -= Creature.Size Else NearY += Creature.Size
                            Dim NearZ As Single = GetZCoord(NearX, NearY, Creature.positionZ, Creature.MapID)
                            If NearZ > (aiTarget.positionZ + 2) Or NearZ < (aiTarget.positionZ - 2) Then NearZ = aiTarget.positionZ
                            NextAttackTimer.Change(Creature.MoveTo(NearX, NearY, NearZ, True), Timeout.Infinite)
                            Exit Sub
                        Else
                            'DONE: Cast spell
                            Dim tmpTargets As New SpellTargets
                            tmpTargets.SetTarget_UNIT(aiTarget)
                            CType(SPELLs(133), SpellInfo).Cast(1, CType(Creature, CreatureObject), tmpTargets)
                            NextAttackTimer.Change(CType(SPELLs(133), SpellInfo).GetCastTime, Timeout.Infinite)
                            Exit Sub
                        End If
                    End If

                    'DONE: Look to target
                    If Not IsInFrontOf(CType(Creature, CreatureObject), CType(aiTarget, BaseUnit)) Then
                        Creature.TurnTo(CType(aiTarget, BaseUnit))
                    End If

                    'DONE: Fix Creature VS Creature
                    Dim damageInfo As DamageInfo = CalculateDamage(CType(Creature, CreatureObject), aiTarget, False, False)
                    SendAttackerStateUpdate(CType(Creature, CreatureObject), CType(aiTarget, BaseUnit), damageInfo)
                    aiTarget.DealDamage(damageInfo.GetDamage)

                    NextAttackTimer.Change(CREATURESDatabase(Creature.ID).BaseAttackTime, Timeout.Infinite)
                Catch e As Exception
                    Console.WriteLine("DEBUG: Error attacking target.")
                    Reset()
                End Try
            End If
        End Sub
    End Class
    Public Class TestMovingAI
        Inherits TBaseAI
        Protected Creature As CreatureObject

        Protected MoveTimer As Threading.Timer = Nothing

        Public Sub DoMove(ByVal state As Object)
            Dim selectedX As Single = Creature.positionX + Rnd.Next(-5, 5)
            Dim selectedY As Single = Creature.positionY + Rnd.Next(-5, 5)

            Dim distance As Single = GetDistance(selectedX, Creature.SpawnX, selectedY, Creature.SpawnY)
            If distance > Creature.MaxDistance Then
                DoMove(Nothing)
            Else
                Creature.MoveTo(selectedX, selectedY, GetZCoord(selectedX, selectedY, Creature.positionZ, Creature.MapID))
            End If
        End Sub
        Public Sub New(ByRef Creature_ As CreatureObject)
            Creature = Creature_
            MoveTimer = New Threading.Timer(AddressOf DoMove, Nothing, 60000, 5000)
        End Sub
    End Class
    Public Class TestDefaultAI
        Inherits TBaseAI

        Protected Const AI_INTERVAL_MOVE As Integer = 3000
        Protected Const AI_INTERVAL_SLEEP As Integer = 6000
        Protected Const AI_INTERVAL_DEAD As Integer = 60000
        Protected Const PIx2 As Single = 2 * Math.PI

        Protected aiCreature As CreatureObject = Nothing
        Protected aiTimer As Timer = Nothing

        Public Sub New(ByRef Creature As CreatureObject)
            State = AIState.AI_WANDERING

            aiCreature = Creature
            aiTarget = Nothing
            aiTimer = New Threading.Timer(AddressOf DoThink, Nothing, AI_INTERVAL_MOVE, Timeout.Infinite)
        End Sub
        Public Overrides Sub OnAttack(ByRef Attacker As BaseUnit)
            aiHateTable(Attacker) += 0
            Me.State = TBaseAI.AIState.AI_ATTACKING
        End Sub
        Public Overrides Sub OnGetHit(ByRef Attacker As BaseUnit, ByVal DamageCaused As Integer)
            aiHateTable(Attacker) += DamageCaused
            Me.State = TBaseAI.AIState.AI_ATTACKING
        End Sub
        Public Overrides Sub OnGenerateHate(ByRef Attacker As BaseUnit, ByVal HateValue As Integer)
            aiHateTable(Attacker) += HateValue
            Me.State = TBaseAI.AIState.AI_ATTACKING
        End Sub

        Protected Shadows Sub DoThink(ByVal state As Object)
            Select Case Me.State
                Case AIState.AI_MOVE_FOR_ATTACK
                    DoMove(Nothing)
                Case AIState.AI_WANDERING
                    If Rnd.NextDouble > 0.2F Then
                        DoMove(Nothing)
                    Else
                        DoNothing(Nothing)
                    End If

                Case AIState.AI_ATTACKING
                    DoAttack(Nothing)
                Case AIState.AI_MOVING
                    DoMove(Nothing)
                Case AIState.AI_DO_NOTHING
                    DoNothing(Nothing)

                Case AIState.AI_DEAD
                    aiTimer.Change(AI_INTERVAL_DEAD, Timeout.Infinite)
                Case Else
                    aiCreature.SendChatMessage("Unknown AI mode!", ChatMsg.CHAT_MSG_MONSTER_SAY, LANGUAGES.LANG_UNIVERSAL)
                    Me.State = TBaseAI.AIState.AI_DO_NOTHING
            End Select

        End Sub
        Protected Sub DoMove(ByVal state As Object)
            Dim distanceToSpawn As Single = GetDistance(aiCreature.positionX, aiCreature.SpawnX, aiCreature.positionY, aiCreature.SpawnY, aiCreature.positionZ, aiCreature.SpawnZ)

            'DONE: Back to spawn if too far
            If distanceToSpawn > aiCreature.MaxDistance * 2 Then
                Me.State = TBaseAI.AIState.AI_WANDERING
                aiCreature.Life.Current = aiCreature.Life.Maximum
                aiTimer.Change(aiCreature.MoveTo(aiCreature.SpawnX, aiCreature.SpawnY, aiCreature.SpawnZ, True), Timeout.Infinite)
                Exit Sub
            End If

            If aiTarget Is Nothing Then

                'DONE: Do simple random movement
                Dim distance As Single = AI_INTERVAL_MOVE / 1000 * aiCreature.CreatureInfo.WalkSpeed
                Dim angle As Single = Rnd.NextDouble * PIx2

                Dim selectedX As Single = aiCreature.positionX + Math.Cos(angle) * distance
                Dim selectedY As Single = aiCreature.positionY + Math.Sin(angle) * distance

                If aiCreature.CanMoveTo(selectedX, selectedY, GetZCoord(selectedX, selectedY, aiCreature.positionZ, aiCreature.MapID)) Then
                    Me.State = TBaseAI.AIState.AI_WANDERING
                    aiTimer.Change(aiCreature.MoveTo(selectedX, selectedY, GetZCoord(selectedX, selectedY, aiCreature.positionZ, aiCreature.MapID), False), Timeout.Infinite)
                Else
                    aiTimer.Change(AI_INTERVAL_MOVE, Timeout.Infinite)
                End If

            Else

                'DONE: Do targeted movement to attack target
                Dim distance As Single = AI_INTERVAL_MOVE / 1000 * aiCreature.CreatureInfo.RunSpeed
                Dim distanceToTarget As Single = GetDistance(CType(aiCreature, CreatureObject), CType(aiTarget, BaseUnit))

                If distanceToTarget < distance Then
                    'DONE: Move to target
                    Me.State = TBaseAI.AIState.AI_ATTACKING

                    Dim NearX As Single = aiTarget.positionX
                    If aiTarget.positionX > aiCreature.positionX Then NearX -= aiCreature.Size Else NearX += aiCreature.Size
                    Dim NearY As Single = aiTarget.positionY
                    If aiTarget.positionY > aiCreature.positionY Then NearY -= aiCreature.Size Else NearY += aiCreature.Size
                    Dim NearZ As Single = GetZCoord(NearX, NearY, aiCreature.positionZ, aiCreature.MapID)
                    If NearZ > (aiTarget.positionZ + 2) Or NearZ < (aiTarget.positionZ - 2) Then NearZ = aiTarget.positionZ

                    If aiCreature.CanMoveTo(NearX, NearY, NearZ) Then
                        aiTimer.Change(aiCreature.MoveTo(NearX, NearY, NearZ, True), Timeout.Infinite)
                    Else
                        'DONE: Select next target
                        aiHateTable.Remove(aiTarget)
                        SelectTarget()
                        If Not CheckTarget() Then aiTimer.Change(AI_INTERVAL_MOVE, Timeout.Infinite)
                    End If

                Else

                    'DONE: Move to target by vector
                    Dim angle As Single = Math.Atan2(aiTarget.positionY - aiCreature.positionY, aiTarget.positionX - aiCreature.positionX)
                    Dim selectedX As Single = aiCreature.positionX + Math.Cos(angle) * distance
                    Dim selectedY As Single = aiCreature.positionY + Math.Sin(angle) * distance

                    If aiCreature.CanMoveTo(selectedX, selectedY, GetZCoord(selectedX, selectedY, aiCreature.positionZ, aiCreature.MapID)) Then
                        aiTimer.Change(aiCreature.MoveTo(selectedX, selectedY, GetZCoord(selectedX, selectedY, aiCreature.positionZ, aiCreature.MapID), True), Timeout.Infinite)
                    Else
                        'DONE: Select next target
                        aiHateTable.Remove(aiTarget)
                        SelectTarget()
                        If Not CheckTarget() Then aiTimer.Change(AI_INTERVAL_MOVE, Timeout.Infinite)
                    End If

                End If
            End If

        End Sub
        Protected Sub DoAttack(ByVal state As Object)

            If aiTarget Is Nothing Then
                Me.SelectTarget()
            End If

            If Me.State <> AIState.AI_ATTACKING Then
                'DONE: Seems like we lost our target
                aiTimer.Change(AI_INTERVAL_SLEEP, Timeout.Infinite)
            Else
                'DONE: Do real melee attack
                Try
                    If ((TypeOf aiTarget Is CharacterObject) AndAlso (CType(aiTarget, CharacterObject).DEAD = True)) OrElse _
                       ((TypeOf aiTarget Is CreatureObject) AndAlso (CType(aiTarget, CreatureObject).AIScript.State = AIState.AI_DEAD)) Then
                        aiTarget = Nothing
                        aiHateTable.Remove(aiTarget)
                        SelectTarget()
                    End If
                    If CheckTarget() Then Exit Sub

                    Dim distance As Single = GetDistance(CType(aiCreature, CreatureObject), CType(aiTarget, BaseUnit))

                    'DONE: Very far objects handling
                    If distance > aiCreature.MaxDistance Then
                        aiCreature.SendChatMessage("Arrgh... you won't get away!", ChatMsg.CHAT_MSG_MONSTER_SAY, LANGUAGES.LANG_UNIVERSAL, aiTarget.GUID)
                        aiTarget = Nothing
                        aiHateTable.Remove(aiTarget)
                        SelectTarget()
                        If CheckTarget() Then Exit Sub
                        distance = GetDistance(CType(aiCreature, CreatureObject), CType(aiTarget, BaseUnit))
                    End If

                    'DONE: Far objects handling
                    If distance > CREATURESDatabase(aiCreature.ID).CombatReach * aiCreature.Size + BaseUnit.CombatReach_Base Then
                        If Rnd.NextDouble > 0.1F Then
                            'DONE: Move closer
                            Me.State = AIState.AI_MOVE_FOR_ATTACK
                            Me.DoMove(Nothing)
                            Exit Sub
                        Else
                            'DONE: Cast spell
                            Dim tmpTargets As New SpellTargets
                            tmpTargets.SetTarget_UNIT(aiTarget)
                            CType(SPELLs(133), SpellInfo).Cast(1, CType(aiCreature, CreatureObject), tmpTargets)
                            aiTimer.Change(CType(SPELLs(133), SpellInfo).GetCastTime, Timeout.Infinite)
                            Exit Sub
                        End If
                    End If

                    'DONE: Look to aiTarget
                    If Not IsInFrontOf(CType(aiCreature, CreatureObject), CType(aiTarget, BaseUnit)) Then
                        aiCreature.TurnTo(CType(aiTarget, BaseUnit))
                    End If

                    'DONE: Fix aiCreature VS aiCreature
                    Dim damageInfo As DamageInfo = CalculateDamage(CType(aiCreature, CreatureObject), aiTarget, False, False)
                    SendAttackerStateUpdate(CType(aiCreature, CreatureObject), CType(aiTarget, BaseUnit), damageInfo)
                    aiTarget.DealDamage(damageInfo.GetDamage)

                    aiTimer.Change(CREATURESDatabase(aiCreature.ID).BaseAttackTime, Timeout.Infinite)
                Catch e As Exception
                    Console.WriteLine("DEBUG: Error attacking aiTarget.")
                    Reset()
                End Try
            End If

        End Sub
        Protected Sub DoNothing(ByVal state As Object)
            aiTimer.Change(AI_INTERVAL_SLEEP, Timeout.Infinite)
        End Sub

        Public Overrides Sub Reset()
            aiHateTable.Clear()
            aiTarget = Nothing

            'DONE: Return to default
            If State <> AIState.AI_DEAD Then
                Me.State = AIState.AI_WANDERING
            End If
        End Sub
        Protected Sub SelectTarget()
            Try
                Dim max As Integer = -1
                Dim tmpTarget As BaseObject = Nothing

                For Each Victim As KeyValuePair(Of BaseUnit, Integer) In aiHateTable
                    If Victim.Value > max Then
                        tmpTarget = Victim.Key
                    End If
                Next

                If Not aiTarget Is tmpTarget Then
                    aiTarget = tmpTarget
                    aiCreature.TurnTo(aiTarget.positionX, aiTarget.positionY)
                    'SendAttackStart(Creature.GUID, Target.GUID, CType(Target, CharacterObject).Client)

                    Me.State = AIState.AI_ATTACKING
                End If
            Catch
                Reset()
            End Try

            If aiTarget Is Nothing Then Reset()
        End Sub
        Protected Function CheckTarget() As Boolean
            If aiTarget Is Nothing Then
                Reset()
                aiTimer.Change(AI_INTERVAL_SLEEP, Timeout.Infinite)
                Return True
            End If

            Return False
        End Function

    End Class

#End Region
#Region "WS.Creatures.AI.StandartAIs"

    'Standart AIs           | move | defend | attack | cooperative | spawn dst. |
    '  DefaultAI:           |  +       +         -          -            +
    '  StandingAI:          |  -       +         -          -            +
    '  GuardAI:             |  -       +         +          -            +
    '  EvilAI:              |  +       +         +          -            +
    '  EvilCooperativeAI:   |  +       +         +          +            +
    'Quest AIs
    '  WaypointAI:          | move in wayponts and defend
    '  EvilWaypointAI:      | move in wayponts, defend and look for enemy
    Public Class DefaultAI
        Inherits TBaseAI

        Protected aiCreature As CreatureObject = Nothing
        Protected aiTimer As Integer = 0

        Protected Const AI_INTERVAL_MOVE As Integer = 3000
        Protected Const AI_INTERVAL_SLEEP As Integer = 6000
        Protected Const AI_INTERVAL_DEAD As Integer = 60000
        Protected Const PIx2 As Single = 2 * Math.PI

        Protected CurrentWayPoint As Integer = 0

        Public Sub New(ByRef Creature As CreatureObject)
            State = AIState.AI_WANDERING

            aiCreature = Creature
            aiTarget = Nothing
        End Sub
        Public Overrides Sub ChangeTimer(ByVal NewValue As Integer)
            aiTimer = NewValue
        End Sub
        Public Overrides Function IsMoving() As Boolean
            If (timeGetTime - aiCreature.LastMove) < aiTimer Then
                Select Case Me.State
                    Case AIState.AI_MOVE_FOR_ATTACK
                        Return True
                    Case AIState.AI_MOVING
                        Return True
                    Case AIState.AI_WANDERING
                        Return True
                    Case Else
                        Return False
                End Select
            Else
                Return False
            End If
        End Function
        Public Overrides Sub OnEnterCombat()
            If aiCreature.Life.Current = 0 Then Exit Sub 'Prevents the creature from doing this below if it's dead already
            'DONE: Decide it's real position if it hasn't stopped
            If (timeGetTime - aiCreature.LastMove) < aiCreature.LastMove_Time Then
                Dim RealDistance As Single = (timeGetTime - aiCreature.LastMove) / 1000 * aiCreature.CreatureInfo.WalkSpeed
                WORLD_CREATUREs(aiCreature.GUID).positionX = aiCreature.OldX + Math.Cos(aiCreature.orientation) * RealDistance
                WORLD_CREATUREs(aiCreature.GUID).positionY = aiCreature.OldY + Math.Sin(aiCreature.orientation) * RealDistance
                WORLD_CREATUREs(aiCreature.GUID).positionZ = GetZCoord(WORLD_CREATUREs(aiCreature.GUID).positionX, WORLD_CREATUREs(aiCreature.GUID).positionY, WORLD_CREATUREs(aiCreature.GUID).positionZ, WORLD_CREATUREs(aiCreature.GUID).MapID)
            End If

            Me.State = AIState.AI_ATTACKING
            DoThink()

            If MonsterSay.ContainsKey(aiCreature.ID) Then
                If MonsterSay(aiCreature.ID).EventNo = MonsterSayEvents.MONSTER_SAY_EVENT_ENTER_COMBAT Then
                    Dim Chance As Integer = (MonsterSay(aiCreature.ID).Chance)
                    If Rnd.Next(1, 101) <= Chance Then
                        Dim TargetGUID As ULong
                        If Not aiTarget Is Nothing Then TargetGUID = aiTarget.GUID
                        aiCreature.SendChatMessage(SelectMonsterSay(aiCreature.ID), ChatMsg.CHAT_MSG_MONSTER_SAY, LANGUAGES.LANG_UNIVERSAL, TargetGUID)
                    End If
                End If
            End If
        End Sub
        Public Overrides Sub OnLeaveCombat(Optional ByVal Reset As Boolean = True)
            'DONE: Remove combat flag from everyone
            For Each Victim As KeyValuePair(Of BaseUnit, Integer) In aiHateTable
                If TypeOf Victim.Key Is CharacterObject Then
                    CType(Victim.Key, CharacterObject).inCombatWith.Remove(aiCreature.GUID)
                    CType(Victim.Key, CharacterObject).CheckCombat()
                End If
            Next

            aiTarget = Nothing
            aiHateTable.Clear()
            aiHateTableRemove.Clear()
            aiCreature.SendTargetUpdate(0)

            If Reset Then
                'DONE: Reset values and move to spawn
                Me.State = TBaseAI.AIState.AI_WANDERING
                WORLD_CREATUREs(aiCreature.GUID).Heal(aiCreature.Life.Maximum - WORLD_CREATUREs(aiCreature.GUID).Life.Current)

                aiTimer = aiCreature.MoveTo(aiCreature.SpawnX, aiCreature.SpawnY, aiCreature.SpawnZ, True)
            End If

            If MonsterSay.ContainsKey(aiCreature.ID) Then
                If MonsterSay(aiCreature.ID).EventNo = MonsterSayEvents.MONSTER_SAY_EVENT_EXIT_COMBAT Then
                    Dim Chance As Integer = (MonsterSay(aiCreature.ID).Chance)
                    If Rnd.Next(1, 101) <= Chance Then
                        Dim TargetGUID As ULong
                        If Not aiTarget Is Nothing Then TargetGUID = aiTarget.GUID
                        aiCreature.SendChatMessage(SelectMonsterSay(aiCreature.ID), ChatMsg.CHAT_MSG_MONSTER_SAY, LANGUAGES.LANG_UNIVERSAL, TargetGUID)
                    End If
                End If
            End If

        End Sub
        Public Overrides Sub OnGetHit(ByRef Attacker As BaseUnit, ByVal DamageCaused As Integer)
            If Me.State <> TBaseAI.AIState.AI_DEAD AndAlso Me.State <> TBaseAI.AIState.AI_RESPAWN Then
                If Me.InCombat = False Then
                    aiHateTable.Add(Attacker, DamageCaused)
                    OnEnterCombat()
                    Me.State = TBaseAI.AIState.AI_ATTACKING
                    Me.DoThink()
                End If

                If aiHateTable.ContainsKey(Attacker) = False Then
                    aiHateTable.Add(Attacker, DamageCaused)
                    If TypeOf Attacker Is CharacterObject AndAlso CType(Attacker, CharacterObject).inCombatWith.Contains(aiCreature.GUID) = False Then
                        CType(Attacker, CharacterObject).inCombatWith.Add(aiCreature.GUID)
                        CType(Attacker, CharacterObject).CheckCombat()
                    End If
                Else
                    aiHateTable(Attacker) += DamageCaused * Attacker.Spell_ThreatModifier
                End If
            End If
        End Sub
        Public Overrides Sub OnGenerateHate(ByRef Attacker As BaseUnit, ByVal HateValue As Integer)
            If Me.State <> TBaseAI.AIState.AI_DEAD AndAlso Me.State <> TBaseAI.AIState.AI_RESPAWN Then
                If Me.InCombat = False Then
                    aiHateTable.Add(Attacker, HateValue)
                    OnEnterCombat()
                    Me.State = TBaseAI.AIState.AI_ATTACKING
                    DoThink()
                End If
                If aiHateTable.ContainsKey(Attacker) = False Then
                    aiHateTable.Add(Attacker, HateValue)
                    If TypeOf Attacker Is CharacterObject AndAlso CType(Attacker, CharacterObject).inCombatWith.Contains(aiCreature.GUID) = False Then
                        CType(Attacker, CharacterObject).inCombatWith.Add(aiCreature.GUID)
                        CType(Attacker, CharacterObject).CheckCombat()
                    End If
                Else
                    aiHateTable(Attacker) += HateValue * Attacker.Spell_ThreatModifier
                End If
            End If
        End Sub
        Public Overrides Sub Reset()
            OnLeaveCombat()
            aiTimer = 0

            'DONE: Return to default
            If State <> AIState.AI_DEAD AndAlso State <> AIState.AI_RESPAWN Then
                Me.State = AIState.AI_WANDERING
            End If
        End Sub
        Protected Sub SelectTarget()
            Try
                Dim max As Integer = -1
                Dim tmpTarget As BaseUnit = Nothing

                'DONE: Select max hate
                For Each Victim As KeyValuePair(Of BaseUnit, Integer) In aiHateTable
                    If Victim.Value > max Then
                        If Victim.Key.isDead Then
                            aiHateTableRemove.Add(Victim.Key, Victim.Value)
                            ''''aiHateTable.Remove(Victim.Key)
                            If TypeOf Victim.Key Is CharacterObject Then
                                CType(Victim.Key, CharacterObject).inCombatWith.Remove(aiCreature.GUID)
                                CType(Victim.Key, CharacterObject).CheckCombat()
                            End If
                        Else
                            max = Victim.Value
                            tmpTarget = Victim.Key
                        End If
                    End If
                Next

                ' Remove From aiHateTable
                For Each VictimRemove As KeyValuePair(Of BaseUnit, Integer) In aiHateTableRemove
                    aiHateTable.Remove(VictimRemove.Key)
                Next

                'DONE: Set the target
                If (Not tmpTarget Is Nothing) AndAlso Not aiTarget Is tmpTarget Then
                    aiTarget = tmpTarget
                    aiCreature.TurnTo(aiTarget.positionX, aiTarget.positionY)
                    aiCreature.SendTargetUpdate(tmpTarget.GUID)

                    Me.State = AIState.AI_ATTACKING
                End If
            Catch
                Reset()
            End Try

            If aiTarget Is Nothing Then Reset()
        End Sub
        Protected Function CheckTarget() As Boolean
            If aiTarget Is Nothing Then
                Reset()
                Return True
            End If

            Return False
        End Function

        Public Overrides Sub DoThink()
            If aiCreature Is Nothing Then Exit Sub 'Fixes a crash
            If aiTimer > TAIManager.UPDATE_TIMER Then
                aiTimer -= TAIManager.UPDATE_TIMER
                Exit Sub
            Else
                aiTimer = 0
            End If

            'DONE: Fixes a bug where creatures attack you when they are dead
            If Me.State <> AIState.AI_DEAD AndAlso Me.State <> AIState.AI_RESPAWN AndAlso aiCreature.Life.Current = 0 Then
                Me.State = AIState.AI_DEAD
            End If

            Select Case Me.State
                Case AIState.AI_DEAD
                    If Me.aiHateTable.Count > 0 Then
                        OnLeaveCombat(False)

                        Dim RespawnTime As Integer = aiCreature.CreatureInfo.RespawnTime
                        If RespawnTime > 0 Then
                            If RespawnTime <= 3 Then
                                If RespawnTime = 1 Then
                                    aiTimer = 30000
                                Else
                                    aiTimer = ((RespawnTime - 1) * 60000)
                                End If
                            Else
                                aiTimer = 180000 '3 minutes till the corpse disappear
                            End If
                        Else
                            aiTimer = 180000 '3 minutes till the corpse disappear
                        End If
                    Else
                        Me.State = AIState.AI_RESPAWN

                        Dim RespawnTime As Integer = aiCreature.CreatureInfo.RespawnTime
                        If RespawnTime > 0 Then
                            If RespawnTime <= 3 Then
                                If RespawnTime = 1 Then
                                    aiTimer = 30000
                                Else
                                    aiTimer = RespawnTime * 60000 'Minutes -> Milliseconds
                                End If
                            Else
                                aiTimer = 180000 '3 minutes till the corpse disappear
                            End If
                            aiCreature.Despawn()
                        Else
                            aiCreature.Destroy()
                        End If
                    End If
                Case AIState.AI_RESPAWN
                    Me.State = AIState.AI_WANDERING
                    aiCreature.Respawn()
                    aiTimer = 10000 'Wait 10 seconds before starting to react
                Case AIState.AI_MOVE_FOR_ATTACK
                    DoMove()
                Case AIState.AI_WANDERING
                    If Rnd.NextDouble > 0.2F Then
                        DoMove()
                    End If
                Case AIState.AI_ATTACKING
                    DoAttack()
                Case AIState.AI_MOVING
                    DoMove()
                Case AIState.AI_DO_NOTHING
                Case Else
                    aiCreature.SendChatMessage("Unknown AI mode!", ChatMsg.CHAT_MSG_MONSTER_SAY, LANGUAGES.LANG_UNIVERSAL)
                    Me.State = TBaseAI.AIState.AI_DO_NOTHING
            End Select
        End Sub

        Protected Sub DoAttack()
            If aiCreature.Spell_Pacifyed Then
                aiTimer = AI_INTERVAL_MOVE
                Exit Sub
            End If

            'DONE: Change the target to the one with most threat
            SelectTarget()

            If Me.State <> AIState.AI_ATTACKING Then
                'DONE: Seems like we lost our target
                aiTimer = AI_INTERVAL_SLEEP
            Else
                'DONE: Do real melee attack
                Try
                    If aiTarget.isDead Then
                        aiHateTable.Remove(aiTarget)
                        aiTarget = Nothing
                        SelectTarget()
                    End If
                    If CheckTarget() Then
                        Me.State = TBaseAI.AIState.AI_WANDERING
                        WORLD_CREATUREs(aiCreature.GUID).Life.Current = aiCreature.Life.Maximum

                        aiTimer = aiCreature.MoveTo(aiCreature.SpawnX, aiCreature.SpawnY, aiCreature.SpawnZ, True)
                        Exit Sub
                    End If

                    Dim distance As Single = GetDistance(CType(aiCreature, CreatureObject), CType(aiTarget, BaseUnit))

                    'DONE: Very far objects handling
                    If distance > aiCreature.MaxDistance Then
                        'aiCreature.SendChatMessage("Arrgh... you won't get away!", ChatMsg.CHAT_MSG_MONSTER_SAY, LANGUAGES.LANG_UNIVERSAL, aiTarget.GUID)
                        aiTarget = Nothing
                        aiHateTable.Remove(aiTarget)
                        SelectTarget()
                        If CheckTarget() Then
                            OnLeaveCombat()
                            Exit Sub
                        End If
                        distance = GetDistance(CType(aiCreature, CreatureObject), CType(aiTarget, BaseUnit))
                    End If

                    'DONE: Far objects handling
                    If distance > (CREATURESDatabase(aiCreature.ID).CombatReach * aiCreature.Size + BaseUnit.CombatReach_Base) Then
                        If aiCreature.Spell_Silenced Or Rnd.NextDouble > 0.1F Then
                            'DONE: Move closer
                            Me.State = AIState.AI_MOVE_FOR_ATTACK
                            Me.DoMove()
                            Exit Sub
                        Else
                            'DONE: Cast spell
                            'TODO: Get spell for every creature
                            'aiTimer = aiCreature.CastSpell(133, aiTarget)
                            Exit Sub
                        End If
                    End If

                    'DONE: Look to aiTarget
                    If Not IsInFrontOf(CType(aiCreature, CreatureObject), CType(aiTarget, BaseUnit)) Then
                        aiCreature.TurnTo(CType(aiTarget, BaseUnit))
                    End If

                    'DONE: Fix aiCreature VS aiCreature
                    Dim damageInfo As DamageInfo = CalculateDamage(CType(aiCreature, CreatureObject), aiTarget, False, False)
                    SendAttackerStateUpdate(CType(aiCreature, CreatureObject), CType(aiTarget, BaseUnit), damageInfo)
                    aiTarget.DealDamage(damageInfo.GetDamage)

                    aiTimer = CREATURESDatabase(aiCreature.ID).BaseAttackTime
                Catch e As Exception
                    Console.WriteLine("DEBUG: Error attacking aiTarget.")
                    Reset()
                End Try
            End If

        End Sub
        Protected Sub DoMove()
            Dim NextWaypoint As Integer
            Dim WaypointCount As Integer
            Dim MySQLQuery As New DataTable
            Dim WaypointSQLQuery As New DataTable
            Dim WaitTime As Integer = 0

            Dim distanceToSpawn As Single = GetDistance(aiCreature.positionX, aiCreature.SpawnX, aiCreature.positionY, aiCreature.SpawnY, aiCreature.positionZ, aiCreature.SpawnZ)

            'DONE: Back to spawn if too far
            If (Not aiCreature.isWaypoint) AndAlso (aiCreature.SpawnID > 0) AndAlso (distanceToSpawn > aiCreature.MaxDistance * 2) Then
                OnLeaveCombat()
                Exit Sub
            End If

            Dim distance As Single = AI_INTERVAL_MOVE / 1000 * aiCreature.CreatureInfo.WalkSpeed
            Dim angle As Single = Rnd.NextDouble * PIx2

            Dim selectedX As Single = aiCreature.positionX + Math.Cos(angle) * distance
            Dim selectedY As Single = aiCreature.positionY + Math.Sin(angle) * distance
            Dim selectedZ As Single = GetZCoord(selectedX, selectedY, aiCreature.positionZ, aiCreature.MapID)

            If aiTarget Is Nothing Then
                If aiCreature.isWaypoint Then ' If this is a creature that moves along waypoints

                    Database.Query(String.Format("SELECT Count(*) FROM creature_movement WHERE spawnid='{0}'", aiCreature.SpawnID), MySQLQuery)
                    WaypointCount = MySQLQuery.Rows(0).Item(0)

                    NextWaypoint = aiCreature.CurrentWaypoint + 1
                    If NextWaypoint > WaypointCount Then
                        aiCreature.CurrentWaypoint = 1
                    Else
                        aiCreature.CurrentWaypoint = NextWaypoint
                    End If

                    Database.Query(String.Format("SELECT * FROM creature_movement WHERE spawnid='{0}' AND waypointid='{1}'", aiCreature.SpawnID, aiCreature.CurrentWaypoint), WaypointSQLQuery)

                    selectedX = WaypointSQLQuery.Rows(0).Item("position_x")
                    selectedY = WaypointSQLQuery.Rows(0).Item("position_y")
                    selectedZ = WaypointSQLQuery.Rows(0).Item("position_z")
                    WaitTime = WaypointSQLQuery.Rows(0).Item("waittime")

                Else ' Not a Waypoint Creature
                    'DONE: Do simple random movement
                    Dim MoveTries As Byte = 0
TryMoveAgain:
                    If MoveTries > 5 Then 'The creature is at a very weird location now
                        Me.State = TBaseAI.AIState.AI_WANDERING
                        WORLD_CREATUREs(aiCreature.GUID).Life.Current = aiCreature.Life.Maximum
                        aiTimer = aiCreature.MoveTo(aiCreature.SpawnX, aiCreature.SpawnY, aiCreature.SpawnZ, True)
                        Exit Sub
                    End If

                    ''''Dim distance As Single = AI_INTERVAL_MOVE / 1000 * aiCreature.CreatureInfo.WalkSpeed
                    ''''Dim angle As Single = Rnd.NextDouble * PIx2

                    WORLD_CREATUREs(aiCreature.GUID).orientation = angle

                    ''''Dim selectedX As Single = aiCreature.positionX + Math.Cos(angle) * distance
                    ''''Dim selectedY As Single = aiCreature.positionY + Math.Sin(angle) * distance
                    ''''Dim selectedZ As Single = GetZCoord(selectedX, selectedY, aiCreature.positionZ, aiCreature.MapID)
                    MoveTries += 1
                    If Math.Abs(aiCreature.positionZ - selectedZ) > 5 Then GoTo TryMoveAgain 'Prevent most cases of wall climbing

                End If ' End If for Waypoint Creature Check

                If aiCreature.CanMoveTo(selectedX, selectedY, selectedZ) Then
                    Me.State = TBaseAI.AIState.AI_WANDERING
                    aiTimer = aiCreature.MoveTo(selectedX, selectedY, selectedZ, False)
                Else
                    If WaitTime > 0 Then
                        aiTimer = WaitTime
                    Else
                        aiTimer = AI_INTERVAL_MOVE
                    End If
                End If

            Else
                'DONE: Change the target to the one with most threat
                SelectTarget()

                'DONE: Do targeted movement to attack target
                ''''Dim distance As Single = AI_INTERVAL_MOVE / 1000 * aiCreature.CreatureInfo.RunSpeed
                distance = AI_INTERVAL_MOVE / 1000 * aiCreature.CreatureInfo.RunSpeed
                Dim distanceToTarget As Single = GetDistance(CType(aiCreature, CreatureObject), CType(aiTarget, BaseUnit))

                If distanceToTarget < distance Then
                    'DONE: Move to target
                    Me.State = TBaseAI.AIState.AI_ATTACKING

                    'DONE: Decide it's real position
                    If IsMoving() AndAlso (timeGetTime - aiCreature.LastMove) < aiCreature.LastMove_Time Then
                        Dim RealDistance As Single = (timeGetTime - aiCreature.LastMove) / 1000 * aiCreature.CreatureInfo.RunSpeed
                        WORLD_CREATUREs(aiCreature.GUID).positionX = aiCreature.OldX + Math.Cos(aiCreature.orientation) * RealDistance
                        WORLD_CREATUREs(aiCreature.GUID).positionY = aiCreature.OldY + Math.Sin(aiCreature.orientation) * RealDistance
                        WORLD_CREATUREs(aiCreature.GUID).positionZ = GetZCoord(WORLD_CREATUREs(aiCreature.GUID).positionX, WORLD_CREATUREs(aiCreature.GUID).positionY, WORLD_CREATUREs(aiCreature.GUID).positionZ, WORLD_CREATUREs(aiCreature.GUID).MapID)
                    End If

                    Dim NearX As Single = aiTarget.positionX
                    If aiTarget.positionX > aiCreature.positionX Then NearX -= aiCreature.Size Else NearX += aiCreature.Size
                    Dim NearY As Single = aiTarget.positionY
                    If aiTarget.positionY > aiCreature.positionY Then NearY -= aiCreature.Size Else NearY += aiCreature.Size
                    Dim NearZ As Single = GetZCoord(NearX, NearY, aiCreature.positionZ, aiCreature.MapID)
                    If NearZ > (aiTarget.positionZ + 2) Or NearZ < (aiTarget.positionZ - 2) Then NearZ = aiTarget.positionZ
                    If aiCreature.CanMoveTo(NearX, NearY, NearZ) Then
                        WORLD_CREATUREs(aiCreature.GUID).orientation = GetOrientation(WORLD_CREATUREs(aiCreature.GUID).positionX, NearX, WORLD_CREATUREs(aiCreature.GUID).positionY, NearY)
                        aiTimer = aiCreature.MoveTo(NearX, NearY, NearZ, True)
                    Else
                        'DONE: Select next target
                        aiHateTable.Remove(aiTarget)
                        If TypeOf aiTarget Is CharacterObject Then
                            CType(aiTarget, CharacterObject).inCombatWith.Remove(aiCreature.GUID)
                            If CType(aiTarget, CharacterObject).inCombatWith.Count = 0 Then SetPlayerOutOfCombat(CType(aiTarget, CharacterObject))
                        End If
                        SelectTarget()
                        If Not CheckTarget() Then
                            OnLeaveCombat()
                        End If
                    End If

                Else
                    'DONE: Move to target by vector
                    Me.State = TBaseAI.AIState.AI_MOVE_FOR_ATTACK

                    ''''Dim angle As Single = GetOrientation(aiTarget.positionY, aiCreature.positionY, aiTarget.positionX, aiCreature.positionX)
                    angle = GetOrientation(aiTarget.positionY, aiCreature.positionY, aiTarget.positionX, aiCreature.positionX)
                    WORLD_CREATUREs(aiCreature.GUID).orientation = angle

                    ''''Dim selectedX As Single = aiCreature.positionX + Math.Cos(angle) * distance
                    ''''Dim selectedY As Single = aiCreature.positionY + Math.Sin(angle) * distance
                    selectedX = aiCreature.positionX + Math.Cos(angle) * distance
                    selectedY = aiCreature.positionY + Math.Sin(angle) * distance

                    If aiCreature.CanMoveTo(selectedX, selectedY, GetZCoord(selectedX, selectedY, aiCreature.positionZ, aiCreature.MapID)) Then
                        aiTimer = aiCreature.MoveTo(selectedX, selectedY, GetZCoord(selectedX, selectedY, aiCreature.positionZ, aiCreature.MapID), True)
                    Else
                        'DONE: Select next target
                        aiHateTable.Remove(aiTarget)
                        If TypeOf aiTarget Is CharacterObject Then
                            CType(aiTarget, CharacterObject).inCombatWith.Remove(aiCreature.GUID)
                            If CType(aiTarget, CharacterObject).inCombatWith.Count = 0 Then SetPlayerOutOfCombat(CType(aiTarget, CharacterObject))
                        End If
                        SelectTarget()
                        If Not CheckTarget() Then
                            OnLeaveCombat()
                        End If
                    End If

                End If
            End If

            If MonsterSay.ContainsKey(aiCreature.ID) Then
                If MonsterSay(aiCreature.ID).EventNo = MonsterSayEvents.MONSTER_SAY_EVENT_RANDOM_WAYPOINT Then
                    Dim Chance As Integer = (MonsterSay(aiCreature.ID).Chance)
                    If Rnd.Next(1, 101) <= Chance Then
                        Dim TargetGUID As ULong
                        If Not aiTarget Is Nothing Then TargetGUID = aiTarget.GUID
                        aiCreature.SendChatMessage(SelectMonsterSay(aiCreature.ID), ChatMsg.CHAT_MSG_MONSTER_SAY, LANGUAGES.LANG_UNIVERSAL, TargetGUID)
                    End If
                End If
            End If
        End Sub

    End Class
#End Region
#Region "WS.Creatures.AI.GuardAI"
    Public Class GuardAI
        Inherits TBaseAI

        Protected aiCreature As CreatureObject = Nothing
        Protected aiTimer As Integer = 0

        Protected Const AI_INTERVAL_MOVE As Integer = 3000
        Protected Const AI_INTERVAL_SLEEP As Integer = 6000
        Protected Const AI_INTERVAL_DEAD As Integer = 60000
        Protected Const PIx2 As Single = 2 * Math.PI

        Public Sub New(ByRef Creature As CreatureObject)
            State = AIState.AI_DO_NOTHING

            aiCreature = Creature
            aiTarget = Nothing
        End Sub
        Public Overrides Sub OnGetHit(ByRef Attacker As BaseUnit, ByVal DamageCaused As Integer)
            If Me.State <> TBaseAI.AIState.AI_DEAD Then
                If aiHateTable.ContainsKey(Attacker) = False Then aiHateTable.Add(Attacker, 0)
                aiHateTable(Attacker) += DamageCaused * Attacker.Spell_ThreatModifier
                Me.State = TBaseAI.AIState.AI_ATTACKING
                Me.DoThink()
            End If
        End Sub
        Public Overrides Sub OnGenerateHate(ByRef Attacker As BaseUnit, ByVal HateValue As Integer)
            If Me.State <> TBaseAI.AIState.AI_DEAD Then
                If aiHateTable.ContainsKey(Attacker) = False Then aiHateTable.Add(Attacker, 0)
                aiHateTable(Attacker) += HateValue * Attacker.Spell_ThreatModifier
                Me.State = TBaseAI.AIState.AI_ATTACKING
            End If
        End Sub
        Public Overrides Sub Reset()

            aiHateTable.Clear()
            aiTarget = Nothing
            aiTimer = 0
            aiCreature.SendTargetUpdate(0)

            'DONE: Return to default
            If State <> AIState.AI_DEAD Then
                Me.State = AIState.AI_WANDERING
            End If
        End Sub
        Protected Sub SelectTarget()
            Try

                Dim max As Integer = -1
                Dim tmpTarget As BaseUnit = Nothing

                'DONE: Select max hate
                For Each Victim As KeyValuePair(Of BaseUnit, Integer) In aiHateTable
                    If Victim.Value > max Then
                        If Not Victim.Key.isDead Then
                            max = Victim.Value
                            tmpTarget = Victim.Key
                        End If
                    End If
                Next

                'DONE: Set the target
                If (Not tmpTarget Is Nothing) AndAlso Not aiTarget Is tmpTarget Then
                    aiTarget = tmpTarget
                    aiCreature.TurnTo(aiTarget.positionX, aiTarget.positionY)
                    aiCreature.SendTargetUpdate(tmpTarget.GUID)

                    Me.State = AIState.AI_ATTACKING
                End If
            Catch
                Reset()
            End Try

            If aiTarget Is Nothing Then Reset()
        End Sub
        Protected Function CheckTarget() As Boolean
            If aiTarget Is Nothing Then
                Reset()
                Return True
            End If

            Return False
        End Function

        Public Overrides Sub DoThink()
            If aiCreature Is Nothing Then Exit Sub 'Fixes a crash
            If aiTimer > 200 Then
                aiTimer -= TAIManager.UPDATE_TIMER
                Exit Sub
            Else
                aiTimer = 0
            End If

            'DONE: Fixes a bug where creatures attack you when they are dead
            If Me.State <> AIState.AI_DEAD AndAlso Me.State <> AIState.AI_RESPAWN AndAlso aiCreature.Life.Current = 0 Then
                Me.State = AIState.AI_DEAD
            End If

            Select Case Me.State
                Case AIState.AI_DEAD
                    If Me.aiHateTable.Count > 0 Then
                        OnLeaveCombat()

                        Dim RespawnTime As Integer = aiCreature.CreatureInfo.RespawnTime
                        If RespawnTime > 0 Then
                            If RespawnTime <= 3 Then
                                If RespawnTime = 1 Then
                                    aiTimer = 30000
                                Else
                                    aiTimer = ((RespawnTime - 1) * 60000)
                                End If
                            Else
                                aiTimer = 180000 '3 minutes till the corpse disappear
                            End If
                        Else
                            aiTimer = 180000 '3 minutes till the corpse disappear
                        End If
                    Else
                        Me.State = AIState.AI_RESPAWN

                        Dim RespawnTime As Integer = aiCreature.CreatureInfo.RespawnTime
                        If RespawnTime > 0 Then
                            If RespawnTime <= 3 Then
                                If RespawnTime = 1 Then
                                    aiTimer = 30000
                                Else
                                    aiTimer = RespawnTime * 60000 'Minutes -> Milliseconds
                                End If
                            Else
                                aiTimer = 180000 '3 minutes till the corpse disappear
                            End If
                            aiCreature.Despawn()
                        Else
                            aiCreature.Destroy()
                        End If
                    End If
                Case AIState.AI_RESPAWN
                    Me.State = AIState.AI_WANDERING
                    aiCreature.Respawn()
                    aiTimer = 10000 'Wait 10 seconds before starting to react
                Case AIState.AI_MOVE_FOR_ATTACK
                    DoMove()
                Case AIState.AI_WANDERING
                    Me.State = AIState.AI_DO_NOTHING
                Case AIState.AI_ATTACKING
                    DoAttack()
                Case AIState.AI_MOVING
                    Me.State = AIState.AI_DO_NOTHING
                Case AIState.AI_DO_NOTHING
                Case Else
                    aiCreature.SendChatMessage("Unknown AI mode!", ChatMsg.CHAT_MSG_MONSTER_SAY, LANGUAGES.LANG_UNIVERSAL)
                    Me.State = TBaseAI.AIState.AI_DO_NOTHING
            End Select
        End Sub

        Protected Sub DoAttack()
            If aiCreature.Spell_Pacifyed Then
                aiTimer = AI_INTERVAL_MOVE
                Exit Sub
            End If

            If aiTarget Is Nothing Then
                Me.SelectTarget()
            End If

            If Me.State <> AIState.AI_ATTACKING Then
                'DONE: Seems like we lost our target
                aiTimer = AI_INTERVAL_SLEEP
            Else
                'DONE: Do real melee attack
                Try
                    If aiTarget.isDead Then
                        aiTarget = Nothing
                        aiHateTable.Remove(aiTarget)
                        SelectTarget()
                    End If
                    If CheckTarget() Then
                        Me.State = TBaseAI.AIState.AI_WANDERING
                        aiCreature.Life.Current = aiCreature.Life.Maximum
                        aiTimer = aiCreature.MoveTo(aiCreature.SpawnX, aiCreature.SpawnY, aiCreature.SpawnZ, True)
                        Exit Sub
                    End If

                    Dim distance As Single = GetDistance(CType(aiCreature, CreatureObject), CType(aiTarget, BaseUnit))

                    'DONE: Very far objects handling
                    If distance > aiCreature.MaxDistance Then
                        aiCreature.SendChatMessage("Arrgh... you won't get away!", ChatMsg.CHAT_MSG_MONSTER_SAY, LANGUAGES.LANG_UNIVERSAL, aiTarget.GUID)
                        aiTarget = Nothing
                        aiHateTable.Remove(aiTarget)
                        SelectTarget()
                        If CheckTarget() Then Exit Sub
                        distance = GetDistance(CType(aiCreature, CreatureObject), CType(aiTarget, BaseUnit))
                    End If

                    'DONE: Far objects handling
                    If distance > (CREATURESDatabase(aiCreature.ID).CombatReach * aiCreature.Size + BaseUnit.CombatReach_Base) Then
                        If aiCreature.Spell_Silenced Or Rnd.NextDouble > 0.1F Then
                            'DONE: Move closer
                            Me.State = AIState.AI_MOVE_FOR_ATTACK
                            Me.DoMove()
                            Exit Sub
                        Else
                            'DONE: Cast spell
                            'TODO: Get spell for every creature
                            'aiTimer = aiCreature.CastSpell(133, aiTarget)
                            Exit Sub
                        End If
                    End If

                    'DONE: Look to aiTarget
                    If Not IsInFrontOf(CType(aiCreature, CreatureObject), CType(aiTarget, BaseUnit)) Then
                        aiCreature.TurnTo(CType(aiTarget, BaseUnit))
                    End If

                    'DONE: Fix aiCreature VS aiCreature
                    Dim damageInfo As DamageInfo = CalculateDamage(CType(aiCreature, CreatureObject), aiTarget, False, False)
                    SendAttackerStateUpdate(CType(aiCreature, CreatureObject), CType(aiTarget, BaseUnit), damageInfo)
                    aiTarget.DealDamage(damageInfo.GetDamage)

                    aiTimer = CREATURESDatabase(aiCreature.ID).BaseAttackTime
                Catch e As Exception
                    Console.WriteLine("DEBUG: Error attacking aiTarget.")
                    Reset()
                End Try
            End If

        End Sub
        Protected Sub DoMove()
            Dim distanceToSpawn As Single = GetDistance(aiCreature.positionX, aiCreature.SpawnX, aiCreature.positionY, aiCreature.SpawnY, aiCreature.positionZ, aiCreature.SpawnZ)

            'DONE: Back to spawn if too far
            If aiCreature.SpawnID > 0 AndAlso distanceToSpawn > aiCreature.MaxDistance * 2 Then
                Me.State = TBaseAI.AIState.AI_DO_NOTHING
                aiCreature.Life.Current = aiCreature.Life.Maximum
                aiTimer = aiCreature.MoveTo(aiCreature.SpawnX, aiCreature.SpawnY, aiCreature.SpawnZ, True)

                'DONE: Remove combat flag from everyone
                For Each Victim As KeyValuePair(Of BaseUnit, Integer) In aiHateTable
                    'TODO: Check if the character is in combat with anyone else
                    If TypeOf Victim.Key Is CharacterObject Then SetPlayerOutOfCombat(CType(Victim.Key, CharacterObject))
                Next
                Exit Sub
            End If

            If aiTarget Is Nothing Then

                'DONE: Do simple random movement
                Dim MoveTries As Byte = 0
TryMoveAgain:
                If MoveTries > 5 Then 'The creature is at a very weird location now
                    Me.State = TBaseAI.AIState.AI_WANDERING
                    aiCreature.Life.Current = aiCreature.Life.Maximum
                    aiTimer = aiCreature.MoveTo(aiCreature.SpawnX, aiCreature.SpawnY, aiCreature.SpawnZ, True)
                    Exit Sub
                End If

                Dim distance As Single = AI_INTERVAL_MOVE / 1000 * aiCreature.CreatureInfo.WalkSpeed
                Dim angle As Single = Rnd.NextDouble * PIx2

                aiCreature.orientation = angle
                Dim selectedX As Single = aiCreature.positionX + Math.Cos(angle) * distance
                Dim selectedY As Single = aiCreature.positionY + Math.Sin(angle) * distance
                Dim selectedZ As Single = GetZCoord(selectedX, selectedY, aiCreature.positionZ, aiCreature.MapID)
                MoveTries += 1
                If Math.Abs(aiCreature.positionZ - selectedZ) > 5 Then GoTo TryMoveAgain 'Prevent most cases of wall climbing

                If aiCreature.CanMoveTo(selectedX, selectedY, selectedZ) Then
                    Me.State = TBaseAI.AIState.AI_WANDERING
                    aiTimer = aiCreature.MoveTo(selectedX, selectedY, selectedZ, False)
                Else
                    aiTimer = AI_INTERVAL_MOVE
                End If

            Else
                'DONE: Do targeted movement to attack target
                Dim distance As Single = AI_INTERVAL_MOVE / 1000 * aiCreature.CreatureInfo.RunSpeed
                Dim distanceToTarget As Single = GetDistance(CType(aiCreature, CreatureObject), CType(aiTarget, BaseUnit))

                If distanceToTarget < distance Then
                    'DONE: Move to target
                    Me.State = TBaseAI.AIState.AI_ATTACKING

                    Dim NearX As Single = aiTarget.positionX
                    If aiTarget.positionX > aiCreature.positionX Then NearX -= aiCreature.Size Else NearX += aiCreature.Size
                    Dim NearY As Single = aiTarget.positionY
                    If aiTarget.positionY > aiCreature.positionY Then NearY -= aiCreature.Size Else NearY += aiCreature.Size
                    Dim NearZ As Single = GetZCoord(NearX, NearY, aiCreature.positionZ, aiCreature.MapID)
                    If NearZ > (aiTarget.positionZ + 2) Or NearZ < (aiTarget.positionZ - 2) Then NearZ = aiTarget.positionZ
                    If aiCreature.CanMoveTo(NearX, NearY, NearZ) Then
                        aiTimer = aiCreature.MoveTo(NearX, NearY, NearZ, True)
                    Else
                        'DONE: Select next target
                        aiHateTable.Remove(aiTarget)
                        SelectTarget()
                        If Not CheckTarget() Then aiTimer = AI_INTERVAL_MOVE
                    End If

                Else

                    'DONE: Move to target by vector
                    Me.State = TBaseAI.AIState.AI_MOVE_FOR_ATTACK

                    Dim angle As Single = Math.Atan2(aiTarget.positionY - aiCreature.positionY, aiTarget.positionX - aiCreature.positionX)
                    Dim selectedX As Single = aiCreature.positionX + Math.Cos(angle) * distance
                    Dim selectedY As Single = aiCreature.positionY + Math.Sin(angle) * distance

                    If aiCreature.CanMoveTo(selectedX, selectedY, GetZCoord(selectedX, selectedY, aiCreature.positionZ, aiCreature.MapID)) Then
                        aiTimer = aiCreature.MoveTo(selectedX, selectedY, GetZCoord(selectedX, selectedY, aiCreature.positionZ, aiCreature.MapID), True)
                    Else
                        'DONE: Select next target
                        aiHateTable.Remove(aiTarget)
                        SelectTarget()
                        If Not CheckTarget() Then aiTimer = AI_INTERVAL_MOVE
                    End If

                End If
            End If

        End Sub

    End Class

#End Region
#Region "WS.Creatures.AI.WayPointAIs"
    Public Class WaypointAI
        ''''Inherits TBaseAI

        ''''Protected aiCreature As CreatureObject = Nothing
        ''''Protected aiTimer As Integer = 0

        ''''Protected Const AI_INTERVAL_MOVE As Integer = 3000
        ''''Protected Const AI_INTERVAL_SLEEP As Integer = 6000
        ''''Protected Const AI_INTERVAL_DEAD As Integer = 60000
        ''''Protected Const PIx2 As Single = 2 * Math.PI

        ''''Protected CurrentWayPoint As Integer = 0

        ''''Public Sub New(ByRef Creature As CreatureObject)
        ''''    State = AIState.AI_WANDERING

        ''''    aiCreature = Creature
        ''''    aiTarget = Nothing
        ''''End Sub
        ''''Public Overrides Sub ChangeTimer(ByVal NewValue As Integer)
        ''''    aiTimer = NewValue
        ''''End Sub
        ''''Public Overrides Function IsMoving() As Boolean
        ''''    If (timeGetTime - aiCreature.LastMove) < aiTimer Then
        ''''        Select Case Me.State
        ''''            Case AIState.AI_MOVE_FOR_ATTACK
        ''''                Return True
        ''''            Case AIState.AI_MOVING
        ''''                Return True
        ''''            Case AIState.AI_WANDERING
        ''''                Return True
        ''''            Case Else
        ''''                Return False
        ''''        End Select
        ''''    Else
        ''''        Return False
        ''''    End If
        ''''End Function
        ''''Public Overrides Sub OnEnterCombat()
        ''''    If aiCreature.Life.Current = 0 Then Exit Sub 'Prevents the creature from doing this below if it's dead already
        ''''    'DONE: Decide it's real position if it hasn't stopped
        ''''    If (timeGetTime - aiCreature.LastMove) < aiCreature.LastMove_Time Then
        ''''        Dim RealDistance As Single = (timeGetTime - aiCreature.LastMove) / 1000 * aiCreature.CreatureInfo.WalkSpeed
        ''''        WORLD_CREATUREs(aiCreature.GUID).positionX = aiCreature.OldX + Math.Cos(aiCreature.orientation) * RealDistance
        ''''        WORLD_CREATUREs(aiCreature.GUID).positionY = aiCreature.OldY + Math.Sin(aiCreature.orientation) * RealDistance
        ''''        WORLD_CREATUREs(aiCreature.GUID).positionZ = GetZCoord(WORLD_CREATUREs(aiCreature.GUID).positionX, WORLD_CREATUREs(aiCreature.GUID).positionY, WORLD_CREATUREs(aiCreature.GUID).positionZ, WORLD_CREATUREs(aiCreature.GUID).MapID)
        ''''    End If

        ''''    Me.State = AIState.AI_ATTACKING
        ''''    DoThink()

        ''''    If MonsterSay.ContainsKey(aiCreature.ID) Then
        ''''        If MonsterSay(aiCreature.ID).EventNo = MonsterSayEvents.MONSTER_SAY_EVENT_ENTER_COMBAT Then
        ''''            Dim Chance As Integer = (MonsterSay(aiCreature.ID).Chance)
        ''''            If Rnd.Next(1, 101) <= Chance Then
        ''''                Dim TargetGUID As ULong
        ''''                If Not aiTarget Is Nothing Then TargetGUID = aiTarget.GUID
        ''''                aiCreature.SendChatMessage(SelectMonsterSay(aiCreature.ID), ChatMsg.CHAT_MSG_MONSTER_SAY, LANGUAGES.LANG_UNIVERSAL, TargetGUID)
        ''''            End If
        ''''        End If
        ''''    End If
        ''''End Sub
        ''''Public Overrides Sub OnLeaveCombat(Optional ByVal Reset As Boolean = True)
        ''''    'DONE: Remove combat flag from everyone
        ''''    For Each Victim As KeyValuePair(Of BaseUnit, Integer) In aiHateTable
        ''''        If TypeOf Victim.Key Is CharacterObject Then
        ''''            CType(Victim.Key, CharacterObject).inCombatWith.Remove(aiCreature.GUID)
        ''''            CType(Victim.Key, CharacterObject).CheckCombat()
        ''''        End If
        ''''    Next

        ''''    aiTarget = Nothing
        ''''    aiHateTable.Clear()
        ''''    ''''aiHateTableRemove.Clear()
        ''''    aiCreature.SendTargetUpdate(0)

        ''''    If Reset Then
        ''''        'DONE: Reset values and move to spawn
        ''''        Me.State = TBaseAI.AIState.AI_WANDERING
        ''''        WORLD_CREATUREs(aiCreature.GUID).Life.Current = aiCreature.Life.Maximum
        ''''        aiTimer = aiCreature.MoveTo(aiCreature.SpawnX, aiCreature.SpawnY, aiCreature.SpawnZ, True)
        ''''    End If

        ''''End Sub
        ''''Public Overrides Sub OnGetHit(ByRef Attacker As BaseUnit, ByVal DamageCaused As Integer)
        ''''    If Me.State <> TBaseAI.AIState.AI_DEAD AndAlso Me.State <> TBaseAI.AIState.AI_RESPAWN Then
        ''''        If Me.InCombat = False Then
        ''''            aiHateTable.Add(Attacker, DamageCaused)
        ''''            OnEnterCombat()
        ''''            Me.State = TBaseAI.AIState.AI_ATTACKING
        ''''            Me.DoThink()
        ''''        End If

        ''''        If aiHateTable.ContainsKey(Attacker) = False Then
        ''''            aiHateTable.Add(Attacker, DamageCaused)
        ''''            If TypeOf Attacker Is CharacterObject AndAlso CType(Attacker, CharacterObject).inCombatWith.Contains(aiCreature.GUID) = False Then
        ''''                CType(Attacker, CharacterObject).inCombatWith.Add(aiCreature.GUID)
        ''''                CType(Attacker, CharacterObject).CheckCombat()
        ''''            End If
        ''''        Else
        ''''            aiHateTable(Attacker) += DamageCaused * Attacker.Spell_ThreatModifier
        ''''        End If
        ''''    End If
        ''''End Sub
        ''''Public Overrides Sub OnGenerateHate(ByRef Attacker As BaseUnit, ByVal HateValue As Integer)
        ''''    If Me.State <> TBaseAI.AIState.AI_DEAD AndAlso Me.State <> TBaseAI.AIState.AI_RESPAWN Then
        ''''        If Me.InCombat = False Then
        ''''            aiHateTable.Add(Attacker, HateValue)
        ''''            OnEnterCombat()
        ''''            Me.State = TBaseAI.AIState.AI_ATTACKING
        ''''            DoThink()
        ''''        End If
        ''''        If aiHateTable.ContainsKey(Attacker) = False Then
        ''''            aiHateTable.Add(Attacker, HateValue)
        ''''            If TypeOf Attacker Is CharacterObject AndAlso CType(Attacker, CharacterObject).inCombatWith.Contains(aiCreature.GUID) = False Then
        ''''                CType(Attacker, CharacterObject).inCombatWith.Add(aiCreature.GUID)
        ''''                CType(Attacker, CharacterObject).CheckCombat()
        ''''            End If
        ''''        Else
        ''''            aiHateTable(Attacker) += HateValue * Attacker.Spell_ThreatModifier
        ''''        End If
        ''''    End If
        ''''End Sub
        ''''Public Overrides Sub Reset()
        ''''    OnLeaveCombat()
        ''''    aiTimer = 0

        ''''    'DONE: Return to default
        ''''    If State <> AIState.AI_DEAD AndAlso State <> AIState.AI_RESPAWN Then
        ''''        Me.State = AIState.AI_WANDERING
        ''''    End If
        ''''End Sub
        ''''Protected Sub SelectTarget()
        ''''    Try
        ''''        Dim max As Integer = -1
        ''''        Dim tmpTarget As BaseUnit = Nothing

        ''''        'DONE: Select max hate
        ''''        For Each Victim As KeyValuePair(Of BaseUnit, Integer) In aiHateTable
        ''''            If Victim.Value > max Then
        ''''                If Victim.Key.isDead Then
        ''''                    ''''aiHateTableRemove.Add(Victim.Key, Victim.Value)
        ''''                    aiHateTable.Remove(Victim.Key)
        ''''                    If TypeOf Victim.Key Is CharacterObject Then
        ''''                        CType(Victim.Key, CharacterObject).inCombatWith.Remove(aiCreature.GUID)
        ''''                        CType(Victim.Key, CharacterObject).CheckCombat()
        ''''                    End If
        ''''                Else
        ''''                    max = Victim.Value
        ''''                    tmpTarget = Victim.Key
        ''''                End If
        ''''            End If
        ''''        Next

        ''''        ' Remove from aiHateTable
        ''''        ''''For Each VictimRemove As KeyValuePair(Of BaseUnit, Integer) In aiHateTableRemove
        ''''        ''''    aiHateTable.Remove(VictimRemove.Key)
        ''''        ''''Next

        ''''        'DONE: Set the target
        ''''        If (Not tmpTarget Is Nothing) AndAlso Not aiTarget Is tmpTarget Then
        ''''            aiTarget = tmpTarget
        ''''            aiCreature.TurnTo(aiTarget.positionX, aiTarget.positionY)
        ''''            aiCreature.SendTargetUpdate(tmpTarget.GUID)

        ''''            Me.State = AIState.AI_ATTACKING
        ''''        End If
        ''''    Catch
        ''''        Reset()
        ''''    End Try

        ''''    If aiTarget Is Nothing Then Reset()
        ''''End Sub
        ''''Protected Function CheckTarget() As Boolean
        ''''    If aiTarget Is Nothing Then
        ''''        Reset()
        ''''        Return True
        ''''    End If

        ''''    Return False
        ''''End Function

        ''''Public Overrides Sub DoThink()
        ''''    If aiCreature Is Nothing Then Exit Sub 'Fixes a crash
        ''''    If aiTimer > TAIManager.UPDATE_TIMER Then
        ''''        aiTimer -= TAIManager.UPDATE_TIMER
        ''''        Exit Sub
        ''''    Else
        ''''        aiTimer = 0
        ''''    End If

        ''''    'DONE: Fixes a bug where creatures attack you when they are dead
        ''''    If Me.State <> AIState.AI_DEAD AndAlso Me.State <> AIState.AI_RESPAWN AndAlso aiCreature.Life.Current = 0 Then
        ''''        Me.State = AIState.AI_DEAD
        ''''    End If

        ''''    Select Case Me.State
        ''''        Case AIState.AI_DEAD
        ''''            If Me.aiHateTable.Count > 0 Then
        ''''                OnLeaveCombat(False)

        ''''                Dim RespawnTime As Integer = aiCreature.CreatureInfo.RespawnTime
        ''''                If RespawnTime > 0 Then
        ''''                    If RespawnTime <= 3 Then
        ''''                        If RespawnTime = 1 Then
        ''''                            aiTimer = 30000
        ''''                        Else
        ''''                            aiTimer = ((RespawnTime - 1) * 60000)
        ''''                        End If
        ''''                    Else
        ''''                        aiTimer = 180000 '3 minutes till the corpse disappear
        ''''                    End If
        ''''                Else
        ''''                    aiTimer = 180000 '3 minutes till the corpse disappear
        ''''                End If
        ''''            Else
        ''''                Me.State = AIState.AI_RESPAWN

        ''''                Dim RespawnTime As Integer = aiCreature.CreatureInfo.RespawnTime
        ''''                If RespawnTime > 0 Then
        ''''                    If RespawnTime <= 3 Then
        ''''                        If RespawnTime = 1 Then
        ''''                            aiTimer = 30000
        ''''                        Else
        ''''                            aiTimer = RespawnTime * 60000 'Minutes -> Milliseconds
        ''''                        End If
        ''''                    Else
        ''''                        aiTimer = 180000 '3 minutes till the corpse disappear
        ''''                    End If
        ''''                    ''''''aiCreature.Despawn()
        ''''                Else
        ''''                    ''''''aiCreature.Destroy()
        ''''                End If
        ''''            End If
        ''''        Case AIState.AI_RESPAWN
        ''''            Me.State = AIState.AI_WANDERING
        ''''            ''''''aiCreature.Respawn()
        ''''            aiTimer = 10000 'Wait 10 seconds before starting to react
        ''''        Case AIState.AI_MOVE_FOR_ATTACK
        ''''            DoMove()
        ''''        Case AIState.AI_WANDERING
        ''''            If Rnd.NextDouble > 0.2F Then
        ''''                DoMove()
        ''''            End If
        ''''        Case AIState.AI_ATTACKING
        ''''            DoAttack()
        ''''        Case AIState.AI_MOVING
        ''''            DoMove()
        ''''        Case AIState.AI_DO_NOTHING
        ''''        Case Else
        ''''            aiCreature.SendChatMessage("Unknown AI mode!", ChatMsg.CHAT_MSG_MONSTER_SAY, LANGUAGES.LANG_UNIVERSAL)
        ''''            Me.State = TBaseAI.AIState.AI_DO_NOTHING
        ''''    End Select
        ''''End Sub

        ''''Protected Sub DoAttack()
        ''''    If aiCreature.Spell_Pacifyed Then
        ''''        aiTimer = AI_INTERVAL_MOVE
        ''''        Exit Sub
        ''''    End If

        ''''    'DONE: Change the target to the one with most threat
        ''''    SelectTarget()
        ''''    ''If aiTarget Is Nothing Then
        ''''    ''    Me.SelectTarget()
        ''''    ''End If

        ''''    If Me.State <> AIState.AI_ATTACKING Then
        ''''        'DONE: Seems like we lost our target
        ''''        aiTimer = AI_INTERVAL_SLEEP
        ''''    Else
        ''''        'DONE: Do real melee attack
        ''''        Try
        ''''            If aiTarget.isDead Then
        ''''                aiTarget = Nothing
        ''''                aiHateTable.Remove(aiTarget)
        ''''                SelectTarget()
        ''''            End If
        ''''            If CheckTarget() Then
        ''''                Me.State = TBaseAI.AIState.AI_WANDERING
        ''''                WORLD_CREATUREs(aiCreature.GUID).Life.Current = aiCreature.Life.Maximum
        ''''                aiTimer = aiCreature.MoveTo(aiCreature.SpawnX, aiCreature.SpawnY, aiCreature.SpawnZ, True)
        ''''                Exit Sub
        ''''            End If

        ''''            Dim distance As Single = GetDistance(CType(aiCreature, CreatureObject), CType(aiTarget, BaseUnit))

        ''''            'DONE: Very far objects handling
        ''''            If distance > aiCreature.MaxDistance Then
        ''''                'aiCreature.SendChatMessage("Arrgh... you won't get away!", ChatMsg.CHAT_MSG_MONSTER_SAY, LANGUAGES.LANG_UNIVERSAL, aiTarget.GUID)
        ''''                aiTarget = Nothing
        ''''                aiHateTable.Remove(aiTarget)
        ''''                SelectTarget()
        ''''                If CheckTarget() Then
        ''''                    OnLeaveCombat()
        ''''                    Exit Sub
        ''''                End If
        ''''                distance = GetDistance(CType(aiCreature, CreatureObject), CType(aiTarget, BaseUnit))
        ''''            End If

        ''''            'DONE: Far objects handling
        ''''            If distance > (CREATURESDatabase(aiCreature.ID).CombatReach * aiCreature.Size + BaseUnit.CombatReach_Base) Then
        ''''                If aiCreature.Spell_Silenced Or Rnd.NextDouble > 0.1F Then
        ''''                    'DONE: Move closer
        ''''                    Me.State = AIState.AI_MOVE_FOR_ATTACK
        ''''                    Me.DoMove()
        ''''                    Exit Sub
        ''''                Else
        ''''                    'DONE: Cast spell
        ''''                    'TODO: Get spell for every creature
        ''''                    'aiTimer = aiCreature.CastSpell(133, aiTarget)
        ''''                    Exit Sub
        ''''                End If
        ''''            End If

        ''''            'DONE: Look to aiTarget
        ''''            If Not IsInFrontOf(CType(aiCreature, CreatureObject), CType(aiTarget, BaseUnit)) Then
        ''''                aiCreature.TurnTo(CType(aiTarget, BaseUnit))
        ''''            End If

        ''''            'DONE: Fix aiCreature VS aiCreature
        ''''            Dim damageInfo As DamageInfo = CalculateDamage(CType(aiCreature, CreatureObject), aiTarget, False, False)
        ''''            SendAttackerStateUpdate(CType(aiCreature, CreatureObject), CType(aiTarget, BaseUnit), damageInfo)
        ''''            aiTarget.DealDamage(damageInfo.GetDamage)

        ''''            aiTimer = CREATURESDatabase(aiCreature.ID).BaseAttackTime
        ''''        Catch e As Exception
        ''''            Console.WriteLine("DEBUG: Error attacking aiTarget.")
        ''''            Reset()
        ''''        End Try
        ''''    End If

        ''''End Sub

        ''''Protected Sub DoMove()
        ''''    Dim NextWaypoint As Integer
        ''''    Dim WaypointCount As Integer
        ''''    Dim MySQLQuery As New DataTable
        ''''    Dim WaypointSQLQuery As New DataTable
        ''''    Dim PositionX As Single
        ''''    Dim PositionY As Single
        ''''    Dim PositionZ As Single
        ''''    Dim WaitTime As Integer

        ''''    Database.Query(String.Format("SELECT Count(*) FROM creature_movement WHERE spawnid='{0}'", aiCreature.SpawnID), MySQLQuery)
        ''''    WaypointCount = MySQLQuery.Rows(0).Item(0)

        ''''    If aiTarget Is Nothing Then

        ''''        NextWaypoint = aiCreature.CurrentWaypoint + 1
        ''''        If NextWaypoint > WaypointCount Then
        ''''            aiCreature.CurrentWaypoint = 1
        ''''        Else
        ''''            aiCreature.CurrentWaypoint = NextWaypoint
        ''''        End If

        ''''        Database.Query(String.Format("SELECT * FROM creature_movement WHERE spawnid='{0}' AND waypointid='{1}'", aiCreature.SpawnID, aiCreature.CurrentWaypoint), WaypointSQLQuery)

        ''''        PositionX = WaypointSQLQuery.Rows(0).Item("position_x")
        ''''        PositionY = WaypointSQLQuery.Rows(0).Item("position_y")
        ''''        PositionZ = WaypointSQLQuery.Rows(0).Item("position_z")
        ''''        WaitTime = WaypointSQLQuery.Rows(0).Item("waittime")

        ''''        If aiCreature.CanMoveTo(PositionX, PositionY, PositionZ) Then
        ''''            Me.State = TBaseAI.AIState.AI_WANDERING
        ''''            aiTimer = aiCreature.MoveTo(PositionX, PositionY, PositionZ, False)
        ''''        Else
        ''''            If WaitTime > 0 Then
        ''''                aiTimer = WaitTime
        ''''            Else
        ''''                aiTimer = AI_INTERVAL_MOVE
        ''''            End If
        ''''        End If
        ''''    Else
        ''''        'DONE: Do targeted movement to attack target
        ''''        Dim distance As Single = AI_INTERVAL_MOVE / 1000 * aiCreature.CreatureInfo.RunSpeed
        ''''        Dim distanceToTarget As Single = GetDistance(CType(aiCreature, CreatureObject), CType(aiTarget, BaseUnit))

        ''''        If distanceToTarget < distance Then
        ''''            'DONE: Move to target
        ''''            Me.State = TBaseAI.AIState.AI_ATTACKING

        ''''            Dim NearX As Single = aiTarget.positionX
        ''''            If aiTarget.positionX > aiCreature.positionX Then NearX -= aiCreature.Size Else NearX += aiCreature.Size
        ''''            Dim NearY As Single = aiTarget.positionY
        ''''            If aiTarget.positionY > aiCreature.positionY Then NearY -= aiCreature.Size Else NearY += aiCreature.Size
        ''''            Dim NearZ As Single = GetZCoord(NearX, NearY, aiCreature.positionZ, aiCreature.MapID)
        ''''            If NearZ > (aiTarget.positionZ + 2) Or NearZ < (aiTarget.positionZ - 2) Then NearZ = aiTarget.positionZ
        ''''            If aiCreature.CanMoveTo(NearX, NearY, NearZ) Then
        ''''                aiTimer = aiCreature.MoveTo(NearX, NearY, NearZ, True)
        ''''            Else
        ''''                'DONE: Select next target
        ''''                aiHateTable.Remove(aiTarget)
        ''''                Me.SelectTarget()
        ''''                If Not CheckTarget() Then aiTimer = AI_INTERVAL_MOVE
        ''''            End If

        ''''        Else

        ''''            'DONE: Move to target by vector
        ''''            Me.State = TBaseAI.AIState.AI_MOVE_FOR_ATTACK

        ''''            Dim angle As Single = Math.Atan2(aiTarget.positionY - aiCreature.positionY, aiTarget.positionX - aiCreature.positionX)
        ''''            Dim selectedX As Single = aiCreature.positionX + Math.Cos(angle) * distance
        ''''            Dim selectedY As Single = aiCreature.positionY + Math.Sin(angle) * distance

        ''''            If aiCreature.CanMoveTo(selectedX, selectedY, GetZCoord(selectedX, selectedY, aiCreature.positionZ, aiCreature.MapID)) Then
        ''''                aiTimer = aiCreature.MoveTo(selectedX, selectedY, GetZCoord(selectedX, selectedY, aiCreature.positionZ, aiCreature.MapID), True)
        ''''            Else
        ''''                'DONE: Select next target
        ''''                aiHateTable.Remove(aiTarget)
        ''''                Me.SelectTarget()
        ''''                If Not Me.CheckTarget() Then aiTimer = AI_INTERVAL_MOVE
        ''''            End If

        ''''        End If
        ''''    End If
        ''''End Sub
    End Class
#End Region
#Region "WS.Creatures.AI.BossAIs"

#End Region

End Module
