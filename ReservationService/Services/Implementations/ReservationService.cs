using ReservationService.Common.Exceptions;
using ReservationService.Domain.Entities;
using ReservationService.Domain.Enums;
using ReservationService.DTO;
using ReservationService.Infrastructure.Clients;
using ReservationService.Repositories.Interfaces;
using ReservationService.Services.Interfaces;

namespace ReservationService.Services.Implementations
{
	public class ReservationService(ICurrentUserService currentUserService,
		IReservationRepository reservationRepository,
		IAccommodationClient accommodationClient,
		IUnitOfWork unitOfWork) : IReservationService
	{

		public async Task CreateAsync(CreateReservationRequestDTO reservationRequest, CancellationToken ct = default)
		{
			var userId = currentUserService.UserId ?? throw new UnauthorizedAccessException();

			var info = await accommodationClient.GetAccommodationReservationInfoAsync(
				reservationRequest.AccommodationId,
				reservationRequest.StartDate,
				reservationRequest.EndDate,
				reservationRequest.GuestsCount,
				ct);

			if (reservationRequest.GuestsCount > info.MaxGuests)
				throw new MaxGuestsExceededException("Guests count exceeds accommodation capacity.");
			await EnsureNoApprovedOverlapAsync(
			reservationRequest.AccommodationId,
			reservationRequest.StartDate,
			reservationRequest.EndDate,
			ct);
			// TODO: Decline all reservation within accepted reservation dates
			var status = info.IsAutoAcceptEnabled ? ReservationStatus.Approved : ReservationStatus.Pending;
			var reservation = new Reservation(
				accommodationId: reservationRequest.AccommodationId,
				guestId: userId,
				hostId: info.HostId,
				accommodationName: info.Name,
				guestEmail: currentUserService.Email ?? throw new InvalidOperationException("Email missing in token."),
				guestUsername: currentUserService.Username ?? throw new InvalidOperationException("Username missing in token."),
				startDate: reservationRequest.StartDate,
				endDate: reservationRequest.EndDate,
				guestsCount: reservationRequest.GuestsCount,
				totalPrice: info.TotalPrice,
				status: status
			);
			await reservationRepository.AddAsync(reservation);
			await unitOfWork.SaveChangesAsync();
		}

		private async Task EnsureNoApprovedOverlapAsync(Guid accommodationId, DateOnly startDate, DateOnly endDate, CancellationToken ct = default)
		{
			var hasOverlap = await reservationRepository.HasOverlappingApprovedReservationAsync(accommodationId, startDate, endDate, ct);
			if (hasOverlap)
				throw new ConflictException("Accommodation already has an approved reservation for the selected dates.");
		}
	}
}
