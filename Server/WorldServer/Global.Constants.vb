Public Module Constants
    Public Function GuidIsCreature(ByVal GUID As ULong) As Boolean
        If GuidHIGH(GUID) = GUID_UNIT Then Return True
        Return False
    End Function
    Public Function GuidIsPet(ByVal GUID As ULong) As Boolean
        If GuidHIGH(GUID) = GUID_PET Then Return True
        Return False
    End Function
    Public Function GuidIsItem(ByVal GUID As ULong) As Boolean
        If GuidHIGH(GUID) = GUID_ITEM Then Return True
        Return False
    End Function
    Public Function GuidIsGameObject(ByVal GUID As ULong) As Boolean
        If GuidHIGH(GUID) = GUID_GAMEOBJECT Then Return True
        Return False
    End Function
    Public Function GuidIsDnyamicObject(ByVal GUID As ULong) As Boolean
        If GuidHIGH(GUID) = GUID_DYNAMICOBJECT Then Return True
        Return False
    End Function
    Public Function GuidIsTransport(ByVal GUID As ULong) As Boolean
        If GuidHIGH(GUID) = GUID_TRANSPORT Then Return True
        Return False
    End Function
    Public Function GuidIsCorpse(ByVal GUID As ULong) As Boolean
        If GuidHIGH(GUID) = GUID_CORPSE Then Return True
        Return False
    End Function
    Public Function GuidIsPlayer(ByVal GUID As ULong) As Boolean
        If GuidHIGH(GUID) = GUID_PLAYER Then Return True
        Return False
    End Function
    Public Function GuidHIGH(ByVal GUID As ULong) As ULong
        'If GuidIsPlayer(GUID) Then Return GUID_PLAYER
        'If GuidIsCreature(GUID) Then Return GUID_UNIT
        'If GuidIsItem(GUID) Then Return GUID_ITEM
        'If GuidIsGameObject(GUID) Then Return GUID_GAMEOBJECT
        'If GuidIsCorpse(GUID) Then Return GUID_CORPSE
        'Return 0
        Return (GUID And GUID_MASK_HIGH)
    End Function
    Public Function GuidLOW(ByVal GUID As ULong) As UInteger
        'If GuidIsPlayer(GUID) Then Return GUID - GUID_PLAYER
        'If GuidIsCreature(GUID) Then Return GUID - GUID_UNIT
        'If GuidIsItem(GUID) Then Return GUID - GUID_ITEM
        'If GuidIsGameObject(GUID) Then Return GUID - GUID_GAMEOBJECT
        'If GuidIsCorpse(GUID) Then Return GUID - GUID_CORPSE
        'Return 0
        Return (GUID And GUID_MASK_LOW)
    End Function
    Public Function GetBuildRevision() As UInteger
        Return Common.RevisionReader.GetBuildRevision()
    End Function

    Public Const GUID_ITEM As ULong = &H4000000000000000
    Public Const GUID_CONTAINER As ULong = &H4000000000000000
    Public Const GUID_PLAYER As ULong = &H0
    Public Const GUID_GAMEOBJECT As ULong = &HF110000000000000UL
    Public Const GUID_TRANSPORT As ULong = &HF120000000000000UL
    Public Const GUID_UNIT As ULong = &HF130000000000000UL
    Public Const GUID_PET As ULong = &HF140000000000000UL
    Public Const GUID_DYNAMICOBJECT As ULong = &HF100000000000000UL
    Public Const GUID_CORPSE As ULong = &HF101000000000000UL
    Public Const GUID_MO_TRANSPORT As ULong = &H1FC0000000000000UL
    Public Const GUID_MASK_LOW As UInteger = &HFFFFFFFFUI
    Public Const GUID_MASK_HIGH As ULong = &HFFFFFFFF00000000UL

    Public Const MAX_FRIENDS_ON_LIST As Byte = 50
    Public Const MAX_IGNORES_ON_LIST As Byte = 25
    Public Const DEFAULT_DISTANCE_VISIBLE As Single = 155.8
    Public Const DEFAULT_DISTANCE_DETECTION As Single = 7

    Public Const DEFAULT_LOCK_TIMEOUT As Integer = 1000
    Public Const DEFAULT_INSTANCE_EXPIRE_TIME As Integer = 3600              '1 hour
    Public Const DEFAULT_BATTLEFIELD_EXPIRE_TIME As Integer = 3600 * 24      '24 hours

    Public SERVER_CONFIG_DISABLED_CLASSES() As Boolean = {False, False, False, False, False, False, False, False, False, True, False}
    Public SERVER_CONFIG_DISABLED_RACES() As Boolean = {False, False, False, False, False, False, False, False, True, False, False}

    Public Const UNIT_NORMAL_WALK_SPEED As Single = 2.5F
    Public Const UNIT_NORMAL_RUN_SPEED As Single = 7.0F
    Public Const UNIT_NORMAL_SWIM_SPEED As Single = 4.722222F
    Public Const UNIT_NORMAL_SWIM_BACK_SPEED As Single = 2.5F
    Public Const UNIT_NORMAL_WALK_BACK_SPEED As Single = 4.5F
    Public Const UNIT_NORMAL_TURN_RATE As Single = Math.PI
    Public Const UNIT_NORMAL_FLY_SPEED As Single = 7.0F
    Public Const UNIT_NORMAL_FLY_BACK_SPEED As Single = 4.5F
    Public Const UNIT_NORMAL_TAXI_SPEED As Single = 32.0F
    Public Const UNIT_NORMAL_PITCH_SPEED As Single = 7

    Public Const PLAYER_VISIBLE_ITEM_SIZE As Integer = 18
    Public Const PLAYER_SKILL_INFO_SIZE As Integer = 384 - 1
    Public Const PLAYER_EXPLORED_ZONES_SIZE As Integer = 64 - 1

    Public Const FIELD_MASK_SIZE_PLAYER As Integer = ((EPlayerFields.PLAYER_END + 32) \ 32) * 32
    Public Const FIELD_MASK_SIZE_UNIT As Integer = ((EUnitFields.UNIT_END + 32) \ 32) * 32
    Public Const FIELD_MASK_SIZE_GAMEOBJECT As Integer = ((EGameObjectFields.GAMEOBJECT_END + 32) \ 32) * 32
    Public Const FIELD_MASK_SIZE_DYNAMICOBJECT As Integer = ((EDynamicObjectFields.DYNAMICOBJECT_END + 32) \ 32) * 32
    Public Const FIELD_MASK_SIZE_ITEM As Integer = ((EContainerFields.CONTAINER_END + 32) \ 32) * 32
    Public Const FIELD_MASK_SIZE_CORPSE As Integer = ((ECorpseFields.CORPSE_END + 32) \ 32) * 32

    Public Const EQUIPMENT_SLOT_START As Byte = 0
    Public Const EQUIPMENT_SLOT_HEAD As Byte = 0
    Public Const EQUIPMENT_SLOT_NECK As Byte = 1
    Public Const EQUIPMENT_SLOT_SHOULDERS As Byte = 2
    Public Const EQUIPMENT_SLOT_BODY As Byte = 3
    Public Const EQUIPMENT_SLOT_CHEST As Byte = 4
    Public Const EQUIPMENT_SLOT_WAIST As Byte = 5
    Public Const EQUIPMENT_SLOT_LEGS As Byte = 6
    Public Const EQUIPMENT_SLOT_FEET As Byte = 7
    Public Const EQUIPMENT_SLOT_WRISTS As Byte = 8
    Public Const EQUIPMENT_SLOT_HANDS As Byte = 9
    Public Const EQUIPMENT_SLOT_FINGER1 As Byte = 10
    Public Const EQUIPMENT_SLOT_FINGER2 As Byte = 11
    Public Const EQUIPMENT_SLOT_TRINKET1 As Byte = 12
    Public Const EQUIPMENT_SLOT_TRINKET2 As Byte = 13
    Public Const EQUIPMENT_SLOT_BACK As Byte = 14
    Public Const EQUIPMENT_SLOT_MAINHAND As Byte = 15
    Public Const EQUIPMENT_SLOT_OFFHAND As Byte = 16
    Public Const EQUIPMENT_SLOT_RANGED As Byte = 17
    Public Const EQUIPMENT_SLOT_TABARD As Byte = 18
    Public Const EQUIPMENT_SLOT_END As Byte = 19

    Public Const INVENTORY_SLOT_BAG_START As Byte = 19
    Public Const INVENTORY_SLOT_BAG_1 As Byte = 19
    Public Const INVENTORY_SLOT_BAG_2 As Byte = 20
    Public Const INVENTORY_SLOT_BAG_3 As Byte = 21
    Public Const INVENTORY_SLOT_BAG_4 As Byte = 22
    Public Const INVENTORY_SLOT_BAG_END As Byte = 23

    Public Const INVENTORY_SLOT_ITEM_START As Byte = 23
    Public Const INVENTORY_SLOT_ITEM_1 As Byte = 23
    Public Const INVENTORY_SLOT_ITEM_2 As Byte = 24
    Public Const INVENTORY_SLOT_ITEM_3 As Byte = 25
    Public Const INVENTORY_SLOT_ITEM_4 As Byte = 26
    Public Const INVENTORY_SLOT_ITEM_5 As Byte = 27
    Public Const INVENTORY_SLOT_ITEM_6 As Byte = 28
    Public Const INVENTORY_SLOT_ITEM_7 As Byte = 29
    Public Const INVENTORY_SLOT_ITEM_8 As Byte = 30
    Public Const INVENTORY_SLOT_ITEM_9 As Byte = 31
    Public Const INVENTORY_SLOT_ITEM_10 As Byte = 32
    Public Const INVENTORY_SLOT_ITEM_11 As Byte = 33
    Public Const INVENTORY_SLOT_ITEM_12 As Byte = 34
    Public Const INVENTORY_SLOT_ITEM_13 As Byte = 35
    Public Const INVENTORY_SLOT_ITEM_14 As Byte = 36
    Public Const INVENTORY_SLOT_ITEM_15 As Byte = 37
    Public Const INVENTORY_SLOT_ITEM_16 As Byte = 38
    Public Const INVENTORY_SLOT_ITEM_END As Byte = 39

    Public Const BANK_SLOT_ITEM_START As Byte = 39
    Public Const BANK_SLOT_ITEM_1 As Byte = 39
    Public Const BANK_SLOT_ITEM_2 As Byte = 40
    Public Const BANK_SLOT_ITEM_3 As Byte = 41
    Public Const BANK_SLOT_ITEM_4 As Byte = 42
    Public Const BANK_SLOT_ITEM_5 As Byte = 43
    Public Const BANK_SLOT_ITEM_6 As Byte = 44
    Public Const BANK_SLOT_ITEM_7 As Byte = 45
    Public Const BANK_SLOT_ITEM_8 As Byte = 46
    Public Const BANK_SLOT_ITEM_9 As Byte = 47
    Public Const BANK_SLOT_ITEM_10 As Byte = 48
    Public Const BANK_SLOT_ITEM_11 As Byte = 49
    Public Const BANK_SLOT_ITEM_12 As Byte = 50
    Public Const BANK_SLOT_ITEM_13 As Byte = 51
    Public Const BANK_SLOT_ITEM_14 As Byte = 52
    Public Const BANK_SLOT_ITEM_15 As Byte = 53
    Public Const BANK_SLOT_ITEM_16 As Byte = 54
    Public Const BANK_SLOT_ITEM_17 As Byte = 55
    Public Const BANK_SLOT_ITEM_18 As Byte = 56
    Public Const BANK_SLOT_ITEM_19 As Byte = 57
    Public Const BANK_SLOT_ITEM_20 As Byte = 58
    Public Const BANK_SLOT_ITEM_21 As Byte = 59
    Public Const BANK_SLOT_ITEM_22 As Byte = 60
    Public Const BANK_SLOT_ITEM_23 As Byte = 61
    Public Const BANK_SLOT_ITEM_24 As Byte = 62
    Public Const BANK_SLOT_ITEM_25 As Byte = 63
    Public Const BANK_SLOT_ITEM_26 As Byte = 64
    Public Const BANK_SLOT_ITEM_27 As Byte = 65
    Public Const BANK_SLOT_ITEM_28 As Byte = 66
    Public Const BANK_SLOT_ITEM_END As Byte = 67

    Public Const BANK_SLOT_BAG_START As Byte = 67
    Public Const BANK_SLOT_BAG_1 As Byte = 67
    Public Const BANK_SLOT_BAG_2 As Byte = 68
    Public Const BANK_SLOT_BAG_3 As Byte = 69
    Public Const BANK_SLOT_BAG_4 As Byte = 70
    Public Const BANK_SLOT_BAG_5 As Byte = 71
    Public Const BANK_SLOT_BAG_6 As Byte = 72
    Public Const BANK_SLOT_BAG_7 As Byte = 73
    Public Const BANK_SLOT_BAG_END As Byte = 74

    Public Const BUYBACK_SLOT_START As Byte = 74
    Public Const BUYBACK_SLOT_1 As Byte = 74
    Public Const BUYBACK_SLOT_2 As Byte = 75
    Public Const BUYBACK_SLOT_3 As Byte = 76
    Public Const BUYBACK_SLOT_4 As Byte = 77
    Public Const BUYBACK_SLOT_5 As Byte = 78
    Public Const BUYBACK_SLOT_6 As Byte = 79
    Public Const BUYBACK_SLOT_7 As Byte = 80
    Public Const BUYBACK_SLOT_8 As Byte = 81
    Public Const BUYBACK_SLOT_9 As Byte = 82
    Public Const BUYBACK_SLOT_10 As Byte = 83
    Public Const BUYBACK_SLOT_11 As Byte = 84
    Public Const BUYBACK_SLOT_12 As Byte = 85
    Public Const BUYBACK_SLOT_END As Byte = 86

    Public Const KEYRING_SLOT_START As Byte = 86
    Public Const KEYRING_SLOT_END As Byte = 118
