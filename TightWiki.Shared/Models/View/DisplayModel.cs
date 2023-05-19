using System.Collections.Generic;
using TightWiki.Shared.Models.Data;

namespace TightWiki.Shared.Models.View
{
    public class DisplayModel : ModelBase
    {
        public string Body { get; set; }
        public List<PageComment> Comments { get; set; }
    }
}

