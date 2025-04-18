﻿using System.Waf.Applications;
using System.Waf.Applications.Services;
using Waf.BookLibrary.Library.Applications.Properties;
using Waf.BookLibrary.Library.Applications.Services;
using Waf.BookLibrary.Library.Applications.ViewModels;

namespace Waf.BookLibrary.Library.Applications.Controllers;

/// <summary>Responsible for the module lifecycle.</summary>
internal class ModuleController : IModuleController
{
    private readonly IMessageService messageService;
    private readonly IEntityController entityController;
    private readonly BookController bookController;
    private readonly PersonController personController;
    private readonly ShellService shellService;
    private readonly Lazy<ShellViewModel> shellViewModel;
    private readonly DelegateCommand exitCommand;

    public ModuleController(IMessageService messageService, IEntityController entityController, BookController bookController, PersonController personController,
        ShellService shellService, Lazy<ShellViewModel> shellViewModel)
    {
        this.messageService = messageService;
        this.entityController = entityController;
        this.bookController = bookController;
        this.personController = personController;
        this.shellService = shellService;
        this.shellViewModel = shellViewModel;
        exitCommand = new(Close);
    }

    private ShellViewModel ShellViewModel => shellViewModel.Value;

    public void Initialize()
    {
        shellService.ShellView = ShellViewModel.View;
        ShellViewModel.ExitCommand = exitCommand;
        ShellViewModel.Closing += ShellViewModelClosing;
        entityController.Initialize();
        bookController.Initialize();
        personController.Initialize();
    }

    public void Run()
    {
        ShellViewModel.Show();
        bookController.Run();
        personController.Run();
    }

    public void Shutdown() => entityController.Shutdown();

    private void ShellViewModelClosing(object? sender, CancelEventArgs e)
    {
        if (!entityController.HasChanges()) return;
        if (entityController.CanSave())
        {
            bool? result = messageService.ShowQuestion(shellService.ShellView, Resources.SaveChangesQuestion);
            if (result == true)
            {
                if (!entityController.SaveCore().GetAwaiter().GetResult())
                {
                    messageService.ShowError(shellService.ShellView, Resources.SaveErrorDatabase);
                    e.Cancel = true;
                }
            }
            else if (result == null)
            {
                e.Cancel = true;
            }
        }
        else
        {
            e.Cancel = !messageService.ShowYesNoQuestion(shellService.ShellView, Resources.LoseChangesQuestion);
        }
    }

    private void Close() => ShellViewModel.Close();
}