End Module

Enum AuthLoginCodes
    CHAR_LOGIN_FAILED = 0                       'Login failed
    CHAR_LOGIN_NO_WORLD = 1                     'World server is down
    CHAR_LOGIN_DUPLICATE_CHARACTER = 2          'A character with that name already exists
    CHAR_LOGIN_NO_INSTANCES = 3                 'No instance servers are available
    CHAR_LOGIN_DISABLED = 4                     'Login for that race and/or class is currently disabled
    CHAR_LOGIN_NO_CHARACTER = 5                 'Character not found
    CHAR_LOGIN_LOCKED_FOR_TRANSFER = 6
    CHAR_LOGIN_LOCKED_BY_BILLING = 7
End Enum
Enum AuthResponseCodes
    RESPONSE_SUCCESS = &H0                      'Success
    RESPONSE_FAILURE = &H1                      'Failure
    RESPONSE_CANCELLED = &H2                    'Cancelled
    RESPONSE_DISCONNECTED = &H3                 'Disconnected from server
    RESPONSE_FAILED_TO_CONNECT = &H4            'Failed to connect
    RESPONSE_CONNECTED = &H5                    'Connected
    RESPONSE_VERSION_MISMATCH = &H6             'Wrong client version

    CSTATUS_CONNECTING = &H7                    'Connecting to server...
    CSTATUS_NEGOTIATING_SECURITY = &H8          'Negotiating Security
    CSTATUS_NEGOTIATION_COMPLETE = &H9          'Security negotiation complete
    CSTATUS_NEGOTIATION_FAILED = &HA            'Security negotiation failed
    CSTATUS_AUTHENTICATING = &HB                'Authenticating

    AUTH_OK = &HC                               'Authentication successful
    AUTH_FAILED = &HD                           'Authentication failed
    AUTH_REJECT = &HE                           'Rejected - please contact customer support
    AUTH_BAD_SERVER_PROOF = &HF                 'Server is not valid
    AUTH_UNAVAILABLE = &H10                     'System unavailable - please try again later
    AUTH_SYSTEM_ERROR = &H11                    'System error
    AUTH_BILLING_ERROR = &H12                   'Billing system error
    AUTH_BILLING_EXPIRED = &H13                 'Account billing has expired
    AUTH_VERSION_MISMATCH = &H14                'Wrong client version
    AUTH_UNKNOWN_ACCOUNT = &H15                 'Unknown account
    AUTH_INCORRECT_PASSWORD = &H16              'Incorrect password
    AUTH_SESSION_EXPIRED = &H17                 'Session expired
    AUTH_SERVER_SHUTTING_DOWN = &H18            'Server shutting down
    AUTH_ALREADY_LOGGING_IN = &H19              'Already logging in
    AUTH_LOGIN_SERVER_NOT_FOUND = &H1A          'Invalid login server
    AUTH_WAIT_QUEUE = &H1B                      'Position in queue - 0
    AUTH_BANNED = &H1C                          'This account has been banned
    AUTH_ALREADY_ONLINE = &H1D                  'This character is still logged on
    AUTH_NO_TIME = &H1E                         'Your WoW subscription has expired
    AUTH_DB_BUSY = &H1F                         'This session has timed out
    AUTH_SUSPENDED = &H20                       'This account has been temporarily suspended
    AUTH_PARENTAL_CONTROL = &H21                'Access to this account blocked by parental controls
    AUTH_LOCKED_ENFORCED = &H22                 'You have applied a lock to your account.

    REALM_LIST_IN_PROGRESS = &H23               'Retrieving realm list
    REALM_LIST_SUCCESS = &H24                   'Realm list retrieved
    REALM_LIST_FAILED = &H25                    'Unable to connect to realm list server
    REALM_LIST_INVALID = &H26                   'Invalid realm list
    REALM_LIST_REALM_NOT_FOUND = &H27           'Realm is down

    ACCOUNT_CREATE_IN_PROGRESS = &H28           'Creating account
    ACCOUNT_CREATE_SUCCESS = &H29               'Account created
    ACCOUNT_CREATE_FAILED = &H2A                'Account creation failed

    CHAR_LIST_RETRIEVED = &H2B                  'Retrieving character list
    CHAR_LIST_SUCCESS = &H2B                    'Character list retrieved
    CHAR_LIST_FAILED = &H2D                     'Error retrieving character list

    CHAR_CREATE_IN_PROGRESS = &H2E              'Creating character
    CHAR_CREATE_SUCCESS = &H2F                  'Character created
    CHAR_CREATE_ERROR = &H30                    'Error creating character
    CHAR_CREATE_FAILED = &H31                   'Character creation failed
    CHAR_CREATE_NAME_IN_USE = &H32              'That name is unavailable
    CHAR_CREATE_DISABLED = &H33                 'Creation of that race/class is disabled
    CHAR_CREATE_PVP_TEAMS_VIOLATION = &H34      'You cannot have both horde and alliance character at pvp realm
    CHAR_CREATE_SERVER_LIMIT = &H35             'You already have maximum number of characters
    CHAR_CREATE_ACCOUNT_LIMIT = &H36            'You already have maximum number of characters
    CHAR_CREATE_SERVER_QUEUE = &H37             'The server is currently queued
    CHAR_CREATE_ONLY_EXISTING = &H38            'Only players who have characters on this realm..
    CHAR_CREATE_EXPANSION = &H39
    CHAR_CREATE_EXPANSION_CLASS = &H3A            'Creation of that race requires an account that has been upgraded to the approciate expansion
    CHAR_CREATE_NEED_LVL_55_CHAR = &H3B
    CHAR_CREATE_UNIQUE_CLASS_LIMIT = &H3C

    CHAR_DELETE_IN_PROGRESS = &H3D         'Deleting character
    CHAR_DELETE_SUCCESS = &H3E                  'Character deleted
    CHAR_DELETE_FAILED = &H3F                   'Char deletion failed
    CHAR_DELETE_FAILED_LOCKED_FOR_TRANSFER = &H40 'You cannot log in until the character update process is complete
    CHAR_DELETE_FAILED_GUILD_LEADER = &H41        'This character is Guild Master and cannot be deleted
    CHAR_DELETE_FAILED_ARENA_CAPTAIN = &H42    'This character is Arena Captain and cannot be deleted

    CHAR_LOGIN_IN_PROGRESS = &H43               'Entering the World of Warcraft
    CHAR_LOGIN_SUCCESS = &H44                   'Login successful
    CHAR_LOGIN_NO_WORLD = &H45                  'World server is down
    CHAR_LOGIN_DUPLICATE_CHARACTER = &H46       'A character with that name already exists
    CHAR_LOGIN_NO_INSTANCES = &H47              'No instance servers are available
    CHAR_LOGIN_FAILED = &H48                    'Login failed
    CHAR_LOGIN_DISABLED = &H49                  'Login for that race and/or class is currently disabled
    CHAR_LOGIN_NO_CHARACTER = &H4A              'Character not found
    CHAR_LOGIN_LOCKED_FOR_TRANSFER = &H4B
    CHAR_LOGIN_LOCKED_BY_BILLING = &H4C

    CHAR_NAME_SUCCESS = &H4D
    CHAR_NAME_FAILURE = &H4E                    'Invalid character name
    CHAR_NAME_NO_NAME = &H4F                    'Enter a name for your character
    CHAR_NAME_TOO_SHORT = &H50                  'Names must be atleast 2 characters long
    CHAR_NAME_TOO_LONG = &H51                   'Names must be no more then 12 characters
    CHAR_NAME_INVALID_CHARACTER = &H52          'Names can only contain letters
    CHAR_NAME_MIXED_LANGUAGES = &H53            'Names must contain only one language
    CHAR_NAME_PROFANE = &H54                    'That name contains mature language
    CHAR_NAME_RESERVED = &H55                   'That name is unavailable
    CHAR_NAME_INVALID_APOSTROPHE = &H56         'You cannot use an apostrophe
    CHAR_NAME_MULTIPLE_APOSTROPHES = &H57       'You can only have one apostrophe
    CHAR_NAME_THREE_CONSECUTIVE = &H58          'You cannot use the same letter three times consecutively
    CHAR_NAME_INVALID_SPACE = &H59              'You cannot use space as the first or last character of your name
    CHAR_NAME_CONSECUTIVE_SPACES = &H5A
    CHAR_NAME_RUSSIAN_CONSECUTIVE_SILENT_CHARACTERS = &H5B
    CHAR_NAME_RUSSIAN_SILENT_CHARACTER_AT_BEGINNING_OR_END = &H5C
    CHAR_NAME_DECLENSION_DOESNT_MATCH_BASE_NAME = &H5D
