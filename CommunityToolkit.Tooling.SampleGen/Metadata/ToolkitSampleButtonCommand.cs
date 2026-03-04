// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Windows.Input;

namespace CommunityToolkit.Tooling.SampleGen.Metadata;

/// <summary>
/// Represents a sample button that can invoke a method on a sample instance.
/// Contains the display title and method name, and implements <see cref="ICommand"/> for XAML binding.
/// </summary>
public class ToolkitSampleButtonCommand : ICommand
{
    private Action? _callback;

    /// <summary>
    /// Creates a new instance of <see cref="ToolkitSampleButtonCommand"/>.
    /// </summary>
    /// <param name="title">The title to display on the button.</param>
    /// <param name="methodName">The name of the method to invoke on the sample instance.</param>
    public ToolkitSampleButtonCommand(string title, string methodName)
    {
        Title = title;
        MethodName = methodName;
    }

    /// <summary>
    /// The title to display on the button.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// The name of the method to invoke on the sample instance.
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// Binds this command to a method on the specified sample instance using reflection.
    /// </summary>
    /// <param name="instance">The sample control instance containing the target method.</param>
    public void BindToInstance(object instance)
    {
        var method = instance.GetType().GetMethod(MethodName,
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
        if (method != null)
        {
            _callback = () => method.Invoke(instance, null);
        }

        CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <inheritdoc />
#pragma warning disable CS0067
    public event EventHandler? CanExecuteChanged;
#pragma warning restore CS0067

    /// <inheritdoc />
    public bool CanExecute(object parameter)
    {
        return _callback != null;
    }

    /// <inheritdoc />
    public void Execute(object parameter)
    {
        _callback?.Invoke();
    }
}
