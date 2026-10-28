
namespace TheaterBooking.Models;

public class Screen
{
    public int ScreenId { get; set; }

    public string ScreenName { get; set; } = string.Empty;

    public int TotalSeats { get; set; }

    public bool IsActive { get; set; } = true;
}
