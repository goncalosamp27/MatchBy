using System.ComponentModel;
using MatchBy.DTOs.Match;
using MatchBy.Enums;
using MatchBy.Models;
using MatchBy.Services.Matches;
using Microsoft.AspNetCore.Identity;
using Microsoft.JSInterop;

namespace MatchBy.Components.Pages.Matches;

public sealed class SearchMatchesViewModel(
    IMatchesService matchesService,
    UserManager<ApplicationUser> userManager,
    IJSRuntime jsRuntime)
    : INotifyPropertyChanged
{
    private const int PageSize = 6;

    public bool DistanceFilterUsed { get; set; }

    public event PropertyChangedEventHandler? PropertyChanged;
    public PaginationResponse<List<MatchDto>>? SearchedMatchesPagination
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(SearchedMatchesPagination));
        }
    }
    public List<MatchDto>? SearchedMatches
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(SearchedMatches));
        }
    }
    public HashSet<Sports> SelectedSports
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(SelectedSports));
        }
    } = [];
    public int SelectedStatus
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(SelectedStatus));
        }
    } = (int)MatchStatus.Pendent;

    public int SelectedRating
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(SelectedRating));
        }
    } = (int)MinimumPlayersAverage.All;
    public int StartHour
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(StartHour));
        }
    }
    public int EndHour
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(EndHour));
        }
    } = 24;
    public string? SelectedCountry
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(SelectedCountry));
        }
    }
    public string? SelectedCity
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(SelectedCity));
        }
    }
    public List<string> Countries
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(Countries));
        }
    } = [];
    public List<string> Cities
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(Cities));
        }
    } = [];
    public int MaxDistanceKm
    {
        get;
        set
        {
            field = value;
            if(value < 55)
            {
                DistanceFilterUsed = true;
            }
            OnPropertyChanged(nameof(MaxDistanceKm));
        }
    } = 55;
    public bool IsGeolocationEnabled
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(IsGeolocationEnabled));
        }
    }
    public double? UserLatitude
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(UserLatitude));
        }
    }
    public double? UserLongitude
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(UserLongitude));
        }
    }
    public int CurrentPage
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(CurrentPage));
        }
    } = 1;
    public int SelectedSorting
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(SelectedSorting));
        }
    } = (int)SortBy.MatchDateTime;

    public int SelectedSortingOrder
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(SelectedSortingOrder));
        }
    } = (int)OrderBy.Ascending;
    public IReadOnlyList<DateTime?> SelectedDates
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(SelectedDates));
        }
    } = [];
    
    public bool FiltersOpen
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(FiltersOpen));
        }
    }

    public ApplicationUser? User
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(User));
        }
    }

    /// <summary>
    /// Initializes the view model by loading matches and setting up user preferences.
    /// </summary>
    public async Task InitializeAsync(System.Security.Claims.ClaimsPrincipal? principal)
    {
        await TryGetGeolocationAsync();

        User = null;

        if (principal?.Identity?.IsAuthenticated == true)
        {
            string? userId = userManager.GetUserId(principal);

            if (!string.IsNullOrEmpty(userId))
            {
                User = await userManager.FindByIdAsync(userId);

                if (User is not null)
                {
                    SelectedSports = User.PreferredSports.ToHashSet();
                }
            }
        }

        await ChangePage(1);
    }

    public async Task ToggleSport(Sports sport)
    {
        if (!SelectedSports.Remove(sport))
        {
            SelectedSports.Add(sport);
        }

        await ChangePage(1);
    }

    public void ToggleFilters()
    {
        FiltersOpen = !FiltersOpen;
    }

    public async Task OnCountryChanged(string? value)
    {
        SelectedCountry = value;

        if (string.IsNullOrEmpty(SelectedCountry))
        {
            SelectedCity = null;
            Cities = [];
            return;
        }
        Result<List<string>> resultCities = await matchesService.GetAllCitiesByCountry(SelectedCountry!, CancellationToken.None);
        if (resultCities.Success)
        {
            Cities = resultCities.Data!;
        }
    }

    public async Task ChangePage(int page)
    {
        DateOnly? startDate = null;
        DateOnly? endDate = null;
        
        switch (SelectedDates.Count)
        {
            case 2:
                startDate = SelectedDates[0] != null ? DateOnly.FromDateTime(SelectedDates[0]!.Value) : null;
                endDate = SelectedDates[1] != null ? DateOnly.FromDateTime(SelectedDates[1]!.Value) : null;
                break;
            case 1:
                startDate = SelectedDates[0] != null ? DateOnly.FromDateTime(SelectedDates[0]!.Value) : null;
                endDate = SelectedDates[0] != null ? DateOnly.FromDateTime(SelectedDates[0]!.Value) : null;
                break;
        }
        
        MatchQueryParametersDto matchQueryParametersDto = new()
        {
            Country = SelectedCountry,
            City = SelectedCity,
            MaxDistanceInKm = DistanceFilterUsed ? MaxDistanceKm : null,
            UserLatitude = UserLatitude,
            UserLongitude = UserLongitude,
            MinimumPlayersAverage = (MinimumPlayersAverage)SelectedRating,
            FromDateUtc = startDate,
            ToDateUtc = endDate,
            FromTimeUtc = StartHour,
            ToTimeUtc = EndHour,
            SortBy = (SortBy)SelectedSorting,
            OrderBy = (OrderBy)SelectedSortingOrder,
            MatchStatus = (Status)SelectedStatus,
            SportsList = SelectedSports.ToList(),
            Q = null,
            UserId = User?.Id ?? null,
            Page = page,
            PageSize = PageSize
        };
        
        Result<PaginationResponse<List<MatchDto>>> result = await matchesService.GetMatches(
            matchQueryParametersDto,
            CancellationToken.None
        );

        if (result.Success)
        {
            SearchedMatchesPagination = result.Data!;
            SearchedMatches = result.Data!.Data;

            Result<List<string>> countriesResult = await matchesService.GetAllMatchCountries(CancellationToken.None);
            if (countriesResult.Success)
            {
                Countries = countriesResult.Data!;
            }
        }
        CurrentPage = page;
    }

    private async Task TryGetGeolocationAsync()
    {
        try
        {
            PositionResult pos = await jsRuntime.InvokeAsync<PositionResult>("getCurrentPosition");

            UserLatitude = pos.Latitude;
            UserLongitude = pos.Longitude;

            IsGeolocationEnabled = true;
        }
        catch
        {
            IsGeolocationEnabled = false;
        }
    }

    public class PositionResult
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

