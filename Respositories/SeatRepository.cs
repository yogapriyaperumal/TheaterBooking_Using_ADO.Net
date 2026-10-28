
using Microsoft.Data.SqlClient;
using TheaterBooking.Helpers;
using TheaterBooking.Models;

namespace TheaterBooking.Repositories;

public class SeatRepository
{
    public async Task<int> AddAsync(Seat seat)
    {
        const string sql = """
            INSERT INTO Seats
                (ScreenId, RowLabel, SeatNumber, SeatTypeId, IsActive)
            OUTPUT INSERTED.SeatId
            VALUES
                (@ScreenId, @RowLabel, @SeatNumber, @SeatTypeId, @IsActive);
            """;

        await using SqlConnection connection =
            DatabaseHelper.CreateConnection();

        await connection.OpenAsync();

        await using SqlCommand command = new(sql, connection);

        command.Parameters.AddWithValue("@ScreenId", seat.ScreenId);
        command.Parameters.AddWithValue("@RowLabel", seat.RowLabel);
        command.Parameters.AddWithValue("@SeatNumber", seat.SeatNumber);
        command.Parameters.AddWithValue("@SeatTypeId", seat.SeatTypeId);
        command.Parameters.AddWithValue("@IsActive", seat.IsActive);

        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public async Task<List<Seat>> GetByScreenIdAsync(
        int screenId,
        bool includeInactive = false)
    {
        string sql = """
            SELECT SeatId, ScreenId, RowLabel, SeatNumber,
                   SeatTypeId, IsActive
            FROM Seats
            WHERE ScreenId = @ScreenId
            """;

        if (!includeInactive)
        {
            sql += " AND IsActive = 1";
        }

        sql += " ORDER BY RowLabel, SeatNumber;";

        var seats = new List<Seat>();

        await using SqlConnection connection =
            DatabaseHelper.CreateConnection();

        await connection.OpenAsync();

        await using SqlCommand command = new(sql, connection);

        command.Parameters.AddWithValue("@ScreenId", screenId);

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            seats.Add(MapSeat(reader));
        }

        return seats;
    }

    public async Task<Seat?> GetByIdAsync(int seatId)
    {
        const string sql = """
            SELECT SeatId, ScreenId, RowLabel, SeatNumber,
                   SeatTypeId, IsActive
            FROM Seats
            WHERE SeatId = @SeatId;
            """;

        await using SqlConnection connection =
            DatabaseHelper.CreateConnection();

        await connection.OpenAsync();

        await using SqlCommand command = new(sql, connection);

        command.Parameters.AddWithValue("@SeatId", seatId);

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return MapSeat(reader);
        }

        return null;
    }

    public async Task<bool> UpdateAsync(Seat seat)
    {
        const string sql = """
            UPDATE Seats
            SET ScreenId = @ScreenId,
                RowLabel = @RowLabel,
                SeatNumber = @SeatNumber,
                SeatTypeId = @SeatTypeId,
                IsActive = @IsActive
            WHERE SeatId = @SeatId;
            """;

        await using SqlConnection connection =
            DatabaseHelper.CreateConnection();

        await connection.OpenAsync();

        await using SqlCommand command = new(sql, connection);

        command.Parameters.AddWithValue("@SeatId", seat.SeatId);
        command.Parameters.AddWithValue("@ScreenId", seat.ScreenId);
        command.Parameters.AddWithValue("@RowLabel", seat.RowLabel);
        command.Parameters.AddWithValue("@SeatNumber", seat.SeatNumber);
        command.Parameters.AddWithValue("@SeatTypeId", seat.SeatTypeId);
        command.Parameters.AddWithValue("@IsActive", seat.IsActive);

        return await command.ExecuteNonQueryAsync() > 0;
    }

    public async Task<bool> DeactivateAsync(int seatId)
    {
        const string sql = """
            UPDATE Seats
            SET IsActive = 0
            WHERE SeatId = @SeatId;
            """;

        await using SqlConnection connection =
            DatabaseHelper.CreateConnection();

        await connection.OpenAsync();

        await using SqlCommand command = new(sql, connection);

        command.Parameters.AddWithValue("@SeatId", seatId);

        return await command.ExecuteNonQueryAsync() > 0;
    }

    private static Seat MapSeat(SqlDataReader reader)
    {
        return new Seat
        {
            SeatId = reader.GetInt32(reader.GetOrdinal("SeatId")),
            ScreenId = reader.GetInt32(reader.GetOrdinal("ScreenId")),
            RowLabel = reader.GetString(reader.GetOrdinal("RowLabel")),
            SeatNumber = reader.GetInt32(reader.GetOrdinal("SeatNumber")),
            SeatTypeId = reader.GetInt32(reader.GetOrdinal("SeatTypeId")),
            IsActive = reader.GetBoolean(reader.GetOrdinal("IsActive"))
        };
    }
}
