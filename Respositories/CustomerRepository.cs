
using Microsoft.Data.SqlClient;
using TheaterBooking.Helpers;
using TheaterBooking.Models;

namespace TheaterBooking.Repositories;

public class CustomerRepository
{
    public async Task<int> AddAsync(Customer customer)
    {
        const string sql = """
            INSERT INTO Customers
                (FullName, PhoneNumber, Email, CreatedAt)
            OUTPUT INSERTED.CustomerId
            VALUES
                (@FullName, @PhoneNumber, @Email, @CreatedAt);
            """;

        await using SqlConnection connection =
            DatabaseHelper.CreateConnection();

        await connection.OpenAsync();

        await using SqlCommand command = new(sql, connection);

        command.Parameters.AddWithValue("@FullName", customer.FullName);
        command.Parameters.AddWithValue(
            "@PhoneNumber", customer.PhoneNumber);
        command.Parameters.AddWithValue(
            "@Email", (object?)customer.Email ?? DBNull.Value);
        command.Parameters.AddWithValue("@CreatedAt", customer.CreatedAt);

        return Convert.ToInt32(await command.ExecuteScalarAsync());
    }

    public async Task<List<Customer>> GetAllAsync()
    {
        const string sql = """
            SELECT CustomerId, FullName, PhoneNumber, Email, CreatedAt
            FROM Customers
            ORDER BY FullName;
            """;

        var customers = new List<Customer>();

        await using SqlConnection connection =
            DatabaseHelper.CreateConnection();

        await connection.OpenAsync();

        await using SqlCommand command = new(sql, connection);

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();

        while (await reader.ReadAsync())
        {
            customers.Add(MapCustomer(reader));
        }

        return customers;
    }

    public async Task<Customer?> GetByIdAsync(int customerId)
    {
        const string sql = """
            SELECT CustomerId, FullName, PhoneNumber, Email, CreatedAt
            FROM Customers
            WHERE CustomerId = @CustomerId;
            """;

        await using SqlConnection connection =
            DatabaseHelper.CreateConnection();

        await connection.OpenAsync();

        await using SqlCommand command = new(sql, connection);

        command.Parameters.AddWithValue("@CustomerId", customerId);

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return MapCustomer(reader);
        }

        return null;
    }

    public async Task<Customer?> GetByPhoneNumberAsync(
        string phoneNumber)
    {
        const string sql = """
            SELECT CustomerId, FullName, PhoneNumber, Email, CreatedAt
            FROM Customers
            WHERE PhoneNumber = @PhoneNumber;
            """;

        await using SqlConnection connection =
            DatabaseHelper.CreateConnection();

        await connection.OpenAsync();

        await using SqlCommand command = new(sql, connection);

        command.Parameters.AddWithValue("@PhoneNumber", phoneNumber);

        await using SqlDataReader reader =
            await command.ExecuteReaderAsync();

        if (await reader.ReadAsync())
        {
            return MapCustomer(reader);
        }

        return null;
    }

    public async Task<bool> UpdateAsync(Customer customer)
    {
        const string sql = """
            UPDATE Customers
            SET FullName = @FullName,
                PhoneNumber = @PhoneNumber,
                Email = @Email
            WHERE CustomerId = @CustomerId;
            """;

        await using SqlConnection connection =
            DatabaseHelper.CreateConnection();

        await connection.OpenAsync();

        await using SqlCommand command = new(sql, connection);

        command.Parameters.AddWithValue(
            "@CustomerId", customer.CustomerId);
        command.Parameters.AddWithValue("@FullName", customer.FullName);
        command.Parameters.AddWithValue(
            "@PhoneNumber", customer.PhoneNumber);
        command.Parameters.AddWithValue(
            "@Email", (object?)customer.Email ?? DBNull.Value);

        return await command.ExecuteNonQueryAsync() > 0;
    }

    private static Customer MapCustomer(SqlDataReader reader)
    {
        int emailOrdinal = reader.GetOrdinal("Email");

        return new Customer
        {
            CustomerId = reader.GetInt32(
                reader.GetOrdinal("CustomerId")),
            FullName = reader.GetString(
                reader.GetOrdinal("FullName")),
            PhoneNumber = reader.GetString(
                reader.GetOrdinal("PhoneNumber")),
            Email = reader.IsDBNull(emailOrdinal)
                ? null
                : reader.GetString(emailOrdinal),
            CreatedAt = reader.GetDateTime(
                reader.GetOrdinal("CreatedAt"))
        };
    }
}
