using System;

namespace uclip
{
    public class CommandAttribute : Attribute
    {
        public string Name { get; }
        public string Description { get; set; }
        public bool IsDefault { get; set; }
        public CommandAttribute()
        {
        }

        public CommandAttribute(string name)
        {
            Name = name;
        }
    }

    public class OptionAttribute : Attribute
    {
        private readonly string[] options;
        public string Description { get; set; }
        public OptionAttribute(string option)
        {
            this.options = option.Split('|');
        }
    }
}