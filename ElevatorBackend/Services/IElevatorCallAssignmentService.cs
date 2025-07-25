using ElevatorBackend.Models;

namespace ElevatorBackend.Services
{
    public interface IElevatorCallAssignmentService
    {
        Task AssignElevatorToCallAsync(ElevatorCallAssignment assignment);
        Task<List<ElevatorCallAssignment>> GetAssignmentsByCallIdAsync(int callId);  // ← שורה זו חדשה
        Task<List<ElevatorCallAssignment>> GetAssignmentsByElevatorIdAsync(int elevatorId);
    }
}
