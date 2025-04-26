using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class NameAttribute : Attribute
{
    public string Name { get; }
    public NameAttribute(string name) => Name = name;
}