End Enum

#Region "Player.Enums"

Public Enum Classes As Byte
    CLASS_WARRIOR = 1
    CLASS_PALADIN = 2
    CLASS_HUNTER = 3
    CLASS_ROGUE = 4
    CLASS_PRIEST = 5
    CLASS_DEATH_KNIGHT = 6
    CLASS_SHAMAN = 7
    CLASS_MAGE = 8
    CLASS_WARLOCK = 9
    CLASS_DRUID = 11
End Enum
Public Enum Races As Byte
    RACE_HUMAN = 1
    RACE_ORC = 2
    RACE_DWARF = 3
    RACE_NIGHT_ELF = 4
    RACE_UNDEAD = 5
    RACE_TAUREN = 6
    RACE_GNOME = 7
    RACE_TROLL = 8
    RACE_GOBLIN = 9
    RACE_BLOOD_ELF = 10
    RACE_DRAENEI = 11
End Enum
Public Enum PlayerFlags As Integer
    PLAYER_FLAG_GROUP_LEADER = &H1
    PLAYER_FLAG_AFK = &H2
    PLAYER_FLAG_DND = &H4
    PLAYER_FLAG_GM = &H8
    PLAYER_FLAG_DEAD = &H10
    PLAYER_FLAG_RESTING = &H20
    PLAYER_FLAG_UNKNOWN1 = &H40
    PLAYER_FLAG_FREE_FOR_ALL_PVP = &H80
    PLAYER_FLAGS_CONTESTED_PVP = &H100
    PLAYER_FLAG_PVP_TOGGLE = &H200
    PLAYER_FLAG_HIDE_HELM = &H400
    PLAYER_FLAG_HIDE_CLOAK = &H800
    PLAYER_FLAG_NEED_REST_3_HOURS = &H1000
    PLAYER_FLAG_NEED_REST_5_HOURS = &H2000
    PLAYER_FLAG_PVP = &H40000
