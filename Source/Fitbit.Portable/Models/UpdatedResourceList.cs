using Fitbit.Models;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace Fitbit.Api.Portable.Models
{
    [XmlRoot("updates")]
    public class UpdatedResourceList
    {
        public UpdatedResourceList() { Resources = new List<UpdatedResource>(); }

        [XmlElement("updatedResource")]
        public List<UpdatedResource> Resources { get; set; }
    }
}
