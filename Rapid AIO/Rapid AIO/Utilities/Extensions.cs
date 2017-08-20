namespace Rapid_AIO.Utilities
{
    using System.Drawing;

    using Aimtec;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Menu;
    using Aimtec.SDK.Menu.Components;
    using Aimtec.SDK.Util.Cache;

    using static Bases.Champion;

    using Spell = Aimtec.SDK.Spell;

    internal static class Extensions
    {
        private static Obj_AI_Hero Player => ObjectManager.GetLocalPlayer();

        internal static void DrawOnScreen(this Spell spell, Color color)
        {
            if (spell == null) return;

            if (RootMenu["Drawings"][$"{spell.Slot}"].As<MenuBool>() != null
                && RootMenu["Drawings"][$"{spell.Slot}"].As<MenuBool>().Enabled
                && spell.IsActivated()) Render.Circle(Player.Position, spell.Range, 40, color);
        }

        internal static Menu GetWhiteList()
        {
            var list = new Menu("WhiteList", "White List");

            foreach (var hero in GameObjects.EnemyHeroes) list.Add(new MenuBool(hero.ChampionName, hero.ChampionName));

            return list;
        }

        internal static float SetDelay(this float delay, float additionalDelay = 0f)
        {
            return delay + Game.Ping / 1000f + additionalDelay;
        }

        internal static Vector3 SetFromPosition(this Vector3 fromPosition, Vector3 targetPosition)
        {
            if (fromPosition != Player.ServerPosition) return fromPosition;

            fromPosition = fromPosition.Extend(
                fromPosition.GetToTargetDirection(targetPosition),
                Player.BoundingRadius);

            return fromPosition;
        }

        private static Vector3 GetToTargetDirection(this Vector3 fromPosition, Vector3 targetPosition)
        {
            return (targetPosition - fromPosition).Normalized();
        }

        private static bool IsActivated(this Spell spell)
        {
            return spell != default(Spell) && (spell.IsSkillShot || spell.IsChargedSpell || spell.IsVectorSkillShot);
        }
    }
}