End Enum
Public Enum PlayerHonorTitle As Byte
    'WARNING: For use with SetFlag
    RANK_NONE = 0
    RANK_A_RIVATE = 1
    RANK_H_SCOUT = 1
    RANK_A_CORPORAL = 2
    RANK_H_GRUNT = 2
    RANK_A_SERGEANT = 3
    RANK_H_SERGEANT = 3
    RANK_A_MASTER_SERGEANT = 4
    RANK_H_SENIOR_SERGEANT = 4
    RANK_A_SERGEANT_MAJOR = 5
    RANK_H_FIRST_SERGEANT = 5
    RANK_A_KNIGHT = 6
    RANK_H_STONE_GUARD = 6
    RANK_A_KNIGHT_LIEUTENANT = 7
    RANK_H_BLOOD_GUARD = 7
    RANK_A_KNIGHT_CAPTAIN = 8
    RANK_H_LEGIONNAIRE = 8
    RANK_A_KNIGHT_CHAMPION = 9
    RANK_H_CENTURION = 9
    RANK_A_LIEUTENANT = 10
    RANK_H_COMMANDER_CHAMPION = 10
    RANK_A_COMMANDER = 11
    RANK_H_LIEUTENANT_GENERAL = 11
    RANK_A_MARSHAL = 12
    RANK_H_GENERAL = 12
    RANK_A_FIELD_MARSHAL = 13
    RANK_H_WARLORD = 13
    RANK_A_GRAND_MARSHAL = 14
    RANK_H_HIGH_WARLORD = 14
