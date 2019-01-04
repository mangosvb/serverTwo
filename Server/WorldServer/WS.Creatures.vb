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
Imports System.Runtime.CompilerServices
Imports mangosVB.Common.BaseWriter
Imports mangosVB.Common

Public Module WS_Creatures

#Region "WS.Creatures.Constants"

    Public Const SKILL_DETECTION_PER_LEVEL As Integer = 5

#End Region
#Region "WS.Creatures.TypeDef"
    Public Delegate Sub CastEvent(ByRef target As Object, ByRef caster As Object)

    'WARNING: Use only with CREATUREsDatabase()
    Public Class CreatureInfo
        Implements IDisposable
        Public Sub New(ByVal CreatureID As Integer)
            Me.New()

            'DONE: Load Item Data from MySQL
            Dim MySQLQuery As New DataTable
            Database.Query(String.Format("SELECT * FROM creatures WHERE creature_id = {0};", CreatureID), MySQLQuery)

            If MySQLQuery.Rows.Count = 0 Then
                Log.WriteLine(LogType.FAILED, "CreatureID {0} not found in SQL database.", CreatureID)
                found_ = False
                'Throw New ApplicationException(String.Format("CreatureID {0} not found in SQL database.", CreatureID))
                Exit Sub
            End If
            found_ = True

            Model = MySQLQuery.Rows(0).Item("creature_model")
            FemaleModel = MySQLQuery.Rows(0).Item("creature_femalemodel")
            Name = MySQLQuery.Rows(0).Item("creature_name")
            Guild = MySQLQuery.Rows(0).Item("creature_guild")
            Info_Str = MySQLQuery.Rows(0).Item("info_str")
            Size = MySQLQuery.Rows(0).Item("creature_size")

            Life = MySQLQuery.Rows(0).Item("creature_life")
            Mana = MySQLQuery.Rows(0).Item("creature_mana")
            ManaType = MySQLQuery.Rows(0).Item("creature_manaType")
            Faction = MySQLQuery.Rows(0).Item("creature_faction")
            Elite = MySQLQuery.Rows(0).Item("creature_elite")
            Damage.Maximum = MySQLQuery.Rows(0).Item("creature_maxDamage")
            RangedDamage.Maximum = MySQLQuery.Rows(0).Item("creature_maxRangedDamage")
            Damage.Minimum = MySQLQuery.Rows(0).Item("creature_minDamage")
            RangedDamage.Minimum = MySQLQuery.Rows(0).Item("creature_minRangedDamage")

            'AtackPower = MySQLQuery.Rows(0).Item("creature_attackPower")
            'RangedAtackPower = MySQLQuery.Rows(0).Item("creature_rangedAttackPower")

            WalkSpeed = MySQLQuery.Rows(0).Item("creature_walkSpeed")
            RunSpeed = MySQLQuery.Rows(0).Item("creature_runSpeed")
            BaseAttackTime = MySQLQuery.Rows(0).Item("creature_baseAttackSpeed")
            BaseRangedAttackTime = MySQLQuery.Rows(0).Item("creature_baseRangedAttackSpeed")

            RespawnTime = MySQLQuery.Rows(0).Item("creature_respawnTime")

            CombatReach = MySQLQuery.Rows(0).Item("creature_combatReach")
            BoundingRadius = MySQLQuery.Rows(0).Item("creature_bondingRadius")
            cNpcFlags = MySQLQuery.Rows(0).Item("creature_npcFlags")
            cFlags = MySQLQuery.Rows(0).Item("creature_flags")
            CreatureType = MySQLQuery.Rows(0).Item("creature_type")
            CreatureFamily = MySQLQuery.Rows(0).Item("creature_family")
            LevelMin = MySQLQuery.Rows(0).Item("creature_minLevel")
            LevelMax = MySQLQuery.Rows(0).Item("creature_maxLevel")

            AIScriptSource = MySQLQuery.Rows(0).Item("creature_aiScript")

            Id = CreatureID

            If Dir(System.AppDomain.CurrentDomain.BaseDirectory() & "scripts\creatures\" & Name.Replace("""", "'").Replace("<", "").Replace(">", "").Replace("*", "").Replace("/", "").Replace("\", "").Replace(":", "").Replace("|", "").Replace("?", "") & ".vb") <> "" Then
                Dim tmpScript As New ScriptedObject("scripts\creatures\" & Replace(Name, """", "'") & ".vb", "", True)
                TalkScript = tmpScript.Invoke("TalkScript")
                tmpScript.Dispose()
            Else
                ''If Info_Str = "Directions" Then
                ''    Dim tmpScript As New ScriptedObject("scripts\creatures\Directions.vb", "", True)
                ''    TalkScript = tmpScript.Invoke("TalkScript")
                ''    tmpScript.Dispose()
                ''End If
                If cNpcFlags = 0 Then
                    TalkScript = Nothing
                ElseIf cNpcFlags = NPCFlags.UNIT_NPC_FLAG_GOSSIP Then
                    TalkScript = New TDefaultTalk
                Else
                    TalkScript = New TDefaultTalk
                End If
            End If
            CREATURESDatabase.Add(Id, Me)
        End Sub

        Public Sub New()
            Damage.Minimum = (0.8F * BaseAttackTime / 1000.0F) * (LevelMin * 10.0F)
            Damage.Maximum = (1.2F * BaseAttackTime / 1000.0F) * (LevelMax * 10.0F)
        End Sub

        Public Sub Dispose() Implements System.IDisposable.Dispose
            CREATURESDatabase.Remove(Id)
        End Sub
        Public Sub Save()
            If found_ = False Then
                Database.Update("INSERT INTO creatures (creature_id)  VALUES (" & Id & ");")
            End If

            Dim tmp As String = "UPDATE creatures SET"

            tmp = tmp & " creature_model=""" & Model & """"
            tmp = tmp & ", creature_name='" & Name & "'"
            tmp = tmp & ", creature_guild=""" & Guild & """"
            tmp = tmp & ", creature_size=""" & Trim(Str(Size)) & """"
            tmp = tmp & ", creature_life=""" & Life & """"
            tmp = tmp & ", creature_mana=""" & Mana & """"
            tmp = tmp & ", creature_manaType=""" & ManaType & """"
            tmp = tmp & ", creature_elite=""" & Elite & """"
            tmp = tmp & ", creature_faction=""" & Faction & """"
            tmp = tmp & ", creature_family=""" & CreatureFamily & """"
            tmp = tmp & ", creature_type=""" & CreatureType & """"
            tmp = tmp & ", creature_minDamage=""" & Damage.Minimum & """"
            tmp = tmp & ", creature_maxDamage=""" & Damage.Maximum & """"
            tmp = tmp & ", creature_minRangedDamage=""" & RangedDamage.Minimum & """"
            tmp = tmp & ", creature_maxRangedDamage=""" & RangedDamage.Maximum & """"
            tmp = tmp & ", creature_attackPower=""" & AtackPower & """"
            tmp = tmp & ", creature_rangedAttackPower=""" & RangedAtackPower & """"
            tmp = tmp & ", creature_walkSpeed=""" & Trim(Str(WalkSpeed)) & """"
            tmp = tmp & ", creature_runSpeed=""" & Trim(Str(RunSpeed)) & """"
            tmp = tmp & ", creature_baseAttackSpeed=""" & BaseAttackTime & """"
            tmp = tmp & ", creature_baseRangedAttackSpeed=""" & BaseRangedAttackTime & """"
            tmp = tmp & ", creature_combatReach=""" & Trim(Str(CombatReach)) & """"
            tmp = tmp & ", creature_bondingRadius=""" & Trim(Str(BoundingRadius)) & """"
            tmp = tmp & ", creature_npcFlags=""" & cNpcFlags & """"
            tmp = tmp & ", creature_flags=""" & cFlags & """"
            tmp = tmp & ", creature_minLevel=""" & LevelMin & """"
            tmp = tmp & ", creature_maxLevel=""" & LevelMax & """"
            tmp = tmp & ", creature_aiScript=""" & AIScriptSource & """"

            tmp = tmp & ", creature_armor=""" & Resistances(0) & """"
            tmp = tmp & ", creature_resHoly=""" & Resistances(1) & """"
            tmp = tmp & ", creature_resFire=""" & Resistances(2) & """"
            tmp = tmp & ", creature_resNature=""" & Resistances(3) & """"
            tmp = tmp & ", creature_resFrost=""" & Resistances(4) & """"
            tmp = tmp & ", creature_resShadow=""" & Resistances(5) & """"
            tmp = tmp & ", creature_resArcane=""" & Resistances(6) & """"

            tmp = tmp + String.Format(" WHERE creature_id = ""{0}"";", Id)
            Database.Update(tmp)
        End Sub
        Private found_ As Boolean = False

        Public Id As Integer = 0
        Public Name As String = "MISSING_CREATURE_INFO"
        Public Guild As String = ""
        Public Info_Str As String = ""
        Public Size As Single = 1
        Public Model As Integer = 262
        Public FemaleModel As Integer = 0
        Public Life As Integer = 1
        Public Mana As Integer = 1
        Public ManaType As Byte = 0
        Public Faction As Short = FactionTemplates.None
        Public CreatureType As Byte = UNIT_TYPE.NOUNITTYPE
        Public CreatureFamily As Byte = CREATURE_FAMILY.NONE
        Public Elite As Byte = CREATURE_ELITE.NORMAL
        Public HonorRank As Byte = 0
        Public Damage As New TDamage
        Public RangedDamage As New TDamage
        Public AtackPower As Integer = 0
        Public RangedAtackPower As Integer = 0
        Public Resistances() As Byte = {0, 0, 0, 0, 0, 0, 0}

        Public WalkSpeed As Single = UNIT_NORMAL_WALK_SPEED
        Public RunSpeed As Single = UNIT_NORMAL_RUN_SPEED
        Public FlySpeed As Single = UNIT_NORMAL_FLY_SPEED
        Public BaseAttackTime As Short = 2000
        Public BaseRangedAttackTime As Short = 2000

        Public RespawnTime As Integer = 0

        Public CombatReach As Single = 1.0
        Public BoundingRadius As Single = 0
        Public cNpcFlags As Integer
        Public cFlags As Integer
        Public LevelMin As Byte = 1
        Public LevelMax As Byte = 1
        Public Civilian As Byte = 0
        Public Leader As Byte = 0

        Public UnkFloat1 As Single = 1
        Public UnkFloat2 As Single = 2

        'Public EquipedItems() As Integer = {0, 0, 0}
        Public AIScriptSource As String = ""

        Public SpellDataID As Integer = 0
        Public SpellsTable As Integer = 0

        Public Event OnCast As CastEvent
        Public Sub Cast(ByRef target As Object, ByRef caster As Object)
            RaiseEvent OnCast(target, caster)
        End Sub

        Public TalkScript As TBaseTalk = Nothing
    End Class

    'WARNING: Use only with WORLD_CREATUREs()
    Public Class CreatureObject
        Inherits BaseUnit
        Implements IDisposable

        Public ReadOnly Property CreatureInfo() As CreatureInfo
            Get
                Return CREATURESDatabase(ID)
            End Get
        End Property

        Public ID As Integer = 0
        Public aiScript As TBaseAI = Nothing
        Public SpawnX As Single = 0
        Public SpawnY As Single = 0
        Public SpawnZ As Single = 0
        Public SpawnO As Single = 0
        Public Faction As Short = 0
        Public MoveType As Byte = 0
        Public cStandState As Byte = 0
        Public ExpireTimer As Timer = Nothing
        Public CurrentWaypoint As Integer = 0

        Public ReadOnly Property Name() As String
            Get
                Return CType(CREATURESDatabase(ID), CreatureInfo).Name
            End Get
        End Property
        Public ReadOnly Property MaxDistance() As Single
            Get
                Return CType(CREATURESDatabase(ID), CreatureInfo).BoundingRadius * 50
            End Get
        End Property

        Public ReadOnly Property isAbleToWalkOnWater() As Boolean
            Get
                'TODO: Fix family filter
                Select Case CType(CREATURESDatabase(ID), CreatureInfo).CreatureFamily
                    Case 3, 10, 11, 12, 20, 21, 27
                        Return False
                    Case Else
                        Return True
                End Select
            End Get
        End Property
        Public ReadOnly Property isAbleToWalkOnGround() As Boolean
            Get
                'TODO: Fix family filter
                Select Case CType(CREATURESDatabase(ID), CreatureInfo).CreatureFamily
                    Case 255
                        Return False
                    Case Else
                        Return True
                End Select
            End Get
        End Property
        Public ReadOnly Property isAbleToFly() As Boolean
            Get
                'TODO: Check if the creature can fly.
                Return False
            End Get
        End Property
        Public ReadOnly Property isGuard() As Boolean
            Get
                Select Case ID
                    Case 68, 197, 240, 466, 727, 853, 1423, 1496, 1642, 1652, 1736, 1738, 1741, 1743, 1744, 1745, 1746, 1756, 1965, 2041, 2714, 2721, 3083, 3084, 3210, 3211, 3212, 3213, 3214, 3215, 3220, 3221, 3222, 3223, 3224, 3296, 3297, 3469, 3502, 3571, 4262, 4624, 5595, 5624, 5952, 5953, 5597, 7980, 8017, 9460, 10676, 10682, 10881, 11190, 12160, 12996, 13839, 14304, 14377, 15371, 15442, 15616, 15940, 16096, 16221, 16222, 16733, 16864, 16921, 18038, 18103, 18948, 18949, 18971, 18986, 19541, 20484, 20485, 20672, 20674, 21976, 22494, 23636, 23721, 25992
                        Return True
                End Select
            End Get
        End Property
        Public ReadOnly Property ExpansionLevel() As ExpansionLevel
            Get
                'TODO: Fill in instance ID's here as well
                Select Case MapID
                    Case 0, 1, 609
                        Return Common.ExpansionLevel.NORMAL
                    Case 530
                        Return Common.ExpansionLevel.EXPANSION_1
                    Case 571
                        Return Common.ExpansionLevel.EXPANSION_2
                    Case Else
                        Return Common.ExpansionLevel.NORMAL
                End Select
            End Get
        End Property
        Public Overrides ReadOnly Property isDead() As Boolean
            Get
                Return aiScript.State = TBaseAI.AIState.AI_DEAD
            End Get
        End Property

        Public ReadOnly Property isWaypoint() As Boolean
            Get
                Dim MySQLQuery As New DataTable
                Database.Query(String.Format("SELECT Count(*) FROM creature_movement WHERE spawnid='{0}'", Me.SpawnID), MySQLQuery)
                If MySQLQuery.Rows(0).Item(0) > 0 Then
                    Return True
                Else
                    Return False
                End If
            End Get
        End Property

        Public Function AggroRange(ByVal c As CharacterObject) As Single
            Dim LevelDiff As Short = CShort(Level) - CShort(c.Level)
            Dim Range As Single = 20 + LevelDiff
            If Range < 5 Then Range = 5
            If Range > 45 Then Range = 45
            Return Range
        End Function

        Public Sub SendTargetUpdate(ByVal TargetGUID As ULong)
            Dim packet As New UpdatePacketClass
            Dim tmpUpdate As New UpdateClass(EUnitFields.UNIT_END)
            tmpUpdate.SetUpdateFlag(EUnitFields.UNIT_FIELD_TARGET, TargetGUID)
            tmpUpdate.AddToPacket(CType(packet, UpdatePacketClass), ObjectUpdateType.UPDATETYPE_VALUES, Me)
            tmpUpdate.Dispose()

            SendToNearPlayers(CType(packet, UpdatePacketClass))
            packet.Dispose()
        End Sub
        Public Sub FillAllUpdateFlags(ByRef Update As UpdateClass)
            Update.SetUpdateFlag(EObjectFields.OBJECT_FIELD_GUID, GUID)
            Update.SetUpdateFlag(EObjectFields.OBJECT_FIELD_SCALE_X, Size)
            Update.SetUpdateFlag(EObjectFields.OBJECT_FIELD_TYPE, CType(ObjectType.TYPE_OBJECT + ObjectType.TYPE_UNIT, Integer))
            Update.SetUpdateFlag(EObjectFields.OBJECT_FIELD_ENTRY, CType(ID, Integer))

            If (Not aiScript Is Nothing) AndAlso (Not aiScript.aiTarget Is Nothing) Then
                Update.SetUpdateFlag(EUnitFields.UNIT_FIELD_TARGET, aiScript.aiTarget.GUID)
            End If

            Update.SetUpdateFlag(EUnitFields.UNIT_FIELD_SUMMONEDBY, SummonedBy)
            Update.SetUpdateFlag(EUnitFields.UNIT_FIELD_CREATEDBY, CreatedBy)
            Update.SetUpdateFlag(EUnitFields.UNIT_CREATED_BY_SPELL, CreatedBySpell)

            Update.SetUpdateFlag(EUnitFields.UNIT_FIELD_DISPLAYID, Me.Model)
            Update.SetUpdateFlag(EUnitFields.UNIT_FIELD_NATIVEDISPLAYID, CREATURESDatabase(ID).Model)
            Update.SetUpdateFlag(EUnitFields.UNIT_FIELD_MOUNTDISPLAYID, Mount)

            Update.SetUpdateFlag(EUnitFields.UNIT_FIELD_BYTES_0, CType(CType(CREATURESDatabase(ID).ManaType, Integer) << 24, Integer))
            Update.SetUpdateFlag(EUnitFields.UNIT_FIELD_BYTES_1, cBytes1)
            Update.SetUpdateFlag(EUnitFields.UNIT_FIELD_BYTES_2, cBytes2)

            Update.SetUpdateFlag(EUnitFields.UNIT_NPC_EMOTESTATE, cEmoteState)

            Update.SetUpdateFlag(EUnitFields.UNIT_FIELD_HEALTH, Life.Current)
            Update.SetUpdateFlag(EUnitFields.UNIT_FIELD_POWER1 + CREATURESDatabase(ID).ManaType, Mana.Current)
            Update.SetUpdateFlag(EUnitFields.UNIT_FIELD_MAXHEALTH, Life.Maximum)
            Update.SetUpdateFlag(EUnitFields.UNIT_FIELD_MAXPOWER1 + CREATURESDatabase(ID).ManaType, Mana.Maximum)

            Update.SetUpdateFlag(EUnitFields.UNIT_FIELD_LEVEL, Level)
            Update.SetUpdateFlag(EUnitFields.UNIT_FIELD_FACTIONTEMPLATE, CType(Faction, Integer))
            Update.SetUpdateFlag(EUnitFields.UNIT_NPC_FLAGS, CREATURESDatabase(ID).cNpcFlags)

            Update.SetUpdateFlag(EUnitFields.UNIT_FIELD_FLAGS, cUnitFlags)

            Update.SetUpdateFlag(EUnitFields.UNIT_DYNAMIC_FLAGS, cDynamicFlags)

            'Update.SetUpdateFlag(EUnitFields.UNIT_FIELD_RESISTANCES + DamageTypes.DMG_PHYSICAL, CREATURESDatabase(ID).Resistances(DamageTypes.DMG_PHYSICAL))
            'Update.SetUpdateFlag(EUnitFields.UNIT_FIELD_RESISTANCES + DamageTypes.DMG_HOLY, CREATURESDatabase(ID).Resistances(DamageTypes.DMG_HOLY))
            'Update.SetUpdateFlag(EUnitFields.UNIT_FIELD_RESISTANCES + DamageTypes.DMG_FIRE, CREATURESDatabase(ID).Resistances(DamageTypes.DMG_FIRE))
            'Update.SetUpdateFlag(EUnitFields.UNIT_FIELD_RESISTANCES + DamageTypes.DMG_NATURE, CREATURESDatabase(ID).Resistances(DamageTypes.DMG_NATURE))
            'Update.SetUpdateFlag(EUnitFields.UNIT_FIELD_RESISTANCES + DamageTypes.DMG_FROST, CREATURESDatabase(ID).Resistances(DamageTypes.DMG_FROST))
            'Update.SetUpdateFlag(EUnitFields.UNIT_FIELD_RESISTANCES + DamageTypes.DMG_SHADOW, CREATURESDatabase(ID).Resistances(DamageTypes.DMG_SHADOW))
            'Update.SetUpdateFlag(EUnitFields.UNIT_FIELD_RESISTANCES + DamageTypes.DMG_ARCANE, CREATURESDatabase(ID).Resistances(DamageTypes.DMG_ARCANE))

            Update.SetUpdateFlag(EUnitFields.UNIT_VIRTUAL_ITEM_SLOT_ID, EquipedItems(0))
            'Update.SetUpdateFlag(EUnitFields.UNIT_VIRTUAL_ITEM_INFO, 0)
            'Update.SetUpdateFlag(EUnitFields.UNIT_VIRTUAL_ITEM_INFO + 1, 0)

            Update.SetUpdateFlag(EUnitFields.UNIT_VIRTUAL_ITEM_SLOT_ID_1 + 1, EquipedItems(1))
            'Update.SetUpdateFlag(EUnitFields.UNIT_VIRTUAL_ITEM_INFO + 2, 0)
            'Update.SetUpdateFlag(EUnitFields.UNIT_VIRTUAL_ITEM_INFO + 2 + 1, 0)

            Update.SetUpdateFlag(EUnitFields.UNIT_VIRTUAL_ITEM_SLOT_ID_2 + 2, EquipedItems(2))
            'Update.SetUpdateFlag(EUnitFields.UNIT_VIRTUAL_ITEM_INFO + 4, 0)
            'Update.SetUpdateFlag(EUnitFields.UNIT_VIRTUAL_ITEM_INFO + 4 + 1, 0)

            'Update.SetUpdateFlag(EUnitFields.UNIT_FIELD_BASEATTACKTIME, CREATURESDatabase(ID).BaseAttackTime)
            'Update.SetUpdateFlag(EUnitFields.UNIT_FIELD_OFFHANDATTACKTIME, CREATURESDatabase(ID).BaseAttackTime)
            'Update.SetUpdateFlag(EUnitFields.UNIT_FIELD_RANGEDATTACKTIME, CREATURESDatabase(ID).BaseRangedAttackTime)
            'Update.SetUpdateFlag(EUnitFields.UNIT_FIELD_ATTACK_POWER, CREATURESDatabase(ID).AtackPower)
            'Update.SetUpdateFlag(EUnitFields.UNIT_FIELD_RANGED_ATTACK_POWER, CREATURESDatabase(ID).RangedAtackPower)

            'Update.SetUpdateFlag(EUnitFields.UNIT_FIELD_BOUNDINGRADIUS, CType(CREATURESDatabase(ID).BoundingRadius, Single))
            'Update.SetUpdateFlag(EUnitFields.UNIT_FIELD_COMBATREACH, CType(CREATURESDatabase(ID).CombatReach, Single))
            'Update.SetUpdateFlag(EUnitFields.UNIT_FIELD_MINRANGEDDAMAGE, CREATURESDatabase(ID).RangedDamage.Minimum)
            'Update.SetUpdateFlag(EUnitFields.UNIT_FIELD_MAXRANGEDDAMAGE, CREATURESDatabase(ID).RangedDamage.Maximum)
        End Sub

        Public Sub MoveToInstant(ByVal x As Single, ByVal y As Single, ByVal z As Single)
            positionX = x
            positionY = y
            positionZ = z

            If SeenBy.Count > 0 Then
                Dim index As Integer = 0

                Dim packet As New PacketClass(OPCODES.MSG_MOVE_HEARTBEAT)
                packet.AddPackGUID(GUID)
                packet.AddInt32(0) 'Movementflags
                packet.AddInt16(0)
                packet.AddInt32(timeGetTime)
                packet.AddSingle(positionX)
                packet.AddSingle(positionY)
                packet.AddSingle(positionZ)
                packet.AddSingle(orientation)
                packet.AddInt32(0)

                SendToNearPlayers(packet)

                packet.Dispose()
            End If
        End Sub
        Public OldX As Single = 0
        Public OldY As Single = 0
        Public OldZ As Single = 0
        Public LastMove As Integer = 0
        Public LastMove_Time As Integer = 0
        Public Sub GetPosition(ByRef x As Single, ByVal y As Single, ByVal z As Single)
            If aiScript IsNot Nothing AndAlso aiScript.IsMoving AndAlso (timeGetTime - LastMove) < LastMove_Time Then
                Dim distance As Single

                If aiScript.State = TBaseAI.AIState.AI_MOVING OrElse aiScript.State = TBaseAI.AIState.AI_WANDERING Then
                    distance = (timeGetTime - LastMove) / 1000 * CreatureInfo.WalkSpeed
                ElseIf aiScript.State = TBaseAI.AIState.AI_FLYING Then
                    distance = (timeGetTime - LastMove) / 1000 * CreatureInfo.FlySpeed
                Else
                    distance = (timeGetTime - LastMove) / 1000 * CreatureInfo.RunSpeed
                End If

                x = OldX + Math.Cos(orientation) * distance
                y = OldY + Math.Sin(orientation) * distance
                z = GetZCoord(positionX, positionY, MapID)
            Else
                x = positionX
                y = positionY
                z = positionZ
            End If
        End Sub
        Public Function MoveTo(ByVal x As Single, ByVal y As Single, ByVal z As Single, Optional ByVal Running As Boolean = False) As Integer
            Try
                If Me.SeenBy.Count = 0 Then
                    Return 10000
                End If
            Catch
            End Try

            Dim TimeToMove As Integer = 1

            Dim SMSG_MONSTER_MOVE As New PacketClass(OPCODES.SMSG_MONSTER_MOVE)
            SMSG_MONSTER_MOVE.AddPackGUID(GUID)
            SMSG_MONSTER_MOVE.AddSingle(positionX)
            SMSG_MONSTER_MOVE.AddSingle(positionY)
            SMSG_MONSTER_MOVE.AddSingle(positionZ)
            SMSG_MONSTER_MOVE.AddInt32(timeGetTime)         'Sequence/MSTime?

            SMSG_MONSTER_MOVE.AddInt8(0)                    'Type [If type is 1 then the packet ends here]
            If Running Then
                SMSG_MONSTER_MOVE.AddInt32(&H100)           'Flags [0x0 - Walk, 0x100 - Run, 0x200 - Waypoint, 0x300 - Fly]
                TimeToMove = CType(Math.Sqrt((x - positionX) ^ 2 + (y - positionY) ^ 2 + (z - positionZ) ^ 2) / CreatureInfo.RunSpeed * 1000 + 0.5, Integer)
            Else
                SMSG_MONSTER_MOVE.AddInt32(0)
                TimeToMove = CType(Math.Sqrt((x - positionX) ^ 2 + (y - positionY) ^ 2 + (z - positionZ) ^ 2) / CreatureInfo.WalkSpeed * 1000 + 0.5, Integer)
            End If

            orientation = GetOrientation(positionX, x, positionY, y)
            OldX = positionX
            OldY = positionY
            OldZ = positionZ
            LastMove = timeGetTime
            LastMove_Time = TimeToMove
            positionX = x
            positionY = y
            positionZ = z

            SMSG_MONSTER_MOVE.AddInt32(TimeToMove)  'Time
            SMSG_MONSTER_MOVE.AddInt32(1)           'Points Count
            SMSG_MONSTER_MOVE.AddSingle(x)          'First Point X
            SMSG_MONSTER_MOVE.AddSingle(y)          'First Point Y
            SMSG_MONSTER_MOVE.AddSingle(z)          'First Point Z

            'The points after that are in the same format only if flag 0x200 is set, else they are compressed in 1 uint32

            SendToNearPlayers(SMSG_MONSTER_MOVE)
            SMSG_MONSTER_MOVE.Dispose()

            MoveCell()
            Return TimeToMove
        End Function
        Public Function CanMoveTo(ByVal x As Single, ByVal y As Single, ByVal z As Single) As Boolean
            If IsOutsideOfMap(Me) Then Return False

            If z < GetWaterLevel(x, y, MapID) Then
                If Not Me.isAbleToWalkOnWater Then Return False
            Else
                If Not Me.isAbleToWalkOnGround Then Return False
            End If

            Return True
        End Function
        Public Sub TurnTo(ByRef Target As BaseObject)
            TurnTo(Target.positionX, Target.positionY)
        End Sub
        Public Sub TurnTo(ByVal x As Single, ByVal y As Single)
            orientation = GetOrientation(positionX, x, positionY, y)

            If SeenBy.Count > 0 Then
                'Dim packet As New PacketClass(OPCODES.SMSG_UPDATE_OBJECT)
                'packet.AddInt32(2)
                'packet.AddInt8(0)
                'Dim tmpUpdate As New UpdateClass(1)
                'tmpUpdate.AddToPacket(packet, ObjectUpdateType.UPDATETYPE_MOVEMENT, Me, 0)
                'tmpUpdate.Dispose()

                Dim packet As New PacketClass(OPCODES.MSG_MOVE_HEARTBEAT)
                packet.AddPackGUID(GUID)
                packet.AddInt32(0) 'Movementflags
                packet.AddInt16(0)
                packet.AddInt32(timeGetTime)
                packet.AddSingle(positionX)
                packet.AddSingle(positionY)
                packet.AddSingle(positionZ)
                packet.AddSingle(orientation)
                packet.AddInt32(0)

                SendToNearPlayers(packet)
                packet.Dispose()
            End If
        End Sub
        Public Sub TurnTo(ByVal orientation_ As Single)
            orientation = orientation_

            If SeenBy.Count > 0 Then
                'Dim packet As New PacketClass(OPCODES.SMSG_UPDATE_OBJECT)
                'packet.AddInt32(1)
                'packet.AddInt8(0)
                'Dim tmpUpdate As New UpdateClass(1)
                'tmpUpdate.AddToPacket(packet, ObjectUpdateType.UPDATETYPE_MOVEMENT, Me, 0)

                Dim packet As New PacketClass(OPCODES.MSG_MOVE_HEARTBEAT)
                packet.AddPackGUID(GUID)
                packet.AddInt32(0) 'Movementflags
                packet.AddInt16(0)
                packet.AddInt32(timeGetTime)
                packet.AddSingle(positionX)
                packet.AddSingle(positionY)
                packet.AddSingle(positionZ)
                packet.AddSingle(orientation)
                packet.AddInt32(0)

                SendToNearPlayers(packet)
                packet.Dispose()
            End If
        End Sub

        Public Overrides Sub Die(ByRef Attacker As BaseUnit)
            cUnitFlags = cUnitFlags Or UnitFlags.UNIT_FLAG_DEAD
            Life.Current = 0
            Mana.Current = 0

            'DONE: Send the update
            Dim packetForNear As New UpdatePacketClass
            Dim UpdateData As New UpdateClass(EUnitFields.UNIT_END)
            UpdateData.SetUpdateFlag(EUnitFields.UNIT_FIELD_HEALTH, CType(Life.Current, Integer))
            UpdateData.SetUpdateFlag(EUnitFields.UNIT_FIELD_POWER1 + ManaType, CType(Mana.Current, Integer))
            UpdateData.SetUpdateFlag(EUnitFields.UNIT_FIELD_FLAGS, cUnitFlags)
            UpdateData.AddToPacket(CType(packetForNear, UpdatePacketClass), ObjectUpdateType.UPDATETYPE_VALUES, Me)

            SendToNearPlayers(CType(packetForNear, UpdatePacketClass))
            packetForNear.Dispose()
            UpdateData.Dispose()

            'DONE: Creature stops while it's dead and everyone sees it at the same position
            If Not aiScript Is Nothing Then
                GetPosition(positionX, positionY, positionZ)
                If (Not Attacker Is Nothing) Then
                    orientation = GetOrientation(positionX, Attacker.positionX, positionY, Attacker.positionY)
                End If
                MoveToInstant(positionX, positionY, positionZ)

                aiScript.State = TBaseAI.AIState.AI_DEAD
                aiScript.DoThink()
            End If

            'DONE: Remove all spells when the creature die
            For i As Integer = 0 To MAX_AURA_EFFECTs_VISIBLE - 1
                If Not ActiveSpells(i) Is Nothing Then RemoveAura(i, ActiveSpells(i).SpellCaster)
            Next

            If TypeOf Attacker Is CharacterObject Then
                'TODO: Don't give xp for guards, civilians or critters
                If CreatureInfo.CreatureType <> UNIT_TYPE.CRITTER AndAlso isGuard = False AndAlso CreatureInfo.cNpcFlags = 0 Then 'Not a critter, guard or a civilian
                    GiveXP(CType(Attacker, CharacterObject))
                End If

                'DONE: Fire quest event to check for if this monster is required for quest
                OnQuestKill(Attacker, Me)

                LootCorpse(CType(Attacker, CharacterObject))
            End If
        End Sub
        Public Overrides Sub DealDamageMagical(ByRef Damage As Integer, ByVal DamageType As DamageTypes, Optional ByRef Attacker As BaseUnit = Nothing)
            If Life.Current = 0 Then Exit Sub

            Select Case DamageType
                Case DamageTypes.DMG_PHYSICAL
                    Me.DealDamage(Damage, Attacker)
                    Return
                Case Else
                    If CType(CREATURESDatabase(ID), CreatureInfo).CreatureType = UNIT_TYPE.CRITTER Then
                        'DONE: Critters die in one shot
                        Life.Current = 0
                    Else
                        Life.Current -= Damage
                    End If
            End Select

            'DONE: Check for dead
            If Life.Current = 0 Then

                If MonsterSay.ContainsKey(Me.ID) Then
                    If MonsterSay(Me.ID).EventNo = MonsterSayEvents.MONSTER_SAY_EVENT_DIED Then
                        Dim Chance As Integer = (MonsterSay(Me.ID).Chance)
                        If Rnd.Next(1, 101) <= Chance Then
                            Dim TargetGUID As ULong = Attacker.GUID
                            Me.SendChatMessage(SelectMonsterSay(Me.ID), ChatMsg.CHAT_MSG_MONSTER_SAY, LANGUAGES.LANG_UNIVERSAL, TargetGUID)
                        End If
                    End If
                End If

                Me.Die(Attacker)

            Else

                If MonsterSay.ContainsKey(Me.ID) Then
                    If MonsterSay(Me.ID).EventNo = MonsterSayEvents.MONSTER_SAY_EVENT_DAMAGE_TAKEN Then
                        Dim Chance As Integer = (MonsterSay(Me.ID).Chance)
                        If Rnd.Next(1, 101) <= Chance Then
                            Dim TargetGUID As ULong = Attacker.GUID
                            Me.SendChatMessage(SelectMonsterSay(Me.ID), ChatMsg.CHAT_MSG_MONSTER_SAY, LANGUAGES.LANG_UNIVERSAL, TargetGUID)
                        End If
                    End If
                End If

            End If

            'DONE: Do health update
            If SeenBy.Count > 0 Then
                Dim packetForNear As New UpdatePacketClass
                Dim UpdateData As New UpdateClass(EUnitFields.UNIT_END)
                UpdateData.SetUpdateFlag(EUnitFields.UNIT_FIELD_HEALTH, CType(Life.Current, Integer))
                UpdateData.SetUpdateFlag(EUnitFields.UNIT_FIELD_FLAGS, cUnitFlags)
                UpdateData.SetUpdateFlag(EUnitFields.UNIT_DYNAMIC_FLAGS, cDynamicFlags)
                UpdateData.AddToPacket(CType(packetForNear, UpdatePacketClass), ObjectUpdateType.UPDATETYPE_VALUES, Me)

                SendToNearPlayers(CType(packetForNear, UpdatePacketClass))
                packetForNear.Dispose()
                UpdateData.Dispose()
            End If

            If Not Attacker Is Nothing Then aiScript.OnGetHit(Attacker, Damage)
        End Sub
        Public Overrides Sub DealDamage(ByVal Damage As Integer, Optional ByRef Attacker As BaseUnit = Nothing)
            If Life.Current = 0 Then Exit Sub

            If Attacker IsNot Nothing AndAlso aiScript IsNot Nothing Then aiScript.OnGetHit(Attacker, Damage)
            Life.Current -= Damage

            'DONE: Check for dead
            If Life.Current = 0 Then

                If MonsterSay.ContainsKey(Me.ID) Then
                    If MonsterSay(Me.ID).EventNo = MonsterSayEvents.MONSTER_SAY_EVENT_DIED Then
                        Dim Chance As Integer = (MonsterSay(Me.ID).Chance)
                        If Rnd.Next(1, 101) <= Chance Then
                            Dim TargetGUID As ULong = Attacker.GUID
                            Me.SendChatMessage(SelectMonsterSay(Me.ID), ChatMsg.CHAT_MSG_MONSTER_SAY, LANGUAGES.LANG_UNIVERSAL, TargetGUID)
                        End If
                    End If
                End If

                Me.Die(Attacker)

            Else

                If MonsterSay.ContainsKey(Me.ID) Then
                    If MonsterSay(Me.ID).EventNo = MonsterSayEvents.MONSTER_SAY_EVENT_DAMAGE_TAKEN Then
                        Dim Chance As Integer = (MonsterSay(Me.ID).Chance)
                        If Rnd.Next(1, 101) <= Chance Then
                            Dim TargetGUID As ULong = Attacker.GUID
                            Me.SendChatMessage(SelectMonsterSay(Me.ID), ChatMsg.CHAT_MSG_MONSTER_SAY, LANGUAGES.LANG_UNIVERSAL, TargetGUID)
                        End If
                    End If
                End If

            End If

            'DONE: Do health update
            If SeenBy.Count > 0 Then
                Dim packetForNear As New UpdatePacketClass
                Dim UpdateData As New UpdateClass(EUnitFields.UNIT_END)
                UpdateData.SetUpdateFlag(EUnitFields.UNIT_FIELD_HEALTH, CType(Life.Current, Integer))
                UpdateData.SetUpdateFlag(EUnitFields.UNIT_FIELD_POWER1 + ManaType, CType(Mana.Current, Integer))
                UpdateData.SetUpdateFlag(EUnitFields.UNIT_FIELD_FLAGS, cUnitFlags)
                UpdateData.SetUpdateFlag(EUnitFields.UNIT_DYNAMIC_FLAGS, cDynamicFlags)
                UpdateData.AddToPacket(CType(packetForNear, UpdatePacketClass), ObjectUpdateType.UPDATETYPE_VALUES, Me)

                SendToNearPlayers(CType(packetForNear, UpdatePacketClass))
                packetForNear.Dispose()
                UpdateData.Dispose()
            End If
        End Sub
        Public Overrides Sub Heal(ByVal Damage As Integer, Optional ByRef Attacker As BaseUnit = Nothing)
            If Life.Current = 0 Then Exit Sub

            Life.Current += Damage

            'DONE: Do health update
            If SeenBy.Count > 0 Then
                Dim packetForNear As New UpdatePacketClass
                Dim UpdateData As New UpdateClass(EUnitFields.UNIT_END)
                UpdateData.SetUpdateFlag(EUnitFields.UNIT_FIELD_HEALTH, CType(Life.Current, Integer))
                UpdateData.AddToPacket(CType(packetForNear, UpdatePacketClass), ObjectUpdateType.UPDATETYPE_VALUES, Me)

                SendToNearPlayers(CType(packetForNear, UpdatePacketClass))
                packetForNear.Dispose()
                UpdateData.Dispose()
            End If
        End Sub
        Public Overrides Sub Energize(ByVal Damage As Integer, ByVal Power As ManaTypes, Optional ByRef Attacker As BaseUnit = Nothing)
            If ManaType <> WS_CharManagment.ManaTypes.TYPE_MANA Then Exit Sub
            If Power <> ManaTypes.TYPE_MANA Then Exit Sub
            If Mana.Current = Mana.Maximum Then Exit Sub

            Mana.Current += Damage

            'DONE: Do health update
            If SeenBy.Count > 0 Then
                Dim packetForNear As New UpdatePacketClass
                Dim UpdateData As New UpdateClass(EUnitFields.UNIT_END)
                UpdateData.SetUpdateFlag(EUnitFields.UNIT_FIELD_POWER1 + ManaType, CType(Mana.Current, Integer))
                UpdateData.AddToPacket(CType(packetForNear, UpdatePacketClass), ObjectUpdateType.UPDATETYPE_VALUES, Me)

                SendToNearPlayers(CType(packetForNear, UpdatePacketClass))
                packetForNear.Dispose()
                UpdateData.Dispose()
            End If
        End Sub
        Public Sub LootCorpse(ByRef Character As CharacterObject)
            'TODO: Add support for skinning
            If GenerateLoot(Character, LootType.LOOTTYPE_CORPSE) Then
                cDynamicFlags = DynamicFlags.UNIT_DYNFLAG_LOOTABLE

                'ElseIf CType(CREATURESDatabase(ID), CreatureInfo).Loot_Skinning <> 0 Then
                'GenerateLoot(Character, LootType.LOOTTYPE_SKINNNING)
                'cUnitFlags += UnitFlags.UNIT_FLAG_SKINNABLE
                '
                ''DONE: Send skinnable and exit
                'Dim SkinnablePacket As New PacketClass(OPCODES.SMSG_UPDATE_OBJECT)
                'SkinnablePacket.AddInt32(1)
                'SkinnablePacket.AddInt8(0)
                'Dim UpdateDataSkinnable As New UpdateClass
                'UpdateDataSkinnable.SetUpdateFlag(EUnitFields.UNIT_DYNAMIC_FLAGS, cDynamicFlags)
                'UpdateDataSkinnable.SetUpdateFlag(EUnitFields.UNIT_FIELD_FLAGS, cUnitFlags)
                'UpdateDataSkinnable.AddToPacket(SkinnablePacket, ObjectUpdateType.UPDATETYPE_VALUES, Me, 0)
                'UpdateDataSkinnable.Dispose()
                'Character.Client.SendMultiplyPackets(SkinnablePacket)
                'Character.SendToNearPlayers(SkinnablePacket)
                'SkinnablePacket.Dispose()
                'Exit Sub
            Else
                'No loot or skinnable
                Exit Sub
            End If

            'DONE: Create packet
            Dim packet As New PacketClass(OPCODES.SMSG_UPDATE_OBJECT)
            packet.AddInt32(1)
            'packet.AddInt8(0)
            Dim UpdateData As New UpdateClass
            UpdateData.SetUpdateFlag(EUnitFields.UNIT_DYNAMIC_FLAGS, cDynamicFlags)
            UpdateData.SetUpdateFlag(EUnitFields.UNIT_FIELD_FLAGS, cUnitFlags)
            UpdateData.AddToPacket(packet, ObjectUpdateType.UPDATETYPE_VALUES, Me)
            UpdateData.Dispose()

            If Character.IsInGroup Then
                'DONE: Group loot rulles
                LootTable(GUID).LootOwner = 0

                Select Case Character.Group.LootMethod
                    Case GroupLootMethod.LOOT_FREE_FOR_ALL
                        For Each c As ULong In Character.Group.LocalMembers
                            If SeenBy.Contains(c) Then
                                LootTable(GUID).LootOwner = c
                                CHARACTERs(c).Client.Send(packet)
                            End If
                        Next

                    Case GroupLootMethod.LOOT_MASTER
                        If Character.Group.LocalLootMaster Is Nothing Then
                            LootTable(GUID).LootOwner = Character.GUID
                            Character.Client.Send(packet)
                        Else
                            LootTable(GUID).LootOwner = Character.Group.LocalLootMaster.GUID
                            Character.Group.LocalLootMaster.Client.Send(packet)
                        End If

                    Case GroupLootMethod.LOOT_GROUP, GroupLootMethod.LOOT_NEED_BEFORE_GREED, GroupLootMethod.LOOT_ROUND_ROBIN
                        Dim cLooter As CharacterObject = Character.Group.GetNextLooter()
                        While Not SeenBy.Contains(cLooter.GUID) AndAlso (Not cLooter Is Character)
                            cLooter = Character.Group.GetNextLooter()
                        End While

                        LootTable(GUID).LootOwner = cLooter.GUID
                        cLooter.Client.Send(packet)
                End Select
            Else
                'GenerateLoot(Character, GUID, LootType.LOOTTYPE_CORPSE)
                LootTable(GUID).LootOwner = Character.GUID
                Character.Client.Send(packet)
            End If

            'DONE: Dispose packet
            packet.Dispose()
        End Sub
        Public Function GenerateLoot(ByRef Character As CharacterObject, ByVal LootType As LootType) As Boolean
            'DONE: Loot generation
            Dim MySQLQuery As New DataTable
            Database.Query(String.Format("SELECT * FROM loots WHERE loot_creature = {0};", ID), MySQLQuery)
            If MySQLQuery.Rows.Count = 0 Then Return False

            'TODO: Check if we're in a heroic instance!
            Dim Loot As New LootObject(GUID, LootType)
            For Each LootRow As DataRow In MySQLQuery.Rows
                If CType(LootRow.Item("loot_chance"), Single) * 10000 > (Rnd.Next(1, 2000001) Mod 1000000) Then
                    Dim ItemCount As Byte = CByte(Rnd.Next(CType(LootRow.Item("loot_min"), Byte), CType(LootRow.Item("loot_max"), Byte) + 1))
                    If ITEMDatabase.ContainsKey(CType(LootRow.Item("loot_item"), Integer)) = False Then Dim tmpItem As ItemInfo = New ItemInfo(CType(LootRow.Item("loot_item"), Integer))
                    If CType(ITEMDatabase(CType(LootRow.Item("loot_item"), Integer)), ItemInfo).ObjectClass = ITEM_CLASS.ITEM_CLASS_QUEST Then
                        'Check if this quest item can be looted
                        If IsItemNeededForQuest(Character, CType(LootRow.Item("loot_item"), Integer)) Then
                            Loot.Items.Add(New LootItem(CType(LootRow.Item("loot_item"), Integer), ItemCount))
                        End If
                    Else
                        Loot.Items.Add(New LootItem(CType(LootRow.Item("loot_item"), Integer), ItemCount))
                    End If
                End If
            Next

            'DONE: Money loot
            'TODO: Get money from DB
            'TODO: Only humanoids and bosses (can a boss be a not humanoid?) drops money.
            If LootType = LootType.LOOTTYPE_CORPSE Then
                If CType(CREATURESDatabase(ID), CreatureInfo).Elite = 0 Then
                    If CType(CREATURESDatabase(ID), CreatureInfo).CreatureType = UNIT_TYPE.HUMANOID Then
                        Loot.Money = Fix(CInt(Level) * Rnd.Next(0, CInt(Level)))
                    End If
                Else
                    Loot.Money = Fix(Level * Rnd.Next(0, Level) * CType(CREATURESDatabase(ID), CreatureInfo).Elite)
                End If
            End If

            Loot.LootOwner = Character.GUID

            Return True
        End Function
        Public Sub GiveXP(ByRef Character As CharacterObject)
            'NOTE: Formulas taken from http://www.wowwiki.com/Formulas:Mob_XP
            Dim XP As Integer = 0
            If ExpansionLevel = Common.ExpansionLevel.NORMAL Then 'Azeroth
                XP = CInt(Level) * 5 + 45
            ElseIf ExpansionLevel = Common.ExpansionLevel.EXPANSION_1 Then 'Outlands
                'TODO: Blood elf and Draenei starting zones should be counted with azeroth XP formula.
                XP = CInt(Level) * 5 + 235
            ElseIf ExpansionLevel = Common.ExpansionLevel.EXPANSION_2 Then 'Northrend
                XP = CInt(Level) * 5 + 580
            End If

            Dim lvlDifference As Integer = CInt(Character.Level) - CInt(Level)

            If lvlDifference > 0 Then 'Higher level mobs
                XP = XP * (1 + 0.05 * (CInt(Level) - CInt(Character.Level)))
            ElseIf lvlDifference < 0 Then 'Lower level mobs
                Dim GrayLevel As Byte = 0
                Select Case Character.Level
                    Case Is <= 5 : GrayLevel = 0
                    Case Is <= 39 : GrayLevel = CInt(Character.Level) - Math.Floor(CInt(Character.Level) / 10) - 5
                    Case Is <= 59 : GrayLevel = CInt(Character.Level) - Math.Floor(CInt(Character.Level) / 5) - 1
                    Case Else : GrayLevel = CInt(Character.Level) - 9
                End Select

                If Level > GrayLevel Then
                    Dim ZD As Integer = 0
                    Select Case Character.Level
                        Case Is <= 7 : ZD = 5
                        Case Is <= 9 : ZD = 6
                        Case Is <= 11 : ZD = 7
                        Case Is <= 15 : ZD = 8
                        Case Is <= 19 : ZD = 9
                        Case Is <= 29 : ZD = 11
                        Case Is <= 39 : ZD = 12
                        Case Is <= 44 : ZD = 13
                        Case Is <= 49 : ZD = 14
                        Case Is <= 54 : ZD = 15
                        Case Is <= 59 : ZD = 16
                        Case Else : ZD = 17
                    End Select

                    XP = XP * (1 - (CInt(Character.Level) - CInt(Level)) / ZD)
                Else
                    XP = 0
                End If
            End If

            'DONE: Killing elites
            If CType(CREATURESDatabase(ID), CreatureInfo).Elite > 0 Then XP *= 2
            'DONE: XP Rate config
            XP *= Config.XPRate

            If Not Character.IsInGroup Then
                'DONE: Rested
                Dim RestedXP As Integer = 0
                If Character.RestXP >= 0 Then
                    RestedXP = XP
                    If RestedXP > Character.RestXP Then RestedXP = Character.RestXP
                    Character.RestXP -= RestedXP
                    XP += RestedXP
                End If

                'DONE: Single kill
                Character.AddXP(XP, RestedXP, GUID)
            Else

                'DONE: Party bonus
                XP /= Character.Group.GetMembersCount()

                Select Case Character.Group.GetMembersCount()
                    Case Is <= 2 : XP *= 1
                    Case 3 : XP *= 1.166
                    Case 4 : XP *= 1.3
                    Case Else : XP *= 1.4
                End Select

                'DONE: Party calculate all levels
                Dim baseLvl As Integer = 0
                For Each Member As ULong In Character.Group.LocalMembers
                    With CHARACTERs(Member)
                        If .DEAD = False AndAlso (Math.Sqrt((.positionX - positionX) ^ 2 + (.positionY - positionY) ^ 2) <= DEFAULT_DISTANCE_VISIBLE) Then
                            baseLvl += .Level
                        End If
                    End With
                Next

                'DONE: Party share
                For Each Member As ULong In Character.Group.LocalMembers
                    With CHARACTERs(Member)
                        If .DEAD = False AndAlso (Math.Sqrt((.positionX - positionX) ^ 2 + (.positionY - positionY) ^ 2) <= DEFAULT_DISTANCE_VISIBLE) Then
                            Dim tmpXP As Integer = XP
                            'DONE: Rested
                            Dim RestedXP As Integer = 0
                            If .RestXP >= 0 Then
                                RestedXP = tmpXP
                                If RestedXP > .RestXP Then RestedXP = .RestXP
                                .RestXP -= RestedXP
                                tmpXP += RestedXP
                            End If

                            tmpXP = Fix(tmpXP * CInt(.Level) / baseLvl)
                            .AddXP(tmpXP, RestedXP, GUID)
                        End If
                    End With
                Next

            End If
        End Sub

        Public Sub ApplySpell(ByVal SpellID As Integer)
            'TODO: Check if the creature can cast the spell

            If SPELLs.ContainsKey(SpellID) = False Then Exit Sub
            Dim t As New SpellTargets
            t.SetTarget_SELF(Me)
            SPELLs(SpellID).Apply(Me, t)
        End Sub
        Public Function CastSpell(ByVal SpellID As Integer, ByVal Target As BaseObject) As Integer
            If Spell_Silenced Then Return -1

            Dim tmpSpell As New CastSpellParameters
            tmpSpell.tmpTargets = New SpellTargets
            tmpSpell.tmpTargets.SetTarget_UNIT(Target)
            tmpSpell.tmpCaster = Me
            tmpSpell.tmpSpellID = SpellID

            ThreadPool.QueueUserWorkItem(New WaitCallback(AddressOf tmpSpell.Cast))
            Return CType(SPELLs(SpellID), SpellInfo).GetCastTime
        End Function
        Public Sub SendChatMessage(ByVal Message As String, ByVal msgType As ChatMsg, ByVal msgLanguage As LANGUAGES, Optional ByVal SecondGUID As ULong = 0)
            Dim packet As New PacketClass(OPCODES.SMSG_MESSAGECHAT)
            Dim flag As Byte = 0

            packet.AddInt8(msgType)
            packet.AddInt32(msgLanguage)

            Select Case msgType
                Case ChatMsg.CHAT_MSG_MONSTER_SAY, ChatMsg.CHAT_MSG_MONSTER_EMOTE, ChatMsg.CHAT_MSG_MONSTER_YELL, ChatMsg.CHAT_MSG_MONSTER_WHISPER, ChatMsg.CHAT_MSG_MONSTER_PARTY, ChatMsg.CHAT_MSG_RAID_BOSS_WHISPER, ChatMsg.CHAT_MSG_RAID_BOSS_EMOTE
                    packet.AddUInt64(GUID)
                    packet.AddInt32(0) 'Unk, 2.1.0
                    packet.AddInt32(System.Text.Encoding.UTF8.GetByteCount(Name) + 1)
                    packet.AddString(Name)
                    packet.AddUInt64(SecondGUID)
                    If SecondGUID AndAlso GuidIsPlayer(SecondGUID) = False Then
                        packet.AddInt32(1) 'Target name length
                        packet.AddInt8(0) 'Target name
                    End If
                Case Else
                    Log.WriteLine(LogType.WARNING, "Creature.SendChatMessage() must not handle this chat type!")
            End Select

            packet.AddInt32(System.Text.Encoding.UTF8.GetByteCount(Message) + 1)
            packet.AddString(Message)
            packet.AddInt8(flag)
            SendToNearPlayers(packet)
            packet.Dispose()
        End Sub

        Public Sub Initialize()
            'DONE: Database loading
            Me.Level = Rnd.Next(CREATURESDatabase(ID).LevelMin, CREATURESDatabase(ID).LevelMax)
            Me.Size = CREATURESDatabase(ID).Size
            If Me.Size = 0 Then Me.Size = 1
            If CREATURESDatabase(ID).FemaleModel <> 0 Then
                If Rnd.Next(0, 2) = 0 Then
                    Me.Model = CREATURESDatabase(ID).FemaleModel
                Else
                    Me.Model = CREATURESDatabase(ID).Model
                End If
            Else
                Me.Model = CREATURESDatabase(ID).Model
            End If
            Me.ManaType = CREATURESDatabase(ID).ManaType
            Me.Mana.Base = CREATURESDatabase(ID).Mana
            Me.Mana.Current = Me.Mana.Maximum
            Me.Life.Base = CREATURESDatabase(ID).Life
            Me.Life.Current = Me.Life.Maximum

            'DONE: Internal Initializators
            Me.CanSeeInvisibility_Stealth = SKILL_DETECTION_PER_LEVEL * Me.Level
            Me.CanSeeInvisibility_Invisibility = 0

            If (CREATURESDatabase(ID).cNpcFlags And NPCFlags.UNIT_NPC_FLAG_SPIRITHEALER) = NPCFlags.UNIT_NPC_FLAG_SPIRITHEALER Then
                Invisibility = InvisibilityLevel.DEAD
                cUnitFlags = UnitFlags.UNIT_FLAG_SPIRITHEALER
            ElseIf CREATURESDatabase(ID).cNpcFlags > 0 Then
                cUnitFlags = UnitFlags.UNIT_FLAG_NOT_ATTACKABLE
            End If

            Me.StandState = Me.cStandState

            Me.CurrentWaypoint = 0

            'DONE: Load scripted AI
            If CREATURESDatabase(ID).AIScriptSource <> "" Then
                aiScript = AI.Invoke(CREATURESDatabase(ID).AIScriptSource, New Object() {Me})
            End If

            'DONE: Load default AI
            If aiScript Is Nothing Then
                ''''If isWaypoint Then
                ''''aiScript = New WaypointAI(Me)
                If CreatureInfo.cNpcFlags = 0 AndAlso StandState = 0 AndAlso cEmoteState = 0 Then
                    aiScript = New DefaultAI(Me)
                ElseIf isGuard Then
                    aiScript = New GuardAI(Me)
                Else
                    aiScript = New TBaseAI
                End If
            End If
        End Sub
        Public Sub New(ByVal GUID_ As ULong, Optional ByRef Info As DataRow = Nothing)
            'WARNING: Use only for loading creature from DB
            If Info Is Nothing Then
                Dim MySQLQuery As New DataTable
                Database.Query(String.Format("SELECT * FROM spawns_creatures WHERE spawned_id = {0};", GUID_), MySQLQuery)
                If MySQLQuery.Rows.Count > 0 Then
                    Info = MySQLQuery.Rows(0)
                Else
                    Log.WriteLine(LogType.FAILED, "Creature Spawn not found in database. [GUID={0:X}]", GUID_)
                    Return
                End If
            End If

            positionX = Info.Item("spawn_positionX")
            positionY = Info.Item("spawn_positionY")
            positionZ = Info.Item("spawn_positionZ")
            orientation = Info.Item("spawn_orientation")

            OldX = positionX
            OldY = positionY
            OldZ = positionZ

            SpawnX = positionX
            SpawnY = positionY
            SpawnZ = positionZ
            SpawnO = orientation

            ID = Info.Item("spawn_entry")
            MapID = Info.Item("spawn_map")
            SpawnID = Info.Item("spawn_id")

            MoveType = Info.Item("spawn_movetype")
            Model = Info.Item("spawn_displayid")
            Faction = Info.Item("spawn_faction")
            Mount = Info.Item("spawn_mount")
            cUnitFlags = Info.Item("spawn_flags")
            'cBytes0 = Info.Item("spawn_bytes0") NOT USED ATM
            cBytes1 = Info.Item("spawn_bytes1")
            cBytes2 = Info.Item("spawn_bytes2")
            cEmoteState = Info.Item("spawn_emotestate")
            cStandState = Info.Item("spawn_standstate")

            EquipedItems(0) = Info.Item("spawn_equipslot1")
            EquipedItems(1) = Info.Item("spawn_equipslot2")
            EquipedItems(2) = Info.Item("spawn_equipslot3")

            If Not CREATURESDatabase.ContainsKey(ID) Then
                Dim baseCreature As New CreatureInfo(ID)
            End If

            GUID = GUID_ + GUID_UNIT
            Initialize()

            Try
                WORLD_CREATUREs_Lock.AcquireWriterLock(DEFAULT_LOCK_TIMEOUT)
                ''''WORLD_CREATUREsClone_Lock.AcquireWriterLock(DEFAULT_LOCK_TIMEOUT)
                WORLD_CREATUREs.Add(GUID, Me)
                WORLD_CREATUREsKeys.Add(GUID)
                ''''WORLD_CREATUREsClone.Add(GUID, Me)
                WORLD_CREATUREs_Lock.ReleaseWriterLock()
                ''''WORLD_CREATUREsClone_Lock.ReleaseWriterLock()
            Catch
            End Try
        End Sub
        Public Sub New(ByVal ID_ As Integer)
            'WARNING: Use only for spawning new crature

            If Not CREATURESDatabase.ContainsKey(ID_) Then
                Dim baseCreature As New CreatureInfo(ID_)
            End If

            ID = ID_
            GUID = GetNewGUID()

            Initialize()

            Try
                WORLD_CREATUREs_Lock.AcquireWriterLock(DEFAULT_LOCK_TIMEOUT)
                ''''WORLD_CREATUREsClone_Lock.AcquireWriterLock(DEFAULT_LOCK_TIMEOUT)
                WORLD_CREATUREs.Add(GUID, Me)
                WORLD_CREATUREsKeys.Add(GUID)
                ''''WORLD_CREATUREsClone.Add(GUID, Me)
                WORLD_CREATUREs_Lock.ReleaseWriterLock()
                ''''WORLD_CREATUREsClone_Lock.ReleaseWriterLock()
            Catch
            End Try
        End Sub
        Public Sub New(ByVal ID_ As Integer, ByVal PosX As Single, ByVal PosY As Single, ByVal PosZ As Single, ByVal Orientation As Single, ByVal Map As Integer, Optional ByVal Duration As Integer = 0)
            'WARNING: Use only for spawning new crature

            If Not CREATURESDatabase.ContainsKey(ID_) Then
                Dim baseCreature As New CreatureInfo(ID_)
            End If

            ID = ID_
            GUID = GetNewGUID()

            positionX = PosX
            positionY = PosY
            positionZ = PosZ
            Orientation = Orientation
            MapID = Map

            SpawnX = PosX
            SpawnY = PosY
            SpawnZ = PosZ
            SpawnO = Orientation

            Initialize()

            'TODO: Duration
            If Duration > 0 Then
                ExpireTimer = New Threading.Timer(AddressOf Destroy, Nothing, Duration, Duration)
            End If

            Try
                WORLD_CREATUREs_Lock.AcquireWriterLock(DEFAULT_LOCK_TIMEOUT)
                ''''WORLD_CREATUREsClone_Lock.AcquireWriterLock(DEFAULT_LOCK_TIMEOUT)
                WORLD_CREATUREs.Add(GUID, Me)
                WORLD_CREATUREsKeys.Add(GUID)
                ''''WORLD_CREATUREsClone.Add(GUID, Me)
                WORLD_CREATUREs_Lock.ReleaseWriterLock()
                ''''WORLD_CREATUREsClone_Lock.ReleaseWriterLock()
            Catch
            End Try
        End Sub
        Private Sub Dispose() Implements System.IDisposable.Dispose
            If Not Me.aiScript Is Nothing Then Me.aiScript.Dispose()

            Me.RemoveFromWorld()

            Try
                WORLD_CREATUREs_Lock.AcquireWriterLock(DEFAULT_LOCK_TIMEOUT)
                ''''WORLD_CREATUREsClone_Lock.AcquireWriterLock(DEFAULT_LOCK_TIMEOUT)
                WORLD_CREATUREs.Remove(GUID)
                WORLD_CREATUREsKeys.Remove(GUID)
                ''''WORLD_CREATUREsClone.Remove(GUID)
                WORLD_CREATUREs_Lock.ReleaseWriterLock()
                ''''WORLD_CREATUREsClone_Lock.ReleaseWriterLock()
            Catch
            End Try
        End Sub
        Public Sub Destroy(Optional ByVal state As Object = Nothing)
            'TODO: Remove pets also
            If SummonedBy > 0 Then
                If GuidIsPlayer(SummonedBy) AndAlso CHARACTERs.ContainsKey(SummonedBy) Then
                    If CHARACTERs(SummonedBy).NonCombatPet IsNot Nothing AndAlso CHARACTERs(SummonedBy).NonCombatPet Is Me Then
                        CHARACTERs(SummonedBy).NonCombatPet = Nothing
                    End If
                End If
            End If

            Dim packet As New PacketClass(OPCODES.SMSG_DESTROY_OBJECT)
            packet.AddUInt64(GUID)
            SendToNearPlayers(packet)
            packet.Dispose()

            Me.Dispose()
        End Sub
        Public Sub Despawn()
            RemoveFromWorld()

            If LootTable.ContainsKey(GUID) Then
                CType(LootTable(GUID), LootObject).Dispose()
            End If
        End Sub
        Public Sub Respawn()
            Life.Current = Life.Maximum
            Mana.Current = Mana.Maximum
            cUnitFlags = cUnitFlags And (Not UnitFlags.UNIT_FLAG_DEAD)
            cDynamicFlags = 0

            positionX = SpawnX
            positionY = SpawnY
            positionZ = SpawnZ
            orientation = SpawnO
            CurrentWaypoint = 0

            If aiScript IsNot Nothing Then
                aiScript.Reset()
            End If

            If SeenBy.Count > 0 Then
                Dim packetForNear As New UpdatePacketClass
                Dim UpdateData As New UpdateClass(EUnitFields.UNIT_END)
                UpdateData.SetUpdateFlag(EUnitFields.UNIT_FIELD_HEALTH, CType(Life.Current, Integer))
                UpdateData.SetUpdateFlag(EUnitFields.UNIT_FIELD_POWER1 + ManaType, CType(Mana.Current, Integer))
                UpdateData.SetUpdateFlag(EUnitFields.UNIT_FIELD_FLAGS, cUnitFlags)
                UpdateData.SetUpdateFlag(EUnitFields.UNIT_DYNAMIC_FLAGS, cDynamicFlags)
                UpdateData.AddToPacket(CType(packetForNear, UpdatePacketClass), ObjectUpdateType.UPDATETYPE_VALUES, Me)

                SendToNearPlayers(CType(packetForNear, UpdatePacketClass))
                packetForNear.Dispose()
                UpdateData.Dispose()

                MoveToInstant(SpawnX, SpawnY, SpawnZ)
            Else
                AddToWorld()
            End If
        End Sub
        Public Sub AddToWorld()
            GetMapTile(positionX, positionY, CellX, CellY)
            If Maps(MapID).Tiles(CellX, CellY) Is Nothing Then MAP_Load(CellX, CellY, MapID)
            Try
                Maps(MapID).Tiles(CellX, CellY).CreaturesHere.Add(GUID)
            Catch
                Exit Sub
            End Try

            Dim list() As ULong

            'DONE: Sending to players in nearby cells
            For i As Short = -1 To 1
                For j As Short = -1 To 1
                    If (CellX + i) >= 0 AndAlso (CellX + i) <= 63 AndAlso (CellY + j) >= 0 AndAlso (CellY + j) <= 63 AndAlso Maps(MapID).Tiles(CellX + i, CellY + j) IsNot Nothing AndAlso Maps(MapID).Tiles(CellX + i, CellY + j).PlayersHere.Count > 0 Then
                        With Maps(MapID).Tiles(CellX + i, CellY + j)
                            list = .PlayersHere.ToArray
                            For Each plGUID As ULong In list
                                If CHARACTERs(plGUID).CanSee(Me) Then
                                    Dim packet As New PacketClass(OPCODES.SMSG_UPDATE_OBJECT)
                                    packet.AddInt32(1)
                                    'packet.AddInt8(0)
                                    Dim tmpUpdate As New UpdateClass(FIELD_MASK_SIZE_UNIT)
                                    FillAllUpdateFlags(tmpUpdate)
                                    tmpUpdate.AddToPacket(packet, ObjectUpdateType.UPDATETYPE_CREATE_OBJECT, Me)
                                    tmpUpdate.Dispose()

                                    CHARACTERs(plGUID).Client.SendMultiplyPackets(packet)

                                    CHARACTERs(plGUID).creaturesNear.Add(GUID)
                                    SeenBy.Add(plGUID)

                                    packet.Dispose()
                                End If
                            Next
                        End With
                    End If
                Next
            Next

        End Sub
        Public Sub RemoveFromWorld()
            GetMapTile(positionX, positionY, CellX, CellY)
            Maps(MapID).Tiles(CellX, CellY).CreaturesHere.Remove(GUID)

            'DONE: Removing from players who can see the creature
            For Each plGUID As ULong In SeenBy.ToArray
                If CHARACTERs.ContainsKey(plGUID) Then
                    CHARACTERs(plGUID).guidsForRemoving_Lock.AcquireWriterLock(DEFAULT_LOCK_TIMEOUT)
                    CHARACTERs(plGUID).guidsForRemoving.Add(GUID)
                    CHARACTERs(plGUID).guidsForRemoving_Lock.ReleaseWriterLock()

                    CHARACTERs(plGUID).creaturesNear.Remove(GUID)
                End If
            Next

            SeenBy.Clear()
        End Sub
        Public Sub MoveCell()
            If CellX <> GetMapTileX(positionX) OrElse CellY <> GetMapTileY(positionY) Then
                Maps(MapID).Tiles(CellX, CellY).CreaturesHere.Remove(GUID)
                GetMapTile(positionX, positionY, CellX, CellY)

                'If creature changes cell then it's sent back to spawn, if the creature is a waypoint walker this won't be very good :/
                ' Hopefully the Not isWaypoint will override the above comment.
                If (Maps(MapID).Tiles(CellX, CellY) Is Nothing) Then
                    aiScript.State = TBaseAI.AIState.AI_WANDERING
                    MoveTo(SpawnX, SpawnY, SpawnZ, True)
                    Exit Sub
                Else
                    Maps(MapID).Tiles(CellX, CellY).CreaturesHere.Add(GUID)
                End If
            End If
        End Sub
    End Class
#End Region
#Region "WS.Creatures.HelperSubs"
    Public Sub On_CMSG_CREATURE_QUERY(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 17 Then Exit Sub
        Dim response As New PacketClass(OPCODES.SMSG_CREATURE_QUERY_RESPONSE)

        packet.GetInt16()
        Dim CreatureID As Integer = packet.GetInt32
        Dim CreatureGUID As ULong = packet.GetUInt64

        Try
            Dim Creature As New CreatureInfo

            If CREATURESDatabase.ContainsKey(CreatureID) = False Then
                Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_CREATURE_QUERY [Creature {2} not loaded.]", Client.IP, Client.Port, CreatureID)

                response.AddUInt32((CreatureID Or &H80000000))
                Client.Send(response)
                response.Dispose()
                Exit Sub
            Else
                Creature = CREATURESDatabase(CreatureID)
                Log.WriteLine(LogType.WARNING, "DEBUG: Creature Name = {0}", Creature.Name)
                'Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_CREATURE_QUERY [CreatureID={2} CreatureGUID={3:X}]", Format(TimeOfDay, "hh:mm:ss"), Client.IP, Client.Port, CreatureID, CreatureGUID - GUID_UNIT)
            End If

            response.AddInt32(Creature.Id)
            response.AddString(Creature.Name)
            response.AddInt8(0)                         'Creature.Name2
            response.AddInt8(0)                         'Creature.Name3
            response.AddInt8(0)                         'Creature.Name4
            response.AddString(Creature.Guild)
            response.AddString(Creature.Info_Str)

            response.AddInt32(Creature.cFlags)
            response.AddInt32(Creature.CreatureType)
            response.AddInt32(Creature.CreatureFamily)
            response.AddInt32(Creature.Elite)           'Rank
            response.AddInt32(0)                        'Unk1
            response.AddInt32(Creature.SpellDataID)
            response.AddInt32(Creature.Model)           'Male model
            response.AddInt32(Creature.FemaleModel)     'Female Model
            response.AddInt32(0)                        'Unkint1
            response.AddSingle(Creature.UnkFloat1)      'UnkFloat1
            response.AddSingle(Creature.UnkFloat2)      'UnkFloat2
            response.AddInt8(Creature.Leader)           'Leader

            Client.Send(response)
            response.Dispose()
            'Log.WriteLine(LogType.DEBUG, "[{0}:{1}] SMSG_CREATURE_QUERY_RESPONSE", Client.IP, Client.Port)
        Catch e As Exception
            Log.WriteLine(LogType.FAILED, "Unknown Error: Unable to find CreatureID={0} in database. Error:{1} : Inner:{2}", CreatureID, e.Message, e.InnerException)
        End Try
    End Sub
    Public Sub On_CMSG_NPC_TEXT_QUERY(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 17 Then Exit Sub
        packet.GetInt16()
        Dim TextID As Long = packet.GetInt32
        Dim TargetGUID As ULong = packet.GetUInt64
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_NPC_TEXT_QUERY [TextID={2}]", Client.IP, Client.Port, TextID)

        Dim MySQLQuery As New DataTable
        Database.Query(String.Format("SELECT * FROM npcText WHERE entry = {0};", TextID), MySQLQuery)

        'DONE: Load TextID
        Dim response As New PacketClass(OPCODES.SMSG_NPC_TEXT_UPDATE)
        'Dim i As Byte
        response.AddInt32(TextID)

        If MySQLQuery.Rows.Count <> 0 Then
            response.AddSingle(1.0F) ' Unknown
            'For i = 1 To 8
            response.AddString(MySQLQuery.Rows(0).Item("text0_0"))        'text0_0        'Text1
            response.AddString(MySQLQuery.Rows(0).Item("text0_1"))        'text0_1        'Text2
            response.AddInt32(MySQLQuery.Rows(0).Item("lang0"))           'Language
            response.AddInt32(MySQLQuery.Rows(0).Item("prob0"))           'Probability
            response.AddInt32(MySQLQuery.Rows(0).Item("em0_0"))           'Emote1.Delay
            response.AddInt32(MySQLQuery.Rows(0).Item("em0_1"))           'Emote1.Emote
            response.AddInt32(MySQLQuery.Rows(0).Item("em0_2"))           'Emote2.Delay
            response.AddInt32(MySQLQuery.Rows(0).Item("em0_3"))           'Emote2.Emote
            response.AddInt32(MySQLQuery.Rows(0).Item("em0_4"))           'Emote3.Delay
            response.AddInt32(MySQLQuery.Rows(0).Item("em0_5"))           'Emote3.Emote
            response.AddString(MySQLQuery.Rows(0).Item("text1_0"))        'text1_0        'Text1
            response.AddString(MySQLQuery.Rows(0).Item("text1_1"))        'text1_1        'Text2
            response.AddInt32(MySQLQuery.Rows(0).Item("lang1"))           'Language
            response.AddInt32(MySQLQuery.Rows(0).Item("prob1"))           'Probability
            response.AddInt32(MySQLQuery.Rows(0).Item("em1_0"))           'Emote1.Delay
            response.AddInt32(MySQLQuery.Rows(0).Item("em1_1"))           'Emote1.Emote
            response.AddInt32(MySQLQuery.Rows(0).Item("em1_2"))           'Emote2.Delay
            response.AddInt32(MySQLQuery.Rows(0).Item("em1_3"))           'Emote2.Emote
            response.AddInt32(MySQLQuery.Rows(0).Item("em1_4"))           'Emote3.Delay
            response.AddInt32(MySQLQuery.Rows(0).Item("em1_5"))           'Emote3.Emote
            response.AddString(MySQLQuery.Rows(0).Item("text2_0"))        'text2_0        'Text1
            response.AddString(MySQLQuery.Rows(0).Item("text2_1"))        'text2_1        'Text2
            response.AddInt32(MySQLQuery.Rows(0).Item("lang2"))           'Language
            response.AddInt32(MySQLQuery.Rows(0).Item("prob2"))           'Probability
            response.AddInt32(MySQLQuery.Rows(0).Item("em2_0"))           'Emote1.Delay
            response.AddInt32(MySQLQuery.Rows(0).Item("em2_1"))           'Emote1.Emote
            response.AddInt32(MySQLQuery.Rows(0).Item("em2_2"))           'Emote2.Delay
            response.AddInt32(MySQLQuery.Rows(0).Item("em2_3"))           'Emote2.Emote
            response.AddInt32(MySQLQuery.Rows(0).Item("em2_4"))           'Emote3.Delay
            response.AddInt32(MySQLQuery.Rows(0).Item("em2_5"))           'Emote3.Emote
            response.AddString(MySQLQuery.Rows(0).Item("text3_0"))        'text3_0        'Text1
            response.AddString(MySQLQuery.Rows(0).Item("text3_1"))        'text3_1        'Text2
            response.AddInt32(MySQLQuery.Rows(0).Item("lang3"))           'Language
            response.AddInt32(MySQLQuery.Rows(0).Item("prob3"))           'Probability
            response.AddInt32(MySQLQuery.Rows(0).Item("em3_0"))           'Emote1.Delay
            response.AddInt32(MySQLQuery.Rows(0).Item("em3_1"))           'Emote1.Emote
            response.AddInt32(MySQLQuery.Rows(0).Item("em3_2"))           'Emote2.Delay
            response.AddInt32(MySQLQuery.Rows(0).Item("em3_3"))           'Emote2.Emote
            response.AddInt32(MySQLQuery.Rows(0).Item("em3_4"))           'Emote3.Delay
            response.AddInt32(MySQLQuery.Rows(0).Item("em3_5"))           'Emote3.Emote
            response.AddString(MySQLQuery.Rows(0).Item("text4_0"))        'text4_0        'Text1
            response.AddString(MySQLQuery.Rows(0).Item("text4_1"))        'text4_1        'Text2
            response.AddInt32(MySQLQuery.Rows(0).Item("lang4"))           'Language
            response.AddInt32(MySQLQuery.Rows(0).Item("prob4"))           'Probability
            response.AddInt32(MySQLQuery.Rows(0).Item("em4_0"))           'Emote1.Delay
            response.AddInt32(MySQLQuery.Rows(0).Item("em4_1"))           'Emote1.Emote
            response.AddInt32(MySQLQuery.Rows(0).Item("em4_2"))           'Emote2.Delay
            response.AddInt32(MySQLQuery.Rows(0).Item("em4_3"))           'Emote2.Emote
            response.AddInt32(MySQLQuery.Rows(0).Item("em4_4"))           'Emote3.Delay
            response.AddInt32(MySQLQuery.Rows(0).Item("em4_5"))           'Emote3.Emote
            response.AddString(MySQLQuery.Rows(0).Item("text5_0"))        'text5_0        'Text1
            response.AddString(MySQLQuery.Rows(0).Item("text5_1"))        'text5_1        'Text2
            response.AddInt32(MySQLQuery.Rows(0).Item("lang5"))           'Language
            response.AddInt32(MySQLQuery.Rows(0).Item("prob5"))           'Probability
            response.AddInt32(MySQLQuery.Rows(0).Item("em5_0"))           'Emote1.Delay
            response.AddInt32(MySQLQuery.Rows(0).Item("em5_1"))           'Emote1.Emote
            response.AddInt32(MySQLQuery.Rows(0).Item("em5_2"))           'Emote2.Delay
            response.AddInt32(MySQLQuery.Rows(0).Item("em5_3"))           'Emote2.Emote
            response.AddInt32(MySQLQuery.Rows(0).Item("em5_4"))           'Emote3.Delay
            response.AddInt32(MySQLQuery.Rows(0).Item("em5_5"))           'Emote3.Emote
            response.AddString(MySQLQuery.Rows(0).Item("text6_0"))        'text6_0        'Text1
            response.AddString(MySQLQuery.Rows(0).Item("text6_1"))        'text6_1        'Text2
            response.AddInt32(MySQLQuery.Rows(0).Item("lang6"))           'Language
            response.AddInt32(MySQLQuery.Rows(0).Item("prob6"))           'Probability
            response.AddInt32(MySQLQuery.Rows(0).Item("em6_0"))           'Emote1.Delay
            response.AddInt32(MySQLQuery.Rows(0).Item("em6_1"))           'Emote1.Emote
            response.AddInt32(MySQLQuery.Rows(0).Item("em6_2"))           'Emote2.Delay
            response.AddInt32(MySQLQuery.Rows(0).Item("em6_3"))           'Emote2.Emote
            response.AddInt32(MySQLQuery.Rows(0).Item("em6_4"))           'Emote3.Delay
            response.AddInt32(MySQLQuery.Rows(0).Item("em6_5"))           'Emote3.Emote
            response.AddString(MySQLQuery.Rows(0).Item("text7_0"))        'text7_0        'Text1
            response.AddString(MySQLQuery.Rows(0).Item("text7_1"))        'text7_1        'Text2
            response.AddInt32(MySQLQuery.Rows(0).Item("lang7"))           'Language
            response.AddInt32(MySQLQuery.Rows(0).Item("prob7"))           'Probability
            response.AddInt32(MySQLQuery.Rows(0).Item("em7_0"))           'Emote1.Delay
            response.AddInt32(MySQLQuery.Rows(0).Item("em7_1"))           'Emote1.Emote
            response.AddInt32(MySQLQuery.Rows(0).Item("em7_2"))           'Emote2.Delay
            response.AddInt32(MySQLQuery.Rows(0).Item("em7_3"))           'Emote2.Emote
            response.AddInt32(MySQLQuery.Rows(0).Item("em7_4"))           'Emote3.Delay
            response.AddInt32(MySQLQuery.Rows(0).Item("em7_5"))           'Emote3.Emote
            'Next

            Client.Send(response)
            response.Dispose()
        Else
            ''Dim response As New PacketClass(OPCODES.SMSG_NPC_TEXT_UPDATE)
            'response.AddInt32(TextID)
            response.AddSingle(1.0F) ' Unknown
            response.AddString("Hey there, $N. How can I help you?")
            response.AddString(" ")
            response.AddInt32(0)
            response.AddInt32(0)
            response.AddInt32(0)
            response.AddInt32(0)
            response.AddInt32(0)
            response.AddInt32(0)
            response.AddInt32(0)
            Client.Send(response)
            response.Dispose()
        End If

    End Sub

    Public Sub On_CMSG_GOSSIP_HELLO(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 13 Then Exit Sub
        packet.GetInt16()
        Dim GUID As ULong = packet.GetUInt64
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GOSSIP_HELLO [GUID={2:X}]", Client.IP, Client.Port, GUID)

        If CREATURESDatabase(WORLD_CREATUREs(GUID).ID).TalkScript Is Nothing Then
            Dim test As New PacketClass(OPCODES.SMSG_NPC_WONT_TALK)
            test.AddUInt64(GUID)
            test.AddInt8(1)
            Client.Send(test)
            test.Dispose()

            Dim npcText As New NPCText
            npcText.Count = 1
            npcText.TextID = 34
            npcText.TextLine1(0) = "Hi $N, I'm not yet scripted to talk with you."
            SendNPCText(Client, npcText)

            Client.Character.SendGossip(GUID, 1)
        Else
            CREATURESDatabase(WORLD_CREATUREs(GUID).ID).TalkScript.OnGossipHello(Client.Character, GUID)
        End If
    End Sub
    Public Sub On_CMSG_GOSSIP_SELECT_OPTION(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 17 Then Exit Sub
        packet.GetInt16()
        Dim GUID As ULong = packet.GetUInt64
        Dim Unk As Integer = packet.GetInt32
        Dim SelOption As Integer = packet.GetInt32
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_GOSSIP_SELECT_OPTION [SelOption={3} GUID={2:X}]", Client.IP, Client.Port, GUID, SelOption)

        If CREATURESDatabase(WORLD_CREATUREs(GUID).ID).TalkScript Is Nothing Then
            Throw New ApplicationException("Invoked OnGossipSelect() on creature without initialized TalkScript!")
        Else
            CREATURESDatabase(WORLD_CREATUREs(GUID).ID).TalkScript.OnGossipSelect(Client.Character, GUID, SelOption)
        End If
    End Sub
    Public Sub On_CMSG_SPIRIT_HEALER_ACTIVATE(ByRef packet As PacketClass, ByRef Client As ClientClass)
        If (packet.Data.Length - 1) < 13 Then Exit Sub
        packet.GetInt16()
        Dim GUID As ULong = packet.GetUInt64
        Log.WriteLine(LogType.DEBUG, "[{0}:{1}] CMSG_SPIRIT_HEALER_ACTIVATE [GUID={2}]", Client.IP, Client.Port, GUID)

        Try
            Dim i As Byte
            For i = 0 To EQUIPMENT_SLOT_END - 1
                If Client.Character.Items.ContainsKey(i) Then Client.Character.Items(i).ModifyDurability(0.25F, Client)
            Next
        Catch e As Exception
            Log.WriteLine(LogType.FAILED, "Error activating spirit healer: {0}", e.ToString)
        End Try

        CharacterResurrect(Client.Character)

        Client.Character.ApplySpell(15007)
    End Sub

    <MethodImplAttribute(MethodImplOptions.Synchronized)> _
    Private Function GetNewGUID() As ULong
        CreatureGUIDCounter += 1
        Return CreatureGUIDCounter
    End Function
    Public Sub SendNPCText(ByRef Client As ClientClass, ByRef text As NPCText)
        Dim response As New PacketClass(OPCODES.SMSG_NPC_TEXT_UPDATE)
        Dim i As Byte
        response.AddInt32(text.TextID)

        For i = 0 To text.Count
            response.AddSingle(1.0F)
            response.AddString(text.TextLine1(i))               'text0_0        'Text1
            response.AddString(text.TextLine2(i))               'text0_1        'Text2
            response.AddInt32(text.Language(i))                 'lang0=         'Language
            response.AddInt32(text.Probability(i))              'dens0          'Probability
            response.AddInt32(text.EmoteDelay1(i))              'unk0_0=        'Emote1.Delay
            response.AddInt32(text.Emote1(i))                   'unk0_1=        'Emote1.Emote
            response.AddInt32(text.EmoteDelay2(i))              'unk0_2=        'Emote2.Delay
            response.AddInt32(text.Emote2(i))                   'unk0_3=        'Emote2.Emote
            response.AddInt32(text.EmoteDelay3(i))              'unk0_4=        'Emote3.Delay
            response.AddInt32(text.Emote3(i))                   'unk0_5=        'Emote3.Emote
        Next

        Client.Send(response)
        response.Dispose()
    End Sub
    Public Class NPCText
        Public Count As Byte = 1

        Public TextID As Integer = 0
        Public Probability() As Byte = {0, 0, 0, 0, 0, 0, 0, 0}
        Public Language() As Byte = {0, 0, 0, 0, 0, 0, 0, 0}
        Public TextLine1() As String = {"", "", "", "", "", "", "", ""}
        Public TextLine2() As String = {"", "", "", "", "", "", "", ""}
        Public Emote1() As Integer = {0, 0, 0, 0, 0, 0, 0, 0}
        Public Emote2() As Integer = {0, 0, 0, 0, 0, 0, 0, 0}
        Public Emote3() As Integer = {0, 0, 0, 0, 0, 0, 0, 0}
        Public EmoteDelay1() As Integer = {0, 0, 0, 0, 0, 0, 0, 0}
        Public EmoteDelay2() As Integer = {0, 0, 0, 0, 0, 0, 0, 0}
        Public EmoteDelay3() As Integer = {0, 0, 0, 0, 0, 0, 0, 0}
    End Class
#End Region
#Region "WS.Creatures.MonsterSay"
    Public MonsterSay As New Dictionary(Of Integer, TMonsterSay)
    Public Class TMonsterSay
        Public Entry As Integer
        Public EventNo As Integer
        Public Chance As Single
        Public Language As Integer
        Public Type As Integer
        Public MonsterName As String
        Public Text0 As String
        Public Text1 As String
        Public Text2 As String
        Public Text3 As String
        Public Text4 As String

        Public Sub New(ByVal Entry_ As Integer, ByVal EventNo_ As Integer, ByVal Chance_ As Single, ByVal Language_ As Integer, ByVal Type_ As Integer, ByVal MonsterName_ As String, ByVal Text0_ As String, ByVal Text1_ As String, ByVal Text2_ As String, ByVal Text3_ As String, ByVal Text4_ As String)
            Entry = Entry_
            EventNo = EventNo_
            Chance = Chance_
            Language = Language_
            Type = Type_
            MonsterName = MonsterName_
            Text0 = Text0_
            Text1 = Text1_
            Text2 = Text2_
            Text3 = Text3_
            Text4 = Text4
        End Sub
    End Class
#End Region

End Module

#Region "WS.Creatures.HelperTypes"
Public Class TLoot
    Public ItemID As Short = 0
    Public Chance As Single = 0
End Class

Public Enum InvisibilityLevel As Byte
    VISIBLE = 0
    STEALTH = 1
    INIVISIBILITY = 2
    DEAD = 3
    GM = 4
End Enum
#End Region
#Region "WS.Creatures.Gossip"
Public Class GossipMenu
    Public Sub AddMenu(ByVal menu As String, Optional ByVal icon As Byte = 0, Optional ByVal isCoded As Byte = 0, Optional ByVal cost As Integer = 0, Optional ByVal WarningMessage As String = "")
        Icons.Add(icon)
        Menus.Add(menu)
        Coded.Add(isCoded)
        Costs.Add(cost)
        WarningMessages.Add(WarningMessage)
    End Sub
    Public Icons As New ArrayList
    Public Menus As New ArrayList
    Public Coded As New ArrayList
    Public Costs As New ArrayList
    Public WarningMessages As New ArrayList
End Class
Public Class QuestMenu
    Public Sub AddMenu(ByVal QuestName As String, ByVal ID As Short, ByVal Level As Short, Optional ByVal Icon As Byte = 0)
        Names.Add(QuestName)
        IDs.Add(ID)
        Icons.Add(Icon)
        Levels.Add(Level)
    End Sub
    Public IDs As ArrayList = New ArrayList
    Public Names As ArrayList = New ArrayList
    Public Icons As ArrayList = New ArrayList
    Public Levels As ArrayList = New ArrayList
End Class
Public Class TBaseTalk
    Public Overridable Sub OnGossipHello(ByRef c As CharacterObject, ByVal cGUID As ULong)

    End Sub
    Public Overridable Sub OnGossipSelect(ByRef c As CharacterObject, ByVal cGUID As ULong, ByVal Selected As Integer)

    End Sub
    Public Overridable Function OnQuestStatus(ByRef c As CharacterObject, ByVal cGUID As ULong) As Integer
        Return QuestgiverStatus.DIALOG_STATUS_NONE
    End Function
    Public Overridable Function OnQuestHello(ByRef c As CharacterObject, ByVal cGUID As ULong) As Boolean
        Return True
    End Function
End Class

Public Enum MenuIcon As Integer
    MENUICON_GOSSIP = &H0
    MENUICON_VENDOR = &H1
    MENUICON_TAXI = &H2
    MENUICON_TRAINER = &H3
    MENUICON_HEALER = &H4
    MENUICON_BINDER = &H5
    MENUICON_BANKER = &H6
    MENUICON_PETITION = &H7
    MENUICON_TABARD = &H8
    MENUICON_BATTLEMASTER = &H9
    MENUICON_AUCTIONER = &HA
    MENUICON_GOSSIP2 = &HB
    MENUICON_GOSSIP3 = &HC
End Enum
#End Region
