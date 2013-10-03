Imports System
Imports Microsoft.VisualBasic
imports mangosVB.WorldServer

Namespace Scripts
    Public Class TalkScript
        Inherits TBaseTalk

        Public Overrides Sub OnGossipHello(ByRef c As CharacterObject, ByVal cGUID As Long)
            Console.WriteLine("[{0}] TestNPC: GossipHello()", Format(TimeOfDay, "hh:mm:ss"))

            Dim npcText As New NPCText
            npcText.Count = 1
            npcText.TextID = 999999991
            npcText.TextLine1(0) = "Sample script driven NPCText."
            SendNPCText(c.Client, npcText)


            Dim npcMenu As New GossipMenu
            npcMenu.AddMenu("Sample menu item [Close action]", 1, 0)
            c.SendGossip(cGUID, 999999991, npcMenu)
        End Sub

        Public Overrides Sub OnGossipSelect(ByRef c As CharacterObject, ByVal cGUID As Long, ByVal Selected As Integer)
            Console.WriteLine("[{0}] TestNPC: GossipSelect()", Format(TimeOfDay, "hh:mm:ss"))
            If Selected = 0 Then
                c.SendGossipComplete()
            End If
        End Sub

        Public Overrides Function OnQuestStatus(ByRef c As CharacterObject, ByVal cGUID As Long) As Integer
            Console.WriteLine("[{0}] TestNPC: OnQuestStatus()", Format(TimeOfDay, "hh:mm:ss"))
        End Function

        Public Overrides Function OnQuestHello(ByRef c As CharacterObject, ByVal cGUID As Long) As Boolean
            Console.WriteLine("[{0}] TestNPC: OnQuestHello()", Format(TimeOfDay, "hh:mm:ss"))
        End Function

    End Class
End Namespace