End Enum
Public Enum PlayerHonorTitles As Integer
    'WARNING: For use as BitMask
    RANK_NONE = 1
    RANK_A_PRIVATE = 2
    RANK_H_SCOUT = 2
    RANK_A_CORPORAL = 4 + RANK_A_PRIVATE
    RANK_H_GRUNT = 4 + RANK_H_SCOUT
    RANK_A_SERGEANT = 8 + RANK_A_CORPORAL
    RANK_H_SERGEANT = 8 + RANK_H_GRUNT
    RANK_A_MASTER_SERGEANT = 16 + RANK_A_SERGEANT
    RANK_H_SENIOR_SERGEANT = 16 + RANK_H_SERGEANT
    RANK_A_SERGEANT_MAJOR = 32 + RANK_A_MASTER_SERGEANT
    RANK_H_FIRST_SERGEANT = 32 + RANK_H_SENIOR_SERGEANT
    RANK_A_KNIGHT = 64 + RANK_A_SERGEANT_MAJOR
    RANK_H_STONE_GUARD = 64 + RANK_H_FIRST_SERGEANT
    RANK_A_KNIGHT_LIEUTENANT = 128 + RANK_A_KNIGHT
    RANK_H_BLOOD_GUARD = 128 + RANK_H_STONE_GUARD
    RANK_A_KNIGHT_CAPTAIN = 256 + RANK_A_KNIGHT_LIEUTENANT
    RANK_H_LEGIONNAIRE = 256 + RANK_H_BLOOD_GUARD
    RANK_A_KNIGHT_CHAMPION = 512 + RANK_A_KNIGHT_CAPTAIN
    RANK_H_CENTURION = 512 + RANK_H_LEGIONNAIRE
    RANK_A_LIEUTENANT = 1024 + RANK_A_KNIGHT_CHAMPION
    RANK_H_COMMANDER_CHAMPION = 1024 + RANK_H_CENTURION
    RANK_A_COMMANDER = 2048 + RANK_A_LIEUTENANT
    RANK_H_LIEUTENANT_GENERAL = 2048 + RANK_H_COMMANDER_CHAMPION
    RANK_A_MARSHAL = 4096 + RANK_A_COMMANDER
    RANK_H_GENERAL = 4096 + RANK_H_LIEUTENANT_GENERAL
    RANK_A_FIELD_MARSHAL = 8192 + RANK_A_MARSHAL
    RANK_H_WARLORD = 8192 + RANK_H_GENERAL
    RANK_A_GRAND_MARSHAL = 16384 + RANK_A_FIELD_MARSHAL
    RANK_H_HIGH_WARLORD = 16384 + RANK_H_WARLORD
End Enum

Public Enum DamageTypes As Byte
    DMG_PHYSICAL = 0
    DMG_HOLY = 1
    DMG_FIRE = 2
    DMG_NATURE = 3
    DMG_FROST = 4
    DMG_SHADOW = 5
    DMG_ARCANE = 6
End Enum

Public Enum DamageMasks As Integer
    DMG_NORMAL = &H0
    DMG_PHYSICAL = &H1
    DMG_HOLY = &H2
    DMG_FIRE = &H4
    DMG_NATURE = &H8
    DMG_FROST = &H10
    DMG_SHADOW = &H20
    DMG_ARCANE = &H40
End Enum
Public Enum StandStates As Byte
    STANDSTATE_STAND = 0
    STANDSTATE_SIT = 1
    STANDSTATE_SIT_CHAIR = 2
    STANDSTATE_SLEEP = 3
    STANDSTATE_SIT_LOW_CHAIR = 4
    STANDSTATE_SIT_MEDIUM_CHAIR = 5
    STANDSTATE_SIT_HIGH_CHAIR = 6
    STANDSTATE_DEAD = 7
    STANDSTATE_KNEEL = 8
End Enum
Public Enum HonorRank As Byte
    NoRank = 0
    Pariah = 1
    Outlaw = 2
    Exiled = 3
    Dishonored = 4
    Private_ = 5
    Corporal = 6
    Sergeant = 7
    MasterSergeant = 8
    SergeantMajor = 9
    Knight = 10
    KnightLieutenant = 11
    KnightCaptain = 12
    KnightChampion = 13
    LieutenantCommander = 14
    Commander = 15
    Marshal = 16
    FieldMarshal = 17
    GrandMarshal = 18
    Leader = 19
End Enum
Public Enum XPSTATE As Byte
    Normal = 2
    Rested = 1
End Enum
Public Enum ReputationRank As Byte
    Hated = 0
    Hostile = 1
    Unfriendly = 2
    Neutral = 3
    Friendly = 4
    Honored = 5
    Revered = 6
    Exalted = 7
End Enum
Public Enum ReputationPoints
    MIN = Integer.MinValue
    Hated = -42000
    Hostile = -6000
    Unfriendly = -3000
    Friendly = 3000
    Neutral = 0
    Honored = 9000
    Revered = 21000
    Exalted = 42000
    MAX = 43000
End Enum

Enum POI_ICON
    ICON_POI_0 = 0                                         ' Grey ?
    ICON_POI_1 = 1                                         ' Red ?
    ICON_POI_2 = 2                                         ' Blue ?
    ICON_POI_BWTOMB = 3                                    ' Blue and White Tomb Stone
    ICON_POI_HOUSE = 4                                     ' House
    ICON_POI_TOWER = 5                                     ' Tower
    ICON_POI_REDFLAG = 6                                   ' Red Flag with Yellow !
    ICON_POI_TOMB = 7                                      ' Tomb Stone
    ICON_POI_BWTOWER = 8                                   ' Blue and White Tower
    ICON_POI_REDTOWER = 9                                  ' Red Tower
    ICON_POI_BLUETOWER = 10                                ' Blue Tower
    ICON_POI_RWTOWER = 11                                  ' Red and White Tower
    ICON_POI_REDTOMB = 12                                  ' Red Tomb Stone
    ICON_POI_RWTOMB = 13                                   ' Red and White Tomb Stone
    ICON_POI_BLUETOMB = 14                                 ' Blue Tomb Stone
    ICON_POI_NOTHING = 15                                  ' NOTHING
    ICON_POI_16 = 16                                       ' Red ?
    ICON_POI_17 = 17                                       ' Grey ?
    ICON_POI_18 = 18                                       ' Blue ?
    ICON_POI_19 = 19                                       ' Red and White ?
    ICON_POI_20 = 20                                       ' Red ?
    ICON_POI_GREYLOGS = 21                                 ' Grey Wood Logs
    ICON_POI_BWLOGS = 22                                   ' Blue and White Wood Logs
    ICON_POI_BLUELOGS = 23                                 ' Blue Wood Logs
    ICON_POI_RWLOGS = 24                                   ' Red and White Wood Logs
    ICON_POI_REDLOGS = 25                                  ' Red Wood Logs
    ICON_POI_26 = 26                                       ' Grey ?
    ICON_POI_27 = 27                                       ' Blue and White ?
    ICON_POI_28 = 28                                       ' Blue ?
    ICON_POI_29 = 29                                       ' Red and White ?
    ICON_POI_30 = 30                                       ' Red ?
    ICON_POI_GREYHOUSE = 31                                ' Grey House
    ICON_POI_BWHOUSE = 32                                  ' Blue and White House
    ICON_POI_BLUEHOUSE = 33                                ' Blue House
    ICON_POI_RWHOUSE = 34                                  ' Red and White House
    ICON_POI_REDHOUSE = 35                                 ' Red House
    ICON_POI_GREYHORSE = 36                                ' Grey Horse
    ICON_POI_BWHORSE = 37                                  ' Blue and White Horse
    ICON_POI_BLUEHORSE = 38                                ' Blue Horse
    ICON_POI_RWHORSE = 39                                  ' Red and White Horse
    ICON_POI_REDHORSE = 40                                 ' Red Horse
