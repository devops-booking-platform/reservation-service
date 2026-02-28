namespace ReservationService.Configuration
{
    public class RabbitMqSettings
    {
        public string Host { get; set; } = default!;
        public string User { get; set; } = default!;
        public string Pass { get; set; } = default!;
        public string VirtualHost { get; set; } = "/";
        public string Exchange { get; set; } = "integration";
    }
}
