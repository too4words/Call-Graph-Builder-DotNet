﻿// ----------------------------------------------------------------------------------
//
// Copyright Microsoft Corporation
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
// http://www.apache.org/licenses/LICENSE-2.0
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// ----------------------------------------------------------------------------------

namespace Microsoft.Azure.Commands.Network.Models
{
    using System.Collections.Generic;

    using Newtonsoft.Json;

    public class PSVirtualNetwork : PSTopLevelResource
    {
        public PSAddressSpace AddressSpace { get; set; }

        public PSDhcpOptions DhcpOptions { get; set; }

        public List<PSResourceId> NetworkInterfaces { get; set; }

        public List<PSSubnet> Subnets { get; set; }

        public string ProvisioningState { get; set; }

        [JsonIgnore]
        public string AddressSpaceText
        {
            get { return JsonConvert.SerializeObject(AddressSpace, Formatting.Indented); }
        }

        [JsonIgnore]
        public string DhcpOptionsText
        {
            get { return JsonConvert.SerializeObject(DhcpOptions, Formatting.Indented); }
        }

        [JsonIgnore]
        public string NetworkInterfacesText
        {
            get { return JsonConvert.SerializeObject(NetworkInterfaces, Formatting.Indented); }
        }

        [JsonIgnore]
        public string SubnetsText
        {
            get { return JsonConvert.SerializeObject(Subnets, Formatting.Indented); }
        }
    }
}
