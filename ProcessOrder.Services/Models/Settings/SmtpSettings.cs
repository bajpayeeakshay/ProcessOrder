using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessOrder.Services.Models.Settings;

public class SmtpSettings
{
    public string SMTPServer { get; set; }
    public string SMTPUserName { get; set; }
    public string SMTPPassword { get; set; }
    public string SMTPEmail { get; set; }

}
