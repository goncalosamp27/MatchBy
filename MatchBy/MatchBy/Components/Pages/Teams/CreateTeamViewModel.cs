using System.ComponentModel;
using Blazorise;
using FluentValidation;
using MatchBy.DTOs.Team;
using MatchBy.Models;
using MatchBy.Services.Teams;
using MatchBy.Services.Users;
using Microsoft.AspNetCore.Components.Forms;
using Microsoft.AspNetCore.Components.Web;

namespace MatchBy.Components.Pages.Teams;

public sealed class CreateTeamViewModel(
    ITeamService teamService,
    IUsersService usersService,
    IValidator<CreateTeamDto> validator,
    IToastService toastService)
    : INotifyPropertyChanged
{
    private CreateTeamDto _model = new()
    {
        Name = string.Empty,
        Description = string.Empty,
        OwnerId = string.Empty,
        Privacy = TeamPrivacy.Public,
        MembersIds = [],
        MaxMembers = 10
    };

    private bool _isLoading = true;
    private bool _isSubmitting;
    private bool _isLoadingMembers;
    private string _memberSearch = string.Empty;
    private int _currentMemberPage = 1;
    private PaginationResponse<List<ApplicationUser>> _availableUsers = new();
    private IBrowserFile? _selectedImage;
    private string? _userId;

    public CreateTeamDto Model
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
        get => _isLoading;
        set
        {
            _isLoading = value;
            OnPropertyChanged(nameof(IsLoading));
        }
    }

    public bool IsSubmitting
    {
        get => _isSubmitting;
        set
        {
            _isSubmitting = value;
            OnPropertyChanged(nameof(IsSubmitting));
        }
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

    public string MemberSearch
    {
        get => _memberSearch;
        set
        {
            _memberSearch = value;
            OnPropertyChanged(nameof(MemberSearch));
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

    public PaginationResponse<List<ApplicationUser>> AvailableUsers
    {
        get => _availableUsers;
        set
        {
            _availableUsers = value;
            OnPropertyChanged(nameof(AvailableUsers));
        }
    }

    public IBrowserFile? SelectedImage
    {
        get => _selectedImage;
        set
        {
            _selectedImage = value;
            OnPropertyChanged(nameof(SelectedImage));
        }
    }

    public string? UserId
    {
        get => _userId;
        set
        {
            _userId = value;
            OnPropertyChanged(nameof(UserId));
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public ValidationStatus ValidateProperty(string propertyPath)
    {
        var errors = validator.Validate(_model).Errors
            .Where(x => x.PropertyName == propertyPath)
            .ToList();

        return errors.Any() ? ValidationStatus.Error : ValidationStatus.Success;
    }

    public string? GetValidationError(string propertyPath)
    {
        var errors = validator.Validate(_model).Errors
            .Where(x => x.PropertyName == propertyPath)
            .ToList();

        return errors.Any() ? errors[0].ErrorMessage : null;
    }

    public void UpdateName(string name)
    {
        Model = Model with { Name = name };
    }

    public void UpdateDescription(string description)
    {
        Model = Model with { Description = description };
    }

    public void UpdatePrivacy(TeamPrivacy privacy)
    {
        Model = Model with { Privacy = privacy };
    }

    public void ToggleMember(string userId)
    {
        if (!_model.MembersIds.Remove(userId))
        {
            _model.MembersIds.Add(userId);
        }

        OnPropertyChanged(nameof(Model));
    }

    public void RemoveMember(string userId)
    {
        _model.MembersIds.Remove(userId);
        OnPropertyChanged(nameof(Model));
    }

    public void OnImageSelected(InputFileChangeEventArgs e)
    {
        SelectedImage = e.File;
    }

    public void RemoveImage()
    {
        SelectedImage = null;
    }

    public async Task LoadMembersAsync()
    {
        if (_isLoadingMembers)
        {
            return;
        }

        IsLoadingMembers = true;
        try
        {
            Result<PaginationResponse<List<ApplicationUser>>> response = await usersService.GetUsers(_memberSearch, _currentMemberPage, 10);
            if (response.Success)
            {
                PaginationResponse<List<ApplicationUser>>? users = response.Data!;
                // Filter out current user
                if (_userId != null)
                {
                    users.Data = users.Data.Where(u => u.Id != _userId).ToList();
                }
                AvailableUsers = users;
            }
            else
            {
                await toastService.Error(response.ErrorMessages.ToString());
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

    public async Task<bool> SubmitTeamAsync()
    {
        IsSubmitting = true;
        try
        {
            var membersToInvite = new List<string>(_model.MembersIds) { _userId! };
            CreateTeamDto createDto = _model with
            {
                OwnerId = _userId!,
                MembersIds = membersToInvite.Distinct().ToList(),
                File = _selectedImage
            };

            Result<TeamDto> result = await teamService.CreateTeamAsync(createDto, CancellationToken.None);

            if (!result.Success)
            {
                await toastService.Error(result.ErrorMessages.Any() ? result.ErrorMessages[0] : "Error creating team.");
                return false;
            }

            // Invites are sent automatically in CreateTeamAsync
            if (_model.MembersIds.Count > 0)
            {
                await toastService.Success($"Team created successfully! Invites sent to {_model.MembersIds.Count} user(s).", "Create Team");
            }
            else
            {
                await toastService.Success("Team created successfully!", "Create Team");
            }

            return true;
        }
        finally
        {
            IsSubmitting = false;
        }
    }
}

