using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Macroc
{
    internal enum ExitCode
    {
        Success = 0x00,
        ArgError,
        NoInputFile,
        LexerError,
        ParserError,
    }
}
