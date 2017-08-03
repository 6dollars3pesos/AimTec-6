namespace Rapid_AIO
{
    using System;
    using System.Reflection;

    using Aimtec;
    using Aimtec.SDK.Events;

    using Rapid_AIO.Bases;

    internal static class Program
    {
        private static string Namespace => MethodBase.GetCurrentMethod().DeclaringType?.Namespace;

        private static void Main()
        {
            GameEvents.GameStart += OnGameStart;
        }

        private static void OnGameStart()
        {
            var championType = Type.GetType($"{Namespace}.Champions.{ObjectManager.GetLocalPlayer().ChampionName}");

            if (championType == null) return;

            ((Champion)Activator.CreateInstance(championType)).Initiate();
        }
    }
}