﻿// <auto-generated> This file has been auto generated by EF Core Power Tools. </auto-generated>
#nullable disable
using System;
using System.Collections.Generic;

namespace botForTRPO.Models;

public partial class CatchedSignal
{
    public long ID { get; set; }

    public string SignalID { get; set; }

    public string AudioPath { get; set; }

    public virtual Signal Signal { get; set; }
}