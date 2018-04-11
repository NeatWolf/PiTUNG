﻿using System;

namespace PiTung
{
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    public class TargetAttribute : Attribute
    {
        /// <summary>
        /// Marks this class as a patch class. 
        /// <para />
        /// Patch classes may contain static methods in order to patch existing
        /// ones. If no patch type (prefix or postfix) is specified on any method, prefix will selected by default.
        /// </summary>
        /// <param name="containerType">The type that contains the methods we want to patch.</param>
        public TargetAttribute(Type containerType)
        {
            this.ContainerType = containerType;
        }

        public Type ContainerType { get; }
    }
}
