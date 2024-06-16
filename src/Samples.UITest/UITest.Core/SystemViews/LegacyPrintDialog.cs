﻿using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;

namespace UITest.SystemViews;

public class LegacyPrintDialog(FrameworkAutomationElementBase element) : Window(element)
{
    public Button PrintButton => this.Find(x => x.ByControlType(ControlType.Button).And(x.ByAutomationId("1"))).AsButton();

    public Button CancelButton => this.Find(x => x.ByControlType(ControlType.Button).And(x.ByAutomationId("2"))).AsButton();
}
