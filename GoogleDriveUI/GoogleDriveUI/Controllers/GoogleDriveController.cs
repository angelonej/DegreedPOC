using GoogleDriveUI.Repositories;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace GoogleDriveUI.Controllers
{
    public class GoogleDriveController : ApiController
    {
        private GoogleDriveRepository _googleRepository;

        public GoogleDriveController()
        {
            // this needs to be read from DB
            GoogleDriveConfig _config = new GoogleDriveConfig(0, "", "Drive API Quickstart", "", "ya29.CjLTAp-1DkwPjYzeiCeZ6cVM2GHenDs4eQ1Wd9VF3UigCqXa_6NMZmHtkApdQTDgQZIomA", "1/7W0urTR9r_-6Y-qWzFkZa4t4yE9CDyTV_UmvojjnWLEMEudVrK5jSpoR30zcRFq6", "515653313915-27ogfmf6ddhvko2bt7mblbhhac30bapi.apps.googleusercontent.com", "b1XDQC-9fbxFvYcU3Za7eeTA", "angelonej@gmail.com", null);
            _googleRepository = new GoogleDriveRepository(_config);
        }

        // GET: api/GoogleDrive
        public IEnumerable<GoogleDriveItem> Get()
        {
            var rc = _googleRepository.GetHeirarchy();
            return rc;
        }

        // GET: api/GoogleDrive/5
        public IEnumerable<GoogleDriveItem> Get(string id)
        {
            var rc = _googleRepository.GetHeirarchy();
            return rc;
        }

        // POST: api/GoogleDrive
        public void Post([FromBody]string value)
        {
        }

        // PUT: api/GoogleDrive/5
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE: api/GoogleDrive/5
        public void Delete(int id)
        {
        }
    }
}
