#region Copyright

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#endregion

namespace CommunityToolkit.Tooling.SampleGen.Attributes;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
public sealed class ToolkitSampleEnumOptionAttribute<TEnum> : ToolkitSampleOptionBaseAttribute where TEnum : struct, Enum
{
    /// <summary>
    /// Creates a new instance of <see cref="ToolkitSampleEnumOptionAttribute{TEnum}"/>.
    /// </summary>
    /// <param name="bindingName">The name of the generated property, which you can bind to in XAML.</param>
    public ToolkitSampleEnumOptionAttribute(string bindingName)
        : base(bindingName, null)
    {
    }

    /// <summary>
    /// The source generator-friendly type name used for casting.
    /// </summary>
    internal override string TypeName { get; } = "int";
}
