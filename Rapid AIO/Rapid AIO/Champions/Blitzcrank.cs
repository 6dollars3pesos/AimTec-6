﻿namespace Rapid_AIO.Champions
{
    using Aimtec;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Menu;
    using Aimtec.SDK.Menu.Components;
    using Aimtec.SDK.Orbwalking;
    using Aimtec.SDK.Prediction.Skillshots;
    using Aimtec.SDK.TargetSelector;

    using Rapid_AIO.Bases;

    using static Utilities.Extensions;

    using Prediction = Rapid_AIO.Utilities.Prediction;
    using Spell = Aimtec.SDK.Spell;

    internal class Blitzcrank : Champion
    {
        private static readonly Prediction OverPowered = new Prediction();

        internal override Spell E { get; set; } = new Spell(SpellSlot.E) { Delay = 0.25f, Range = Player.AttackRange };

        internal override Spell Q { get; set; } =
            new Spell(SpellSlot.Q)
                {
                    IsSkillShot = true,
                    Type = SkillshotType.Line,
                    Delay = 0.25f,
                    Width = 70,
                    Speed = 1800,
                    Range = 1050,
                    Collision = true
                };

        internal override void Combo()
        {
            this.CastQ();
            this.CastE();
        }

        internal override void SetMenu()
        {
            Orbwalker.Implementation.Attach(RootMenu);

            var comboMenu =
                new Menu("Combo", "Combo") { new MenuBool("Q", "Use Q"), GetWhiteList(), new MenuBool("E", "Use E") };

            RootMenu.Add(comboMenu);

            var drawingsMenu =
                new Menu("Drawings", "Drawings") { new MenuBool("Q", "Draw Q"), new MenuBool("E", "Draw E") };

            RootMenu.Add(drawingsMenu);

            RootMenu.Attach();
        }

        private void CastE()
        {
            if (!RootMenu["Combo"]["E"].As<MenuBool>().Enabled || !this.E.Ready) return;

            var target = TargetSelector.GetTarget(this.E.Range);

            if (!target.IsValidTarget(this.E.Range) && !target.HasBuffOfType(BuffType.Knockup)) return;

            this.E.Cast();
            Orbwalker.Implementation.ForceTarget(target);
        }

        private void CastQ()
        {
            if (!RootMenu["Combo"]["Q"].As<MenuBool>().Enabled || !this.Q.Ready) return;

            var target = TargetSelector.GetTarget(this.Q.Range);

            if (!target.IsValidTarget(this.Q.Range) || !RootMenu["Combo"]["WhiteList"][target.ChampionName]
                    .As<MenuBool>().Enabled) return;

            var input = this.Q.GetPredictionInput(target);

            var output = OverPowered.GetPrediction(input);

            if (output.HitChance < HitChance.Low) return;

            this.Q.Cast(output.CastPosition);
        }
    }
}