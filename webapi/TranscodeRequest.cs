using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Transcoder.WebApi
{
    public class TranscodeRequest
    {
        public required string FilePath { get; set; }
        public required string TranscodedDirectory { get; set; }
        public required string Definition { get; set; }
    }
}