namespace Rapid
{
    using System;
    using System.Linq;

    using Aimtec;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Prediction.Skillshots;

    using static Prediction;

    using Spell = Aimtec.SDK.Spell;

    public static class Extensions
    {
        private static Obj_AI_Hero Player => ObjectManager.GetLocalPlayer();

        public static bool CastRapid(this Spell spell, Obj_AI_Hero target)
        {
            if (spell == null || target == null) return false;

            if (!target.IsValidTarget(spell.Range)) return false;

            var input = spell.GetPredictionInput(target);

            var output = GetPrediction(input);

            if (output == default(PredictionOutput) || output.HitChance == HitChance.OutOfRange) return false;

            if (output.CollisionObjects.Count >= 1 || output.HitChance == HitChance.Collision) return false;

            spell.Cast(output.CastPosition);
            return true;
        }

        internal static float GetImmobileTime(this Obj_AI_Hero target)
        {           

            var result = target.Buffs
                .Where(
                    buff => buff.IsActive && Game.ClockTime <= buff.EndTime
                            && (buff.Type == BuffType.Charm || buff.Type == BuffType.Knockup
                                || buff.Type == BuffType.Stun || buff.Type == BuffType.Suppression
                                || buff.Type == BuffType.Snare)).Aggregate(
                    0f,
                    (current, buff) => Math.Max(current, buff.EndTime));
            return result - Game.ClockTime;
        }

        internal static bool IsGoingToHit(this PredictionInput input, Vector3 castPosition)
        {
            var paths = input.Unit.GetWaypoints();

            for (var i = 0; i < paths.Count - 1; i++)
            {
                var previousPath = paths[i];
                var currentPath = paths[i + 1];
                var direction = (currentPath - previousPath).Normalized().To3D();
                var velocity = direction * input.Unit.MoveSpeed;

                var toCastDirection = (castPosition - input.From).Normalized();
                var cosTheta = Vector3.Dot(toCastDirection, direction);
                cosTheta = cosTheta < 0.1 || cosTheta > -0.1 ? 1 : cosTheta;

                var a = Vector3.Dot(velocity, velocity) - (float)Math.Pow(input.Speed, 2);
                var b = 2 * Vector3.Dot(velocity, toCastDirection) * cosTheta;
                var c = Vector3.Dot(toCastDirection, toCastDirection);

                var discriminant = Math.Pow(b, 2) - 4 * a * c;

                if (discriminant < 0) return false;
            }

            return true;
        }

        internal static bool IsImmobile(this Obj_AI_Hero target)
        {
            if (target.HasBuffOfType(BuffType.Knockup) || target.HasBuffOfType(BuffType.Stun)
                || target.HasBuffOfType(BuffType.Snare) || target.HasBuffOfType(BuffType.Suppression)
                || target.HasBuffOfType(BuffType.Charm)) return true;

            return false;
        }

        internal static float SetDelay(this float delay, float additionalDelay = 0.06f)
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
    }
}