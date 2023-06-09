// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace ProjectTemplateExperiment.Samples;

[ToolkitSampleBoolOption("IsTextVisible", true, Title = "IsVisible")]
// Single values without a colon are used for both label and value.
// To provide a different label for the value, separate with a colon surrounded by a single space on both sides ("label : value").
[ToolkitSampleNumericOption("TextSize", 12, 8, 48, 2, true, Title = "FontSize")]
[ToolkitSampleMultiChoiceOption("TextFontFamily", "Segoe UI", "Arial", "Consolas", Title = "Font family")]
[ToolkitSampleMultiChoiceOption("TextForeground", "Teal : #0ddc8c", "Sand : #e7a676", "Dull green : #5d7577", Title = "Text foreground")]

[ToolkitSample(id: nameof(ProjectTemplateTemplatedStyleCustomSample), "Templated control (restyled)", description: "A sample for showing how to create a use and templated control with a custom style.")]
public sealed partial class ProjectTemplateTemplatedStyleCustomSample : Page
{
    public ProjectTemplateTemplatedStyleCustomSample()
    {
        this.InitializeComponent();
    }
}
