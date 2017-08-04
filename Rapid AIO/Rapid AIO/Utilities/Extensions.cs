namespace Rapid_AIO.Utilities
{
    using System.Drawing;
    using System.Linq;

    using Aimtec;
    using Aimtec.SDK.Menu;
    using Aimtec.SDK.Menu.Components;
    using Aimtec.SDK.Util.Cache;

    using static Rapid_AIO.Bases.Champion;

    using Spell = Aimtec.SDK.Spell;

    internal static class Extensions
    {
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

            foreach (var hero in GameObjects.EnemyHeroes)
            {
                list.Add(new MenuBool(hero.ChampionName, hero.ChampionName));
            }

            return list;
        }

        private static bool IsActivated(this Spell spell)
        {
            return spell != default(Spell) && (spell.IsSkillShot || spell.IsChargedSpell || spell.IsVectorSkillShot);
        }
    }
}