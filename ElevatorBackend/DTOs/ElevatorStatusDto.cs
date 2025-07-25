public class ElevatorStatusDto
{
    public int ElevatorId { get; set; }
    public int CurrentFloor { get; set; }
    public string Direction { get; set; }
    public string Status { get; set; }
    public string DoorStatus { get; set; }
    public int? LastCallId { get; set; }

}
