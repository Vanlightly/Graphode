using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace WhaleSightingsInc.WebService
{
    [ServiceContract]
    public interface IWhaleSightingsService
    {
        [OperationContract]
        List<WhaleSightingDTO> GetWhales();

        [OperationContract]
        void AddWhale(WhaleSightingDTO whale);

        [OperationContract]
        void UpdateWhale(WhaleSightingDTO whale);
    }
}
