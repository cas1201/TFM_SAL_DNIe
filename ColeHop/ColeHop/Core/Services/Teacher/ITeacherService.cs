using ColeHop.Model.Domain;

namespace ColeHop.Core.Services.Teacher
{
    public interface ITeacherService
    {
        Task<IReadOnlyList<Child>> GetPendingApprovalsAsync(string teacherId);
        Task ApproveChildAsync(string teacherId, string childId);
        Task RejectChildAsync(string teacherId, string childId, string reason);
    }
}
