﻿using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.Input;
using ReactiveUI;

namespace EltrovoUI.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private string? _inFolderPath;
    public string? InFolderPath
    {
        get
        {
            return _inFolderPath;
        }
        set
        {
            this.RaiseAndSetIfChanged(ref _inFolderPath, value);
            Enabled = InFolderPath is not null && OutFilePath is not null;
        }
    }

    private string? _outFilePath;
    public string? OutFilePath
    {
        get
        {
            return _outFilePath;
        }
        set
        {
            this.RaiseAndSetIfChanged(ref _outFilePath, value);
            Enabled = InFolderPath is not null && OutFilePath is not null;
        }
    }

    private bool _enabled;
    public bool Enabled
    {
        get => _enabled;
        set => this.RaiseAndSetIfChanged(ref _enabled, value);
    }

    [RelayCommand]
    private async Task SelectInputFolder(CancellationToken token)
    {
        var folder = await DoOpenFolderPickerAsync();

        InFolderPath = folder?.TryGetLocalPath();

        return;
    }

    [RelayCommand]
    private async Task SelectOutputFile(CancellationToken token)
    {
        var file = await DoSaveFilePickerAsync();

        OutFilePath = file?.TryGetLocalPath();

        return;
    }

    [RelayCommand]
    private async Task RunFolder(CancellationToken token)
    {
        if (InFolderPath is not null) {
            var fileset = new HashingOperations(InFolderPath);
            fileset.FindBinaryMatches();
            fileset.FindPerceptualMatches();
            fileset.SaveGraph(OutFilePath);
        }

        return;
    }

    private async Task<IStorageFolder?> DoOpenFolderPickerAsync()
    {
        // For learning purposes, we opted to directly get the reference
        // for StorageProvider APIs here inside the ViewModel. 

        // For your real-world apps, you should follow the MVVM principles
        // by making service classes and locating them with DI/IoC.

        // See IoCFileOps project for an example of how to accomplish this.
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.StorageProvider is not { } provider)
            throw new NullReferenceException("Missing StorageProvider instance.");

        var folders = await provider.OpenFolderPickerAsync(new FolderPickerOpenOptions()
        {
            Title = "Open Folder",
            AllowMultiple = false
        });

        return folders?.Count >= 1 ? folders[0] : null;
    }

    private async Task<IStorageFile?> DoSaveFilePickerAsync()
    {
        // For learning purposes, we opted to directly get the reference
        // for StorageProvider APIs here inside the ViewModel. 

        // For your real-world apps, you should follow the MVVM principles
        // by making service classes and locating them with DI/IoC.

        // See IoCFileOps project for an example of how to accomplish this.
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop ||
            desktop.MainWindow?.StorageProvider is not { } provider)
            throw new NullReferenceException("Missing StorageProvider instance.");

        var file = await provider.SaveFilePickerAsync(new FilePickerSaveOptions()
        {
            Title = "Output File"
        });

        return file;
    }
}
