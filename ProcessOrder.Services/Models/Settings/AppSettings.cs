using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessOrder.Services.Models.Settings
{
    public class AppSettings
    {
        public string FilePath { get; set; }

        public string OrderManagementSystemUrl { get; set; }

        public string AccountManagerEmail { get; set; }

        public string XmlFilePath { get; set; }
    }
}
