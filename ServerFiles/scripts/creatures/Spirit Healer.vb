Imports System
imports mangosVB.WorldServer

Namespace Scripts
	Public Class TalkScript
		Inherits TBaseTalk

        'NOTE: Spirit Healers must have flag UNIT_NPC_FLAG_SPIRITHEALER

		Public Overrides Sub OnGossipHello(ByRef c As CharacterObject, cGUID as ULong)

			'Dim response As New PacketClass(OPCODES.SMSG_SPIRIT_HEALER_CONFIRM)
			'response.AddInt64(cGUID)
			'c.Client.Send(response)
			'response.Dispose()
			'Exit Sub

			Dim npcText As New NPCText
			npcText.Count=1
			npcText.TextID=34
			npcText.TextLine1(0)="It is not yet your time. I shall aid your journey back to the realm of the living... for a price."
			SendNPCText(c.Client,npcText)


			Dim npcMenu As New GossipMenu
            npcMenu.AddMenu("Bring me back to life.", 4, 0)
            c.SendGossip(cGUID, 34, npcMenu)
		End Sub

		Public Overrides Sub OnGossipSelect(ByRef c As CharacterObject, ByVal cGUID As ULong, ByVal Selected As Integer)
			if Selected = 0 then
				Dim response As New PacketClass(OPCODES.SMSG_SPIRIT_HEALER_CONFIRM)
				response.AddInt64(cGUID)
				c.Client.Send(response)
				response.Dispose()

				c.SendGossipComplete()
			end if
		End Sub
		
	End Class
End Namespace