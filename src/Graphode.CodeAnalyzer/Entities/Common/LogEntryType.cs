﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Graphode.CodeAnalyzer.Entities.Common
{
    public enum LogEntryType
    {
        NotDefined,
        Info,
        BacktrackingFailed,
        SecondDatabase,
        NoDatabase,
        ParserFailure_UnsupportedIlInstruction
    }
}
