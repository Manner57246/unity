using Rouge.Core;
namespace Rouge.Tests
{
    internal static class TravelTestExtensions
    {
        public static bool Travel(this RunSession run,int id)
        {
            if (!run.BeginTravel(id)) return false;
            while (run.Phase == RunPhase.Travelling) run.AdvanceJourney();
            if(run.IsRoadside && run.Phase==RunPhase.Event)run.LeaveEvent(run.Encounter);
            return true;
        }
    }
}
