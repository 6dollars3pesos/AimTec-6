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

            if (output == Vector3.Zero) return;

            var collision = Collision.GetCollision(new List<Vector3>() { output }, input);

            if (spell.Collision && collision.Count >= 1)
            {
                return;
            }

            spell.Cast(output);
        }

        private static float GetImpactTime(PredictionInput input)
        {
            float result = 0;
            var position = input.Unit.ServerPosition;

            if (input.AoE)
            {
                input.Radius /= 2;
            }

            if (!input.Unit.IsMoving) return 0;

            var wayPoints = input.Unit.GetWaypoints();

            for (var i = 0; i < wayPoints.Count - 1; i++)
            {
                var direction = (wayPoints[i].To3D() - wayPoints[i + 1].To3D()).Normalized();

                position.Extend(direction, input.Unit.MoveSpeed * input.Delay);
                position.Extend(-direction, input.Unit.BoundingRadius);

                var toUnitDirection = (position - input.From).Normalized();
                var cosTheta = (direction * toUnitDirection).Length;
                var castDirection = (direction + toUnitDirection) / 2;
                position.Extend(-castDirection, input.Radius * cosTheta);

                var distance = position.Distance(input.From);

                var a = Math.Pow(input.Unit.MoveSpeed, 2) - Math.Pow(input.Speed, 2);
                var b = 2 * input.Unit.MoveSpeed * distance * cosTheta;
                var c = Math.Pow(distance, 2);

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

        private static Vector3 GetPrediction(PredictionInput input)
        {
            if (!input.Unit.IsValidTarget()) return Vector3.Zero;

            input.From = input.From - (input.Unit.ServerPosition - input.From).Normalized()
                         * ObjectManager.GetLocalPlayer().BoundingRadius;

            input.Delay += Game.Ping / 1000f;
            var result = Vector3.Zero;

            if (!input.Unit.IsMoving) return Vector3.Zero;

            var wayPoints = input.Unit.GetWaypoints();

            for (var i = 0; i < wayPoints.Count - 1; i++)
            {
                var a = wayPoints[i];
                var b = wayPoints[i + 1];

                var impactTime = GetImpactTime(input);

                if (impactTime == 0) return Vector3.Zero;

                var vcm = Vector2Extensions.VectorMovementCollision(
                    a,
                    b,
                    input.Unit.MoveSpeed,
                    (Vector2)input.From,
                    input.Speed,
                    impactTime);

                if (input.From.Distance(vcm.Item2) > input.Range)
                {
                    return Vector3.Zero;
                }

                result = vcm.Item2.To3D();
            }

            return result;
        }
    }
}