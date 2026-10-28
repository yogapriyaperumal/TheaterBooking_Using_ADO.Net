
using System.Data;
using Microsoft.Data.SqlClient;
using TheaterBooking.Helpers;
using TheaterBooking.Models;

namespace TheaterBooking.Repositories;

public class BookingRepository
{
    // 1. Add a booking record.
    public async Task<int> AddAsync(Booking booking)
    {
        const string sql = """
            INSERT INTO Bookings
                (CustomerId, ShowId, BookingDate, TotalAmount, Status)
            OUTPUT INSERTED.BookingId
            VALUES
                (@CustomerId, @ShowId, @BookingDate,
                 @TotalAmount, @Status);
            """;

        await using SqlConnection connection =
            DatabaseHelper.CreateConnection();

        await connection.OpenAsync();

        await using SqlCommand command = new(sql, connection);

        command.Parameters.Add("@CustomerId", SqlDbType.Int)
            .Value = booking.CustomerId;

        command.Parameters.Add("@ShowId", SqlDbType.Int)
            .Value = booking.ShowId;

        command.Parameters.Add("@BookingDate", SqlDbType.DateTime2)
            .Value = booking.BookingDate;

        AddDecimalParameter(
            command, "@TotalAmount", booking.TotalAmount);

        command.Parameters.Add("@Status", SqlDbType.NVarChar, 20)
            .Value = booking.Status;

        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    // 2. Get a booking by its ID.
    public async Task<Booking?> GetByIdAsync(int bookingId)
    {
        const string sql = """
            SELECT BookingId, CustomerId, ShowId, BookingDate,
                   TotalAmount, Status
            FROM Bookings
            WHERE BookingId = @BookingId;
            """;

        await using SqlConnection connection =
            DatabaseHelper.CreateConnection();

        await connection.OpenAsync();

        await using SqlCommand command = new(sql, connection);

        command.Parameters.Add("@BookingId", SqlDbType.Int)
            .Value = bookingId;

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
            return MapBooking(reader);

        return null;
    }

    // 3. Get all bookings belonging to a customer.
    public async Task<List<Booking>> GetByCustomerIdAsync(
        int customerId)
    {
        const string sql = """
            SELECT BookingId, CustomerId, ShowId, BookingDate,
                   TotalAmount, Status
            FROM Bookings
            WHERE CustomerId = @CustomerId
            ORDER BY BookingDate DESC;
            """;

        List<Booking> bookings = new();

        await using SqlConnection connection =
            DatabaseHelper.CreateConnection();

        await connection.OpenAsync();

        await using SqlCommand command = new(sql, connection);

        command.Parameters.Add("@CustomerId", SqlDbType.Int)
            .Value = customerId;

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
            bookings.Add(MapBooking(reader));

        return bookings;
    }

    // 4. Add a seat to an existing booking.
    public async Task<int> AddBookingSeatAsync(
        BookingSeat bookingSeat)
    {
        const string sql = """
            INSERT INTO BookingSeats
                (BookingId, ShowId, SeatId, Price)
            OUTPUT INSERTED.BookingSeatId
            VALUES
                (@BookingId, @ShowId, @SeatId, @Price);
            """;

        await using SqlConnection connection =
            DatabaseHelper.CreateConnection();

        await connection.OpenAsync();

        await using SqlCommand command = new(sql, connection);

        command.Parameters.Add("@BookingId", SqlDbType.Int)
            .Value = bookingSeat.BookingId;

        command.Parameters.Add("@ShowId", SqlDbType.Int)
            .Value = bookingSeat.ShowId;

        command.Parameters.Add("@SeatId", SqlDbType.Int)
            .Value = bookingSeat.SeatId;

        AddDecimalParameter(command, "@Price", bookingSeat.Price);

        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    // 5. Get all seats included in a booking.
    public async Task<List<BookingSeat>> GetSeatsByBookingIdAsync(
        int bookingId)
    {
        const string sql = """
            SELECT BookingSeatId, BookingId, ShowId,
                   SeatId, Price, Status
            FROM BookingSeats
            WHERE BookingId = @BookingId
            ORDER BY BookingSeatId;
            """;

        List<BookingSeat> seats = new();

        await using SqlConnection connection =
            DatabaseHelper.CreateConnection();

        await connection.OpenAsync();

        await using SqlCommand command = new(sql, connection);

        command.Parameters.Add("@BookingId", SqlDbType.Int)
            .Value = bookingId;

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            seats.Add(new BookingSeat
            {
                BookingSeatId = reader.GetInt32(
                    reader.GetOrdinal("BookingSeatId")),

                BookingId = reader.GetInt32(
                    reader.GetOrdinal("BookingId")),

                ShowId = reader.GetInt32(
                    reader.GetOrdinal("ShowId")),

                SeatId = reader.GetInt32(
                    reader.GetOrdinal("SeatId")),

                Price = reader.GetDecimal(
                    reader.GetOrdinal("Price")),

                Status = reader.GetString(
                    reader.GetOrdinal("Status"))
            });
        }

        return seats;
    }

    // 6. Update a booking's status.
    public async Task<bool> UpdateStatusAsync(
        int bookingId,
        string status)
    {
        const string sql = """
            UPDATE Bookings
            SET Status = @Status
            WHERE BookingId = @BookingId;
            """;

        await using SqlConnection connection =
            DatabaseHelper.CreateConnection();

        await connection.OpenAsync();

        await using SqlCommand command = new(sql, connection);

        command.Parameters.Add("@BookingId", SqlDbType.Int)
            .Value = bookingId;

        command.Parameters.Add("@Status", SqlDbType.NVarChar, 20)
            .Value = status;

        return await command.ExecuteNonQueryAsync() > 0;
    }

    // 7. Book multiple seats in one transaction.
    public async Task<int> BookSeatsAsync(
        int customerId,
        int showId,
        IReadOnlyList<int> seatIds)
    {
        if (customerId <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(customerId), "Select a valid customer.");

        if (showId <= 0)
            throw new ArgumentOutOfRangeException(
                nameof(showId), "Select a valid show.");

        if (seatIds is null ||
            seatIds.Count is < 1 or > PricingHelper.MaxSeatsPerBooking)
        {
            throw new ArgumentException(
                $"Select 1 to {PricingHelper.MaxSeatsPerBooking} seats.",
                nameof(seatIds));
        }

        if (seatIds.Any(id => id <= 0))
            throw new ArgumentException(
                "Seat IDs must be positive.", nameof(seatIds));

        if (seatIds.Distinct().Count() != seatIds.Count)
            throw new ArgumentException(
                "The same seat cannot be selected twice.",
                nameof(seatIds));

        await using SqlConnection connection =
            DatabaseHelper.CreateConnection();

        await connection.OpenAsync();

        await using SqlTransaction transaction =
            (SqlTransaction)await connection.BeginTransactionAsync();

        try
        {
            // Create the booking header.
            const string bookingSql = """
                INSERT INTO Bookings
                    (CustomerId, ShowId, BookingDate, TotalAmount, Status)
                VALUES
                    (@CustomerId, @ShowId, SYSDATETIME(), 0, N'Confirmed');

                SELECT CAST(SCOPE_IDENTITY() AS INT);
                """;

            int bookingId;

            await using (SqlCommand command =
                new(bookingSql, connection, transaction))
            {
                command.Parameters.Add("@CustomerId", SqlDbType.Int)
                    .Value = customerId;

                command.Parameters.Add("@ShowId", SqlDbType.Int)
                    .Value = showId;

                bookingId = Convert.ToInt32(
                    await command.ExecuteScalarAsync());
            }

            // Insert each seat and calculate its price in SQL Server.
            const string seatSql = """
                INSERT INTO BookingSeats
                    (BookingId, ShowId, SeatId, Price)
                SELECT
                    @BookingId,
                    sh.ShowId,
                    s.SeatId,
                    CAST(
                        ROUND(sh.BasePrice * st.PriceMultiplier, 2)
                        AS DECIMAL(10,2)
                    )
                FROM Shows AS sh
                INNER JOIN Seats AS s
                    ON s.ScreenId = sh.ScreenId
                INNER JOIN SeatTypes AS st
                    ON st.SeatTypeId = s.SeatTypeId
                WHERE sh.ShowId = @ShowId
                  AND s.SeatId = @SeatId
                  AND s.IsActive = 1
                  AND sh.IsActive = 1
                  AND sh.ShowTime > SYSDATETIME();
                """;

            foreach (int seatId in seatIds)
            {
                await using SqlCommand command =
                    new(seatSql, connection, transaction);

                command.Parameters.Add("@BookingId", SqlDbType.Int)
                    .Value = bookingId;

                command.Parameters.Add("@ShowId", SqlDbType.Int)
                    .Value = showId;

                command.Parameters.Add("@SeatId", SqlDbType.Int)
                    .Value = seatId;

                int rows = await command.ExecuteNonQueryAsync();

                if (rows != 1)
                {
                    throw new InvalidOperationException(
                        $"Seat {seatId} is unavailable for this show.");
                }
            }

            // Calculate seat prices plus the convenience fee.
            const string totalSql = """
                UPDATE Bookings
                SET TotalAmount =
                    (
                        SELECT SUM(Price)
                        FROM BookingSeats
                        WHERE BookingId = @BookingId
                    )
                    + (@FeePerTicket * @SeatCount)
                WHERE BookingId = @BookingId;
                """;

            await using (SqlCommand command =
                new(totalSql, connection, transaction))
            {
                command.Parameters.Add("@BookingId", SqlDbType.Int)
                    .Value = bookingId;

                AddDecimalParameter(
                    command,
                    "@FeePerTicket",
                    PricingHelper.ConvenienceFeePerTicket);

                command.Parameters.Add("@SeatCount", SqlDbType.Int)
                    .Value = seatIds.Count;

                await command.ExecuteNonQueryAsync();
            }

            // Record the booking in the audit log.
            const string auditSql = """
                INSERT INTO AuditLogs
                    (EntityName, EntityId, Action, Details)
                VALUES
                    (@EntityName, @EntityId, @Action, @Details);
                """;

            await using (SqlCommand command =
                new(auditSql, connection, transaction))
            {
                command.Parameters.Add(
                    "@EntityName", SqlDbType.NVarChar, 50)
                    .Value = "Booking";

                command.Parameters.Add("@EntityId", SqlDbType.Int)
                    .Value = bookingId;

                command.Parameters.Add(
                    "@Action", SqlDbType.NVarChar, 20)
                    .Value = "INSERT";

                command.Parameters.Add(
                    "@Details", SqlDbType.NVarChar, 500)
                    .Value =
                    $"Customer {customerId} booked {seatIds.Count} seat(s) for show {showId}.";

                await command.ExecuteNonQueryAsync();
            }

            await transaction.CommitAsync();

            return bookingId;
        }
        catch (SqlException ex) when (ex.Number is 2601 or 2627)
        {
            await transaction.RollbackAsync();

            throw new InvalidOperationException(
                "A selected seat is already booked. Please choose different seats.",
                ex);
        }
        catch
        {
            await transaction.RollbackAsync();
            throw;
        }
    }

    // 8. Map a database row to a Booking object.
    private static Booking MapBooking(SqlDataReader reader)
    {
        return new Booking
        {
            BookingId = reader.GetInt32(
                reader.GetOrdinal("BookingId")),

            CustomerId = reader.GetInt32(
                reader.GetOrdinal("CustomerId")),

            ShowId = reader.GetInt32(
                reader.GetOrdinal("ShowId")),

            BookingDate = reader.GetDateTime(
                reader.GetOrdinal("BookingDate")),

            TotalAmount = reader.GetDecimal(
                reader.GetOrdinal("TotalAmount")),

            Status = reader.GetString(
                reader.GetOrdinal("Status"))
        };
    }

    // Shared helper for decimal SQL parameters.
    private static void AddDecimalParameter(
        SqlCommand command,
        string parameterName,
        decimal value)
    {
        SqlParameter parameter =
            command.Parameters.Add(parameterName, SqlDbType.Decimal);

        parameter.Precision = 10;
        parameter.Scale = 2;
        parameter.Value = value;
    }
}
