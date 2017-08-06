namespace Rapid
{
    using System.Collections.Generic;
    using System.Linq;

    using Aimtec;
    using Aimtec.SDK.Events;
    using Aimtec.SDK.Extensions;
    using Aimtec.SDK.Prediction.Collision;
    using Aimtec.SDK.Prediction.Skillshots;

    public static class Prediction
    {
        public static PredictionOutput GetPrediction(PredictionInput input)
        {
            var result = default(PredictionOutput);

            if (!input.Unit.IsValidTarget(input.Range)) return result;

            input.From = input.From.SetFromPosition(input.Unit.ServerPosition);
            input.Delay = input.Delay.SetDelay();

            if (input.Unit.IsDashing()) result = GetDashPrediction(input);
            if (input.Unit.IsImmobile()) result = GetImmobilePrediction(input);
            if (input.Unit.IsMoving) result = GetLinearPrediction(input);
            if (!input.Unit.IsMoving) result = GetIdlePrediction(input);

            return result;
        }

        private static PredictionOutput GetDashPrediction(PredictionInput input)
        {
            var dash = input.Unit.GetDashInfo();
            List<Obj_AI_Base> collisionObjects;

            if (dash.IsBlink)
            {
                collisionObjects = Collision.GetCollision(new List<Vector3> { dash.EndPos.To3D() }, input);

                if (dash.Path.GetPathLength() / input.Speed < dash.Duration)
                    return new PredictionOutput
                               {
                                   UnitPosition = dash.EndPos.To3D(),
                                   CastPosition = dash.EndPos.To3D(),
                                   CollisionObjects = collisionObjects,
                                   HitChance = collisionObjects.Count >= 1
                                                   ? HitChance.Collision
                                                   : HitChance.Dashing
                               };
            }

            var direction = (dash.EndPos - dash.StartPos).Normalized();
            var toTargetDirection = (dash.Unit.ServerPosition - input.From).Normalized().To2D();
            var cosTheta = Vector2.Dot(toTargetDirection, direction);
            cosTheta = cosTheta < 0.1 || cosTheta > -0.1 ? 1 : cosTheta;

            var vcm = Vector2Extensions.VectorMovementCollision(
                dash.StartPos,
                dash.EndPos,
                dash.Speed,
                (Vector2)input.From,
                input.Speed * cosTheta,
                input.Delay);

            collisionObjects = Collision.GetCollision(new List<Vector3> { vcm.Item2.To3D() }, input);

            return new PredictionOutput
                       {
                           UnitPosition = vcm.Item2.To3D(),
                           CastPosition = vcm.Item2.To3D(),
                           CollisionObjects = collisionObjects,
                           HitChance = collisionObjects.Count >= 1
                                           ? HitChance.Collision
                                           : HitChance.Dashing
                       };
        }

        private static PredictionOutput GetIdlePrediction(PredictionInput input)
        {
            var collisionObjects = Collision.GetCollision(new List<Vector3> { input.Unit.ServerPosition }, input);

            return new PredictionOutput
                       {
                           UnitPosition = input.Unit.ServerPosition,
                           CastPosition = input.Unit.ServerPosition,
                           CollisionObjects = collisionObjects,
                           HitChance = collisionObjects.Count >= 1
                                           ? HitChance.Collision
                                           : HitChance.Low
                       };
        }

        private static PredictionOutput GetImmobilePrediction(PredictionInput input)
        {
            var immobileTime = input.Unit.GetImmobileTime();
            var distance = input.From.Distance(input.Unit.ServerPosition);
            var collisionObjects = Collision.GetCollision(new List<Vector3> { input.Unit.ServerPosition }, input);

            if (distance / input.Speed < immobileTime)
                return new PredictionOutput
                           {
                               UnitPosition = input.Unit.ServerPosition,
                               CastPosition = input.Unit.ServerPosition,
                               CollisionObjects = collisionObjects,
                               HitChance = collisionObjects.Count >= 1
                                               ? HitChance.Collision
                                               : HitChance.Immobile
                           };

            return new PredictionOutput
                       {
                           UnitPosition = input.Unit.ServerPosition,
                           CastPosition = input.Unit.ServerPosition,
                           CollisionObjects = collisionObjects,
                           HitChance = collisionObjects.Count >= 1
                                           ? HitChance.Collision
                                           : HitChance.Low
                       };
        }

        private static PredictionOutput GetLinearPrediction(PredictionInput input)
        {
            var paths = input.Unit.GetWaypoints();

            var unitPosition = input.Unit.ServerPosition;
            var castPosition = input.Unit.ServerPosition;

            for (var i = 0; i < paths.Count - 1; i++)
            {
                var previousPath = paths[i];
                var currentPath = paths[i + 1];
                var direction = (currentPath - previousPath).Normalized();
                var velocity = direction * input.Unit.MoveSpeed;

                unitPosition = unitPosition.Extend(velocity, input.Delay);

                var toTargetDirection = (unitPosition - input.From).Normalized().To2D();
                var cosTheta = Vector2.Dot(toTargetDirection, direction);

                cosTheta = cosTheta < 0.1 || cosTheta > -0.1 ? 1 : cosTheta;

                var vcm = Vector2Extensions.VectorMovementCollision(
                    previousPath,
                    currentPath,
                    input.Unit.MoveSpeed,
                    (Vector2)input.From,
                    input.Speed * cosTheta,
                    input.Delay);

                if (!input.IsGoingToHit(vcm.Item2.To3D()) || input.From.Distance(vcm.Item2) > input.Range)
                    return new PredictionOutput { HitChance = HitChance.OutOfRange };

                var pathLength = (currentPath - previousPath).Length;

                if (pathLength / input.Unit.MoveSpeed < vcm.Item1)
                {
                    unitPosition = currentPath.To3D();
                    continue;
                }

                if (currentPath == paths.Last()) unitPosition = paths.Last().To3D();

                unitPosition = vcm.Item2.To3D();
                castPosition = unitPosition;
            }

            var collisionObjects = Collision.GetCollision(new List<Vector3> { unitPosition, castPosition }, input);

            return new PredictionOutput
                       {
                           UnitPosition = unitPosition,
                           CastPosition = castPosition,
                           CollisionObjects = collisionObjects,
                           HitChance = collisionObjects.Count >= 1
                                           ? HitChance.Collision
                                           : HitChance.Medium
                       };
        }
    }
}