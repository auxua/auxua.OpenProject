using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;

namespace auxua.OpenProject.Authentication
{
    public interface IAuthProvider
    {
        void Apply(HttpRequestMessage request);
    }
}
