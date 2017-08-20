namespace Rapid_AIO.Champions
{
    using Aimtec;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Menu;
    using Aimtec.SDK.Menu.Components;
    using Aimtec.SDK.Orbwalking;
    using Aimtec.SDK.Prediction.Skillshots;
    using Aimtec.SDK.TargetSelector;

    using Rapid_AIO.Bases;

    using Prediction = Rapid_AIO.Utilities.Prediction;
    using Spell = Aimtec.SDK.Spell;

    internal class Ezreal : Champion
    {
        private static readonly Prediction OverPowered = new Prediction();

        internal override Spell Q { get; set; } =
            new Spell(SpellSlot.Q)
                {
                    IsSkillShot = true,
                    Type = SkillshotType.Line,
                    Delay = 0.25f,
                    Width = 60,
                    Speed = 2000,
                    Range = 1200,
                    Collision = true
                };

        internal override void Combo()
        {
            this.CastQ();
        }

        internal override void SetMenu()
        {
            Orbwalker.Implementation.Attach(RootMenu);

            var comboMenu = new Menu("Combo", "Combo") { new MenuBool("Q", "Use Q") };

            RootMenu.Add(comboMenu);

            var drawingsMenu = new Menu("Drawings", "Drawings") { new MenuBool("Q", "Draw Q") };

            RootMenu.Add(drawingsMenu);

            RootMenu.Attach();
        }

        private void CastQ()
        {
            if (!RootMenu["Combo"]["Q"].As<MenuBool>().Enabled || !this.Q.Ready) return;

            var target = TargetSelector.GetTarget(this.Q.Range);

            if (!target.IsValidTarget(this.Q.Range)) return;

            var input = this.Q.GetPredictionInput(target);

            var output = OverPowered.GetPrediction(input);

            if (output.HitChance < HitChance.Low) return;

            this.Q.Cast(output.CastPosition);
        }
    }
}