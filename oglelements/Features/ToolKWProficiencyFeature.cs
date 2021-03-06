﻿using OGL.Keywords;
using System.Collections.Generic;

namespace OGL.Features
{
    public class ToolKWProficiencyFeature: Feature
    {
        /*[XmlArrayItem(Type = typeof(Keyword)),
        XmlArrayItem(Type = typeof(Versatile)),
        XmlArrayItem(Type = typeof(Range))]
        public List<Keyword> Keywords; //AND connection*/
        public string Condition { get; set; }
        public string Description { get; set; }
        public ToolKWProficiencyFeature()
            : base()
        {
            Keywords = new List<Keyword>();
        }
        public ToolKWProficiencyFeature(string name, string text, string condition, string description, int level = 1, bool hidden = true)
            : base(name, text, level, hidden)
        {
            Condition = condition;
            Description = description;
        }
        public override string Displayname()
        {
            return "Tool Proficiency by Expression Feature";
        }
    }
}
