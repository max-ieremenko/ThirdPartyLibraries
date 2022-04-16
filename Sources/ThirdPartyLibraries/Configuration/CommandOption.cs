﻿namespace ThirdPartyLibraries.Configuration
{
    public readonly struct CommandOption
    {
        public CommandOption(string name)
            : this(name, null)
        {
        }

        public CommandOption(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; }

        public string Value { get; }
    }
}