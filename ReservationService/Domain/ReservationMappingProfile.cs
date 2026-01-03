using AutoMapper;
using ReservationService.Domain.Entities;
using ReservationService.DTO;

namespace ReservationService.Domain;

public class ReservationMappingProfile : Profile
{
    public ReservationMappingProfile()
    {
        CreateMap<Reservation, GetReservationResponse>();
    }
}