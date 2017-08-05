﻿namespace Aimtec.SDK.Prediction.Skillshots
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    using Aimtec.SDK.Events;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Menu.Config;
    using Aimtec.SDK.Prediction.Collision;
    using Aimtec.SDK.Prediction.Skillshots.AoE;

    // todo modular
    public class Prediction
    {
        #region Public Methods and Operators

        public static PredictionOutput GetPrediction(Obj_AI_Base unit, float delay)
        {
            return GetPrediction(new PredictionInput { Unit = unit, Delay = delay });
        }

        public static PredictionOutput GetPrediction(Obj_AI_Base unit, float delay, float radius)
        {
            return GetPrediction(new PredictionInput { Unit = unit, Delay = delay, Radius = radius });
        }

        public static PredictionOutput GetPrediction(Obj_AI_Base unit, float delay, float radius, float speed)
        {
            return GetPrediction(new PredictionInput { Unit = unit, Delay = delay, Radius = radius, Speed = speed });
        }

        public static PredictionOutput GetPrediction(PredictionInput input)
        {
            return GetPrediction(input, true, true);
        }

        #endregion

        #region Methods

        internal static PredictionOutput GetAdvancedPrediction(PredictionInput input, float additionalSpeed = 0)
        {
            var speed = Math.Abs(additionalSpeed) < float.Epsilon ? input.Speed : input.Speed * additionalSpeed;

            if (Math.Abs(speed - int.MaxValue) < float.Epsilon)
            {
                speed = 90000;
            }

            var unit = input.Unit;
            var position = PositionAfter(unit, 1, unit.MoveSpeed - 100);
            var prediction = position + speed * (input.Delay / 1000);

            return new PredictionOutput()
                       {
                           UnitPosition = new Vector3(position.X, position.Y, unit.ServerPosition.Z),
                           CastPosition = new Vector3(prediction.X, prediction.Y, unit.ServerPosition.Z),
                           HitChance = HitChance.High
                       };
        }

        internal static PredictionOutput GetDashingPrediction(PredictionInput input)
        {
            var dashData = input.Unit.GetDashInfo();
            var result = new PredictionOutput { Input = input };
            input.Delay += 0.1f;

            if (dashData.IsBlink)
            {
                return result;
            }

            var dashPred = GetPositionOnPath(
                input,
                new List<Vector2> { (Vector2)input.Unit.ServerPosition, dashData.Path.Last() },
                dashData.Speed);
            if (dashPred.HitChance >= HitChance.High)
            {
                dashPred.CastPosition = dashPred.UnitPosition;
                dashPred.HitChance = HitChance.Dashing;
                return dashPred;
            }

            if (dashData.Path.GetPathLength() > 200)
            {
                var endP = dashData.Path.Last();
                var timeToPoint = input.Delay + input.From.Distance(endP) / input.Speed;
                if (timeToPoint <= input.Unit.Distance(endP) / dashData.Speed + input.RealRadius / input.Unit.MoveSpeed)
                {
                    return new PredictionOutput
                               {
                                   CastPosition = (Vector3)endP, UnitPosition = (Vector3)endP,
                                   HitChance = HitChance.Dashing
                               };
                }
            }

            result.CastPosition = (Vector3)dashData.Path.Last();
            result.UnitPosition = result.CastPosition;

            return result;
        }

        internal static PredictionOutput GetImmobilePrediction(PredictionInput input, double remainingImmobileT)
        {
            var timeToReachTargetPosition = input.Delay + input.Unit.Distance(input.From) / input.Speed;

            if (timeToReachTargetPosition <= remainingImmobileT + input.RealRadius / input.Unit.MoveSpeed)
            {
                return new PredictionOutput
                           {
                               CastPosition = input.Unit.ServerPosition, UnitPosition = input.Unit.Position,
                               HitChance = HitChance.Immobile
                           };
            }

            return new PredictionOutput
                       {
                           Input = input, CastPosition = input.Unit.ServerPosition,
                           UnitPosition = input.Unit.ServerPosition, HitChance = HitChance.High
                       };
        }

        internal static PredictionOutput GetPositionOnPath(PredictionInput input, List<Vector2> path, float speed = -1)
        {
            speed = Math.Abs(speed) < float.Epsilon ? input.Unit.MoveSpeed : speed;

            if (path.Count <= 1 || (input.Unit.SpellBook.IsAutoAttacking && !input.Unit.IsDashing()))
            {
                return new PredictionOutput
                           {
                               Input = input, UnitPosition = input.Unit.ServerPosition,
                               CastPosition = input.Unit.ServerPosition, HitChance = HitChance.VeryHigh
                           };
            }

            var pLength = path.GetPathLength();

            if (pLength >= input.Delay * speed - input.RealRadius
                && Math.Abs(input.Speed - float.MaxValue) < float.Epsilon)
            {
                var tDistance = input.Delay * speed - input.RealRadius;

                for (var i = 0; i < path.Count - 1; i++)
                {
                    var a = path[i];
                    var b = path[i + 1];
                    var d = a.Distance(b);

                    if (d >= tDistance)
                    {
                        var direction = (b - a).Normalized();

                        var cp = a + direction * tDistance;
                        var p = a + direction * (i == path.Count - 2
                                                     ? Math.Min(tDistance + input.RealRadius, d)
                                                     : tDistance + input.RealRadius);

                        return new PredictionOutput
                                   {
                                       Input = input, CastPosition = (Vector3)cp, UnitPosition = (Vector3)p,
                                       HitChance = PathTracker.GetCurrentPath(input.Unit).Time < 0.1d
                                                       ? HitChance.VeryHigh
                                                       : HitChance.High
                                   };
                    }

                    tDistance -= d;
                }
            }

            if (pLength >= input.Delay * speed - input.RealRadius
                && Math.Abs(input.Speed - float.MaxValue) > float.Epsilon)
            {
                var d = input.Delay * speed - input.RealRadius;
                if (input.Type == SkillshotType.Line || input.Type == SkillshotType.Cone)
                {
                    if (input.From.DistanceSquared(input.Unit.ServerPosition) < 200 * 200)
                    {
                        d = input.Delay * speed;
                    }
                }

                path = path.CutPath(d);
                var tT = 0f;
                for (var i = 0; i < path.Count - 1; i++)
                {
                    var a = path[i];
                    var b = path[i + 1];
                    var tB = a.Distance(b) / speed;
                    var direction = (b - a).Normalized();
                    a = a - speed * tT * direction;
                    var sol = Vector2Extensions.VectorMovementCollision(
                        a,
                        b,
                        speed,
                        (Vector2)input.From,
                        input.Speed,
                        tT);
                    var t = sol.Item1;
                    var pos = sol.Item2;

                    if (!pos.IsZero && t >= tT && t <= tT + tB)
                    {
                        if (pos.DistanceSquared(b) < 20)
                        {
                            break;
                        }

                        var p = pos + input.RealRadius * direction;

                        return new PredictionOutput
                                   {
                                       Input = input, CastPosition = (Vector3)pos, UnitPosition = (Vector3)p,
                                       HitChance = PathTracker.GetCurrentPath(input.Unit).Time < 0.1d
                                                       ? HitChance.VeryHigh
                                                       : HitChance.High
                                   };
                    }

                    tT += tB;
                }
            }

            var position = path.Last();

            return new PredictionOutput
                       {
                           Input = input, CastPosition = (Vector3)position, UnitPosition = (Vector3)position,
                           HitChance = HitChance.Medium
                       };
        }

        /// <summary>
        ///     Returns Calculated Prediction based off given data values.
        /// </summary>
        /// <param name="input">
        ///     <see cref="PredictionInput" /> input
        /// </param>
        /// <param name="ft">Add Delay</param>
        /// <param name="checkCollision">Check Collision</param>
        /// <returns>
        ///     <see cref="PredictionOutput" /> output
        /// </returns>
        internal static PredictionOutput GetPrediction(PredictionInput input, bool ft, bool checkCollision)
        {
            PredictionOutput result = null;

            if (!input.Unit.IsValidTarget())
            {
                return new PredictionOutput();
            }

            input.From = input.From - (input.Unit.ServerPosition - input.From).Normalized()
                         * ObjectManager.GetLocalPlayer().BoundingRadius;

            if (ft)
            {
                if (!AimtecMenu.UseBetaPrediction)
                {
                    input.Delay += Game.Ping / 2000f + 0.06f;
                }

                if (input.AoE)
                {
                    return AoePrediction.GetAoEPrediction(input);
                }
            }

            if (Math.Abs(input.Range - float.MaxValue) > float.Epsilon && input.Unit.DistanceSqr(input.RangeCheckFrom)
                > Math.Pow(input.Range * 1.5, 2))
            {
                return new PredictionOutput { Input = input };
            }

            if (input.Unit.IsDashing())
            {
                result = GetDashingPrediction(input);
            }
            else
            {
                var remainingImmobileT = UnitIsImmobileUntil(input.Unit);
                if (remainingImmobileT >= 0d)
                {
                    result = GetImmobilePrediction(input, remainingImmobileT);
                }
            }

            if (result == null)
            {
                if (input.Speed != float.MaxValue)
                {
                    result = GetStandardPrediction(input);
                }
                else
                {
                    result = AimtecMenu.UseBetaPrediction ? GetRapidPrediction(input) : GetStandardPrediction(input);
                }
            }

            if (result.HitChance == HitChance.None)
            {
                result = GetStandardPrediction(input);
            }
           

            if (Math.Abs(input.Range - float.MaxValue) > float.Epsilon)
            {
                if (result.HitChance >= HitChance.High && input.RangeCheckFrom.DistanceSqr(input.Unit.Position)
                    > Math.Pow(input.Range + input.RealRadius * 3 / 4, 2))
                {
                    result.HitChance = HitChance.Medium;
                }

                if (input.RangeCheckFrom.DistanceSqr(result.UnitPosition) > Math.Pow(
                        input.Range + (input.Type == SkillshotType.Circle ? input.RealRadius : 0),
                        2))
                {
                    result.HitChance = HitChance.OutOfRange;
                }

                if (input.RangeCheckFrom.DistanceSqr(result.CastPosition) > Math.Pow(input.Range, 2))
                {
                    if (result.HitChance != HitChance.OutOfRange)
                    {
                        result.CastPosition = input.RangeCheckFrom
                                              + input.Range
                                              * (result.UnitPosition - input.RangeCheckFrom).Normalized().FixHeight();
                    }
                    else
                    {
                        result.HitChance = HitChance.OutOfRange;
                    }
                }
            }

            if (!checkCollision || !input.Collision)
            {
                return result;
            }

            var positions = new List<Vector3> { result.UnitPosition, result.CastPosition, input.Unit.Position };
            var originalUnit = input.Unit;

            result.CollisionObjects = Collision.GetCollision(positions, input);
            result.CollisionObjects.RemoveAll(x => x.NetworkId == originalUnit.NetworkId);
            result.HitChance = result.CollisionObjects.Count > 0 ? HitChance.Collision : result.HitChance;

            return result;
        }

        internal static PredictionOutput GetRapidPrediction(PredictionInput input)
        {
            if (!input.Unit.IsValidTarget())
                return new PredictionOutput
                           {
                              HitChance = HitChance.None, CastPosition = Vector3.Zero, UnitPosition = Vector3.Zero 
                           };

            input.From = input.From - (input.Unit.ServerPosition - input.From).Normalized()
                         * ObjectManager.GetLocalPlayer().BoundingRadius;

            input.Delay += Game.Ping / 1000f;

            if (!input.Unit.IsMoving)
                return new PredictionOutput
                           {
                              HitChance = HitChance.None, CastPosition = Vector3.Zero, UnitPosition = Vector3.Zero 
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
                                  HitChance = HitChance.None, CastPosition = Vector3.Zero, UnitPosition = Vector3.Zero 
                               };

                input.Speed /= (float)(Math.Atan(1) * 2);

                unitPosition = input.Unit.ServerPosition + velocity * (impactTime * input.Speed);

                if (input.From.Distance(unitPosition) > input.Range)
                    return new PredictionOutput
                               {
                                  HitChance = HitChance.None, CastPosition = Vector3.Zero, UnitPosition = Vector3.Zero 
                               };

                var toCastDirection = (unitPosition - input.From).Normalized();
                var castDirection = (toCastDirection + direction) / 2;
                var cosTheta = Vector3.Dot(castDirection, toCastDirection);

                castPosition = unitPosition.Extend(-direction * cosTheta, input.Unit.BoundingRadius / (float)Math.Atan(1) * 2);
            }

            return new PredictionOutput
                       {
                          HitChance = HitChance.Low, CastPosition = castPosition, UnitPosition = unitPosition 
                       };
        }

        internal static PredictionOutput GetStandardPrediction(PredictionInput input)
        {
            var speed = input.Unit.MoveSpeed;

            if (input.Unit.DistanceSqr(input.From) < 200 * 200)
            {
                // input.Delay /= 2;
                speed /= 1.5f;
            }

            var result = GetPositionOnPath(input, input.Unit.GetWaypoints(), speed);

            if (result.HitChance >= HitChance.High && input.Unit is Obj_AI_Hero)
            {
            }

            return result;
        }

        internal static Vector2 PositionAfter(Obj_AI_Base unit, float t, float speed = float.MaxValue)
        {
            var distance = t * speed;
            var path = unit.Path.Select(x => (Vector2)x).ToArray();

            for (var i = 0; i < path.Length - 1; i++)
            {
                var a = path[i];
                var b = path[i + 1];
                var d = a.Distance(b);

                if (d < distance)
                {
                    distance -= d;
                }
                else
                {
                    return a + distance * (b - a).Normalized();
                }
            }

            return path[path.Length - 1];
        }

        internal static double UnitIsImmobileUntil(Obj_AI_Base unit)
        {
            var result = unit.Buffs
                .Where(
                    buff => buff.IsActive && Game.ClockTime <= buff.EndTime
                            && (buff.Type == BuffType.Charm || buff.Type == BuffType.Knockup
                                || buff.Type == BuffType.Stun || buff.Type == BuffType.Suppression
                                || buff.Type == BuffType.Snare)).Aggregate(
                    0d,
                    (current, buff) => Math.Max(current, buff.EndTime));
            return result - Game.ClockTime;
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

        #endregion
    }
}