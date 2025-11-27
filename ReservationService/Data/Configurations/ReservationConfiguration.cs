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
            builder.ToTable("Reservations");

            builder.HasKey(x => x.Id);

            builder.Property(x => x.Id)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("NEWSEQUENTIALID()");

            builder.Property(x => x.AccommodationId)
                .IsRequired();

            builder.Property(x => x.GuestId)
                .IsRequired();

            builder.Property(x => x.GuestEmail)
                .IsRequired()
                .HasMaxLength(ValidationConstants.MaxStringLength);

            builder.Property(x => x.GuestUsername)
                .IsRequired()
                .HasMaxLength(ValidationConstants.MaxStringLength);

            builder.Property(x => x.GuestsCount)
                .IsRequired();

            builder.Property(x => x.StartDate)
                .IsRequired();

            builder.Property(x => x.EndDate)
                .IsRequired();

            builder.Property(x => x.Status)
                .IsRequired()
                .HasConversion<string>();

            builder.HasIndex(x => x.AccommodationId);
            builder.HasIndex(x => x.GuestId);
        }
    }
}
