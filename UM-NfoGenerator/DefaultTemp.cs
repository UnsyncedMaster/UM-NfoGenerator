using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UM_NfoGenerator
{
    internal class DefaultTemp
    {
        public static string DefaultTemplate()
        {
            return @"───────────────────────────────
Album:   {ALBUM}
Artist:  {ARTIST}
Year:    {YEAR}
Genre:   {GENRE}
───────────────────────────────

Tracklist (Duration):
{TRACKLIST}

Files:
{FILELIST}

-------MD5 Checksums-------
// Not Implemented Yet!!!!!!
───────────────────────────────
Ripped by: {RIPPER}
Encoded by: {ENCODER}
Date: {DATE}
───────────────────────────────
Comment:
{COMMENT}
";
        }
    }
}
