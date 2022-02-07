// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.Labs.Core.SourceGenerators.Attributes;
using System.ComponentModel;

namespace CommunityToolkit.Labs.Core.SourceGenerators.Metadata
{
    /// <summary>
    /// A metadata container for data defined in <see cref="ToolkitSampleMultiChoiceOptionAttribute"/> with INPC support.
    /// </summary>
    public class ToolkitSampleMultiChoiceOptionMetadataViewModel : IToolkitSampleOptionViewModel
    {
        private string? _title;
        private object? _value;

        /// <summary>
        /// Creates a new instance of <see cref="ToolkitSampleMultiChoiceOptionMetadataViewModel"/>.
        /// </summary>
        public ToolkitSampleMultiChoiceOptionMetadataViewModel(string name, MultiChoiceOption[] options, string? title = null)
        {
            Name = name;
            Options = options;
            _title = title;
            _value = options[0].Value;
        }

        /// <inheritdoc cref="INotifyPropertyChanged.PropertyChanged"/>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// A unique identifier for this option.
        /// </summary>
        /// <remarks>
        /// Used by the sample system to match up <see cref="ToolkitSampleMultiChoiceOptionMetadataViewModel"/> to the original <see cref="ToolkitSampleMultiChoiceOptionAttribute"/> and the control that declared it.
        /// </remarks>
        public string Name { get; }

        /// <summary>
        /// The available options presented to the user.
        /// </summary>
        public MultiChoiceOption[] Options { get; }

        /// <summary>
        /// The current boolean value.
        /// </summary>
        public object? Value
        {
            get => _value;
            set
            {
                if (value is MultiChoiceOption op)
                    _value = op.Value;
                else
                    _value = value;

                // Value is null when selection changes
                if (value is not null)
                    PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(Name));
            }
        }

        /// <summary>
        /// A label to display along the boolean option.
        /// </summary>
        public string? Title
        {
            get => _title;
            set
            {
                _title = value;
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Title)));
            }
        }
    }
}
