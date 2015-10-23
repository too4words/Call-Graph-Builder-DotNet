﻿// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using System;
using System.Diagnostics;
using System.Linq;
using System.Xml.Linq;

namespace Microsoft.DotNet.Build.Tasks
{
    public class GetNextRevisionNumber : Task
    {
        [Required]
        public string VersionPropsFile { get; set; }

        [Output]
        public string RevisionNumber { get; set; }

        public override bool Execute()
        {
            Log.LogMessage("Starting GetNextRevisionNumber");

            XDocument versionProps = XDocument.Load(VersionPropsFile);
            var defaultNS = versionProps.Root.GetDefaultNamespace();

            var buildNode =
                (from el in versionProps.Descendants(defaultNS + "RevisionNumber")
                 select el).FirstOrDefault();

            Debug.Assert(buildNode != null, "buildNode shouldn't be null");
            var incRevNum = int.Parse(buildNode.Value) + 1;

            // the value of RevisionNumber must be numeric and contain enough
            // leading zeroes to satisfy a 16-bit integer in base-10 format.  this
            // is needed because SemVer v1 performs a lexical comparison of the
            // prerelease version number and without the leading zeroes foo-20 is
            // smaller than foo-4.
            RevisionNumber = incRevNum.ToString("D5");

            buildNode.Value = RevisionNumber;
            versionProps.Save(VersionPropsFile);

            Log.LogMessage(string.Format("GetNextRevisionNumber completed successfully - the revision number is now {0}", RevisionNumber));

            return true;
        }
    }
}
