using ElevatorBackend.Models;

public class ElevatorCallAssignment
{
    public int Id { get; set; }

    public int ElevatorId { get; set; }
    public Elevator Elevator { get; set; } = null!;

    public int ElevatorCallId { get; set; }
    public ElevatorCall ElevatorCall { get; set; } = null!;

    public DateTime AssignmentTime { get; set; }
}