End Enum

#End Region
#Region "Player.Groups"

<Flags()> _
Public Enum GroupFlags As Byte
    NOTE = 0
    ASSISTANT = 1
    MAIN_TANK = 2
    MAIN_ASSIST = 4
End Enum
Public Enum GroupType As Byte
    PARTY = 0
    RAID = 1
End Enum
<Flags()> _
Public Enum GroupMemberOnlineStatus
    MEMBER_STATUS_OFFLINE = &H0
    MEMBER_STATUS_ONLINE = &H1
    MEMBER_STATUS_PVP = &H2
    MEMBER_STATUS_UNK0 = &H4            ' dead? (health=0)
    MEMBER_STATUS_UNK1 = &H8            ' ghost? (health=1)
    MEMBER_STATUS_UNK2 = &H10           ' never seen
    MEMBER_STATUS_UNK3 = &H20           ' never seen
    MEMBER_STATUS_UNK4 = &H40           ' appears with dead and ghost flags
    MEMBER_STATUS_UNK5 = &H80           ' never seen
End Enum
Public Enum GroupDungeonDifficulty As Byte
    DIFFICULTY_NORMAL = 0
    DIFFICULTY_HEROIC = 1
End Enum
Public Enum GroupLootMethod As Byte
    LOOT_FREE_FOR_ALL = 0
    LOOT_ROUND_ROBIN = 1
    LOOT_MASTER = 2
    LOOT_GROUP = 3
    LOOT_NEED_BEFORE_GREED = 4
End Enum
Public Enum GroupLootThreshold As Byte
    Uncommon = 2
    Rare = 3
    Epic = 4
End Enum

#End Region
#Region "Player.Chat"

Public Enum LANGUAGES As Integer
    LANG_GLOBAL = 0
    LANG_UNIVERSAL = 0
    LANG_ORCISH = 1
    LANG_DARNASSIAN = 2
    LANG_TAURAHE = 3
    LANG_DWARVISH = 6
    LANG_COMMON = 7
    LANG_DEMONIC = 8
    LANG_TITAN = 9
    LANG_THALASSIAN = 10
    LANG_DRACONIC = 11
    LANG_KALIMAG = 12
    LANG_GNOMISH = 13
    LANG_TROLL = 14
    LANG_GUTTERSPEAK = 33
    LANG_DRAENEI = 35
    LANG_ADDON = &HFFFFFFFF
End Enum
Public Enum ChatMsg As Integer
    CHAT_MSG_ADDON = &HFFFFFFFF
    CHAT_MSG_SYSTEM = &H0
    CHAT_MSG_SAY = &H1
    CHAT_MSG_PARTY = &H2
    CHAT_MSG_RAID = &H3
    CHAT_MSG_GUILD = &H4
    CHAT_MSG_OFFICER = &H5
    CHAT_MSG_YELL = &H6
    CHAT_MSG_WHISPER = &H7
    CHAT_MSG_WHISPER_INFORM = &H8
    CHAT_MSG_REPLY = &H9
    CHAT_MSG_EMOTE = &HA
    CHAT_MSG_TEXT_EMOTE = &HB
    CHAT_MSG_MONSTER_SAY = &HC
    CHAT_MSG_MONSTER_PARTY = &HD
    CHAT_MSG_MONSTER_YELL = &HE
    CHAT_MSG_MONSTER_WHISPER = &HF
    CHAT_MSG_MONSTER_EMOTE = &H10
    CHAT_MSG_CHANNEL = &H11
    CHAT_MSG_CHANNEL_JOIN = &H12
    CHAT_MSG_CHANNEL_LEAVE = &H13
    CHAT_MSG_CHANNEL_LIST = &H14
    CHAT_MSG_CHANNEL_NOTICE = &H15
    CHAT_MSG_CHANNEL_NOTICE_USER = &H16
    CHAT_MSG_AFK = &H17
    CHAT_MSG_DND = &H18
    CHAT_MSG_IGNORED = &H19
    CHAT_MSG_SKILL = &H1A
    CHAT_MSG_LOOT = &H1B
    CHAT_MSG_MONEY = &H1C
    CHAT_MSG_OPENING = &H1D
    CHAT_MSG_TRADESKILLS = &H1E
    CHAT_MSG_PET_INFO = &H1F
    CHAT_MSG_COMBAT_MISC_INFO = &H20
    CHAT_MSG_COMBAT_XP_GAIN = &H21
    CHAT_MSG_COMBAT_HONOR_GAIN = &H22
    CHAT_MSG_COMBAT_FACTION_CHANGE = &H23
    CHAT_MSG_BG_SYSTEM_NEUTRAL = &H24
    CHAT_MSG_BG_SYSTEM_ALLIANCE = &H25
    CHAT_MSG_BG_SYSTEM_HORDE = &H26
    CHAT_MSG_RAID_LEADER = &H27
    CHAT_MSG_RAID_WARNING = &H28
    CHAT_MSG_RAID_BOSS_WHISPER = &H29
    CHAT_MSG_RAID_BOSS_EMOTE = &H2A
    CHAT_MSG_FILTERED = &H2B
    CHAT_MSG_BATTLEGROUND = &H2C
    CHAT_MSG_BATTLEGROUND_LEADER = &H2D
    CHAT_MSG_RESTRICTED = &H2E

    CHAT_COMBAT_MISC_INFO = 25
    CHAT_MONSTER_WHISPER = 26
    CHAT_COMBAT_SELF_HITS = 27
    CHAT_COMBAT_SELF_MISSES = 28
    CHAT_COMBAT_PET_HITS = 29
    CHAT_COMBAT_PET_MISSES = 30
    CHAT_COMBAT_PARTY_HITS = 31
    CHAT_COMBAT_PARTY_MISSES = 32
    CHAT_COMBAT_FRIENDLYPLAYER_HITS = 33
    CHAT_COMBAT_FRIENDLYPLAYER_MISSES = 34
    CHAT_COMBAT_HOSTILEPLAYER_HITS = 35
    CHAT_COMBAT_HOSTILEPLAYER_MISSES = 36
    CHAT_COMBAT_CREATURE_VS_SELF_HITS = 37
    CHAT_COMBAT_CREATURE_VS_SELF_MISSES = 38
    CHAT_COMBAT_CREATURE_VS_PARTY_HITS = 39
    CHAT_COMBAT_CREATURE_VS_PARTY_MISSES = 40
    CHAT_COMBAT_CREATURE_VS_CREATURE_HITS = 41
    CHAT_COMBAT_CREATURE_VS_CREATURE_MISSES = 42
    CHAT_COMBAT_FRIENDLY_DEATH = 43
    CHAT_COMBAT_HOSTILE_DEATH = 44
    CHAT_COMBAT_XP_GAIN = 45
    CHAT_SPELL_SELF_DAMAGE = 46
    CHAT_SPELL_SELF_BUFF = 47
    CHAT_SPELL_PET_DAMAGE = 48
    CHAT_SPELL_PET_BUFF = 49
    CHAT_SPELL_PARTY_DAMAGE = 50
    CHAT_SPELL_PARTY_BUFF = 51
    CHAT_SPELL_FRIENDLYPLAYER_DAMAGE = 52
    CHAT_SPELL_FRIENDLYPLAYER_BUFF = 53
    CHAT_SPELL_HOSTILEPLAYER_DAMAGE = 54
    CHAT_SPELL_HOSTILEPLAYER_BUFF = 55
    CHAT_SPELL_CREATURE_VS_SELF_DAMAGE = 56
    CHAT_SPELL_CREATURE_VS_SELF_BUFF = 57
    CHAT_SPELL_CREATURE_VS_PARTY_DAMAGE = 58
    CHAT_SPELL_CREATURE_VS_PARTY_BUFF = 59
    CHAT_SPELL_CREATURE_VS_CREATURE_DAMAGE = 60
    CHAT_SPELL_CREATURE_VS_CREATURE_BUFF = 61
    CHAT_SPELL_TRADESKILLS = 62
    CHAT_SPELL_DAMAGESHIELDS_ON_SELF = 63
    CHAT_SPELL_DAMAGESHIELDS_ON_OTHERS = 64
    CHAT_SPELL_AURA_GONE_SELF = 65
    CHAT_SPELL_AURA_GONE_PARTY = 66
    CHAT_SPELL_AURA_GONE_OTHER = 67
    CHAT_SPELL_ITEM_ENCHANTMENTS = 68
    CHAT_SPELL_BREAK_AURA = 69
    CHAT_SPELL_PERIODIC_SELF_DAMAGE = 70
    CHAT_SPELL_PERIODIC_SELF_BUFFS = 71
    CHAT_SPELL_PERIODIC_PARTY_DAMAGE = 72
    CHAT_SPELL_PERIODIC_PARTY_BUFFS = 73
    CHAT_SPELL_PERIODIC_FRIENDLYPLAYER_DAMAGE = 74
    CHAT_SPELL_PERIODIC_FRIENDLYPLAYER_BUFFS = 75
    CHAT_SPELL_PERIODIC_HOSTILEPLAYER_DAMAGE = 76
    CHAT_SPELL_PERIODIC_HOSTILEPLAYER_BUFFS = 77
    CHAT_SPELL_PERIODIC_CREATURE_DAMAGE = 78
    CHAT_SPELL_PERIODIC_CREATURE_BUFFS = 79
    CHAT_SPELL_FAILED_LOCALPLAYER = 80
    CHAT_COMBAT_HONOR_GAIN = 81
    CHAT_BG_SYSTEM_NEUTRAL = 82
    CHAT_BG_SYSTEM_ALLIANCE = 83
    CHAT_BG_SYSTEM_HORDE = 84
