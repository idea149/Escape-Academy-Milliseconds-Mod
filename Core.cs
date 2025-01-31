using MelonLoader;
using HarmonyLib;
using System.Reflection;
using Escape.GUI;
using Escape.Analytics;
using Escape.Audio;
using Escape.Metagame;
using Escape.OnlineMultiplayer;
using Escape.Rooms;
using Photon.Pun;
using System.Runtime.CompilerServices;
using Escape.Metagame.Metadata;
using Escape.ProcGen.LevelData;
using Escape.ProcGen.Managers;
using Escape.Utilities;
using TMPro;
using System.Security.Cryptography;

[assembly: MelonInfo(typeof(MillisecondsTimer.Core), "MillisecondsTimer", "1.0.0", "idea149", null)]
[assembly: MelonGame("CoinCrewGames", "Escape Academy")]

namespace MillisecondsTimer
{
    public static class State
    {
        public static float winTime;
    }

    public class Core : MelonMod
    {
        public override void OnInitializeMelon()
        {
            LoggerInstance.Msg("Initialized.");
        }
    }

    // Clock Formatting
    [HarmonyPatch(typeof(Escape.GUI.GameClock), "ConvertSecondsToMMSSUnits", new Type[] { typeof(float), typeof(bool), typeof(float) })]
    public static class ConvertSecondsToMMSSUnits_Patch
    {
        private static void Postfix(ref string __result, float seconds, bool monoSpace, float spacing)
        {

            Type gameClockType = typeof(Escape.GUI.GameClock);
            FieldInfo monoEndValue = gameClockType.GetField("monoEnd", BindingFlags.NonPublic | BindingFlags.Static);
            FieldInfo monoStartValue = gameClockType.GetField("monoStart", BindingFlags.NonPublic | BindingFlags.Static);
            string monoStart = (string)monoStartValue.GetValue(null);
            string monoEnd = (string)monoEndValue.GetValue(null);

            string milliseconds = ((int)(seconds % 1f * 1000f)).ToString("000");
            __result += ".";
            if (monoSpace)
            {
                __result += string.Format(monoStart, spacing);
            }
            __result += milliseconds;
            if (monoSpace)
            {
                __result += monoEnd;
            }
        }
    }

    // Use saved win time when writing time to save file
    [HarmonyPatch(typeof(Escape.Metagame.Metadata.EscapeMetaData), "SetTimeAtTrue", new Type[] { typeof(float) })]
    public static class SetTimeAtTrue_Patch
    {
        private static void Postfix(Escape.Metagame.Metadata.EscapeMetaData __instance)
        {
            Type escapeMetaDataType = typeof(Escape.Metagame.Metadata.EscapeMetaData);
            FieldInfo timeAtTrueField = escapeMetaDataType.GetField("timeAtTrue", BindingFlags.NonPublic | BindingFlags.Instance);
            float levelCompletedTime = (float)timeAtTrueField.GetValue(__instance);

            // Sanity check: If saved winTime is reasonably close to integer time, use saved time instead
            if (Math.Abs(levelCompletedTime - State.winTime) < 1)
            {
                levelCompletedTime = State.winTime;
            }

            timeAtTrueField.SetValue(__instance, levelCompletedTime);
        }
    }

    // Save win time on room win
    [HarmonyPatch(typeof(Escape.Rooms.RoomManager), "WinRoom")]
    public static class WinRoom_Patch
    {
        private static void Prefix(Escape.Rooms.RoomManager __instance)
        {
            MelonLogger.Msg("WinRoom");
            if (__instance.CurrentState == RoomManager.State.Won || __instance.CurrentState == RoomManager.State.Lost)
            {
                return;
            }
            State.winTime = __instance.TimeSpentInRoom;
        }
    }

    // Reset win time on room restart
    [HarmonyPatch(typeof(Escape.Metagame.Metadata.LevelMetaData), "StartRoom")]
    public static class StartRoom_Patch
    {
        private static void Prefix()
        {
            State.winTime = 9999;
        }
    }

    // Use float win time for text on ending card
    [HarmonyPatch(typeof(Escape.Rooms.RoomReview.ReportCard), "PopulateReportCard", new Type[] {typeof(Escape.Metagame.Metadata.LevelMetaData)})]
    public static class PopulateReportCard_Patch
    {
 
        private static void Postfix(Escape.Rooms.RoomReview.ReportCard __instance)
        {
            __instance.escapeTimeValue.text = GameClock.ConvertSecondsToMMSSUnits(State.winTime, false, 0.5f);
            
        }
    }
}