
using TheaterBooking.Helpers;
using TheaterBooking.Models;
using TheaterBooking.Repositories;

namespace TheaterBooking.Services;

public class BookingService
{
    private readonly BookingRepository _repository;

    public BookingService(BookingRepository repository)
    {
        _repository = repository
            ?? throw new ArgumentNullException(nameof(repository));
    }

    // 1. Validate input and book seats.
    public Task<int> BookSeatsAsync(
        int customerId,
        int showId,
        IReadOnlyList<int> seatIds)
    {
        if (customerId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(customerId),
                "Select a valid customer.");
        }

        if (showId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(showId),
                "Select a valid show.");
        }

        if (seatIds is null ||
            seatIds.Count < 1 ||
            seatIds.Count > PricingHelper.MaxSeatsPerBooking)
        {
            throw new ArgumentException(
                $"Select 1 to {PricingHelper.MaxSeatsPerBooking} seats.",
                nameof(seatIds));
        }

        if (seatIds.Any(seatId => seatId <= 0))
        {
            throw new ArgumentException(
                "Seat IDs must be positive.",
                nameof(seatIds));
        }

        if (seatIds.Distinct().Count() != seatIds.Count)
        {
            throw new ArgumentException(
                "The same seat cannot be selected twice.",
                nameof(seatIds));
        }

        // Repository handles the database transaction.
        return _repository.BookSeatsAsync(
            customerId, showId, seatIds);
    }

    // 2. Get a booking by ID.
    public async Task<Booking?> GetBookingByIdAsync(int bookingId)
    {
        if (bookingId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bookingId),
                "Booking ID must be positive.");
        }

        return await _repository.GetByIdAsync(bookingId);
    }

    // 3. Get all bookings for a customer.
    public async Task<List<Booking>> GetCustomerBookingsAsync(
        int customerId)
    {
        if (customerId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(customerId),
                "Customer ID must be positive.");
        }

        return await _repository.GetByCustomerIdAsync(customerId);
    }

    // 4. Get seats associated with a booking.
    public async Task<List<BookingSeat>> GetBookingSeatsAsync(
        int bookingId)
    {
        if (bookingId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(bookingId),
                "Booking ID must be positive.");
        }

        return await _repository.GetSeatsByBookingIdAsync(bookingId);
    }
}