End Enum
<Flags()> _
Public Enum ChatFlag As Byte
    FLAG_NONE = 0
    FLAG_AFK = 1
    FLAG_DND = 2
    FLAG_GM = 4
End Enum

#End Region
#Region "Object.Flags"

Enum DynamicFlags   'Dynamic flags for units
    'Unit has blinking stars effect showing lootable
    UNIT_DYNFLAG_LOOTABLE = &H1
    'Shows marked unit as small red dot on radar
    UNIT_DYNFLAG_TRACK_UNIT = &H2
    'Gray mob title marks that mob is tagged by another player
    UNIT_DYNFLAG_OTHER_TAGGER = &H4
    'Blocks player character from moving
    UNIT_DYNFLAG_ROOTED = &H8
    'Shows infos like Damage and Health of the enemy
    UNIT_DYNFLAG_SPECIALINFO = &H10
    'Unit falls on the ground and shows like dead
    UNIT_DYNFLAG_DEAD = &H20
End Enum
Enum UnitFlags   'Flags for units
    UNIT_FLAG_NONE = &H0
    UNIT_FLAG_UNK1 = &H1
    UNIT_FLAG_NOT_ATTACKABLE = &H2                                                  'Unit is not attackable
    UNIT_FLAG_DISABLE_MOVE = &H4                                                    'Unit is frozen, rooted or stunned
    UNIT_FLAG_ATTACKABLE = &H8                                                      'Unit becomes temporarily hostile, shows in red, allows attack
    UNIT_FLAG_RENAME = &H10
    UNIT_FLAG_RESTING = &H20
    UNIT_FLAG_UNK5 = &H40
    UNIT_FLAG_NOT_ATTACKABLE_1 = &H80                                               'Unit cannot be attacked by player, shows no attack cursor
    UNIT_FLAG_UNK6 = &H100
    UNIT_FLAG_UNK7 = &H200
    UNIT_FLAG_NON_PVP_PLAYER = UNIT_FLAG_ATTACKABLE + UNIT_FLAG_NOT_ATTACKABLE_1    'Unit cannot be attacked by player, shows in blue
    UNIT_FLAG_LOOTING = &H400
    UNIT_FLAG_PET_IN_COMBAT = &H800
    UNIT_FLAG_PVP = &H1000
    UNIT_FLAG_SILENCED = &H2000
    UNIT_FLAG_DEAD = &H4000
    UNIT_FLAG_UNK11 = &H8000
    UNIT_FLAG_ROOTED = &H10000
    UNIT_FLAG_PACIFIED = &H20000
    UNIT_FLAG_STUNTED = &H40000
    UNIT_FLAG_IN_COMBAT = &H80000
    UNIT_FLAG_TAXI_FLIGHT = &H100000
    UNIT_FLAG_DISARMED = &H200000
    UNIT_FLAG_CONFUSED = &H400000
    UNIT_FLAG_FLEEING = &H800000
    UNIT_FLAG_UNK21 = &H1000000
    UNIT_FLAG_NOT_SELECTABLE = &H2000000
    UNIT_FLAG_SKINNABLE = &H4000000
    UNIT_FLAG_MOUNT = &H8000000
    UNIT_FLAG_UNK25 = &H10000000
    UNIT_FLAG_UNK26 = &H20000000
    UNIT_FLAG_SKINNABLE_AND_DEAD = UNIT_FLAG_SKINNABLE + UNIT_FLAG_DEAD
    UNIT_FLAG_SPIRITHEALER = UNIT_FLAG_UNK21 + UNIT_FLAG_NOT_ATTACKABLE + UNIT_FLAG_DISABLE_MOVE + UNIT_FLAG_RESTING + UNIT_FLAG_UNK5
    UNIT_FLAG_SHEATHE = &H40000000
