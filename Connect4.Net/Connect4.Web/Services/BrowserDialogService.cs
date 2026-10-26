using Microsoft.JSInterop;
using Connect4.App.Models;
using Connect4.App.Services;
using Connect4.App.ViewModels;
using Connect4.Web.Components;
using GameShell;

namespace Connect4.Web.Services;

/// <summary>The game's dialogs, shown by GameShell's DialogHost through ShellDialogs.</summary>
public sealed class BrowserDialogService(IJSInProcessRuntime js, ShellDialogs dialogs) : IDialogService
{
    private const string Caption = "Connect 4";

    public async Task<GameSettings?> EditSettingsAsync(GameSettings current)
    {
        var settings = new SettingsViewModel(current);
        int button = await ShowAsync(new DialogRequest { Title = "Settings", Body = SettingsForm.For(settings), Buttons = ["OK", "Cancel"] });
        return button == 0 ? settings.ToSettings() : null;
    }

    public async Task<bool> ConfirmAsync(string title, string message)
    {
        int button = await ShowAsync(new DialogRequest { Title = title, Message = message, Buttons = ["Yes", "No"] });
        return button == 0;
    }

    public void ShowError(string message) =>
        _ = ShowAsync(new DialogRequest { Title = Caption, Message = message, Buttons = ["OK"] });

    public void ShowAbout() => _ = ShowAsync(new DialogRequest
    {
        Title = AboutInfo.Title,
        Heading = AboutInfo.Version,
        Paragraphs = AboutInfo.Paragraphs,
        Picture = "images/claus-pedersen.jpg",
        PictureCaption = AboutInfo.PictureCaption,
        Buttons = ["OK"],
    });

    public void Beep() => js.InvokeVoid("connect4.beep");

    /// <returns>The text the user entered, or null if the user cancelled.</returns>
    public async Task<string?> AskTextAsync(string message, string text)
    {
        var request = new DialogRequest { Title = Caption, Message = message, Text = text, Buttons = ["OK", "Cancel"] };
        return await ShowAsync(request) == 0 ? request.Text : null;
    }

    private Task<int> ShowAsync(DialogRequest request) => dialogs.ShowAsync(request);
}
