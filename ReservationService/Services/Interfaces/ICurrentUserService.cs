namespace ReservationService.Services.Interfaces
{
    public interface ICurrentUserService
    {
        Guid? UserId { get; }
		string? Email { get; }
		string? Username { get; }
		string? Role { get; }
        bool IsAuthenticated { get; }
    }
}
