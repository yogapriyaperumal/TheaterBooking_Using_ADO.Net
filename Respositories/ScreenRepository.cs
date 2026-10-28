
using Microsoft.Data.SqlClient;
using System.Data;
using TheaterBooking.Helpers;
using TheaterBooking.Models;

namespace TheaterBooking.Repositories;

public class ScreenRepository
{
    // Add a new screen and return its generated ID.
    public async Task<int> AddAsync(Screen screen)
    {
        const string sql = """
            INSERT INTO Screens (ScreenName, TotalSeats)
            VALUES (@ScreenName, @TotalSeats);

            SELECT CAST(SCOPE_IDENTITY() AS INT);
            """;

        await using SqlConnection connection =
            DatabaseHelper.CreateConnection();

        await using SqlCommand command = new(sql, connection);

        command.Parameters.Add("@ScreenName", SqlDbType.NVarChar, 50)
            .Value = screen.ScreenName;

        command.Parameters.Add("@TotalSeats", SqlDbType.Int)
            .Value = screen.TotalSeats;

        await connection.OpenAsync();

        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    // Retrieve all screens, optionally including inactive screens.
    public async Task<List<Screen>> GetAllAsync(
        bool includeInactive = false)
    {
        const string sql = """
            SELECT ScreenId, ScreenName, TotalSeats, IsActive
            FROM Screens
            WHERE @IncludeInactive = 1 OR IsActive = 1
            ORDER BY ScreenId;
            """;

        List<Screen> screens = new();

        await using SqlConnection connection =
            DatabaseHelper.CreateConnection();

        await using SqlCommand command = new(sql, connection);

        command.Parameters.Add("@IncludeInactive", SqlDbType.Bit)
            .Value = includeInactive;

        await connection.OpenAsync();

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            screens.Add(MapScreen(reader));
        }

        return screens;
    }

    // Retrieve one screen using its ID.
    public async Task<Screen?> GetByIdAsync(int screenId)
    {
        const string sql = """
            SELECT ScreenId, ScreenName, TotalSeats, IsActive
            FROM Screens
            WHERE ScreenId = @ScreenId;
            """;

        await using SqlConnection connection =
            DatabaseHelper.CreateConnection();

        await using SqlCommand command = new(sql, connection);

        command.Parameters.Add("@ScreenId", SqlDbType.Int)
            .Value = screenId;

        await connection.OpenAsync();

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return MapScreen(reader);
        }

        return null;
    }

    // Update screen details.
    public async Task<bool> UpdateAsync(Screen screen)
    {
        const string sql = """
            UPDATE Screens
            SET ScreenName = @ScreenName,
                TotalSeats = @TotalSeats
            WHERE ScreenId = @ScreenId
              AND IsActive = 1;
            """;

        await using SqlConnection connection =
            DatabaseHelper.CreateConnection();

        await using SqlCommand command = new(sql, connection);

        command.Parameters.Add("@ScreenId", SqlDbType.Int)
            .Value = screen.ScreenId;

        command.Parameters.Add("@ScreenName", SqlDbType.NVarChar, 50)
            .Value = screen.ScreenName;

        command.Parameters.Add("@TotalSeats", SqlDbType.Int)
            .Value = screen.TotalSeats;

        await connection.OpenAsync();

        return await command.ExecuteNonQueryAsync() > 0;
    }

    // Deactivate a screen without deleting its records.
    public async Task<bool> DeactivateAsync(int screenId)
    {
        const string sql = """
            UPDATE Screens
            SET IsActive = 0
            WHERE ScreenId = @ScreenId
              AND IsActive = 1;
            """;

        await using SqlConnection connection =
            DatabaseHelper.CreateConnection();

        await using SqlCommand command = new(sql, connection);

        command.Parameters.Add("@ScreenId", SqlDbType.Int)
            .Value = screenId;

        await connection.OpenAsync();

        return await command.ExecuteNonQueryAsync() > 0;
    }

    // Map a database row to a Screen object.
    private static Screen MapScreen(SqlDataReader reader)
    {
        return new Screen
        {
            ScreenId = reader.GetInt32(0),
            ScreenName = reader.GetString(1),
            TotalSeats = reader.GetInt32(2),
            IsActive = reader.GetBoolean(3)
        };
    }
}
