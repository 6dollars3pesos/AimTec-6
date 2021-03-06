﻿namespace Rapid_AIO.Utilities
{
    using System;
    using System.Collections.Generic;

    using Aimtec;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Prediction.Collision;
    using Aimtec.SDK.Prediction.Skillshots;

    using Rapid_AIO.Interfaces;

    internal class Prediction : ISkillshotPrediction, IPrediction
    {
        public PredictionOutput GetDashPrediction(PredictionInput input)
        {
            throw new NotImplementedException();
        }

        public PredictionOutput GetIdlePrediction(PredictionInput input)
        {
            throw new NotImplementedException();
        }

        public PredictionOutput GetImmobilePrediction(PredictionInput input)
        {
            throw new NotImplementedException();
        }

        public PredictionOutput GetMovementPrediction(PredictionInput input)
        {
            var unitPosition = Vector3.Zero;
            var castPosition = Vector3.Zero;

            var paths = input.Unit.Path;

            for (var i = 0; i < paths.Length - 1; i++)
            {
                var previousPath = paths[i];
                var currentPath = paths[i + 1];
                var direction = (currentPath - previousPath).Normalized();
                var velocity = direction * input.Unit.MoveSpeed;

                unitPosition = input.Unit.ServerPosition + velocity * input.Delay;

                var distance = input.From.Distance(unitPosition);
                var impactTime = distance / input.Speed;

                if (input.Unit.ServerPosition.Distance(currentPath) / input.Unit.MoveSpeed < impactTime)
                {
                    castPosition = currentPath;
                    break;
                }

                unitPosition = input.Unit.ServerPosition + velocity * impactTime;


                var toUnitDirection = (castPosition - input.From).Normalized();
                var sinTheta = Vector3.Dot(direction, toUnitDirection);
                var cosTheta = Vector3.Dot(direction, direction);
                castPosition = unitPosition - direction * (sinTheta * (input.Unit.BoundingRadius + input.Radius) * cosTheta);

                toUnitDirection = (castPosition - input.From).Normalized();
                sinTheta = Vector3.Dot(direction, toUnitDirection);
                cosTheta = Vector3.Dot(toUnitDirection, toUnitDirection);
                castPosition = unitPosition + toUnitDirection * (sinTheta * (input.Unit.BoundingRadius + input.Radius) * cosTheta);

                var checkPosition = castPosition + velocity * input.Delay;
                if (input.From.Distance(checkPosition) > input.Range)
                    return new PredictionOutput { HitChance = HitChance.OutOfRange };
            }

            var collisionObjects = Collision.GetCollision(new List<Vector3> { castPosition }, input);

            return new PredictionOutput
                       {
                           UnitPosition = unitPosition,
                           CastPosition = castPosition,
                           CollisionObjects = collisionObjects,
                           HitChance = collisionObjects.Count >= 1
                                           ? HitChance.Collision
                                           : HitChance.Low
                       };
        }

        public PredictionOutput GetPrediction(PredictionInput input, bool ft, bool collision)
        {
            return this.GetPrediction(input);
        }

        public PredictionOutput GetPrediction(PredictionInput input)
        {
            var result = default(PredictionOutput);

            if (!input.Unit.IsValidTarget()) return result;

            input.From = input.From.SetFromPosition(input.Unit.ServerPosition);
            input.Delay = input.Delay.SetDelay();

            if (input.Unit.IsMoving) return this.GetMovementPrediction(input);

            return result;
        }
    }
}