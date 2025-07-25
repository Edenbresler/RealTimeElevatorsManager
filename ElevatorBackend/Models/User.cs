namespace ElevatorBackend.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;

        public ICollection<Building> Buildings { get; set; } = new List<Building>();
    }
}
