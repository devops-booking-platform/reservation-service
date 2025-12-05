using ReservationService.Domain.Enums;

namespace ReservationService.Domain.Entities
{
	public class Reservation : EntityWithGuidId
	{
		public Guid AccommodationId { get; private set; }
		public Guid GuestId { get; private set; }
		public Guid HostId { get; private set; }
		public Guid IdempotencyKey { get; private set; }
		public string AccommodationName { get; private set; } = default!;
		public string GuestEmail { get; private set; } = default!;
		public string GuestUsername { get; private set; } = default!;
		public DateTimeOffset StartDate { get; private set; }
		public DateTimeOffset EndDate { get; private set; }
		public int GuestsCount { get; private set; }
		public ReservationStatus Status { get; private set; }
		public DateTime CreatedAt { get; private set; }
		public decimal TotalPrice { get; private set; }

		private Reservation() { }
		public void Cancel()
		{
			if (Status == ReservationStatus.CancelledByGuest)
				return;
			ValidateCancellation();
			Status = ReservationStatus.CancelledByGuest;
		}
		private void ValidateCancellation()
		{
			if (Status != ReservationStatus.Approved)
				throw new InvalidOperationException("Only approved reservations can be cancelled.");
			var todayUtc = DateOnly.FromDateTime(DateTime.UtcNow);
			var startUtc = DateOnly.FromDateTime(StartDate.UtcDateTime);
			if (todayUtc >= startUtc)
				throw new InvalidOperationException("Reservation can be cancelled only until the day before start date.");
		}
		public Reservation(
			Guid accommodationId,
			Guid guestId,
			Guid hostId,
			string accommodationName,
			string guestEmail,
			string guestUsername,
			DateTimeOffset startDate,
			DateTimeOffset endDate,
			int guestsCount,
			decimal totalPrice,
			ReservationStatus status,
			Guid idempotencyKey)
		{
			ValidateReservation(
				accommodationId,
				guestId,
				hostId,
				accommodationName,
				guestEmail,
				guestUsername,
				startDate,
				endDate,
				guestsCount,
				totalPrice);

			AccommodationId = accommodationId;
			GuestId = guestId;
			HostId = hostId;
			AccommodationName = accommodationName.Trim();
			GuestEmail = guestEmail.Trim();
			GuestUsername = guestUsername.Trim();
			StartDate = startDate;
			EndDate = endDate;
			GuestsCount = guestsCount;
			TotalPrice = totalPrice;
			Status = status;
			CreatedAt = DateTime.UtcNow;
			IdempotencyKey = idempotencyKey;
		}

		private static void ValidateReservation(
			Guid accommodationId,
			Guid guestId,
			Guid hostId,
			string accommodationName,
			string guestEmail,
			string guestUsername,
			DateTimeOffset startDate,
			DateTimeOffset endDate,
			int guestsCount,
			decimal totalPrice)
		{
			if (accommodationId == Guid.Empty)
				throw new ArgumentException("AccommodationId cannot be empty.", nameof(accommodationId));

			if (guestId == Guid.Empty)
				throw new ArgumentException("GuestId cannot be empty.", nameof(guestId));

			if (hostId == Guid.Empty)
				throw new ArgumentException("HostId cannot be empty.", nameof(hostId));

			if (string.IsNullOrWhiteSpace(accommodationName))
				throw new ArgumentException("Accommodation name is required.", nameof(accommodationName));

			if (string.IsNullOrWhiteSpace(guestEmail))
				throw new ArgumentException("Guest email is required.", nameof(guestEmail));

			if (string.IsNullOrWhiteSpace(guestUsername))
				throw new ArgumentException("Guest username is required.", nameof(guestUsername));

			if (endDate < startDate)
				throw new ArgumentOutOfRangeException(nameof(endDate), "End date must be after start date.");

			if (guestsCount <= 0)
				throw new ArgumentOutOfRangeException(nameof(guestsCount), "Guests count must be positive.");

			if (totalPrice < 0)
				throw new ArgumentOutOfRangeException(nameof(totalPrice), "Total price cannot be negative.");
		}
	}
}
