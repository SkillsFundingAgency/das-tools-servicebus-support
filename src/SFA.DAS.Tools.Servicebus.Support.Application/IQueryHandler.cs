using System;
using System.Collections.Generic;
using System.Runtime;
using System.Text;
using System.Threading.Tasks;

namespace SFA.DAS.Tools.Servicebus.Support.Application
{
    public interface IQueryHandler<in TIn, TOut>
    {
        Task<TOut> Handle(TIn query);
    }
}
