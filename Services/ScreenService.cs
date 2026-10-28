
using TheaterBooking.Models;
using TheaterBooking.Repositories;

namespace TheaterBooking.Services;

public class ScreenService
{
    private readonly ScreenRepository _repository;

    public ScreenService(ScreenRepository repository)
    {
        _repository = repository;
    }

    // Add a new screen after validation.
    public async Task<int> AddAsync(Screen screen)
    {
        ValidateScreen(screen);

        List<Screen> screens = await _repository.GetAllAsync();

        bool duplicate = screens.Any(s =>
            string.Equals(
                s.ScreenName,
                screen.ScreenName,
                StringComparison.OrdinalIgnoreCase));

        if (duplicate)
        {
            throw new InvalidOperationException(
                "A screen with this name already exists.");
        }

        return await _repository.AddAsync(screen);
    }

    // Get all screens.
    public Task<List<Screen>> GetAllAsync(
        bool includeInactive = false)
    {
        return _repository.GetAllAsync(includeInactive);
    }

    // Get a screen by ID.
    public async Task<Screen?> GetByIdAsync(int screenId)
    {
        if (screenId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(screenId),
                "Screen ID must be positive.");
        }

        return await _repository.GetByIdAsync(screenId);
    }

    // Update an existing active screen.
    public async Task<bool> UpdateAsync(Screen screen)
    {
        if (screen.ScreenId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(screen.ScreenId),
                "Invalid screen ID.");
        }

        ValidateScreen(screen);

        Screen? existing =
            await _repository.GetByIdAsync(screen.ScreenId);

        if (existing is null || !existing.IsActive)
        {
            throw new InvalidOperationException(
                "Screen not found or inactive.");
        }

        List<Screen> screens = await _repository.GetAllAsync();

        bool duplicate = screens.Any(s =>
            s.ScreenId != screen.ScreenId &&
            string.Equals(
                s.ScreenName,
                screen.ScreenName,
                StringComparison.OrdinalIgnoreCase));

        if (duplicate)
        {
            throw new InvalidOperationException(
                "Another screen already uses this name.");
        }

        return await _repository.UpdateAsync(screen);
    }

    // Deactivate an existing screen.
    public async Task<bool> DeactivateAsync(int screenId)
    {
        if (screenId <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(screenId),
                "Screen ID must be positive.");
        }

        Screen? screen =
            await _repository.GetByIdAsync(screenId);

        if (screen is null || !screen.IsActive)
        {
            throw new InvalidOperationException(
                "Screen not found or already inactive.");
        }

        return await _repository.DeactivateAsync(screenId);
    }

    // Validate screen details.
    private static void ValidateScreen(Screen screen)
    {
        ArgumentNullException.ThrowIfNull(screen);

        if (string.IsNullOrWhiteSpace(screen.ScreenName) ||
            screen.ScreenName.Trim().Length > 50)
        {
            throw new ArgumentException(
                "Screen name is required and must not exceed 50 characters.");
        }

        if (screen.TotalSeats <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(screen.TotalSeats),
                "Total seats must be greater than zero.");
        }

        screen.ScreenName = screen.ScreenName.Trim();
    }
}
