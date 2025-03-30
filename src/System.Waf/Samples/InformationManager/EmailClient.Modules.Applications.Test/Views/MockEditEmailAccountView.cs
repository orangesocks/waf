﻿using System.Waf.UnitTesting.Mocks;
using Waf.InformationManager.EmailClient.Modules.Applications.ViewModels;
using Waf.InformationManager.EmailClient.Modules.Applications.Views;

namespace Test.InformationManager.EmailClient.Modules.Applications.Views;

public class MockEditEmailAccountView : MockDialogView<MockEditEmailAccountView, EditEmailAccountViewModel>, IEditEmailAccountView
{
}
