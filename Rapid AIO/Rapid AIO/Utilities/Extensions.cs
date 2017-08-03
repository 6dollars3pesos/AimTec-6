namespace Rapid_AIO.Utilities
{
    using System.Drawing;

    using Aimtec;
    using Aimtec.SDK.Menu.Components;

    using static Bases.Champion;

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

        private static bool IsActivated(this Spell spell)
        {
            return spell != default(Spell) && (spell.IsSkillShot || spell.IsChargedSpell || spell.IsVectorSkillShot);
        }
    }
}