﻿using System;

namespace PiTung_Bootstrap
{
    public abstract class Patch
    {
        public Patch()
        {
            if (this.GetType().GetCustomAttributes(typeof(TargetAttribute), false).Length == 0)
            {
                throw new Exception($"Patch class {this.GetType().Name} must have a TargetAttribute.");
            }
        }


    }
}