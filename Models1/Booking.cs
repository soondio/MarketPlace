namespace WebApplicationLab2.Models1
{
    public partial class Booking
    {
        public int Id { get; set; }

        public int ClientId { get; set; }

        public int ComputerId { get; set; }

        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public virtual Client Client { get; set; } = null!;

        public virtual Computer Computer { get; set; } = null!;
    }
}
