using ColeHop.Services.Teacher;
using ColeHop.Services.TutorManagement;
using ColeHop.Models;

namespace ColeHop.Services.Teacher
{
    public sealed class MockTeacherService : ITeacherService
    {
        private readonly ITutorManagementService _tutorService;

        public MockTeacherService(ITutorManagementService tutorService)
        {
            _tutorService = tutorService;
        }

        public async Task<IReadOnlyList<Child>> GetPendingApprovalsAsync(string teacherId)
        {
            var allChildren = await _tutorService.GetChildrenAsync("tutor1");
            return allChildren.Where(c => c.IsPending).ToList();
        }

        public async Task ApproveChildAsync(string teacherId, string childId)
        {
            var allChildren = await _tutorService.GetChildrenAsync("tutor1");
            var child = allChildren.FirstOrDefault(c => c.Id == childId);
            if (child != null)
                child.ApprovalStatus = ApprovalStatus.Approved;
        }

        public async Task RejectChildAsync(string teacherId, string childId, string reason)
        {
            var allChildren = await _tutorService.GetChildrenAsync("tutor1");
            var child = allChildren.FirstOrDefault(c => c.Id == childId);
            if (child != null)
            {
                child.ApprovalStatus = ApprovalStatus.Rejected;
                child.RejectionReason = reason;
            }
        }
    }
}
