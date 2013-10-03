Imports System
imports mangosVB.WorldServer

Namespace Scripts

    Public Enum Guards As Integer
        Stormwind_Guard = 1423
        Stormwind_City_Guard = 68
        Stormwind_City_Patroller = 1976
        Darnassus_Sentinel = 4262
        Undercity_Guardian = 5624
        Teldrassil_Sentinel = 3571
        Silvermoon_City_Guardian = 16222
        Exodar_Peacekeeper = 16733
        Shield_of_Velen = 20674
        Orgrimmar_Grunt = 3296
        Bluffwatcher = 3084
        Brave_Wildrunner = 3222
        Brave_Cloudmane = 3224
        Brave_Darksky = 3220
        Brave_Leaping_Deer = 3219
        Brave_Dawn_Eagle = 3217
        Brave_Strongbash = 3215
        Brave_Swiftwind = 3218
        Brave_Rockhorn = 3221
        Brave_Rainchaser = 3223
        Brave_IronHorn = 3212
        Razor_Hill_Grunt = 5953
        Deathguard_Lundmark = 5725
        Deathguard_Terrence = 1738
        Deathguard_Burgess = 1652
        Deathguard_Cyrus = 1746
        Deathguard_Morris = 1745
        Deathguard_Lawrence = 1743
        Deathguard_Mort = 1744
        Deathguard_Dillinger = 1496
        Deathguard_Bartholomew = 1742
        Ironforge_Guard = 5595
        Ironforge_Mountaineer = 727
        Silvermoon_Guardian = 16221
        Azuremyst_Peacekeeper = 18038
    End Enum

    Public Enum Gossips As Integer
        GoldShire = 1
        StormWind = 2
        Darnassus = 3
        UnderCity = 4
        Dolonar = 5
        SilverMoonCity = 6
        Exodar = 7
        Orgrimmar = 8
        ThunderBluff = 9
        BloodHoof = 10
        RazorHill = 11
        Brill = 12
        IronForge = 13
        Khar = 14
        Falcon = 15
        AzureMyst = 16
    End Enum

    Public Class TalkScript
        Inherits TBaseTalk

        Public Overrides Sub OnGossipHello(ByRef c As CharacterObject, ByVal cGUID As ULong)
            'NOTE: This will have to be fixed to change according to character location
            Dim GuardGossip As Integer
            Dim npcText As New NPCText
            Dim npcMenu As New GossipMenu

            GuardGossip = GetGossips(WORLD_CREATUREs(cGUID).ID)

            Select Case GuardGossip

                Case Gossips.GoldShire
                    npcText.Count = 1
                    npcText.TextID = 99999999
                    npcText.Probability(0) = 1
                    npcText.TextLine1(0) = "What are you looking for?"
                    npcText.TextLine2(0) = "What are you looking for?"
                    SendNPCText(c.Client, npcText)

                    npcMenu.AddMenu("Bank", MenuIcon.MENUICON_GOSSIP)
                    npcMenu.AddMenu("Gryphon Master", MenuIcon.MENUICON_GOSSIP)
                    npcMenu.AddMenu("Guild Master", MenuIcon.MENUICON_GOSSIP)
                    npcMenu.AddMenu("Inn", MenuIcon.MENUICON_GOSSIP)
                    npcMenu.AddMenu("Stable Master", MenuIcon.MENUICON_GOSSIP)
                    npcMenu.AddMenu("Class Trainer", MenuIcon.MENUICON_GOSSIP)
                    npcMenu.AddMenu("Profession Trainer", MenuIcon.MENUICON_GOSSIP)
                    c.SendGossip(cGUID, 99999999, npcMenu)

                Case Gossips.StormWind
                    npcText.Count = 1
                    npcText.TextID = 99999999
                    npcText.Probability(0) = 1
                    npcText.TextLine1(0) = "What do you need directions to?"
                    npcText.TextLine2(0) = "What do you need directions to?"
                    SendNPCText(c.Client, npcText)

                    npcMenu.AddMenu("Auction House", MenuIcon.MENUICON_GOSSIP)
                    npcMenu.AddMenu("Bank of Stormwind", MenuIcon.MENUICON_GOSSIP)
                    npcMenu.AddMenu("Deeprun Tram", MenuIcon.MENUICON_GOSSIP)
                    npcMenu.AddMenu("The Inn", MenuIcon.MENUICON_GOSSIP)
                    npcMenu.AddMenu("Gryphon Master", MenuIcon.MENUICON_GOSSIP)
                    npcMenu.AddMenu("Guild Master", MenuIcon.MENUICON_GOSSIP)
                    npcMenu.AddMenu("Mailbox", MenuIcon.MENUICON_GOSSIP)
                    npcMenu.AddMenu("Stable Master", MenuIcon.MENUICON_GOSSIP)
                    npcMenu.AddMenu("Weapons Trainer", MenuIcon.MENUICON_GOSSIP)
                    npcMenu.AddMenu("Officers' Lounge", MenuIcon.MENUICON_GOSSIP)
                    npcMenu.AddMenu("Battlemaster", MenuIcon.MENUICON_GOSSIP)
                    npcMenu.AddMenu("Class Trainer", MenuIcon.MENUICON_GOSSIP)
                    npcMenu.AddMenu("Profession Trainer", MenuIcon.MENUICON_GOSSIP)
                    c.SendGossip(cGUID, 99999999, npcMenu)

                Case Else
                    npcText.Count = 1
                    npcText.TextID = 99999999
                    npcText.Probability(0) = 1
                    npcText.TextLine1(0) = "What are you looking for?"
                    npcText.TextLine2(0) = "What are you looking for?"
                    SendNPCText(c.Client, npcText)

                    npcMenu.AddMenu("Merchants", MenuIcon.MENUICON_GOSSIP)
                    npcMenu.AddMenu("The Bank", MenuIcon.MENUICON_GOSSIP)
                    npcMenu.AddMenu("The Inn", MenuIcon.MENUICON_GOSSIP)
                    npcMenu.AddMenu("Class Trainer", MenuIcon.MENUICON_GOSSIP)
                    npcMenu.AddMenu("Profession Trainer", MenuIcon.MENUICON_GOSSIP)
                    npcMenu.AddMenu("Suppliers", MenuIcon.MENUICON_GOSSIP)
                    npcMenu.AddMenu("The stable Master", MenuIcon.MENUICON_GOSSIP)
                    npcMenu.AddMenu("Gryphon Master", MenuIcon.MENUICON_GOSSIP)
                    npcMenu.AddMenu("Guild Master", MenuIcon.MENUICON_GOSSIP)
                    npcMenu.AddMenu("Auction House", MenuIcon.MENUICON_GOSSIP)
                    npcMenu.AddMenu("Mailboxes", MenuIcon.MENUICON_GOSSIP)
                    npcMenu.AddMenu("Weapons Trainer", MenuIcon.MENUICON_GOSSIP)
                    c.SendGossip(cGUID, 99999999, npcMenu)

            End Select
        End Sub

        Public Overrides Sub OnGossipSelect(ByRef c As CharacterObject, ByVal cGUID As ULong, ByVal Selected As Integer)
            ' TODO: This needs to be coded, it also will need to be changed according to character location
            Dim GuardGossip As Integer
            Dim npcText As New NPCText
            Dim npcMenu As New GossipMenu

            GuardGossip = GetGossips(WORLD_CREATUREs(cGUID).ID)

            Select Case GuardGossip
                Case Gossips.GoldShire
                    Select Case Selected
                        Case 0 ' First Menu Choice
                            Select Case c.MenuNumber
                                Case 0 ' First Menu - Bank
                                    c.SendGossip(cGUID, 4260)
                                    c.MenuNumber = 0
                                Case 1 ' Class Trainer Menu - Druid
                                    c.SendGossip(cGUID, 4265)
                                    c.MenuNumber = 0
                                Case 2 ' Profession Trainer Menu - Alchemy
                                    c.SendGossip(cGUID, 4274)
                                    c.SendPointOfInterest(-9057.04, 153.63, 6, 6, 0, "Alchemist Mallory")
                                    c.MenuNumber = 0
                            End Select
                        Case 1 ' Second Menu Choice
                            Select Case c.MenuNumber
                                Case 0 ' First Menu - Gryphon Master
                                    c.SendGossip(cGUID, 4261)
                                    c.MenuNumber = 0
                                Case 1 ' Class Trainer Menu - Hunter
                                    c.SendGossip(cGUID, 4266)
                                    c.MenuNumber = 0
                                Case 2 ' Profession Trainer Menu - Blacksmithing
                                    c.SendGossip(cGUID, 4275)
                                    c.SendPointOfInterest(-9456.58, 87.9, 6, 6, 0, "Smith Argus")
                                    c.MenuNumber = 0
                            End Select
                        Case 2 ' Third Menu Choice
                            Select Case c.MenuNumber
                                Case 0 ' First Menu - Guild Master
                                    c.SendGossip(cGUID, 4262)
                                    c.MenuNumber = 0
                                Case 1 ' Class Trainer Menu - Mage
                                    c.SendGossip(cGUID, 4268)
                                    c.SendPointOfInterest(-9471.12, 33.44, 6, 6, 0, "Zaldimar Wefhellt")
                                    c.MenuNumber = 0
                                Case 2 ' Profession Trainer Menu - Cooking
                                    c.SendGossip(cGUID, 4276)
                                    c.SendPointOfInterest(-9467.54, -3.16, 6, 6, 0, "Tomas")
                                    c.MenuNumber = 0
                            End Select
                        Case 3 ' Fourth Menu Choice
                            Select Case c.MenuNumber
                                Case 0 ' First Menu - Inn
                                    c.SendGossip(cGUID, 4263)
                                    c.SendPointOfInterest(-9459.34, 42.08, 99, 6, 0, "Lion's Pride Inn")
                                    c.MenuNumber = 0
                                Case 1 ' Class Trainer Menu - Paladin
                                    c.SendGossip(cGUID, 4269)
                                    c.SendPointOfInterest(-9469, 108.05, 6, 6, 0, "Brother Wilhelm")
                                    c.MenuNumber = 0
                                Case 2 ' Profession Trainer Menu - Enchanting
                                    c.SendGossip(cGUID, 4277)
                                    c.MenuNumber = 0
                            End Select
                        Case 4 ' Fifth Menu Choice
                            Select Case c.MenuNumber
                                Case 0 ' First Menu - Stable Master
                                    c.SendGossip(cGUID, 5983)
                                    c.SendPointOfInterest(-9466.62, 45.87, 99, 6, 0, "Erma")
                                    c.MenuNumber = 0
                                Case 1 ' Class Trainer Menu - Priest
                                    c.SendGossip(cGUID, 4267)
                                    c.SendPointOfInterest(-9461.07, 32.6, 6, 6, 0, "Priestess Josetta")
                                    c.MenuNumber = 0
                                Case 2 ' Profession Trainer Menu - Engineering
                                    c.SendGossip(cGUID, 4278)
                                    c.MenuNumber = 0
                            End Select
                        Case 5 ' Sixth Menu Choice
                            Select Case c.MenuNumber
                                Case 0 ' First Menu - Class Trainer
                                    npcText.Count = 1
                                    npcText.TextID = 99999999
                                    npcText.Probability(0) = 1
                                    npcText.TextLine1(0) = "There are quite a few trainers available in Elwynn. Which one are you looking for?"
                                    npcText.TextLine2(0) = "There are quite a few trainers available in Elwynn. Which one are you looking for?"
                                    SendNPCText(c.Client, npcText)

                                    npcMenu.AddMenu("Druid", MenuIcon.MENUICON_GOSSIP)
                                    npcMenu.AddMenu("Hunter", MenuIcon.MENUICON_GOSSIP)
                                    npcMenu.AddMenu("Mage", MenuIcon.MENUICON_GOSSIP)
                                    npcMenu.AddMenu("Paladin", MenuIcon.MENUICON_GOSSIP)
                                    npcMenu.AddMenu("Priest", MenuIcon.MENUICON_GOSSIP)
                                    npcMenu.AddMenu("Rogue", MenuIcon.MENUICON_GOSSIP)
                                    npcMenu.AddMenu("Shaman", MenuIcon.MENUICON_GOSSIP)
                                    npcMenu.AddMenu("Warlock", MenuIcon.MENUICON_GOSSIP)
                                    npcMenu.AddMenu("Warrior", MenuIcon.MENUICON_GOSSIP)
                                    c.MenuNumber = 1
                                    c.SendGossip(cGUID, 99999999, npcMenu)

                                Case 1 ' Class Trainer Menu - Rogue
                                    c.SendGossip(cGUID, 4270)
                                    c.SendPointOfInterest(-9465.13, 13.29, 6, 6, 0, "Keryn Sylvius")
                                    c.MenuNumber = 0
                                Case 2 ' Profession Trainer Menu - First Aid
                                    c.SendGossip(cGUID, 4279)
                                    c.SendPointOfInterest(-9456.82, 30.49, 6, 6, 0, "Michelle Belle")
                                    c.MenuNumber = 0
                            End Select
                        Case 6 ' Seventh Menu Choice
                            Select Case c.MenuNumber
                                Case 0 ' First Menu - Profession Trainer
                                    npcText.Count = 1
                                    npcText.TextID = 99999999
                                    npcText.Probability(0) = 1
                                    npcText.TextLine1(0) = "I know of a few people around here that practice a profession or two.  Which profession do you have in mind?"
                                    npcText.TextLine2(0) = "I know of a few people around here that practice a profession or two.  Which profession do you have in mind?"
                                    SendNPCText(c.Client, npcText)

                                    npcMenu.AddMenu("Alchemy", MenuIcon.MENUICON_GOSSIP)
                                    npcMenu.AddMenu("Blacksmithing", MenuIcon.MENUICON_GOSSIP)
                                    npcMenu.AddMenu("Cooking", MenuIcon.MENUICON_GOSSIP)
                                    npcMenu.AddMenu("Enchanting", MenuIcon.MENUICON_GOSSIP)
                                    npcMenu.AddMenu("Engineering", MenuIcon.MENUICON_GOSSIP)
                                    npcMenu.AddMenu("First Aid", MenuIcon.MENUICON_GOSSIP)
                                    npcMenu.AddMenu("Fishing", MenuIcon.MENUICON_GOSSIP)
                                    npcMenu.AddMenu("Herbalism", MenuIcon.MENUICON_GOSSIP)
                                    npcMenu.AddMenu("Leatherworking", MenuIcon.MENUICON_GOSSIP)
                                    npcMenu.AddMenu("Mining", MenuIcon.MENUICON_GOSSIP)
                                    npcMenu.AddMenu("Skinning", MenuIcon.MENUICON_GOSSIP)
                                    npcMenu.AddMenu("Tailoring", MenuIcon.MENUICON_GOSSIP)
                                    c.MenuNumber = 2
                                    c.SendGossip(cGUID, 99999999, npcMenu)

                                Case 1 ' Class Trainer Menu - Shaman
                                    c.SendGossip(cGUID, 3513)
                                    c.SendPointOfInterest(-9031.54, 549.87, 6, 6, 0, "Farseer Umbrua")
                                    c.MenuNumber = 0
                                Case 2 ' Profession Trainer Menu - Fishing
                                    c.SendGossip(cGUID, 4280)
                                    c.SendPointOfInterest(-9386.54, -118.73, 6, 6, 0, "Lee Brown")
                                    c.MenuNumber = 0
                            End Select
                        Case 7 ' Eighth Menu Choice
                            Select Case c.MenuNumber
                                Case 1 ' Class Trainer Menu - Warlock
                                    c.SendGossip(cGUID, 4272)
                                    c.SendPointOfInterest(-9473.21, -4.08, 6, 6, 0, "Maximillian Crowe")
                                    c.MenuNumber = 0
                                Case 2 ' Profession Trainer Menu - Herbalism
                                    c.SendGossip(cGUID, 4281)
                                    c.SendPointOfInterest(-9060.7, 149.23, 6, 6, 0, "Herbalist Pomeroy")
                                    c.MenuNumber = 0
                            End Select
                        Case 8 ' Ninth Menu Choice
                            Select Case c.MenuNumber
                                Case 1 ' Class Trainer Menu - Warrior
                                    c.SendGossip(cGUID, 4271)
                                    c.SendPointOfInterest(-9461.82, 109.5, 6, 6, 0, "Lyria Du Lac")
                                    c.MenuNumber = 0
                                Case 2 ' Profession Trainer Menu - Leatherworking
                                    c.SendGossip(cGUID, 4282)
                                    c.SendPointOfInterest(-9376.12, -75.23, 6, 6, 0, "Adele Fielder")
                                    c.MenuNumber = 0
                            End Select
                        Case 9 ' Tenth Menu Choice
                            Select Case c.MenuNumber
                                Case 2 ' Profession Trainer Menu - Mining
                                    c.SendGossip(cGUID, 4283)
                                    c.MenuNumber = 0
                            End Select
                        Case 10 ' Eleventh Menu Choice
                            Select Case c.MenuNumber
                                Case 2 ' Profession Trainer Menu - Skinning
                                    c.SendGossip(cGUID, 4284)
                                    c.SendPointOfInterest(-9536.91, -1212.76, 6, 6, 0, "Helene Peltskinner")
                                    c.MenuNumber = 0
                            End Select
                        Case 11 ' Twelvth Menu Choice
                            Select Case c.MenuNumber
                                Case 2 ' Profession Trainer Menu - Tailoring
                                    c.SendGossip(cGUID, 4285)
                                    c.SendPointOfInterest(-9376.12, -75.23, 6, 6, 0, "Eldrin")
                                    c.MenuNumber = 0
                            End Select
                    End Select

                Case Gossips.StormWind
                    Select Case Selected
                        Case 0 ' First Menu Choice
                            Select Case c.MenuNumber
                                Case 0 ' First Menu - Auction House
                                    c.SendGossip(cGUID, 3834)
                                    c.SendPointOfInterest(-8811.46, 667.46, 6, 6, 0, "Stormwind Auction House")
                                    c.MenuNumber = 0
                                Case 1 ' Class Trainer Menu - Druid
                                    c.SendGossip(cGUID, 902)
                                    c.SendPointOfInterest(-8751.0, 1124.5, 6, 6, 0, "The Park")
                                    c.MenuNumber = 0
                                Case 2 ' Profession Trainer Menu - Alchemy
                                    c.SendGossip(cGUID, 919)
                                    c.SendPointOfInterest(-8988.0, 759.6, 6, 6, 0, "Alchemy Needs")
                                    c.MenuNumber = 0
                            End Select
                        Case 1 ' Second Menu Choice
                            Select Case c.MenuNumber
                                Case 0 ' First Menu - Bank of Stormwind
                                    c.SendGossip(cGUID, 764)
                                    c.SendPointOfInterest(-8916.87, 622.87, 6, 6, 0, "Stormwind Bank")
                                    c.MenuNumber = 0
                                Case 1 ' Class Trainer Menu - Hunter
                                    c.SendGossip(cGUID, 905)
                                    c.SendPointOfInterest(-8413.0, 541.5, 6, 6, 0, "Hunter Lodge")
                                    c.MenuNumber = 0
                                Case 2 ' Profession Trainer Menu - Blacksmithing
                                    c.SendGossip(cGUID, 920)
                                    c.SendPointOfInterest(-8424.0, 616.9, 6, 6, 0, "Therum Deepforge")
                                    c.MenuNumber = 0
                            End Select
                        Case 2 ' Third Menu Choice
                            Select Case c.MenuNumber
                                Case 0 ' First Menu - Deeprun Tram
                                    c.SendGossip(cGUID, 3813)
                                    c.SendPointOfInterest(-8378.88, 554.23, 6, 6, 0, "The Deeprun Tram")
                                    c.MenuNumber = 0
                                Case 1 ' Class Trainer Menu - Mage
                                    c.SendGossip(cGUID, 899)
                                    c.SendPointOfInterest(-9012.0, 867.6, 6, 6, 0, "Wizard`s Sanctum")
                                    c.MenuNumber = 0
                                Case 2 ' Profession Trainer Menu - Cooking
                                    c.SendGossip(cGUID, 921)
                                    c.SendPointOfInterest(-8611.0, 364.6, 6, 6, 0, "Pig and Whistle Tavern")
                                    c.MenuNumber = 0
                            End Select
                        Case 3 ' Fourth Menu Choice
                            Select Case c.MenuNumber
                                Case 0 ' First Menu - The Inn
                                    c.SendGossip(cGUID, 3860)
                                    c.SendPointOfInterest(-8869.0, 675.4, 6, 6, 0, "The Gilded Rose")
                                    c.MenuNumber = 0
                                Case 1 ' Class Trainer Menu - Paladin
                                    c.SendGossip(cGUID, 904)
                                    c.SendPointOfInterest(-8577.0, 881.7, 6, 6, 0, "Cathedral Of Light")
                                    c.MenuNumber = 0
                                Case 2 ' Profession Trainer Menu - Enchanting
                                    c.SendGossip(cGUID, 941)
                                    c.SendPointOfInterest(-8858.0, 803.7, 6, 6, 0, "Lucan Cordell")
                                    c.MenuNumber = 0
                            End Select
                        Case 4 ' Fifth Menu Choice
                            Select Case c.MenuNumber
                                Case 0 ' First Menu - Gryphon Master
                                    c.SendGossip(cGUID, 879)
                                    c.SendPointOfInterest(-8837.0, 493.5, 6, 6, 0, "Stormwind Gryphon Master")
                                    c.MenuNumber = 0
                                Case 1 ' Class Trainer Menu - Priest
                                    c.SendGossip(cGUID, 903)
                                    c.SendPointOfInterest(-8512.0, 862.4, 6, 6, 0, "Cathedral Of Light")
                                    c.MenuNumber = 0
                                Case 2 ' Profession Trainer Menu - Engineering
                                    c.SendGossip(cGUID, 922)
                                    c.SendPointOfInterest(-8347.0, 644.1, 6, 6, 0, "Lilliam Sparkspindle")
                                    c.MenuNumber = 0
                            End Select
                        Case 5 ' Sixth Menu Choice
                            Select Case c.MenuNumber
                                Case 0 ' First Menu - Guild Master
                                    c.SendGossip(cGUID, 882)
                                    c.SendPointOfInterest(-8894.0, 611.2, 6, 6, 0, "Stormwind Vistor`s Center")
                                    c.MenuNumber = 0
                                Case 1 ' Class Trainer Menu - Rogue
                                    c.SendGossip(cGUID, 900)
                                    c.SendPointOfInterest(-8753.0, 367.8, 6, 6, 0, "Stormwind - Rogue House")
                                    c.MenuNumber = 0
                                Case 2 ' Profession Trainer Menu - First Aid
                                    c.SendGossip(cGUID, 923)
                                    c.SendPointOfInterest(-8513.0, 801.8, 6, 6, 0, "Shaina Fuller")
                                    c.MenuNumber = 0
                            End Select
                        Case 6 ' Seventh Menu Choice
                            Select Case c.MenuNumber
                                Case 0 ' First Menu - Mailbox
                                    c.SendGossip(cGUID, 3518)
                                    c.SendPointOfInterest(-8876.48, 649.18, 6, 6, 0, "Stormwind Mailbox")
                                    c.MenuNumber = 0
                                Case 1 ' Class Trainer Menu - Shaman
                                    c.SendGossip(cGUID, 10106)
                                    c.SendPointOfInterest(-9031.54, 549.87, 6, 6, 0, "Farseer Umbrua")
                                    c.MenuNumber = 0
                                Case 2 ' Profession Trainer Menu - Fishing
                                    c.SendGossip(cGUID, 940)
                                    c.SendPointOfInterest(-8803.0, 767.5, 6, 6, 0, "Arnold Leland")
                                    c.MenuNumber = 0
                            End Select
                        Case 7 ' Eighth Menu Choice
                            Select Case c.MenuNumber
                                Case 0 ' First Menu - Stable Master
                                    c.SendGossip(cGUID, 5984)
                                    c.SendPointOfInterest(-8433.0, 554.7, 6, 6, 0, "Jenova Stoneshield")
                                    c.MenuNumber = 0
                                Case 1 ' Class Trainer Menu - Warlock
                                    c.SendGossip(cGUID, 906)
                                    c.SendPointOfInterest(-8948.91, 998.35, 6, 6, 0, "The Slaughtered Lamb")
                                    c.MenuNumber = 0
                                Case 2 ' Profession Trainer Menu - Herbalism
                                    c.SendGossip(cGUID, 924)
                                    c.SendPointOfInterest(-8967.0, 779.5, 6, 6, 0, "Alchemy Needs")
                                    c.MenuNumber = 0
                            End Select
                        Case 8 ' Ninth Menu Choice
                            Select Case c.MenuNumber
                                Case 0 ' First Menu - Weapons Trainer
                                    c.SendGossip(cGUID, 4516)
                                    c.SendPointOfInterest(-8797.0, 612.8, 6, 6, 0, "Woo Ping")
                                    c.MenuNumber = 0
                                Case 1 ' Class Trainer Menu - Warrior
                                    c.SendGossip(cGUID, 901)
                                    c.SendPointOfInterest(-8714.14, 334.96, 6, 6, 0, "Stormwind Barracks")
                                    c.MenuNumber = 0
                                Case 2 ' Profession Trainer Menu - Leatherworking
                                    c.SendGossip(cGUID, 925)
                                    c.SendPointOfInterest(-8726.0, 477.4, 6, 6, 0, "The Protective Hide")
                                    c.MenuNumber = 0
                            End Select
                        Case 9 ' Tenth Menu Choice
                            Select Case c.MenuNumber
                                Case 0 ' First Menu - Officers' Lounge
                                    c.SendGossip(cGUID, 7047)
                                    c.SendPointOfInterest(-8759.92, 399.69, 6, 6, 0, "Champions` Hall")
                                    c.MenuNumber = 0
                                Case 2 ' Profession Trainer Menu - Mining
                                    c.SendGossip(cGUID, 927)
                                    c.SendPointOfInterest(-8434.0, 692.8, 6, 6, 0, "Gelman Stonehand")
                                    c.MenuNumber = 0
                            End Select
                        Case 10 ' Eleventh Menu Choice
                            Select Case c.MenuNumber
                                Case 0 ' First Menu - Battlemaster
                                    c.SendGossip(cGUID, 7499)
                                    c.SendPointOfInterest(-8393.62, 274.21, 6, 6, 0, "Battlemasters Stormwind")
                                    c.MenuNumber = 0
                                Case 2 ' Profession Trainer Menu - Skinning
                                    c.SendGossip(cGUID, 928)
                                    c.SendPointOfInterest(-8716.0, 469.4, 6, 6, 0, "The Protective Hide")
                                    c.MenuNumber = 0
                            End Select
                        Case 11 ' Twelvth Menu Choice
                            Select Case c.MenuNumber
                                Case 0 ' Class Trainer
                                    npcText.Count = 1
                                    npcText.TextID = 99999999
                                    npcText.Probability(0) = 1
                                    npcText.TextLine1(0) = "What class trainer are you looking for?"
                                    npcText.TextLine2(0) = "What class trainer are you looking for?"
                                    SendNPCText(c.Client, npcText)

                                    npcMenu.AddMenu("Druid", MenuIcon.MENUICON_GOSSIP)
                                    npcMenu.AddMenu("Hunter", MenuIcon.MENUICON_GOSSIP)
                                    npcMenu.AddMenu("Mage", MenuIcon.MENUICON_GOSSIP)
                                    npcMenu.AddMenu("Paladin", MenuIcon.MENUICON_GOSSIP)
                                    npcMenu.AddMenu("Priest", MenuIcon.MENUICON_GOSSIP)
                                    npcMenu.AddMenu("Rogue", MenuIcon.MENUICON_GOSSIP)
                                    npcMenu.AddMenu("Shaman", MenuIcon.MENUICON_GOSSIP)
                                    npcMenu.AddMenu("Warlock", MenuIcon.MENUICON_GOSSIP)
                                    npcMenu.AddMenu("Warrior", MenuIcon.MENUICON_GOSSIP)
                                    c.MenuNumber = 1
                                    c.SendGossip(cGUID, 99999999, npcMenu)
                                Case 2 ' Profession Trainer Menu - Tailoring
                                    c.SendGossip(cGUID, 929)
                                    c.SendPointOfInterest(-8938.0, 800.7, 6, 6, 0, "Duncan`s Textiles")
                                    c.MenuNumber = 0
                            End Select
                        Case 12 ' Profession Trainer
                            npcText.Count = 1
                            npcText.TextID = 99999999
                            npcText.Probability(0) = 1
                            npcText.TextLine1(0) = "Which profession trainer are you looking for?"
                            npcText.TextLine2(0) = "Which profession trainer are you looking for?"
                            SendNPCText(c.Client, npcText)

                            npcMenu.AddMenu("Alchemy", MenuIcon.MENUICON_GOSSIP)
                            npcMenu.AddMenu("Blacksmithing", MenuIcon.MENUICON_GOSSIP)
                            npcMenu.AddMenu("Cooking", MenuIcon.MENUICON_GOSSIP)
                            npcMenu.AddMenu("Enchanting", MenuIcon.MENUICON_GOSSIP)
                            npcMenu.AddMenu("Engineering", MenuIcon.MENUICON_GOSSIP)
                            npcMenu.AddMenu("First Aid", MenuIcon.MENUICON_GOSSIP)
                            npcMenu.AddMenu("Fishing", MenuIcon.MENUICON_GOSSIP)
                            npcMenu.AddMenu("Herbalism", MenuIcon.MENUICON_GOSSIP)
                            npcMenu.AddMenu("Leatherworking", MenuIcon.MENUICON_GOSSIP)
                            npcMenu.AddMenu("Mining", MenuIcon.MENUICON_GOSSIP)
                            npcMenu.AddMenu("Skinning", MenuIcon.MENUICON_GOSSIP)
                            npcMenu.AddMenu("Tailoring", MenuIcon.MENUICON_GOSSIP)
                            c.MenuNumber = 2
                            c.SendGossip(cGUID, 99999999, npcMenu)
                    End Select

                Case Else
                    c.MenuNumber = 0
                    Select Case Selected
                        Case 0 ' Merchants
                            c.SendGossipComplete()
                        Case 1 ' The Bank
                            c.SendGossipComplete()
                        Case 2 ' The Inn
                            c.SendGossipComplete()
                        Case 3 ' Class trainers
                            c.SendGossipComplete()
                        Case 4 ' Profession trainers
                            c.SendGossipComplete()
                        Case 5 ' Suppliers
                            c.SendGossipComplete()
                        Case 6 ' The stable masters
                            c.SendGossipComplete()
                        Case 7 ' Gryphon masters
                            c.SendGossipComplete()
                        Case 8 ' Guild masters
                            c.SendGossipComplete()
                        Case 9 ' Auction House
                            c.SendGossipComplete()
                        Case 10 ' Mailboxes
                            c.SendGossipComplete()
                        Case 11 ' Weapons Trainers
                            c.SendGossipComplete()
                    End Select

            End Select

        End Sub

        Public Function GetGossips(ByVal Guard As Integer) As Integer

            Select Case Guard
                Case Guards.Azuremyst_Peacekeeper
                    GetGossips = Gossips.AzureMyst

                Case Guards.Bluffwatcher
                    GetGossips = Gossips.ThunderBluff

                Case Guards.Brave_Cloudmane, Guards.Brave_Darksky, Guards.Brave_Dawn_Eagle, Guards.Brave_IronHorn, Guards.Brave_Leaping_Deer, Guards.Brave_Rainchaser, Guards.Brave_Rockhorn, Guards.Brave_Strongbash, Guards.Brave_Swiftwind, Guards.Brave_Wildrunner
                    GetGossips = Gossips.BloodHoof

                Case Guards.Darnassus_Sentinel
                    GetGossips = Gossips.Darnassus

                Case Guards.Deathguard_Bartholomew, Guards.Deathguard_Burgess, Guards.Deathguard_Cyrus, Guards.Deathguard_Dillinger, Guards.Deathguard_Lawrence, Guards.Deathguard_Lundmark, Guards.Deathguard_Morris, Guards.Deathguard_Mort, Guards.Deathguard_Terrence
                    GetGossips = Gossips.Brill

                Case Guards.Exodar_Peacekeeper, Guards.Shield_of_Velen
                    GetGossips = Gossips.Exodar

                Case Guards.Ironforge_Guard
                    GetGossips = Gossips.IronForge

                Case Guards.Ironforge_Mountaineer
                    GetGossips = Gossips.Khar

                Case Guards.Orgrimmar_Grunt
                    GetGossips = Gossips.Orgrimmar

                Case Guards.Razor_Hill_Grunt
                    GetGossips = Gossips.RazorHill

                Case Guards.Silvermoon_City_Guardian
                    GetGossips = Gossips.SilverMoonCity

                Case Guards.Silvermoon_Guardian
                    GetGossips = Gossips.Falcon

                Case Guards.Stormwind_City_Guard, Guards.Stormwind_City_Patroller
                    GetGossips = Gossips.StormWind

                Case Guards.Stormwind_Guard
                    GetGossips = Gossips.GoldShire

                Case Else
                    GetGossips = 0

            End Select

        End Function
    End Class
End Namespace