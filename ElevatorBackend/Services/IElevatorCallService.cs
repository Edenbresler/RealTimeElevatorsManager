using ElevatorBackend.Models;

namespace ElevatorBackend.Services
{
    public interface IElevatorCallService
    {
        Task CreateCallAsync(ElevatorCall call);
        Task<bool> UpdateDestinationFloorAsync(int callId, int destinationFloor);
        Task<bool> MarkCallAsHandledAsync(int callId);
    }
}