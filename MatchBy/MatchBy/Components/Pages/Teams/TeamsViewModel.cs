using System;
using System.ComponentModel;
using MatchBy.DTOs.Team;
using MatchBy.Models;
using MatchBy.Services.Teams;

namespace MatchBy.Components.Pages.Teams;

public sealed class TeamsViewModel(ITeamService teamService) : INotifyPropertyChanged
{
    private const int PageSize = 6;

    private bool _isAuthenticated;
    private string? _userId;
    private string _activeTab = "my";
    private List<TeamDto>? _teamsYouOwn;
    private List<TeamDto>? _teamsYouParticipate;
    private List<TeamDto>? _searchedTeams;
    private PaginationResponse<List<TeamDto>>? _teamsYouOwnPagination;
    private PaginationResponse<List<TeamDto>>? _teamsYouParticipatePagination;
    private PaginationResponse<List<TeamDto>>? _searchedTeamsPagination;
    private int _currentPageOwnTeams = 1;
    private int _currentPageParticipatingTeams = 1;
    private int _currentPageSearchedTeams = 1;

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

    public string? UserId
    {
        get => _userId;
        private set
        {
            _userId = value;
            OnPropertyChanged(nameof(UserId));
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

    public List<TeamDto>? TeamsYouOwn
    {
        get => _teamsYouOwn;
        private set
        {
            _teamsYouOwn = value;
            OnPropertyChanged(nameof(TeamsYouOwn));
        }
    }

    public List<TeamDto>? TeamsYouParticipate
    {
        get => _teamsYouParticipate;
        private set
        {
            _teamsYouParticipate = value;
            OnPropertyChanged(nameof(TeamsYouParticipate));
        }
    }

    public List<TeamDto>? SearchedTeams
    {
        get => _searchedTeams;
        private set
        {
            _searchedTeams = value;
            OnPropertyChanged(nameof(SearchedTeams));
        }
    }

    public PaginationResponse<List<TeamDto>>? TeamsYouOwnPagination
    {
        get => _teamsYouOwnPagination;
        private set
        {
            _teamsYouOwnPagination = value;
            OnPropertyChanged(nameof(TeamsYouOwnPagination));
        }
    }

    public PaginationResponse<List<TeamDto>>? TeamsYouParticipatePagination
    {
        get => _teamsYouParticipatePagination;
        private set
        {
            _teamsYouParticipatePagination = value;
            OnPropertyChanged(nameof(TeamsYouParticipatePagination));
        }
    }

    public PaginationResponse<List<TeamDto>>? SearchedTeamsPagination
    {
        get => _searchedTeamsPagination;
        private set
        {
            _searchedTeamsPagination = value;
            OnPropertyChanged(nameof(SearchedTeamsPagination));
        }
    }

    public int CurrentPageOwnTeams
    {
        get => _currentPageOwnTeams;
        private set
        {
            _currentPageOwnTeams = value;
            OnPropertyChanged(nameof(CurrentPageOwnTeams));
        }
    }

    public int CurrentPageParticipatingTeams
    {
        get => _currentPageParticipatingTeams;
        private set
        {
            _currentPageParticipatingTeams = value;
            OnPropertyChanged(nameof(CurrentPageParticipatingTeams));
        }
    }

    public int CurrentPageSearchedTeams
    {
        get => _currentPageSearchedTeams;
        private set
        {
            _currentPageSearchedTeams = value;
            OnPropertyChanged(nameof(CurrentPageSearchedTeams));
        }
    }

    private TeamQueryParametersDto _teamQueryParameters = new()
    {
        UserId = "",
        Page = 0,
        PageSize = 0,
        Query = "",
        SortBy = SortBy.Name,
        OrderBy = OrderBy.Ascending,
        Privacy = Privacy.All
    };

    public TeamQueryParametersDto TeamQueryParameters
    {
        get => _teamQueryParameters;
        private set
        {
            _teamQueryParameters = value;
            OnPropertyChanged(nameof(TeamQueryParameters));
        }
    }

    /// <summary>
    /// Initializes the view model with user authentication information and loads initial team data.
    /// </summary>
    /// <param name="userId">The unique identifier of the current user, or null if not authenticated.</param>
    /// <param name="isAuthenticated">Indicates whether the user is authenticated.</param>
    public async Task InitializeAsync(string? userId, bool isAuthenticated)
    {
        UserId = userId;
        IsAuthenticated = isAuthenticated;
        await LoadTeamsAsync();
    }

    /// <summary>
    /// Loads all team data for the current user, including owned teams, participating teams, and searched teams.
    /// </summary>
    /// <remarks>
    /// This method loads teams based on authentication status. If the user is not authenticated,
    /// it initializes empty lists. Otherwise, it loads owned and participating teams if they haven't been loaded yet.
    /// </remarks>
    public async Task LoadTeamsAsync()
    {
        TeamQueryParameters = TeamQueryParameters with
        {
            Page = _currentPageSearchedTeams,
            PageSize = PageSize,
            UserId = _isAuthenticated ? _userId ?? string.Empty : string.Empty
        };

        if (SearchedTeams is null)
        {
            await SearchTeamsAsync();
        }

        if (!_isAuthenticated || string.IsNullOrEmpty(_userId))
        {
            TeamsYouOwn ??= [];
            TeamsYouParticipate ??= [];
            return;
        }

        if (TeamsYouOwn is null)
        {
            await LoadOwnTeamsAsync();
        }

        if (TeamsYouParticipate is null)
        {
            await LoadParticipatingTeamsAsync();
        }
    }

    /// <summary>
    /// Updates the team query parameters using a functional update pattern.
    /// </summary>
    /// <param name="updater">A function that takes the current query parameters and returns updated parameters.</param>
    public void UpdateTeamQueryParameters(Func<TeamQueryParametersDto, TeamQueryParametersDto> updater)
    {
        TeamQueryParameters = updater(TeamQueryParameters);
    }

    /// <summary>
    /// Searches for available teams based on the current query parameters and updates the searched teams list.
    /// </summary>
    /// <remarks>
    /// This method resets the searched teams list and performs a new search using the current page
    /// and query parameters. Errors are logged to the console and an empty list is set on failure.
    /// </remarks>
    public async Task SearchTeamsAsync()
    {
        SearchedTeams = null;

        TeamQueryParameters = TeamQueryParameters with
        {
            Page = _currentPageSearchedTeams,
            PageSize = PageSize,
            UserId = _isAuthenticated ? _userId ?? "" : ""
        };

        Result<PaginationResponse<List<TeamDto>>> teamsYouSearchedResult = await teamService.GetAvailableTeamsAsync(TeamQueryParameters, CancellationToken.None);
        if (!teamsYouSearchedResult.Success)
        {
            Console.WriteLine("Error fetching searched teams: " + string.Join(", ", teamsYouSearchedResult.ErrorMessages));
            SearchedTeams = [];
            return;
        }

        SearchedTeams = teamsYouSearchedResult.Data!.Data;
        SearchedTeamsPagination = teamsYouSearchedResult.Data;
    }

    /// <summary>
    /// Loads teams that the current user owns, using pagination.
    /// </summary>
    /// <remarks>
    /// If the user ID is null, an empty list is set. Errors are logged to the console
    /// and an empty list is set on failure.
    /// </remarks>
    public async Task LoadOwnTeamsAsync()
    {
        if (_userId == null)
        {
            TeamsYouOwn = [];
            return;
        }

        TeamsYouOwn = null;
        Result<PaginationResponse<List<TeamDto>>> own = await teamService.GetTeamsUserOwnAsync(_userId, _currentPageOwnTeams, PageSize, string.Empty, CancellationToken.None);
        if (!own.Success)
        {
            Console.WriteLine("Error fetching user own teams: " + string.Join(", ", own.ErrorMessages));
            TeamsYouOwn = [];
            return;
        }

        TeamsYouOwn = own.Data!.Data;
        TeamsYouOwnPagination = own.Data;
    }

    /// <summary>
    /// Loads teams that the current user participates in (as a member but not owner), using pagination.
    /// </summary>
    /// <remarks>
    /// If the user ID is null, an empty list is set. Errors are logged to the console
    /// and an empty list is set on failure.
    /// </remarks>
    public async Task LoadParticipatingTeamsAsync()
    {
        if (_userId == null)
        {
            TeamsYouParticipate = [];
            return;
        }

        TeamsYouParticipate = null;
        Result<PaginationResponse<List<TeamDto>>> part = await teamService.GetTeamsUserParticipateAsync(_userId, _currentPageParticipatingTeams, PageSize, string.Empty, CancellationToken.None);
        if (!part.Success)
        {
            Console.WriteLine("Error fetching user participating teams: " + string.Join(", ", part.ErrorMessages));
            TeamsYouParticipate = [];
            return;
        }

        TeamsYouParticipate = part.Data!.Data;
        TeamsYouParticipatePagination = part.Data;
    }

    /// <summary>
    /// Handles page changes for different team list types and reloads the appropriate data.
    /// </summary>
    /// <param name="page">The new page number to load.</param>
    /// <param name="pageType">The type of team list to paginate (OwnTeams, ParticipatingTeams, or SearchedTeams).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when an invalid pageType is provided.</exception>
    public async Task OnPageChangedAsync(int page, TeamsPageType pageType)
    {
        switch (pageType)
        {
            case TeamsPageType.OwnTeams:
                CurrentPageOwnTeams = page;
                await LoadOwnTeamsAsync();
                break;
            case TeamsPageType.ParticipatingTeams:
                CurrentPageParticipatingTeams = page;
                await LoadParticipatingTeamsAsync();
                break;
            case TeamsPageType.SearchedTeams:
                CurrentPageSearchedTeams = page;
                await SearchTeamsAsync();
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

public enum TeamsPageType
{
    OwnTeams,
    ParticipatingTeams,
    SearchedTeams
}

