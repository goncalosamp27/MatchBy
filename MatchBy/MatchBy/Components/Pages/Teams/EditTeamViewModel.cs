using System.ComponentModel;
using Blazorise;
using FluentValidation;
using MatchBy.DTOs.Team;
using MatchBy.DTOs.TeamInvite;
using MatchBy.DTOs.User;
using MatchBy.Models;
using MatchBy.Services.S3;
using MatchBy.Services.TeamInvites;
using MatchBy.Services.Teams;
using MatchBy.Services.Users;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;

namespace MatchBy.Components.Pages.Teams;

public sealed class EditTeamViewModel(
    ITeamService teamService,
    IUsersService usersService,
    ITeamsInvitesService teamInvitesService,
    IValidator<UpdateTeamDto> validator,
    IToastService toastService) : INotifyPropertyChanged
{
    private const long MaxPreviewBytes = 5 * 1024 * 1024;

    private UpdateTeamDto? _model;
    private string? _userId;
    private string? _teamId;
    private bool _isLoadingMembers;
    private bool _isLoadingInviteUsers;
    private string _memberSearch = string.Empty;
    private string _inviteSearch = string.Empty;
    private int _currentMemberPage = 1;
    private int _currentInvitePage = 1;
    private readonly HashSet<string> _selectedMemberIds = [];
    private List<UserDto> _currentTeamMembers = [];
    private IBrowserFile? _selectedImage;
    private string? _currentTeamImageUrl;

    public event PropertyChangedEventHandler? PropertyChanged;

    public UpdateTeamDto? Model
    {
        get => _model;
        private set
        {
            _model = value;
            OnPropertyChanged(nameof(Model));
        }
    }

    public string? UserId
    {
        get => _userId;
        private set
        {
            _userId = value;
            OnPropertyChanged(nameof(UserId));
            OnPropertyChanged(nameof(MemberBadgeCount));
        }
    }

    public string? TeamId
    {
        get => _teamId;
        private set
        {
            _teamId = value;
            OnPropertyChanged(nameof(TeamId));
        }
    }

    public bool IsLoading
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(IsLoading));
        }
    } = true;

    public bool IsSubmitting
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(IsSubmitting));
        }
    }

    public bool IsLoadingMembers
    {
        get => _isLoadingMembers;
        private set
        {
            _isLoadingMembers = value;
            OnPropertyChanged(nameof(IsLoadingMembers));
        }
    }

    public bool IsLoadingInvites
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(IsLoadingInvites));
        }
    }

    public bool IsLoadingInviteUsers
    {
        get => _isLoadingInviteUsers;
        private set
        {
            _isLoadingInviteUsers = value;
            OnPropertyChanged(nameof(IsLoadingInviteUsers));
        }
    }

    public string MemberSearch
    {
        get => _memberSearch;
        set
        {
            _memberSearch = value;
            OnPropertyChanged(nameof(MemberSearch));
        }
    }

    public string InviteSearch
    {
        get => _inviteSearch;
        set
        {
            _inviteSearch = value;
            OnPropertyChanged(nameof(InviteSearch));
        }
    }

    public int CurrentMemberPage
    {
        get => _currentMemberPage;
        private set
        {
            _currentMemberPage = value;
            OnPropertyChanged(nameof(CurrentMemberPage));
        }
    }

    public int CurrentInvitePage
    {
        get => _currentInvitePage;
        private set
        {
            _currentInvitePage = value;
            OnPropertyChanged(nameof(CurrentInvitePage));
        }
    }

    public IReadOnlyCollection<string> SelectedMemberIds => _selectedMemberIds;

    public PaginationResponse<List<UserDto>> AvailableUsers
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(AvailableUsers));
        }
    } = new()
    {
        Page = 0,
        TotalCount = 0,
        PageSize = 0,
        Data = []
    };

    public PaginationResponse<List<UserDto>> AvailableInviteUsers
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(AvailableInviteUsers));
        }
    } = new()
    {
        Page = 0,
        TotalCount = 0,
        PageSize = 0,
        Data = []
    };

    public List<UserDto> CurrentTeamMembers
    {
        get => _currentTeamMembers;
        private set
        {
            _currentTeamMembers = value;
            OnPropertyChanged(nameof(CurrentTeamMembers));
        }
    }

    public List<TeamInviteDto>? PendingInvites
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(PendingInvites));
        }
    }

    public IBrowserFile? SelectedImage
    {
        get => _selectedImage;
        private set
        {
            _selectedImage = value;
            OnPropertyChanged(nameof(SelectedImage));
        }
    }

    public string? SelectedImagePreviewUrl
    {
        get;
        private set
        {
            field = value;
            OnPropertyChanged(nameof(SelectedImagePreviewUrl));
            OnPropertyChanged(nameof(ImagePreviewSource));
        }
    }

    public string? CurrentTeamImageUrl
    {
        get => _currentTeamImageUrl;
        private set
        {
            _currentTeamImageUrl = value;
            OnPropertyChanged(nameof(CurrentTeamImageUrl));
            OnPropertyChanged(nameof(ImagePreviewSource));
        }
    }

    public string? ImagePreviewSource => SelectedImagePreviewUrl ?? CurrentTeamImageUrl;

    public int MemberBadgeCount
    {
        get
        {
            int offset = string.IsNullOrEmpty(_userId) ? 0 : 1;
            return Math.Max(0, _selectedMemberIds.Count - offset);
        }
    }

    public bool HasAdditionalMembers => _selectedMemberIds.Any(id => id != _userId);

    public ValidationStatus ValidateProperty(string propertyPath)
    {
        if (_model is null)
        {
            return ValidationStatus.Error;
        }

        var errors = validator.Validate(_model).Errors
            .Where(x => x.PropertyName == propertyPath)
            .ToList();

        return errors.Any() ? ValidationStatus.Error : ValidationStatus.Success;
    }

    public string? GetValidationError(string propertyPath)
    {
        if (_model is null)
        {
            return "Model is not ready yet.";
        }

        var errors = validator.Validate(_model).Errors
            .Where(x => x.PropertyName == propertyPath)
            .ToList();

        return errors.Any() ? errors[0].ErrorMessage : null;
    }

    public async Task<EditTeamInitializationResult> InitializeAsync(string teamId, string userId, CancellationToken ct = default)
    {
        TeamId = teamId;
        UserId = userId;

        IsLoading = true;
        try
        {
            await LoadMembersAsync(ct);
            await LoadInviteUsersAsync(ct);
            return await LoadTeamAsync(ct);
        }
        finally
        {
            IsLoading = false;
        }
    }

    public void UpdateName(string name)
    {
        if (_model is null)
        {
            return;
        }

        Model = _model with { Name = name };
    }

    public void UpdateDescription(string description)
    {
        if (_model is null)
        {
            return;
        }

        Model = _model with { Description = description };
    }

    public void UpdatePrivacy(TeamPrivacy privacy)
    {
        if (_model is null)
        {
            return;
        }

        Model = _model with { Privacy = privacy };
    }

    public async Task OnImageSelectedAsync(InputFileChangeEventArgs e)
    {
        SelectedImage = e.File;
        try
        {
            await using Stream stream = e.File.OpenReadStream(MaxPreviewBytes);
            await using var memory = new MemoryStream();
            await stream.CopyToAsync(memory);
            string base64 = Convert.ToBase64String(memory.ToArray());
            SelectedImagePreviewUrl = $"data:{e.File.ContentType};base64,{base64}";
        }
        catch (Exception ex)
        {
            SelectedImagePreviewUrl = null;
            await toastService.Error($"Unable to generate preview: {ex.Message}");
        }
    }

    public async Task RemoveImageAsync()
    {
        if (SelectedImage != null)
        {
            SelectedImage = null;
            SelectedImagePreviewUrl = null;
            return;
        }

        if (string.IsNullOrEmpty(_teamId) || string.IsNullOrEmpty(_userId))
        {
            return;
        }

        if (!string.IsNullOrEmpty(_currentTeamImageUrl))
        {
            try
            {
                Result<bool> result = await teamService.DeleteTeamImageAsync(_teamId, _userId, CancellationToken.None);

                if (result.Success)
                {
                    await toastService.Success("Team image deleted successfully!", "Delete Image");
                    CurrentTeamImageUrl = null;
                    SelectedImagePreviewUrl = null;
                }
                else
                {
                    await toastService.Error(result.ErrorMessages.Any() ? result.ErrorMessages[0] : "Failed to delete team image.");
                }
            }
            catch (Exception ex)
            {
                await toastService.Error($"Error deleting image: {ex.Message}");
            }
        }

        SelectedImage = null;
        SelectedImagePreviewUrl = null;
    }

    public void ToggleMember(string userId)
    {
        if (!_selectedMemberIds.Add(userId))
        {
            _selectedMemberIds.Remove(userId);
        }

        OnPropertyChanged(nameof(SelectedMemberIds));
        OnPropertyChanged(nameof(MemberBadgeCount));
        OnPropertyChanged(nameof(HasAdditionalMembers));
    }

    public void RemoveMember(string userId)
    {
        _selectedMemberIds.Remove(userId);
        OnPropertyChanged(nameof(SelectedMemberIds));
        OnPropertyChanged(nameof(MemberBadgeCount));
        OnPropertyChanged(nameof(HasAdditionalMembers));
    }

    public async Task LoadMembersAsync(CancellationToken ct = default)
    {
        if (_isLoadingMembers)
        {
            return;
        }

        IsLoadingMembers = true;
        try
        {
            Result<PaginationResponse<List<UserDto>>> response = await usersService.GetUsers(_memberSearch, _currentMemberPage, 10, ct);
            if (response.Success)
            {
                PaginationResponse<List<UserDto>>? users = response.Data!;
                if (_userId != null)
                {
                    users.Data = users.Data.Where(u => u.Id != _userId).ToList();
                }

                AvailableUsers = users;
            }
            else
            {
                await toastService.Error(response.ErrorMessages.Any() ? response.ErrorMessages[0] : "Failed to load members.");
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

    public async Task LoadInviteUsersAsync(CancellationToken ct = default)
    {
        if (_isLoadingInviteUsers)
        {
            return;
        }

        IsLoadingInviteUsers = true;
        try
        {
            Result<PaginationResponse<List<UserDto>>> response = await usersService.GetUsers(_inviteSearch, _currentInvitePage, 10, ct);
            if (response.Success)
            {
                PaginationResponse<List<UserDto>>? users = response.Data!;
                if (_userId != null)
                {
                    users.Data = users.Data.Where(u => u.Id != _userId).ToList();
                }

                AvailableInviteUsers = users;
            }
            else
            {
                await toastService.Error(response.ErrorMessages.Any() ? response.ErrorMessages[0] : "Failed to load invite users.");
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

    public async Task<bool> SendInviteToUserAsync(string receiverId)
    {
        if (string.IsNullOrEmpty(_teamId) || string.IsNullOrEmpty(_userId) || _model is null)
        {
            return false;
        }

        try
        {
            var teamInviteDto = new CreateTeamInviteDto
            {
                Content = $"You've been invited to join {_model.Name}!",
                SenderId = _userId,
                ReceiverId = receiverId,
                TeamId = _teamId
            };

            Result<TeamInviteDto> result = await teamInvitesService.CreateInvite(teamInviteDto, CancellationToken.None);

            if (result.Success)
            {
                await toastService.Success("Invite sent successfully!", "Invite User");
                await LoadPendingInvitesAsync();
                return true;
            }

            await toastService.Error(result.ErrorMessages.Any() ? result.ErrorMessages[0] : "Failed to send invite.");
        }
        catch (Exception ex)
        {
            await toastService.Error($"Error sending invite: {ex.Message}");
        }

        return false;
    }

    public async Task<bool> RemoveInviteAsync(string inviteId)
    {
        if (string.IsNullOrEmpty(_userId))
        {
            return false;
        }

        try
        {
            Result<bool> result = await teamInvitesService.DeleteInvite(inviteId, _userId, CancellationToken.None);
            if (result.Success)
            {
                await toastService.Success("Invite cancelled successfully!", "Cancel Invite");
                await LoadPendingInvitesAsync();
                return true;
            }

            await toastService.Error(result.ErrorMessages.Any() ? result.ErrorMessages[0] : "Failed to cancel invite.");
        }
        catch (Exception ex)
        {
            await toastService.Error($"Error cancelling invite: {ex.Message}");
        }

        return false;
    }

    public async Task<bool> SubmitTeamAsync(Validations validations)
    {
        if (_model is null || !await validations.ValidateAll())
        {
            return false;
        }

        IsSubmitting = true;
        try
        {
            var currentMemberIds = _currentTeamMembers.Select(m => m.Id).ToHashSet();
            var newMemberIds = _selectedMemberIds.ToHashSet();
            var usersToInvite = newMemberIds.Except(currentMemberIds).ToList();

            UpdateTeamDto updateDto = _model with
            {
                MembersIds = _selectedMemberIds.ToList(),
                File = _selectedImage
            };

            Result<TeamDto> result = await teamService.UpdateTeamAsync(updateDto, CancellationToken.None);

            if (!result.Success)
            {
                await toastService.Error(result.ErrorMessages.Any() ? result.ErrorMessages[0] : "Error updating team.");
                return false;
            }

            int invitesSent = 0;
            if (usersToInvite.Any() && _teamId != null && _userId != null)
            {
                foreach (CreateTeamInviteDto teamInviteDto in usersToInvite.Select(receiverId => new CreateTeamInviteDto
                         {
                             Content = $"You've been invited to join {_model.Name}!",
                             SenderId = _userId,
                             ReceiverId = receiverId,
                             TeamId = _teamId
                         }))
                {
                    Result<TeamInviteDto> inviteResult = await teamInvitesService.CreateInvite(teamInviteDto, CancellationToken.None);
                    if (inviteResult.Success)
                    {
                        invitesSent++;
                    }
                }
            }

            if (invitesSent > 0)
            {
                await toastService.Success($"Team updated successfully! Invites sent to {invitesSent} user(s).", "Update Team");
            }
            else
            {
                await toastService.Success("Team updated successfully!", "Update Team");
            }

            return true;
        }
        finally
        {
            IsSubmitting = false;
        }
    }

    private async Task<EditTeamInitializationResult> LoadTeamAsync(CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_teamId) || string.IsNullOrEmpty(_userId))
        {
            return new EditTeamInitializationResult(EditTeamLoadStatus.Invalid, "Invalid identifiers.");
        }

        Result<TeamDto> result = await teamService.GetTeamByIdAsync(_teamId, _userId, ct);

        if (!result.Success || result.Data is null)
        {
            string message = result.ErrorMessages.FirstOrDefault() ?? "Unable to load team.";
            return new EditTeamInitializationResult(EditTeamLoadStatus.NotFound, message);
        }

        TeamDto? team = result.Data;

        if (team.OwnerId != _userId)
        {
            return new EditTeamInitializationResult(EditTeamLoadStatus.Unauthorized, "You don't have permission to edit this team.");
        }

        Model = new UpdateTeamDto
        {
            Id = _teamId,
            Name = team.Name,
            Description = team.Description,
            OwnerId = team.OwnerId,
            MembersIds = team.Members.Select(m => m.Id).ToList(),
            MaxMembers = team.MaxMembers,
            Privacy = team.Privacy
        };

        _selectedMemberIds.Clear();
        foreach (string id in team.Members.Select(m => m.Id))
        {
            _selectedMemberIds.Add(id);
        }
        OnPropertyChanged(nameof(SelectedMemberIds));
        OnPropertyChanged(nameof(MemberBadgeCount));
        OnPropertyChanged(nameof(HasAdditionalMembers));

        CurrentTeamMembers = team.Members.ToList();
        CurrentTeamImageUrl = team.ImageUrl;

        await LoadPendingInvitesAsync(ct);

        return new EditTeamInitializationResult(EditTeamLoadStatus.Success, null);
    }

    private async Task LoadPendingInvitesAsync(CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(_teamId))
        {
            PendingInvites = [];
            return;
        }

        IsLoadingInvites = true;
        try
        {
            Result<PaginationResponse<List<TeamInviteDto>>> result = await teamInvitesService.GetInvites(_teamId, 1, 10, ct);
            PendingInvites = result is { Success: true, Data: not null }
                ? result.Data.Data
                    .Where(i => i is { Status: InviteStatus.Pending, DeletedAtUtc: null })
                    .ToList()
                : [];
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error loading invites: {ex.Message}");
            PendingInvites = [];
        }
        finally
        {
            IsLoadingInvites = false;
        }
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

public enum EditTeamLoadStatus
{
    Success,
    NotFound,
    Unauthorized,
    Invalid
}

public sealed record EditTeamInitializationResult(EditTeamLoadStatus Status, string? ErrorMessage);



