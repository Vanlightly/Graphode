using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;

namespace WhaleRidesInc.WebService
{
    [ServiceContract]
    public interface IWhalesService
    {
        [OperationContract]
        List<WhaleDTO> GetWhales();

        [OperationContract]
        void AddWhale(WhaleDTO whale);

        [OperationContract]
        void UpdateWhale(WhaleDTO whale);
    }
}
