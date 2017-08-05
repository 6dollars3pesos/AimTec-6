namespace Rapid_AIO.Utilities
{
    using System;
    using System.Collections.Generic;

    using Aimtec;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Prediction.Collision;
    using Aimtec.SDK.Prediction.Skillshots;

    using Spell = Aimtec.SDK.Spell;

    internal static class Prediction
    {
        internal static void CastEx(this Spell spell, Obj_AI_Base target)
        {
            var input = spell.GetPredictionInput(target);
            var output = GetPrediction(input);

            if (output.HitChance == HitChance.None) return;

            var collision = Collision.GetCollision(
                new List<Vector3> { output.UnitPosition, output.CastPosition },
                input);

            if (spell.Collision && collision.Count >= 1) return;

            spell.Cast(output.CastPosition);
        }

        private static float GetImpactTime(PredictionInput input)
        {
            float result = 0;
            var position = input.Unit.ServerPosition;

            if (input.AoE) input.Radius /= 2;

            if (!input.Unit.IsMoving) return 0;

            var wayPoints = input.Unit.GetWaypoints();

            for (var i = 0; i < wayPoints.Count - 1; i++)
            {
                var direction = (wayPoints[i].To3D() - wayPoints[i + 1].To3D()).Normalized();
                var velocity = input.Unit.MoveSpeed * direction;

                position.Extend(velocity, input.Delay);

                var toUnitDirection = (position - input.From).Normalized();
                var cosTheta = Vector3.Dot(direction, toUnitDirection);

                var a = Vector3.Dot(velocity, velocity) - (float)Math.Pow(input.Speed, 2);
                var b = 2 * Vector3.Dot(velocity, toUnitDirection) * cosTheta;
                var c = Vector3.Dot(toUnitDirection, toUnitDirection);

                var discriminant = Math.Pow(b, 2) - 4 * a * c;

                if (discriminant < 0) return 0;

                var sd = Math.Sqrt(discriminant);

                var t1 = 0.5 * (-b + sd) / a;
                var t2 = 0.5 * (-b - sd) / a;

                result = (float)Math.Min(t1, t2);

                if (result < 0) result = (float)Math.Max(t1, t2);

                if (result < 0) result = 0;
            }

            return result;
        }

        private static PredictionOutput GetPrediction(PredictionInput input)
        {
            if (!input.Unit.IsValidTarget())
                return new PredictionOutput
                           {
                               HitChance = HitChance.None,
                               CastPosition = Vector3.Zero,
                               UnitPosition = Vector3.Zero
                           };

            input.From = input.From - (input.Unit.ServerPosition - input.From).Normalized()
                         * ObjectManager.GetLocalPlayer().BoundingRadius;

            input.Delay += Game.Ping / 1000f;

            if (!input.Unit.IsMoving)
                return new PredictionOutput
                           {
                               HitChance = HitChance.None,
                               CastPosition = Vector3.Zero,
                               UnitPosition = Vector3.Zero
                           };

            var wayPoints = input.Unit.GetWaypoints();

            var unitPosition = Vector3.Zero;
            var castPosition = Vector3.Zero;

            for (var i = 0; i < wayPoints.Count - 1; i++)
            {
                var a = wayPoints[i];
                var b = wayPoints[i + 1];
                var direction = (b - a).Normalized().To3D();
                var velocity = direction * input.Unit.MoveSpeed;

                var impactTime = GetImpactTime(input);

                if (impactTime == 0)
                    return new PredictionOutput
                               {
                                   HitChance = HitChance.None,
                                   CastPosition = Vector3.Zero,
                                   UnitPosition = Vector3.Zero
                               };

                input.Speed /= (float)(Math.Atan(1) * 2);

                unitPosition = input.Unit.ServerPosition + velocity * (impactTime * input.Speed);

                if (input.From.Distance(unitPosition) > input.Range)
                    return new PredictionOutput
                               {
                                   HitChance = HitChance.None,
                                   CastPosition = Vector3.Zero,
                                   UnitPosition = Vector3.Zero
                               };

                var toCastPosition = (unitPosition - input.From).Normalized();
                var castDirection = (toCastPosition + direction) / 2;
                var cosTheta = Vector3.Dot(castDirection, toCastPosition);

                castPosition = unitPosition.Extend(
                    -direction,
                    (input.Unit.BoundingRadius + input.Radius) * cosTheta);
            }

            return new PredictionOutput
                       {
                           HitChance = HitChance.Low,
                           CastPosition = unitPosition,
                           UnitPosition = castPosition
                       };
        }
    }
}