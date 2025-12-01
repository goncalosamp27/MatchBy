using System.ComponentModel;
using Blazorise;
using FluentValidation;
using FluentValidation.Results;
using MatchBy.DTOs.Match;
using MatchBy.DTOs.MatchInvite;
using MatchBy.DTOs.User;
using MatchBy.Models;
using MatchBy.Services.Matches;
using MatchBy.Services.MatchInvites;
using MatchBy.Services.Users;
using MatchBy.Enums;
using Microsoft.AspNetCore.Components.Web;

namespace MatchBy.Components.Pages.Matches;

public enum EditMatchLoadStatus
{
    Success,
    NotFound,
    Unauthorized,
    Invalid
}

public sealed class EditMatchLoadResult
{
    public EditMatchLoadStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
}

public sealed class EditMatchViewModel(
    IMatchesService matchesService,
    IUsersService usersService,
    IMatchesInvitesService matchesInvitesService,
    IValidator<UpdateMatchDto> validator,
    IToastService toastService)
    : INotifyPropertyChanged
{
    private string _userId;
    private string _matchId = string.Empty;
    private string _inviteSearch = string.Empty;
    private int _currentInvitePage = 1;

    // Current participants of the match
    public List<ApplicationUser> CurrentMatchParticipants { get; private set; } = [];
    
    // Pending invites for the match
    public List<MatchInviteDto> PendingInvites { get; private set; } = [];
    
    // Users available to be invited (search results)
    public PaginationResponse<List<ApplicationUser>> AvailableInviteUsers { get; private set; } = new()
    {
        Data = [],
        TotalCount = 0
    };

    public UpdateMatchDto? Model
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(Model));
        }
    }

    public string MatchId
    {
        get => _matchId;
        set
        {
            _matchId = value;
            OnPropertyChanged(nameof(MatchId));
        }
    }
    
    public string UserId
    {
        get => _userId;
        set
        {
            _userId = value;
            OnPropertyChanged(nameof(UserId));
        }
    }

    public bool IsLoading
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(IsLoading));
        }
    } = true;

    public bool IsSubmitting
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(IsSubmitting));
        }
    }

    public bool IsLoadingInvites
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(IsLoadingInvites));
        }
    }

    public bool IsLoadingInviteUsers
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(IsLoadingInviteUsers));
        }
    }


    public string InviteSearch
    {
        get => _inviteSearch;
        set
        {
            if (_inviteSearch != value)
            {
                _inviteSearch = value;
                OnPropertyChanged(nameof(InviteSearch));
            }
        }
    }

    public int CurrentInvitePage
    {
        get => _currentInvitePage;
        set
        {
            _currentInvitePage = value;
            OnPropertyChanged(nameof(CurrentInvitePage));
        }
    }

    // Helper to check if a user is already a participant
    public bool IsParticipant(string userId) => CurrentMatchParticipants.Any(p => p.Id == userId);

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public async Task<EditMatchLoadResult> InitializeAsync(string matchId, string userId)
    {
        _matchId = matchId;
        _userId = userId;
        IsLoading = true;

        try
        {
            Result<MatchDto> matchResult = await matchesService.GetMatchById(matchId, userId, CancellationToken.None);
            if (!matchResult.Success || matchResult.Data is null)
            {
                return new EditMatchLoadResult { Status = EditMatchLoadStatus.NotFound };
            }

            MatchDto? match = matchResult.Data;

            if (match.CreatorId != userId)
            {
                return new EditMatchLoadResult 
                { 
                    Status = EditMatchLoadStatus.Unauthorized, 
                    ErrorMessage = "Only the creator can edit this match." 
                };
            }

            Model = new UpdateMatchDto
            {
                MatchId = match.Id,
                Location = match.Location,
                MatchDateTimeUtc = match.MatchDateTimeUtc,
                Address = match.Address,
                Description = match.Description,
                Privacy = match.Privacy,
                Sport = match.Sport,
                MinPlayers = match.MinPlayers,
                MaxPlayers = match.MaxPlayers,
                MinimumPlayersRating = match.MinimumPlayersRating,
                UserId = userId
            };
            
            CurrentMatchParticipants = match.Participants.Select(p => new ApplicationUser 
            { 
                Id = p.Id, 
                DisplayName = p.DisplayName, 
                UserName = p.DisplayName, // Fallback
                // Map AvatarUrl to FileStore if present
                ProfileImage = !string.IsNullOrEmpty(p.AvatarUrl) ? new FileStore(p.AvatarUrl, DateTime.MaxValue, "dummy", FileCategory.ProfileImage, FileType.Image, DateTime.MinValue) : null
            }).ToList();

            await LoadInvitesAsync();
            await LoadInviteUsersAsync();

            return new EditMatchLoadResult { Status = EditMatchLoadStatus.Success };
        }
        finally
        {
            IsLoading = false;
        }
    }

    private async Task LoadInvitesAsync()
    {
        IsLoadingInvites = true;
        try
        {
            Result<PaginationResponse<List<MatchInviteDto>>> result = await matchesInvitesService.GetInvitesForMatch(_matchId, 1, 100, CancellationToken.None);
            if (result.Success && result.Data != null)
            {
                PendingInvites = result.Data.Data
                    .Where(i => i.Status == InviteStatus.Pending && !i.IsExpired)
                    .ToList();
                OnPropertyChanged(nameof(PendingInvites));
            }
        }
        finally
        {
            IsLoadingInvites = false;
        }
    }

    public async Task LoadInviteUsersAsync()
    {
        IsLoadingInviteUsers = true;
        try
        {
            Result<PaginationResponse<List<ApplicationUser>>> result = await usersService.GetUsers(_inviteSearch, _currentInvitePage, 10, CancellationToken.None);
            if (result.Success && result.Data != null)
            {
                AvailableInviteUsers = result.Data;
                OnPropertyChanged(nameof(AvailableInviteUsers));
            }
        }
        finally
        {
            IsLoadingInviteUsers = false;
        }
    }

    public async Task OnInviteSearchKeyDownAsync(KeyboardEventArgs e)
    {
        if (e.Key == "Enter")
        {
            CurrentInvitePage = 1;
            await LoadInviteUsersAsync();
        }
    }

    public async Task OnInvitePageChangedAsync(int page)
    {
        CurrentInvitePage = page;
        await LoadInviteUsersAsync();
    }
    
    public void UpdateDescription(string value)
    {
        if (Model is not null) 
        {
            Model = Model with { Description = value };
        }
    }
    
    public void UpdateAddress(string value)
    {
        if (Model is not null) 
        {
            Model = Model with { Address = value };
        }
    }
    
    public void UpdatePrivacy(MatchPrivacy value)
    {
        if (Model is not null) 
        {
            Model = Model with { Privacy = value };
        }
    }

    public ValidationStatus ValidateProperty(string propertyName)
    {
        if (Model is null) 
        {
            return ValidationStatus.None;
        }
        ValidationResult result = validator.Validate(Model);
        return result.Errors.Any(e => e.PropertyName == propertyName) ? ValidationStatus.Error : ValidationStatus.Success;
    }

    public string? GetValidationError(string propertyName)
    {
        if (Model is null) 
        {
            return null;
        }
        ValidationResult result = validator.Validate(Model);
        return result.Errors.FirstOrDefault(e => e.PropertyName == propertyName)?.ErrorMessage;
    }

    public async Task<bool> SubmitMatchAsync(Validations validations)
    {
        if (Model is null) 
        {
            return false;
        }
        
        if (!await validations.ValidateAll()) 
        {
            return false;
        }

        IsSubmitting = true;
        try
        {
            Result<bool> result = await matchesService.UpdateMatch(Model, CancellationToken.None);
            
            if (!result.Success)
            {
                await toastService.Error(result.ErrorMessages.Any() ? result.ErrorMessages[0] : "Error updating match.");
                return false;
            }

            await toastService.Success("Match updated successfully!", "Update Match");
            return true;
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    public async Task SendInviteToUserAsync(string receiverId)
    {
        if (string.IsNullOrEmpty(_matchId) || string.IsNullOrEmpty(_userId)) 
        {
            return;
        }

        try
        {
            var createDto = new CreateMatchInviteDto
            {
                MatchId = _matchId,
                SenderId = _userId,
                ReceiverId = receiverId,
                Content = $"You've been invited to join the match!" 
            };

            Result<MatchInviteDto> result = await matchesInvitesService.CreateInvite(createDto, CancellationToken.None);

            if (result.Success)
            {
                await toastService.Success("Invite sent successfully!");
                await LoadInvitesAsync(); 
                OnPropertyChanged(nameof(PendingInvites));
            }
            else
            {
                await toastService.Error(result.ErrorMessages.Any() ? result.ErrorMessages[0] : "Failed to send invite.");
            }
        }
        catch (Exception ex)
        {
            await toastService.Error($"An error occurred: {ex.Message}");
        }
    }

    public async Task RemoveInviteAsync(string inviteId)
    {
        if (string.IsNullOrEmpty(inviteId)) 
        {
            return;
        }

        try
        {
            Result<bool> result = await matchesInvitesService.DeleteInvite(inviteId, _userId, CancellationToken.None);
            if (result.Success)
            {
                await toastService.Success("Invite cancelled.");
                await LoadInvitesAsync();
            }
            else
            {
                await toastService.Error("Failed to cancel invite.");
            }
        }
        catch (Exception ex)
        {
            await toastService.Error($"An error occurred: {ex.Message}");
        }
    }
    
    public async Task RemoveParticipantAsync(string participantId)
    {
        if (participantId == _userId) 
        {
            return; 
        }

        try 
        {
            Result<bool> result = await matchesService.LeaveMatch(_matchId, participantId, CancellationToken.None);
            if (result.Success)
            {
                 await toastService.Success("Participant removed.");
                 Result<MatchDto> matchResult = await matchesService.GetMatchById(_matchId, _userId, CancellationToken.None);
                 if (matchResult.Success && matchResult.Data != null)
                 {
                     CurrentMatchParticipants = matchResult.Data.Participants.Select(p => new ApplicationUser 
                    { 
                        Id = p.Id, 
                        DisplayName = p.DisplayName, 
                        UserName = p.DisplayName,
                        ProfileImage = !string.IsNullOrEmpty(p.AvatarUrl) ? new FileStore(p.AvatarUrl, DateTime.MaxValue, "dummy", FileCategory.ProfileImage, FileType.Image, DateTime.MinValue) : null
                    }).ToList();
                    OnPropertyChanged(nameof(CurrentMatchParticipants));
                 }
            }
            else 
            {
                 await toastService.Error("Failed to remove participant.");
            }
        }
        catch(Exception ex)
        {
             await toastService.Error($"An error occurred: {ex.Message}");
        }
    }
}
