// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Tooling.SampleGen.Attributes;

/// <summary>
/// Assembly-level attribute emitted by the source generator in sample projects to carry
/// <see cref="ToolkitSampleButtonAttribute"/> metadata across the compilation boundary.
/// Private method attributes are not visible from PE metadata references, so the generator
/// encodes button data as assembly-level attributes that the head project can read.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public class ToolkitSampleButtonDataAttribute : Attribute
{
    /// <summary>
    /// The fully qualified type name of the sample class that contains the button method.
    /// </summary>
    public string SampleTypeName { get; }

    /// <summary>
    /// The name of the method to invoke on the sample instance.
    /// </summary>
    public string MethodName { get; }

    /// <summary>
    /// The title to display on the button.
    /// </summary>
    public string Title { get; }

    /// <summary>
    /// Creates a new instance of <see cref="ToolkitSampleButtonDataAttribute"/>.
    /// </summary>
    /// <param name="sampleTypeName">The fully qualified type name of the sample class.</param>
    /// <param name="methodName">The name of the method to invoke.</param>
    /// <param name="title">The title to display on the button.</param>
    public ToolkitSampleButtonDataAttribute(string sampleTypeName, string methodName, string title)
    {
        SampleTypeName = sampleTypeName;
        MethodName = methodName;
        Title = title;
    }
}
