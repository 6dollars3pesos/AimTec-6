namespace Rapid_AIO.Bases
{
    using System.Drawing;

    using Aimtec;
    using Aimtec.SDK.Menu;
    using Aimtec.SDK.Orbwalking;

    using Rapid_AIO.Utilities;

    using Spell = Aimtec.SDK.Spell;

    internal abstract class Champion
    {
        internal static Obj_AI_Hero Player => ObjectManager.GetLocalPlayer();

        internal static Menu RootMenu { get; set; }

        internal virtual Spell E { get; set; } = default(Spell);

        internal virtual Spell E2 { get; set; } = default(Spell);

        internal virtual Spell Q { get; set; } = default(Spell);

        internal virtual Spell Q2 { get; set; } = default(Spell);

        internal virtual Spell R { get; set; } = default(Spell);

        internal virtual Spell R2 { get; set; } = default(Spell);

        internal virtual Spell W { get; set; } = default(Spell);

        internal virtual Spell W2 { get; set; } = default(Spell);

        internal abstract void Combo();

        internal virtual void Harass()
        {
        }

        internal void Initiate()
        {
            this.SetMenu();
            this.SetEvents();
        }

        internal virtual void Laneclear()
        {
        }

        internal virtual void Lasthit()
        {
        }

        internal virtual void OnGameUpdate()
        {
            if (Player.IsDead || MenuGUI.IsChatOpen() || MenuGUI.IsShopOpen()) return;

            switch (Orbwalker.Implementation.Mode)
            {
                case OrbwalkingMode.None: break;
                case OrbwalkingMode.Combo:
                    this.Combo();
                    break;
                case OrbwalkingMode.Mixed:
                    this.Harass();
                    break;
                case OrbwalkingMode.Laneclear:
                    this.Laneclear();
                    break;
                case OrbwalkingMode.Lasthit:
                    this.Lasthit();
                    break;
                case OrbwalkingMode.Freeze: break;
                case OrbwalkingMode.Custom: break;
            }
        }

        internal virtual void OnRenderPresent()
        {
            if (Player.IsDead || MenuGUI.IsChatOpen() || MenuGUI.IsShopOpen()) return;

            this.Q.DrawOnScreen(Color.Red);

            this.W.DrawOnScreen(Color.Green);

            this.E.DrawOnScreen(Color.Blue);

            this.R.DrawOnScreen(Color.Yellow);
        }

        internal abstract void SetMenu();

        private void SetEvents()
        {
            Game.OnUpdate += this.OnGameUpdate;
            Render.OnPresent += this.OnRenderPresent;
        }
    }
}