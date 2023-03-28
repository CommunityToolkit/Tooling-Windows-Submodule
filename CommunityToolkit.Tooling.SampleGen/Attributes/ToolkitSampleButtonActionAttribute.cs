// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Tooling.SampleGen.Attributes;

/// <summary>
/// Generates a command that invokes the decorated method, and a button in the sample option panel that invokes the command when clicked.
/// </summary>
/// <remarks>
/// Using this attribute will automatically generate an <see cref="INotifyPropertyChanged"/>-enabled property
/// that you can bind to in XAML, and displays an options pane alonside your sample which allows the user to manipulate the property.
/// </remarks>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public sealed class ToolkitSampleButtonActionAttribute : ToolkitSampleOptionBaseAttribute
{
    /// <summary>
    /// Creates a new instance of <see cref="ToolkitSampleButtonActionAttribute"/>.
    /// </summary>
    /// <param name="label">The text displayed inside the button.</param>
    public ToolkitSampleButtonActionAttribute(string label)
        : base(string.Empty, null)
    {
        Label = label;
    }

    public string Label { get; }

    /// <summary>
    /// The source generator-friendly type name used for casting.
    /// </summary>
    internal override string TypeName { get; } = "System.Windows.Input.ICommand";
}
