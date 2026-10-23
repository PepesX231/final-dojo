using System.Collections.Generic;
using UnityEngine;

namespace MamasComing
{
    /// Tools you have to fetch (plunger, wrench). They return to their home spot when put back.
    public class ToolItem : MessyProp
    {
        public enum Kind { Plunger, Wrench }
        public Kind kind;
        public override bool CanSettle => false;
        public override bool IsProblem => false;
    }
}
