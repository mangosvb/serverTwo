' Here are listed sctipted area triggers, called on packet recv.
' All not scripted triggers are supposed to be teleport trigers,
' handled by the core.
'
'Last  update: 08.03.2007

Imports System
Imports Microsoft.VisualBasic
imports mangosVB.WorldServer

Namespace Scripts
	Public Module AreaTriggers

		'Area-52 Neuralyzer
		Public Sub HandleAreaTrigger_4422(ByVal GUID as ULong)
			If CHARACTERs(GUID).HaveAura(34400) = False Then CHARACTERs(GUID).CastOnSelf(34400)
		End Sub
		Public Sub HandleAreaTrigger_4466(ByVal GUID as ULong)
			If CHARACTERs(GUID).HaveAura(34400) = False Then CHARACTERs(GUID).CastOnSelf(34400)
		End Sub
		Public Sub HandleAreaTrigger_4471(ByVal GUID as ULong)
			If CHARACTERs(GUID).HaveAura(34400) = False Then CHARACTERs(GUID).CastOnSelf(34400)
		End Sub
		Public Sub HandleAreaTrigger_4472(ByVal GUID as ULong)
			If CHARACTERs(GUID).HaveAura(34400) = False Then CHARACTERs(GUID).CastOnSelf(34400)
		End Sub

	End Module
End Namespace
