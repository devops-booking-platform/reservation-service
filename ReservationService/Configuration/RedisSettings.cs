namespace ReservationService.Configuration;

public class RedisSettings
{
    public string Host { get; set; } = "redis";
    public int Port { get; set; } = 6379;
    public string Password { get; set; } = "";
}