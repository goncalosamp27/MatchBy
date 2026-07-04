using System.ComponentModel;
using MatchBy.DTOs.Match;
using MatchBy.Models;
using MatchBy.Services.Matches;

namespace MatchBy.Components.Pages.Matches;

public sealed class MatchesViewModel(IMatchesService matchesService) : INotifyPropertyChanged
{
    private const int PageSize = 6;

    private bool _isAuthenticated;
    private string? _userId;
    private List<MatchDto>? _yourCreatedMatches;
    private List<MatchDto>? _attendingMatches;
    private PaginationResponse<List<MatchDto>>? _yourCreatedMatchesPagination;
    private PaginationResponse<List<MatchDto>>? _attendingMatchesPagination;
    private int _currentPageCreatedMatches = 1;
    private int _currentPageAttendingMatches = 1;
    private string _activeTab = "my";

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsAuthenticated
    {
        get => _isAuthenticated;
        private set
        {
            _isAuthenticated = value;
            OnPropertyChanged(nameof(IsAuthenticated));
        }
    }
    
    public string ActiveTab
    {
        get => _activeTab;
        set
        {
            _activeTab = value;
            OnPropertyChanged(nameof(ActiveTab));
        }
    }

    public string? UserId
    {
        get => _userId;
        private set
        {
            _userId = value;
            OnPropertyChanged(nameof(UserId));
        }
    }

    public List<MatchDto>? YourCreatedMatches
    {
        get => _yourCreatedMatches;
        private set
        {
            _yourCreatedMatches = value;
            OnPropertyChanged(nameof(YourCreatedMatches));
        }
    }

    public List<MatchDto>? AttendingMatches
    {
        get => _attendingMatches;
        private set
        {
            _attendingMatches = value;
            OnPropertyChanged(nameof(AttendingMatches));
        }
    }

    public PaginationResponse<List<MatchDto>>? YourCreatedMatchesPagination
    {
        get => _yourCreatedMatchesPagination;
        private set
        {
            _yourCreatedMatchesPagination = value;
            OnPropertyChanged(nameof(YourCreatedMatchesPagination));
        }
    }

    public PaginationResponse<List<MatchDto>>? AttendingMatchesPagination
    {
        get => _attendingMatchesPagination;
        private set
        {
            _attendingMatchesPagination = value;
            OnPropertyChanged(nameof(AttendingMatchesPagination));
        }
    }

    public int CurrentPageCreatedMatches
    {
        get => _currentPageCreatedMatches;
        private set
        {
            _currentPageCreatedMatches = value;
            OnPropertyChanged(nameof(CurrentPageCreatedMatches));
        }
    }

    public int CurrentPageAttendingMatches
    {
        get => _currentPageAttendingMatches;
        private set
        {
            _currentPageAttendingMatches = value;
            OnPropertyChanged(nameof(CurrentPageAttendingMatches));
        }
    }

    /// <summary>
    /// Initializes the view model with user authentication information and loads initial match data.
    /// </summary>
    /// <param name="userId">The unique identifier of the current user, or null if not authenticated.</param>
    /// <param name="isAuthenticated">Indicates whether the user is authenticated.</param>
    public async Task InitializeAsync(string? userId, bool isAuthenticated)
    {
        UserId = userId;
        IsAuthenticated = isAuthenticated;
        await LoadMatchesAsync();
    }

    /// <summary>
    /// Loads all match data for the current user, including created matches and attending matches.
    /// </summary>
    /// <remarks>
    /// This method loads matches based on authentication status. If the user is not authenticated,
    /// it initializes empty lists. Otherwise, it loads created and attending matches.
    /// </remarks>
    public async Task LoadMatchesAsync()
    {
        if (!_isAuthenticated || string.IsNullOrEmpty(_userId))
        {
            YourCreatedMatches ??= [];
            AttendingMatches ??= [];
            return;
        }

        if (YourCreatedMatches is null)
        {
            await LoadCreatedMatchesAsync();
        }

        if (AttendingMatches is null)
        {
            await LoadAttendingMatchesAsync();
        }
    }

    /// <summary>
    /// Loads matches that the current user created, using pagination.
    /// </summary>
    /// <remarks>
    /// If the user ID is null, an empty list is set. Errors are logged to the console
    /// and an empty list is set on failure.
    /// </remarks>
    public async Task LoadCreatedMatchesAsync()
    {
        if (_userId == null)
        {
            YourCreatedMatches = [];
            return;
        }

        YourCreatedMatches = null;
        Result<PaginationResponse<List<MatchDto>>> result = await matchesService.GetMatchesForUser(
            _userId, 
            null, 
            _currentPageCreatedMatches, 
            PageSize, 
            CancellationToken.None);
        
        if (!result.Success)
        {
            Console.WriteLine("Error fetching user created matches: " + string.Join(", ", result.ErrorMessages));
            YourCreatedMatches = [];
            return;
        }

        YourCreatedMatches = result.Data!.Data;
        YourCreatedMatchesPagination = result.Data;
    }

    /// <summary>
    /// Loads matches that the current user is attending (as participant, not creator), using pagination.
    /// </summary>
    /// <remarks>
    /// If the user ID is null, an empty list is set. Errors are logged to the console
    /// and an empty list is set on failure.
    /// </remarks>
    public async Task LoadAttendingMatchesAsync()
    {
        if (_userId == null)
        {
            AttendingMatches = [];
            return;
        }

        AttendingMatches = null;
        Result<PaginationResponse<List<MatchDto>>> result = await matchesService.GetMatchesUserAttending(
            _userId, 
            null, 
            _currentPageAttendingMatches, 
            PageSize, 
            CancellationToken.None);
        
        if (!result.Success)
        {
            Console.WriteLine("Error fetching user attending matches: " + string.Join(", ", result.ErrorMessages));
            AttendingMatches = [];
            return;
        }

        AttendingMatches = result.Data!.Data;
        AttendingMatchesPagination = result.Data;
    }

    /// <summary>
    /// Handles page changes for different match list types and reloads the appropriate data.
    /// </summary>
    /// <param name="page">The new page number to load.</param>
    /// <param name="pageType">The type of match list to paginate (CreatedMatches or AttendingMatches).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an invalid pageType is provided.</exception>
    public async Task OnPageChangedAsync(int page, MatchesPageType pageType)
    {
        switch (pageType)
        {
            case MatchesPageType.CreatedMatches:
                CurrentPageCreatedMatches = page;
                await LoadCreatedMatchesAsync();
                break;
            case MatchesPageType.AttendingMatches:
                CurrentPageAttendingMatches = page;
                await LoadAttendingMatchesAsync();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(pageType), pageType, null);
        }
    }

    /// <summary>
    /// Raises the PropertyChanged event to notify subscribers that a property value has changed.
    /// </summary>
    /// <param name="propertyName">The name of the property that changed.</param>
    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public enum MatchesPageType
{
    CreatedMatches,
    AttendingMatches
}


