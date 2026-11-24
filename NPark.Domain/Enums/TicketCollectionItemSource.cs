using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NPark.Domain.Enums
{
    public enum TicketCollectionItemSource
    {
        Ticket = 1,
        TicketArchive = 2,
        TicketExceedTransaction = 3
    }
}
namespace NPark.Domain.Enums
{
    public enum FinancialItemType { BaseTicketPayment = 1, ExceedPayment = 2 }
}
