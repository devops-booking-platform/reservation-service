using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReservationService.Common.Constants;
using ReservationService.Domain.Entities;

namespace ReservationService.Data.Configurations
{
	public class ReservationConfiguration : IEntityTypeConfiguration<Reservation>
	{
		public void Configure(EntityTypeBuilder<Reservation> builder)
		{
			builder.ToTable("Reservations", t =>
			{
				t.HasCheckConstraint("CK_Reservations_GuestsCount_Positive", "[GuestsCount] > 0");
				t.HasCheckConstraint("CK_Reservations_Dates_Valid", "[EndDate] > [StartDate]");
				t.HasCheckConstraint("CK_Reservations_TotalPrice_NonNegative", "[TotalPrice] >= 0");
			});

			builder.HasKey(x => x.Id);

			builder.Property(x => x.Id)
				   .ValueGeneratedNever();

			builder.Property(x => x.AccommodationId)
				   .IsRequired();

			builder.Property(x => x.GuestId)
				   .IsRequired();

			builder.Property(x => x.HostId)
				   .IsRequired();

			builder.Property(x => x.IdempotencyKey)
				   .IsRequired();

			builder.Property(x => x.AccommodationName)
				   .IsRequired()
				   .HasMaxLength(ValidationConstants.MaxStringLength);

			builder.Property(x => x.GuestEmail)
				   .IsRequired()
				   .HasMaxLength(ValidationConstants.MaxStringLength);

			builder.Property(x => x.GuestUsername)
				   .IsRequired()
				   .HasMaxLength(ValidationConstants.MaxStringLength);

			builder.Property(x => x.StartDate)
				   .IsRequired()
				   .HasColumnType("date");

			builder.Property(x => x.EndDate)
				   .IsRequired()
				   .HasColumnType("date");

			builder.Property(x => x.GuestsCount)
				   .IsRequired();

			builder.Property(x => x.Status)
				   .IsRequired()
				   .HasConversion<string>();

			builder.Property(x => x.CreatedAt)
				   .IsRequired()
				   .HasColumnType("datetime2");

			builder.Property(x => x.TotalPrice)
				   .IsRequired()
				   .HasColumnType("decimal(18,2)");

			builder.HasIndex(x => x.AccommodationId);
			builder.HasIndex(x => x.GuestId);
			builder.HasIndex(x => x.HostId);
			builder.HasIndex(x => new { x.GuestId, x.IdempotencyKey }).IsUnique();
			builder.HasIndex(x => x.IdempotencyKey);
		}
	}
}
