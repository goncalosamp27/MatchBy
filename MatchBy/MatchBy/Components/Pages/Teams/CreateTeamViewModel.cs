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

    private bool _isLoadingMembers;
    private string _memberSearch = string.Empty;
    private int _currentMemberPage = 1;
    private IBrowserFile? _selectedImage;
    private string? _userId;
    private const long MaxPreviewBytes = 5 * 1024 * 1024;

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
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(AvailableUsers));
        }
    } = new();
    
    public List<ApplicationUser> SelectedUsers
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged(nameof(SelectedUsers));
        }
    } = new();

    public IBrowserFile? SelectedImage
    {
        get => _selectedImage;
        set
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
        }
    }

    public string? UserId
    {
        get => _userId;
        set
        {
            _userId = value;
            if(_userId != null)
            {
                var members = new List<string>(Model.MembersIds);
                if (!members.Contains(_userId))
                {
                    members.Add(_userId);
                }
                Model = Model with { MembersIds = members};
                OnPropertyChanged(nameof(Model));
            }
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
        
        SelectedUsers = AvailableUsers.Data
            .Where(u => _model.MembersIds.Contains(u.Id))
            .ToList();

        OnPropertyChanged(nameof(Model));
    }

    public void RemoveMember(string userId)
    {
        if (!_model.MembersIds.Remove(userId))
        {
            return;
        }

        SelectedUsers = AvailableUsers.Data
            .Where(u => _model.MembersIds.Contains(u.Id))
            .ToList();
        
        OnPropertyChanged(nameof(Model));
    }

    public async Task OnImageSelectedAsync(InputFileChangeEventArgs e)
    {
        SelectedImage = e.File;
        await using Stream readStream = e.File.OpenReadStream(MaxPreviewBytes);
        await using var memory = new MemoryStream();
        await readStream.CopyToAsync(memory);
        string base64 = Convert.ToBase64String(memory.ToArray());
        SelectedImagePreviewUrl = $"data:{e.File.ContentType};base64,{base64}";
    }

    public void RemoveImage()
    {
        SelectedImage = null;
        SelectedImagePreviewUrl = null;
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
            var membersToInvite = new List<string>(_model.MembersIds)
            {
                _userId!
            };
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

