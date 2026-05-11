namespace ColeHop.Models
{
    public sealed class Child
    {
        public string Id { get; init; } = default!;
        public string Name { get; init; } = default!;
        public string LastName { get; init; } = default!;
        public string EducationType { get; init; } = default!;
        public string Course { get; init; } = default!;
        public string Group { get; init; } = default!;
        public string TutorId { get; init; } = default!;
        public ApprovalStatus ApprovalStatus { get; set; } = ApprovalStatus.Pending;
        public string? RejectionReason { get; set; }
        public bool IsApproved => ApprovalStatus == ApprovalStatus.Approved;
        public bool IsRejected => ApprovalStatus == ApprovalStatus.Rejected;
        public bool IsPending => ApprovalStatus == ApprovalStatus.Pending;
        public string FullName => $"{Name} {LastName}";
        public string Grade => $"{EducationType} - {Course} {Group}";
    }
}
