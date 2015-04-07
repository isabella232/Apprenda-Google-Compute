using System;
using System.Linq;
using System.Collections.Generic;

namespace Apprenda.SaaSGrid.Addons.Google.Compute
{
    internal class GoogleCloudDeveloperOptions
    {
        internal string ProjectId { get; private set; }
        internal string Zone { get; set; }
        internal string InstanceName { get; set; }
        internal string DiskType { get; set; }
        internal string SourceImage { get; set; }
        internal string MachineType { get; set; }

        // parses the developer options into a usable model - these are the ones that come from the web form.
        internal static GoogleCloudDeveloperOptions Parse(IEnumerable<AddonParameter> parameters, AddonManifest manifest)
        {
            var options = new GoogleCloudDeveloperOptions();
            setDefaults(options, manifest);
            foreach (var parameter in parameters)
            {
                MapToOption(options, parameter.Key.ToLowerInvariant(), parameter.Value);
            }
            return options;
        }

        private static void setDefaults(GoogleCloudDeveloperOptions options, AddonManifest manifest)
        {
            //set default values from manifest
            var manifestprops = manifest.GetProperties().ToDictionary(x => x.Key, x => x.Value);
            options.InstanceName = manifestprops["DefaultInstanceName"];
            options.Zone = manifestprops["DefaultZone"];
            options.SourceImage = manifestprops["DefaultSourceImage"];
            options.DiskType = manifestprops["DefaultDiskType"];
            options.MachineType = manifestprops["DefaultMachineType"];
        }

        private static void MapToOption(GoogleCloudDeveloperOptions options, string key, string value)
        {
            if (key.Equals("projectid"))
            {
                options.ProjectId = value;
                return;
            }
            if (key.Equals("instancename"))
            {
                options.InstanceName = value;
                return;
            }
            if(key.Equals("zone"))
            {
                options.Zone = value;
                return;
            }
            if(key.Equals("sourceimage"))
            {
                options.SourceImage = value;
                return;
            }
            if (key.Equals("disktype"))
            {
                options.DiskType = value;
                return;
            }
            if (key.Equals("machinetype"))
            {
                options.MachineType = value;
                return;
            }
            throw new ArgumentException(string.Format("Developer parameter {0} is either not readable, or not supported at this time.", key));
        }
    }
}
