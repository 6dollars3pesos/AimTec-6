using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Aimtec.SDK.Prediction.Skillshots
{
    /// <summary>
    /// Interface IPrediction
    /// </summary>
    public interface IPrediction
    {
        /// <summary>
        /// Calculates the target position.
        /// </summary>
        /// <param name="target">The target.</param>
        /// <param name="delay">The delay.</param>
        /// <param name="radius">The radius.</param>
        /// <param name="speed">The speed.</param>
        /// <param name="from">From.</param>
        /// <param name="type">The type.</param>
        /// <returns>PredictedTargetPosition.</returns>
        PredictedTargetPosition CalculateTargetPosition(
            Obj_AI_Base target,
            float delay,
            float radius,
            float speed,
            Vector3 from,
            SkillType type);

        /// <summary>
        /// Gets the prediction.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <returns>PredictionOutput.</returns>
        PredictionOutput GetPrediction(PredictionInput input);
    }

    /// <summary>
    /// Struct PredictedTargetPosition
    /// </summary>
    public struct PredictedTargetPosition
    {
        /// <summary>
        /// Gets or sets the cast position.
        /// </summary>
        /// <value>The cast position.</value>
        public Vector3 CastPosition { get; set; }

        /// <summary>
        /// Gets or sets the unit position.
        /// </summary>
        /// <value>The unit position.</value>
        public Vector3 UnitPosition { get; set; }
    }
}
