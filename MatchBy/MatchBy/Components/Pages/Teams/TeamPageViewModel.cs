using System.ComponentModel;
using Blazorise;
using MatchBy.DTOs.Team;
using MatchBy.DTOs.TeamInvite;
using MatchBy.Models;
using MatchBy.Services.TeamInvites;
using MatchBy.Services.Teams;

namespace MatchBy.Components.Pages.Teams;

public sealed class TeamPageViewModel(
    ITeamService teamService,
    ITeamsInvitesService teamInvitesService,
    IToastService toastService) : INotifyPropertyChanged
{
    private TeamDto? _team;
    private List<TeamInviteDto>? _pendingInvites;
    private string? _userId;
    private string? _teamId;
    private bool _isLoading = true;
    private bool _hasError;
    private string _errorMessage = string.Empty;
    private bool _isRetrying;
    private bool _isLoadingInvites;
    private bool _isJoiningTeam;
    private bool _isLeavingTeam;

    public event PropertyChangedEventHandler? PropertyChanged;

    public TeamDto? Team
    {
        get => _team;
        private set
        {
            _team = value;
            OnPropertyChanged(nameof(Team));
        }
    }

    public List<TeamInviteDto>? PendingInvites
    {
        get => _pendingInvites;
        private set
        {
            _pendingInvites = value;
            OnPropertyChanged(nameof(PendingInvites));
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
        get => _isLoading;
        private set
        {
            _isLoading = value;
            OnPropertyChanged(nameof(IsLoading));
        }
    }

    public bool HasError
    {
        get => _hasError;
        private set
        {
            _hasError = value;
            OnPropertyChanged(nameof(HasError));
        }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        private set
        {
            _errorMessage = value;
            OnPropertyChanged(nameof(ErrorMessage));
        }
    }

    public bool IsRetrying
    {
        get => _isRetrying;
        private set
        {
            _isRetrying = value;
            OnPropertyChanged(nameof(IsRetrying));
        }
    }

    public bool IsLoadingInvites
    {
        get => _isLoadingInvites;
        private set
        {
            _isLoadingInvites = value;
            OnPropertyChanged(nameof(IsLoadingInvites));
        }
    }

    public bool IsJoiningTeam
    {
        get => _isJoiningTeam;
        private set
        {
            _isJoiningTeam = value;
            OnPropertyChanged(nameof(IsJoiningTeam));
        }
    }

    public bool IsLeavingTeam
    {
        get => _isLeavingTeam;
        private set
        {
            _isLeavingTeam = value;
            OnPropertyChanged(nameof(IsLeavingTeam));
        }
    }

    public async Task InitializeAsync(string teamId, string userId)
    {
        TeamId = teamId;
        UserId = userId;
        await LoadTeamDataAsync();
    }

    public async Task LoadTeamDataAsync()
    {
        if (string.IsNullOrWhiteSpace(_teamId))
        {
            SetErrorState("Invalid team ID provided.");
            return;
        }

        IsLoading = true;
        HasError = false;
        ErrorMessage = string.Empty;

        try
        {
            Result<TeamDto> result = await teamService.GetTeamByIdAsync(_teamId, _userId ?? string.Empty, CancellationToken.None);

            if (!result.Success)
            {
                string errorMsg = result.ErrorMessages.Any()
                    ? string.Join(", ", result.ErrorMessages)
                    : "Failed to load team details.";
                SetErrorState(errorMsg);
                return;
            }

            Team = result.Data;
            HasError = false;
            ErrorMessage = string.Empty;

            if (_team?.OwnerId == _userId)
            {
                await LoadPendingInvitesAsync();
            }
            else
            {
                PendingInvites = null;
            }
        }
        catch (Exception ex)
        {
            SetErrorState($"An unexpected error occurred: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task RetryLoadTeamAsync()
    {
        if (_isRetrying)
        {
            return;
        }

        IsRetrying = true;
        await LoadTeamDataAsync();
        IsRetrying = false;
    }

    public async Task LoadPendingInvitesAsync()
    {
        if (string.IsNullOrWhiteSpace(_teamId) || _isLoadingInvites)
        {
            return;
        }

        IsLoadingInvites = true;
        try
        {
            Result<PaginationResponse<List<TeamInviteDto>>> result = await teamInvitesService.GetInvites(_teamId, 1, int.MaxValue, CancellationToken.None);

            if (result is { Success: true, Data: not null })
            {
                PendingInvites = result.Data.Data
                    .Where(i => i.Status == InviteStatus.Pending && !i.IsExpired)
                    .ToList();
            }
            else
            {
                PendingInvites = [];
                if (result.ErrorMessages.Any(m => !m.Contains("not found", StringComparison.OrdinalIgnoreCase)))
                {
                    string errorMsg = result.ErrorMessages.Any()
                        ? string.Join(", ", result.ErrorMessages)
                        : "Failed to load pending invitations.";
                    await toastService.Error(errorMsg);
                }
            }
        }
        catch (Exception ex)
        {
            PendingInvites = [];
            await toastService.Error($"Error loading invitations: {ex.Message}");
        }
        finally
        {
            IsLoadingInvites = false;
        }
    }

    public async Task LeaveTeamAsync()
    {
        if (string.IsNullOrWhiteSpace(_userId) || _isLeavingTeam || _isJoiningTeam || string.IsNullOrWhiteSpace(_teamId))
        {
            return;
        }

        IsLeavingTeam = true;
        try
        {
            Result<int> result = await teamService.LeaveTeamAsync(_teamId, _userId, CancellationToken.None);

            if (!result.Success)
            {
                string errorMsg = result.ErrorMessages.Any()
                    ? string.Join(", ", result.ErrorMessages)
                    : "Error leaving team.";
                await toastService.Error(errorMsg);
                return;
            }

            if (result.Data == 1)
            {
                await toastService.Success("Team deleted successfully!", "Leave Team");
                Team = null;
                return;
            }

            if (_team?.Privacy == TeamPrivacy.Private)
            {
                await toastService.Success("Successfully left the team!", "Leave Team");
                Team = null;
                return;
            }

            await toastService.Success("Successfully left the team!", "Leave Team");
            await ReloadTeamDataAsync();
        }
        catch (Exception ex)
        {
            await toastService.Error($"An error occurred while leaving the team: {ex.Message}");
        }
        finally
        {
            IsLeavingTeam = false;
        }
    }

    public async Task JoinTeamAsync()
    {
        if (string.IsNullOrWhiteSpace(_userId) || _isJoiningTeam || _isLeavingTeam || string.IsNullOrWhiteSpace(_teamId))
        {
            return;
        }

        IsJoiningTeam = true;
        try
        {
            Result<bool> result = await teamService.JoinTeamAsync(_teamId, _userId, CancellationToken.None);

            if (!result.Success)
            {
                string errorMsg = result.ErrorMessages.Any()
                    ? string.Join(", ", result.ErrorMessages)
                    : "Error joining team.";
                await toastService.Error(errorMsg);
                return;
            }

            await toastService.Success("Successfully joined the team!", "Join Team");
            await ReloadTeamDataAsync();
        }
        catch (Exception ex)
        {
            await toastService.Error($"An error occurred while joining the team: {ex.Message}");
        }
        finally
        {
            IsJoiningTeam = false;
        }
    }

    public async Task ReloadTeamDataAsync()
    {
        if (string.IsNullOrWhiteSpace(_teamId) || string.IsNullOrWhiteSpace(_userId))
        {
            return;
        }

        try
        {
            Result<TeamDto> result = await teamService.GetTeamByIdAsync(_teamId, _userId, CancellationToken.None);

            if (result.Success)
            {
                Team = result.Data;
                if (_team?.OwnerId == _userId)
                {
                    await LoadPendingInvitesAsync();
                }
            }
            else
            {
                string errorMsg = result.ErrorMessages.Any()
                    ? string.Join(", ", result.ErrorMessages)
                    : "Failed to refresh team data.";
                await toastService.Error(errorMsg);
            }
        }
        catch (Exception ex)
        {
            await toastService.Error($"Error refreshing team data: {ex.Message}");
        }
    }

    private void SetErrorState(string errorMessage)
    {
        IsLoading = false;
        HasError = true;
        ErrorMessage = errorMessage;
        Team = null;
        PendingInvites = null;
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}



