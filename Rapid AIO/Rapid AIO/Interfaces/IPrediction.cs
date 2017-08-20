namespace Rapid_AIO.Interfaces
{
    using Aimtec.SDK.Prediction.Skillshots;

    internal interface IPrediction
    {
        PredictionOutput GetDashPrediction(PredictionInput input);

        PredictionOutput GetIdlePrediction(PredictionInput input);

        PredictionOutput GetImmobilePrediction(PredictionInput input);

        PredictionOutput GetMovementPrediction(PredictionInput input);
    }
}