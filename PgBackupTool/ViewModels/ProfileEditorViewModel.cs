using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PgBackupTool.Models;

namespace PgBackupTool.ViewModels;

public partial class ProfileEditorViewModel : ObservableObject
{
    [ObservableProperty] private string _name = "";
    [ObservableProperty] private string _host = "localhost";
    [ObservableProperty] private int _port = 5432;
    [ObservableProperty] private string _database = "";
    [ObservableProperty] private string _username = "postgres";
    [ObservableProperty] private string _password = "";
    [ObservableProperty] private string _validationError = "";

    public bool Saved { get; private set; }

    public Action? CloseAction { get; set; }

    public ProfileEditorViewModel() { }

    public ProfileEditorViewModel(ConnectionProfile profile)
    {
        _name = profile.Name;
        _host = profile.Host;
        _port = profile.Port;
        _database = profile.Database;
        _username = profile.Username;
        _password = profile.Password;
    }

    [RelayCommand]
    private void Save()
    {
        if (!Validate()) return;
        Saved = true;
        CloseAction?.Invoke();
    }

    [RelayCommand]
    private void Cancel() => CloseAction?.Invoke();

    public ConnectionProfile BuildProfile(Guid? existingId = null) => new()
    {
        Id = existingId ?? Guid.NewGuid(),
        Name = Name.Trim(),
        Host = Host.Trim(),
        Port = Port,
        Database = Database.Trim(),
        Username = Username.Trim(),
        Password = Password,
    };

    private bool Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            ValidationError = "Name is required.";
            return false;
        }
        if (string.IsNullOrWhiteSpace(Database))
        {
            ValidationError = "Database is required.";
            return false;
        }
        if (Port is < 1 or > 65535)
        {
            ValidationError = "Port must be between 1 and 65535.";
            return false;
        }
        ValidationError = "";
        return true;
    }
}
