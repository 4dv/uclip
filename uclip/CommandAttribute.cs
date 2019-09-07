using System;

namespace uclip
{
    public class CommandAttribute : Attribute
    {
        public string Description { get; set; }
        public bool DefaultCommand { get; set; }
        public CommandAttribute()
        {
        }

        public CommandAttribute(string name, params string[] argNames)
        {
        }
    }
}