using System.ComponentModel;
using Blazorise;
using FluentValidation;
using FluentValidation.Results;
using MatchBy.DTOs.Match;
using MatchBy.Models;
using MatchBy.Services.Matches;
using MatchBy.Services.Users;
using MatchBy.Enums;
using Microsoft.AspNetCore.Components.Web;

namespace MatchBy.Components.Pages.Matches;

public sealed class CreateMatchViewModel(
    IMatchesService matchesService,
    IUsersService usersService,
    IValidator<CreateMatchDto> validator,
    IToastService toastService)
    : INotifyPropertyChanged
{
    private CreateMatchDto _model = new()
    {
        Location = new Location(0, 0, string.Empty, string.Empty),
        MatchDateTimeUtc = DateTime.UtcNow.AddDays(8),
        Address = string.Empty,
        Description = string.Empty,
        Privacy = MatchPrivacy.Public,
        Sport = Sports.Football,
        MinPlayers = 1,
        MaxPlayers = 10,
        MinimumPlayersRating = MinimumPlayersAverage.All,
        CreatorId = string.Empty,
        MembersIds = []
    };

    private bool _isLoadingMembers;
    private string _memberSearch = string.Empty;
    private int _currentMemberPage = 1;
    private string? _userId;

    public CreateMatchDto Model
    {
        get => _model;
        set
        {
            _model = value;
            OnPropertyChanged(nameof(Model));
        }
    }

    public bool IsLoading
    {
        get;
        set;
    } = true;

    public bool IsSubmitting
    {
        get;
        set;
    }

    public bool IsLoadingMembers
    {
        get => _isLoadingMembers;
        set
        {
            _isLoadingMembers = value;
            OnPropertyChanged(nameof(IsLoadingMembers));
        }
    }

    public string UserId
    {
        get => _userId ?? string.Empty;
        set => _userId = value;
    }

    public string MemberSearch
    {
        get => _memberSearch;
        set
        {
            if (_memberSearch != value)
            {
                _memberSearch = value;
                OnPropertyChanged(nameof(MemberSearch));
            }
        }
    }

    public int CurrentMemberPage
    {
        get => _currentMemberPage;
        set
        {
            _currentMemberPage = value;
            OnPropertyChanged(nameof(CurrentMemberPage));
        }
    }

    public PaginationResponse<List<ApplicationUser>> AvailableUsers { get; private set; } = new()
    {
        Data = [],
        TotalCount = 0
    };

    public List<ApplicationUser> SelectedUsers { get; private set; } = [];

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public void UpdateDescription(string value)
    {
        Model = Model with { Description = value };
    }

    public void UpdatePrivacy(MatchPrivacy value)
    {
        Model = Model with { Privacy = value };
    }
    
    public void UpdateAddress(string value)
    {
        Model = Model with { Address = value };
    }

    public void UpdateMinPlayers(int value)
    {
        Model = Model with { MinPlayers = value };
    }
    
    public void UpdateMaxPlayers(int value)
    {
        Model = Model with { MaxPlayers = value };
    }
    
    public void UpdateSport(Sports value)
    {
        Model = Model with { Sport = value };
    }
    
    public void UpdateMinimumPlayersRating(MinimumPlayersAverage value)
    {
        Model = Model with { MinimumPlayersRating = value };
    }

    public void UpdateMatchDateTimeUtc(DateTime value)
    {
        Model = Model with { MatchDateTimeUtc = value };
    }

    public void UpdateLocation(Location value)
    {
        Model = Model with { Location = value };
    }
    
    public ValidationStatus ValidateProperty(string propertyName)
    {
        ValidationResult result = validator.Validate(Model);
        return result.Errors.Any(e => e.PropertyName == propertyName) ? ValidationStatus.Error : ValidationStatus.Success;
    }

    public string? GetValidationError(string propertyName)
    {
        ValidationResult result = validator.Validate(Model);
        return result.Errors.FirstOrDefault(e => e.PropertyName == propertyName)?.ErrorMessage;
    }

    public async Task LoadMembersAsync()
    {
        IsLoadingMembers = true;
        try
        {
            // Use GetUsers as defined in IUsersService interface
            Result<PaginationResponse<List<ApplicationUser>>> result = await usersService.GetUsers(_memberSearch, _currentMemberPage, 10, CancellationToken.None);
            if (result.Success && result.Data != null)
            {
                AvailableUsers = result.Data;
                OnPropertyChanged(nameof(AvailableUsers));
            }
        }
        finally
        {
            IsLoadingMembers = false;
        }
    }

    public async Task OnMemberSearchKeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            CurrentMemberPage = 1;
            await LoadMembersAsync();
        }
    }

    public async Task OnMemberPageChangedAsync(int page)
    {
        CurrentMemberPage = page;
        await LoadMembersAsync();
    }

    public void ToggleMember(string userId)
    {
        if (userId == UserId)
        {
            return;
        }

        var currentMembers = Model.MembersIds.ToList();
        if (currentMembers.Remove(userId)) 
        {
            ApplicationUser? userToRemove = SelectedUsers.FirstOrDefault(u => u.Id == userId);
            if (userToRemove != null)
            {
                SelectedUsers.Remove(userToRemove);
            }
        }
        else
        {
            currentMembers.Add(userId);
            ApplicationUser? user = AvailableUsers.Data.FirstOrDefault(u => u.Id == userId);
            if (user != null && SelectedUsers.All(u => u.Id != userId))
            {
                SelectedUsers.Add(user);
            }
        }

        Model = Model with { MembersIds = currentMembers };
        OnPropertyChanged(nameof(SelectedUsers));
    }
    
    public void RemoveMember(string userId)
    {
        var currentMembers = Model.MembersIds.ToList();
        if (!currentMembers.Contains(userId))
        {
            return;
        }

        currentMembers.Remove(userId);
        ApplicationUser? userToRemove = SelectedUsers.FirstOrDefault(u => u.Id == userId);
        if (userToRemove != null)
        {
            SelectedUsers.Remove(userToRemove);
        }

        Model = Model with { MembersIds = currentMembers };
        OnPropertyChanged(nameof(SelectedUsers));
    }

    public async Task<bool> SubmitMatchAsync()
    {
        IsSubmitting = true;
        try
        {
            CreateMatchDto createDto = _model with
            {
                CreatorId = _userId!,
                MembersIds = _model.MembersIds.Distinct().ToList()
            };

            Result<MatchDto> result = await matchesService.CreateMatch(createDto, CancellationToken.None);

            if (!result.Success)
            {
                await toastService.Error(result.ErrorMessages.Any() ? result.ErrorMessages[0] : "Error creating match.");
                return false;
            }

            if (_model.MembersIds.Count > 0)
            {
                await toastService.Success($"Match created successfully! Invites sent to {_model.MembersIds.Count} user(s).", "Create Match");
            }
            else
            {
                await toastService.Success("Match created successfully!", "Create Match");
            }

            return true;
        }
        finally
        {
            IsSubmitting = false;
        }
    }
}
