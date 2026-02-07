using System;
using System.Collections.Generic;
using System.Text;

namespace auxua.OpenProject.WriteModel
{
    public interface IChangeSet
    {
        // build JSON for API (inkl. _links etc.)
        Newtonsoft.Json.Linq.JObject ToPayload();

    }
}
