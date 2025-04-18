﻿using System.Net.Mime;
using System.Runtime.Serialization;
using System.Waf.Applications;
using System.Waf.Foundation;
using Waf.InformationManager.EmailClient.Modules.Applications.SampleData;
using Waf.InformationManager.EmailClient.Modules.Domain.AccountSettings;
using Waf.InformationManager.EmailClient.Modules.Domain.Emails;
using Waf.InformationManager.Infrastructure.Interfaces.Applications;

namespace Waf.InformationManager.EmailClient.Modules.Applications.Controllers;

/// <summary>Responsible for the whole module. This controller delegates the tasks to other controllers.</summary>
internal class ModuleController : IModuleController
{
    private const string documentPartPath = "EmailClient/Content.xml";

    private readonly IShellService shellService;
    private readonly IDocumentService documentService;
    private readonly INavigationService navigationService;
    private readonly EmailAccountsController emailAccountsController;
    private readonly Func<EmailFolderController> emailFolderControllerFactory;
    private readonly Func<NewEmailController> newEmailControllerFactory;
    private readonly DelegateCommand newEmailCommand;
    private readonly Lazy<DataContractSerializer> serializer;
    private readonly List<ItemCountSynchronizer> itemCountSynchronizers = [];
    private EmailFolderController? activeEmailFolderController;

    public ModuleController(IShellService shellService, IDocumentService documentService, INavigationService navigationService, EmailAccountsController emailAccountsController,
        Func<EmailFolderController> emailFolderControllerFactory, Func<NewEmailController> newEmailControllerFactory)
    {
        this.shellService = shellService;
        this.documentService = documentService;
        this.navigationService = navigationService;
        this.emailAccountsController = emailAccountsController;
        this.emailFolderControllerFactory = emailFolderControllerFactory;
        this.newEmailControllerFactory = newEmailControllerFactory;
        newEmailCommand = new(NewEmail);
        serializer = new(CreateDataContractSerializer);
    }

    internal EmailClientRoot Root { get; private set; } = null!;

    public void Initialize()
    {
        using (var stream = documentService.GetStream(documentPartPath, MediaTypeNames.Text.Xml, FileMode.Open))
        {
            if (stream.Length == 0)
            {
                Root = new();
                Root.AddEmailAccount(SampleDataProvider.CreateEmailAccount());
                foreach (var email in SampleDataProvider.CreateInboxEmails()) { Root.Inbox.AddEmail(email); }
                foreach (var email in SampleDataProvider.CreateSentEmails()) { Root.Sent.AddEmail(email); }
                foreach (var email in SampleDataProvider.CreateDrafts()) { Root.Drafts.AddEmail(email); }
            }
            else
            {
                Root = (EmailClientRoot)serializer.Value.ReadObject(stream)!;
            }
        }
        emailAccountsController.Root = Root;

        var node = navigationService.AddNavigationNode("InboxNode", "Inbox", ShowInbox, CloseCurrentView, 1, 1);
        itemCountSynchronizers.Add(new(node, Root.Inbox));
        node = navigationService.AddNavigationNode("OutboxNode", "Outbox", ShowOutbox, CloseCurrentView, 1, 2);
        itemCountSynchronizers.Add(new(node, Root.Outbox));
        node = navigationService.AddNavigationNode("SentNode", "Sent", ShowSentEmails, CloseCurrentView, 1, 3);
        itemCountSynchronizers.Add(new(node, Root.Sent));
        node = navigationService.AddNavigationNode("DraftsNode", "Drafts", ShowDrafts, CloseCurrentView, 1, 4);
        itemCountSynchronizers.Add(new(node, Root.Drafts));
        node = navigationService.AddNavigationNode("DeletedNode", "Deleted", ShowDeletedEmails, CloseCurrentView, 1, 5);
        itemCountSynchronizers.Add(new(node, Root.Deleted));
    }

    public void Run() { }

    public void Shutdown()
    {
        using var stream = documentService.GetStream(documentPartPath, MediaTypeNames.Text.Xml, FileMode.Create);
        serializer.Value.WriteObject(stream, Root);
    }

    private void ShowEmails(EmailFolder emailFolder)
    {
        activeEmailFolderController = emailFolderControllerFactory();
        activeEmailFolderController.EmailFolder = emailFolder;
        activeEmailFolderController.Initialize();
        activeEmailFolderController.Run();
        var uiNewEmailCommand = new ToolBarCommand("NewEmailCommand", newEmailCommand, "_New email", "Creates a new email.");
        var uiDeleteEmailCommand = new ToolBarCommand("DeleteEmailCommand", activeEmailFolderController.DeleteEmailCommand, "_Delete", "Deletes the selected email.");
        var uiEmailAccountsCommand = new ToolBarCommand("EmailAccountsCommand", emailAccountsController.EmailAccountsCommand, "_Email accounts", "Opens a window that shows the email accounts.");
        shellService.AddToolBarCommands([ uiNewEmailCommand, uiDeleteEmailCommand, uiEmailAccountsCommand ]);
    }

    private void ShowInbox() => ShowEmails(Root.Inbox);

    private void ShowOutbox() => ShowEmails(Root.Outbox);

    private void ShowSentEmails() => ShowEmails(Root.Sent);

    private void ShowDrafts() => ShowEmails(Root.Drafts);

    private void ShowDeletedEmails() => ShowEmails(Root.Deleted);

    private void CloseCurrentView()
    {
        shellService.ClearToolBarCommands();
        activeEmailFolderController?.Shutdown();
        activeEmailFolderController = null;
    }

    private void NewEmail()
    {
        var newEmailController = newEmailControllerFactory();
        newEmailController.Root = Root;
        newEmailController.Initialize();
        newEmailController.Run();
    }

    private DataContractSerializer CreateDataContractSerializer() => new(typeof(EmailClientRoot), [ typeof(ExchangeSettings), typeof(Pop3Settings) ]);


    private class ItemCountSynchronizer : Model
    {
        private readonly INavigationNode node;
        private readonly EmailFolder folder;

        public ItemCountSynchronizer(INavigationNode node, EmailFolder folder)
        {
            this.node = node;
            this.folder = folder;
            WeakEvent.CollectionChanged.Add(folder.Emails, EmailsCollectionChanged);
            UpdateItemCount();
        }

        private void EmailsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateItemCount();

        private void UpdateItemCount() => node.ItemCount = folder.Emails.Count;
    }
}
