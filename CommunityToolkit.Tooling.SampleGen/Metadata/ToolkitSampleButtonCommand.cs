// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Input;

namespace CommunityToolkit.Tooling.SampleGen.Metadata;

/// <summary>
/// A command that invokes the provided <see cref="Action"/> when executed.
/// </summary>
public class ToolkitSampleButtonCommand : ICommand
{
    private readonly Action _callback;

    public ToolkitSampleButtonCommand(Action callback)
    {
        _callback = callback;
    }

    /// <inheritdoc />
#pragma warning disable CS0067
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067

    /// <inheritdoc />
    public bool CanExecute(object parameter)
    {
        return true;
    }
    /// <inheritdoc />
    public void Execute(object parameter)
    {
        _callback();
    }
}
