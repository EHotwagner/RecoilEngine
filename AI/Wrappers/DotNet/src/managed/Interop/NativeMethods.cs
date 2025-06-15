using System;
using System.Runtime.InteropServices;

namespace SpringAI.Interop
{
    /// <summary>
    /// P/Invoke declarations for the Spring AI interface
    /// </summary>
    internal static class NativeMethods
    {
        public const int CALLING_CONV_CDECL = 1;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int HandleCommandDelegate(int skirmishAIId, int commandTopicId, IntPtr commandData);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int SendTextMessageDelegate(int skirmishAIId, IntPtr message, int zone);

        // Core AI callback structure - simplified version
        [StructLayout(LayoutKind.Sequential)]
        public struct SSkirmishAICallback
        {
            public HandleCommandDelegate HandleCommand;
            public SendTextMessageDelegate SendTextMessage;
            // ... many more function pointers would go here
        }

        // Event structures
        [StructLayout(LayoutKind.Sequential)]
        public struct SInitEvent
        {
            public int skirmishAIId;
            public IntPtr callback; // SSkirmishAICallback*
            public byte savedGame;  // bool as byte for marshalling
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SUpdateEvent
        {
            public int frame;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SUnitCreatedEvent
        {
            public int unit;
            public int builder;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SUnitDamagedEvent
        {
            public int unit;
            public int attacker;
            public float damage;
            public IntPtr dir_posF3; // float*
            public int weaponDefId;
            public byte paralyzer; // bool as byte
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SUnitDestroyedEvent
        {
            public int unit;
            public int attacker;
            public int weaponDefID;
        }

        // Event topic IDs (from AISEvents.h)
        public const int EVENT_NULL = 0;
        public const int EVENT_INIT = 1;
        public const int EVENT_RELEASE = 2;
        public const int EVENT_UPDATE = 3;
        public const int EVENT_MESSAGE = 4;
        public const int EVENT_UNIT_CREATED = 5;
        public const int EVENT_UNIT_FINISHED = 6;
        public const int EVENT_UNIT_IDLE = 7;
        public const int EVENT_UNIT_MOVE_FAILED = 8;
        public const int EVENT_UNIT_DAMAGED = 9;
        public const int EVENT_UNIT_DESTROYED = 10;
        public const int EVENT_UNIT_GIVEN = 11;
        public const int EVENT_UNIT_CAPTURED = 12;
        public const int EVENT_ENEMY_ENTER_LOS = 13;
        public const int EVENT_ENEMY_LEAVE_LOS = 14;
        public const int EVENT_ENEMY_ENTER_RADAR = 15;
        public const int EVENT_ENEMY_LEAVE_RADAR = 16;
        public const int EVENT_ENEMY_DAMAGED = 17;
        public const int EVENT_ENEMY_DESTROYED = 18;
        public const int EVENT_WEAPON_FIRED = 19;
        public const int EVENT_PLAYER_COMMAND = 20;
        public const int EVENT_SEISMIC_PING = 21;
        public const int EVENT_COMMAND_FINISHED = 22;
        public const int EVENT_LOAD = 23;
        public const int EVENT_SAVE = 24;
        public const int EVENT_ENEMY_CREATED = 25;
        public const int EVENT_ENEMY_FINISHED = 26;
        public const int EVENT_LUA_MESSAGE = 27;
    }
}
