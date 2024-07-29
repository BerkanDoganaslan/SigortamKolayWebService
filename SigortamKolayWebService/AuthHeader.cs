using System.Web.Services.Protocols;

namespace SigortamKolayWebService
{
    public class AuthHeader : SoapHeader
    {
        public string Username;
        public string Password;
    }
}