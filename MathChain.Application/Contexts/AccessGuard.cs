using MathChain.Domain.Entities;
using MathChain.Domain.Enums;
using MathChain.Domain.Exceptions;
using MathChain.Domain.Gateways;
using MathChain.Domain.Interfaces;

namespace MathChain.Application.Contexts
{
    public class AccessGuard
    {
        private readonly ICurrentUser _currentUser;
        private readonly IClassRoomGateway _classRoomGateway;

        public AccessGuard(ICurrentUser currentUser, IClassRoomGateway classRoomGateway)
        {
            _currentUser = currentUser;
            _classRoomGateway = classRoomGateway;
        }

        public bool IsAdmin => _currentUser.Role == UserRole.Admin;

        public string RequireWallet()
        {
            if (!_currentUser.IsAuthenticated || string.IsNullOrWhiteSpace(_currentUser.WalletAddress))
            {
                throw new UnauthorizedException("You must be logged in to do this.");
            }

            return _currentUser.WalletAddress.Trim().ToLowerInvariant();
        }

        public string RequireRole(params UserRole[] allowedRoles)
        {
            var wallet = RequireWallet();

            if (!IsAdmin && (_currentUser.Role == null || !allowedRoles.Contains(_currentUser.Role.Value)))
            {
                throw new ForbiddenException("Your role does not allow this action.");
            }

            return wallet;
        }

        public string RequireSelfOrAdmin(string walletAddress)
        {
            var wallet = RequireWallet();

            if (!IsAdmin && !IsSameWallet(wallet, walletAddress))
            {
                throw new ForbiddenException("You can only access your own data.");
            }

            return wallet;
        }

        public bool IsOwnerOrAdmin(ClassRoom classRoom)
        {
            return IsAdmin || IsSameWallet(RequireWallet(), classRoom.TeacherWallet);
        }

        public async Task<ClassRoom> RequireOwnerAsync(Guid classRoomId)
        {
            var classRoom = await GetClassRoomOrThrowAsync(classRoomId);

            if (!IsOwnerOrAdmin(classRoom))
            {
                throw new ForbiddenException("Only the teacher of this classroom can do this.");
            }

            return classRoom;
        }

        public async Task<ClassRoom> RequireMemberAsync(Guid classRoomId)
        {
            var classRoom = await GetClassRoomOrThrowAsync(classRoomId);

            if (IsOwnerOrAdmin(classRoom) || await IsEnrolledAsync(classRoom.Id))
            {
                return classRoom;
            }

            throw new ForbiddenException("You are not a member of this classroom.");
        }

        public async Task<string> RequireEnrolledStudentAsync(Guid classRoomId)
        {
            await GetClassRoomOrThrowAsync(classRoomId);

            if (!await IsEnrolledAsync(classRoomId))
            {
                throw new ForbiddenException("Only students enrolled in this classroom can do this.");
            }

            return RequireWallet();
        }

        private async Task<bool> IsEnrolledAsync(Guid classRoomId)
        {
            return await _classRoomGateway.IsStudentEnrolledAsync(classRoomId, RequireWallet());
        }

        private async Task<ClassRoom> GetClassRoomOrThrowAsync(Guid classRoomId)
        {
            RequireWallet();

            var classRoom = await _classRoomGateway.GetByIdAsync(classRoomId);

            if (classRoom == null)
            {
                throw new NotFoundException("Classroom not found.");
            }

            return classRoom;
        }

        private static bool IsSameWallet(string first, string? second)
        {
            return string.Equals(first.Trim(), second?.Trim(), StringComparison.OrdinalIgnoreCase);
        }
    }
}
