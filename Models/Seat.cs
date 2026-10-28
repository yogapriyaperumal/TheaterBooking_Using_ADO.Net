
namespace TheaterBooking.Models;

public class Seat
{
    public int SeatId { get; set; }

    public int ScreenId { get; set; }

    public string RowLabel { get; set; } = string.Empty;

    public int SeatNumber { get; set; }

    public int SeatTypeId { get; set; }

    public bool IsActive { get; set; } = true;

    // Example: A1, A2, B1
    public string Label => $"{RowLabel}{SeatNumber}";
}
