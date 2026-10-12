using System.Windows;
using Titanium.Domain.Entities;

namespace Titanium.Desktop.Views;

public partial class WorkspaceSettingsDialog : Window
{
    public Workspace Workspace { get; }
    public bool IsDeleted { get; private set; }

    public WorkspaceSettingsDialog(Workspace workspace)
    {
        InitializeComponent();
        Workspace = workspace;

        WorkspaceNameTextBox.Text = workspace.Name ?? string.Empty;
        WorkspaceDescriptionTextBox.Text = workspace.Description ?? string.Empty;

        var folders = workspace.TargetFolders ?? new List<string>();
        TargetFoldersTextBox.Text = folders.Count > 0 ? string.Join(", ", folders) : "Inbox";
    }

    private void DeleteButton_Click(object sender, RoutedEventArgs e)
    {
        var confirm = MessageBox.Show(
            $"Are you sure you want to delete workspace '{Workspace.Name}'?\n\nThis will permanently delete this workspace and all its associated communications, claims, and matters.",
            "Confirm Workspace Deletion",
            MessageBoxButton.YesNo,
            MessageBoxImage.Warning);

        if (confirm == MessageBoxResult.Yes)
        {
            IsDeleted = true;
            DialogResult = true;
            Close();
        }
    }

    private void SaveButton_Click(object sender, RoutedEventArgs e)
    {
        Workspace.Name = WorkspaceNameTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(Workspace.Name))
        {
            Workspace.Name = "Workspace";
        }

        Workspace.Description = WorkspaceDescriptionTextBox.Text.Trim();

        var list = TargetFoldersTextBox.Text
            .Split(new[] { ',', ';', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .Where(f => !string.IsNullOrEmpty(f))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (list.Count == 0)
        {
            list = new List<string> { "Inbox" };
        }

        Workspace.TargetFolders = list;

        DialogResult = true;
        Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }
}
