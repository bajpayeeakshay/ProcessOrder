using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessOrder.Services.Services
{
    public interface IEmailNotifier
    {
        Task<bool> SendNotificationAsync(string toEmail, string subject, string body);
    }
}
