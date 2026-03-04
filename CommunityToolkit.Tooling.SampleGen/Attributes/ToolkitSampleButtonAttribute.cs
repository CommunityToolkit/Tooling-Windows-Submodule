// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.Tooling.SampleGen.Attributes;

[AttributeUsage(AttributeTargets.Method, AllowMultiple = false)]
public class ToolkitSampleButtonAttribute : Attribute
{
    /// <summary>
    /// The title to display on the button.
    /// </summary>
    public string? Title { get; set; }

    /// <summary>
    /// The name of the method this attribute is attached to.
    /// Set during source generation.
    /// </summary>
    public string? MethodName { get; set; }
}
