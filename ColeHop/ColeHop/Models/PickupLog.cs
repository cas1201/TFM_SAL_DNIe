namespace ColeHop.Models
{
    public sealed class PickupLog
    {
        public string Id { get; }
        public string ChildId { get; }
        public string AuthorizedDni { get; }
        public string TeacherId { get; }
        public DateTime PickupDateTime { get; }

        public PickupLog(string id, string childId, string authorizedDni, string teacherId, DateTime pickupDateTime)
        {
            Id = id;
            ChildId = childId;
            AuthorizedDni = authorizedDni;
            TeacherId = teacherId;
            PickupDateTime = pickupDateTime;
        }
    }
}
