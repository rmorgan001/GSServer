﻿using System;
using System.Collections.Generic;
using System.Text;

namespace GS.Server.Alignment
{
    public class MapResult
    {
        public CartesCoord Position { get; set;} 

        public bool InTriangle { get;set;} = false;
    }
}