End Enum
Enum NPCFlags
    UNIT_NPC_FLAG_NONE = &H0
    UNIT_NPC_FLAG_GOSSIP = &H1
    UNIT_NPC_FLAG_QUESTGIVER = &H2
    UNIT_NPC_FLAG_UNK1 = &H4
    UNIT_NPC_FLAG_UNK2 = &H8
    UNIT_NPC_FLAG_TRAINER = &H10
    UNIT_NPC_FLAG_TRAINER_CLASS = &H20
    UNIT_NPC_FLAG_TRAINER_PROFESSION = &H40
    UNIT_NPC_FLAG_VENDOR = &H80
    UNIT_NPC_FLAG_VENDOR_AMMO = &H100
    UNIT_NPC_FLAG_VENDOR_FOOD = &H200
    UNIT_NPC_FLAG_VENDOR_POISON = &H400
    UNIT_NPC_FLAG_VENDOR_REAGENT = &H800
    UNIT_NPC_FLAG_REPAIR = &H1000
    UNIT_NPC_FLAG_FLIGHTMASTER = &H2000
    UNIT_NPC_FLAG_SPIRITHEALER = &H4000
    UNIT_NPC_FLAG_SPIRITGUIDE = &H8000
    UNIT_NPC_FLAG_INNKEEPER = &H10000
    UNIT_NPC_FLAG_BANKER = &H20000
    UNIT_NPC_FLAG_PETITIONER = &H40000
    UNIT_NPC_FLAG_TABARDDESIGNER = &H80000
    UNIT_NPC_FLAG_BATTLEMASTER = &H100000
    UNIT_NPC_FLAG_AUCTIONEER = &H200000
    UNIT_NPC_FLAG_STABLEMASTER = &H400000
    UNIT_NPC_FLAG_GUILD_BANKER = &H800000
    UNIT_NPC_FLAG_SPELLCLICK = &H1000000
    UNIT_NPC_FLAG_GUARD = &H10000000
End Enum

#End Region
#Region "Creatures.Types"

Enum UNIT_TYPE
    NOUNITTYPE = 0
    BEAST = 1
    DRAGONKIN = 2
    DEMON = 3
    ELEMENTAL = 4
    GIANT = 5
    UNDEAD = 6
    HUMANOID = 7
    CRITTER = 8
    MECHANICAL = 9
    MOUNT = 10
End Enum
Enum CREATURE_FAMILY As Integer
    NONE = 0
    WOLF = 1
    CAT = 2
    SPIDER = 3
    BEAR = 4
    BOAR = 5
    CROCILISK = 6
    CARRION_BIRD = 7
    CRAB = 8
    GORILLA = 9
    RAPTOR = 11
    TALLSTRIDER = 12
    FELHUNTER = 15
    VOIDWALKER = 16
    SUCCUBUS = 17
    DOOMGUARD = 19
    SCORPID = 20
    TURTLE = 21
    IMP = 23
    BAT = 24
    HYENA = 25
    OWL = 26
    WIND_SERPENT = 27
End Enum
Enum CREATURE_ELITE As Integer
    NORMAL = 0
    ELITE = 1
    RAREELITE = 2
    WORLDBOSS = 3
    RARE = 4
End Enum

#End Region

Public Enum MonsterSayEvents
    MONSTER_SAY_EVENT_ENTER_COMBAT = 0
    MONSTER_SAY_EVENT_RANDOM_WAYPOINT = 1
    MONSTER_SAY_EVENT_CALL_FOR_HELP = 2
    MONSTER_SAY_EVENT_EXIT_COMBAT = 3
    MONSTER_SAY_EVENT_DAMAGE_TAKEN = 4
    MONSTER_SAY_EVENT_DIED = 5
End Enum

Enum InvalidReason
    DontHaveReq = 0
    DontHaveReqItems = 19
    DontHaveReqMoney = 21
    NotAvailableRace = 6
    NotEnoughLevel = 1
    ReadyHaveThatQuest = 13
    ReadyHaveTimedQuest = 12
End Enum
Enum Attributes
    Agility = 3
    Health = 1
    Iq = 5
    Mana = 0
    Spirit = 6
    Stamina = 7
    Strenght = 4
End Enum
Enum Slots
    ' Fields
    Back = 14
    BackpackEnd = 39
    BackpackStart = 23
    Bag1 = 19
    Bag2 = 20
    Bag3 = 21
    Bag4 = 22
    BagsEnd = 261
    BagsStart = 81
    BankBagsEnd = 70
    BankBagsStart = 63
    BankEnd = 67
    BankStart = 39
    BuybackEnd = 81
    BuybackStart = 69
    Chest = 4
    Feet = 7
    FingerLeft = 10
    FingerRight = 11
    Hands = 9
    Head = 0
    ItemsEnd = 261
    Legs = 6
    MainHand = 15
    Neck = 1
    None = -1
    OffHand = 16
    Ranged = 17
    Shirt = 3
    Shoulders = 2
    Tabard = 18
    TrinketLeft = 12
    TrinketRight = 13
    Waist = 5
    Wrists = 8
End Enum
Enum EnviromentalDamage
    DAMAGE_EXHAUSTED = 0
    DAMAGE_DROWNING = 1
    DAMAGE_FALL = 2
    DAMAGE_LAVA = 3
    DAMAGE_SLIME = 4
    DAMAGE_FIRE = 5
End Enum

Public Enum MapTypes As Integer
    MAP_COMMON = 0
    MAP_INSTANCE = 1
    MAP_RAID = 2
    MAP_BATTLEGROUND = 3
    MAP_ARENA = 4
End Enum
Public Enum BattlefieldType
    TYPE_BATTLEGROUND = 3
    TYPE_ARENA = 4
End Enum
Public Enum BattlefieldMapType As Byte
    BATTLEGROUND_AlteracValley = 1
    BATTLEGROUND_WarsongGulch = 2
    BATTLEGROUND_ArathiBasin = 3
    BATTLEGROUND_EyeOfTheStorm = 7

    ARENA_NagrandArena = 4
    ARENA_BladesEdgeArena = 5
    ARENA_AllArenas = 6
    ARENA_RuinsofLordaeron = 8
End Enum
Public Enum BattlefieldArenaType As Byte
    ARENA_TYPE_NONE = 0
    ARENA_TYPE_2v2 = 2
    ARENA_TYPE_3v3 = 3
    ARENA_TYPE_5v5 = 5
End Enum