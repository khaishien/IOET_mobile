using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.UI.Xaml.Data;

namespace CaregiverMobile
{
    class Common
    {
        ApplicationDataContainer settings = ApplicationData.Current.LocalSettings;


        public string getIP()
        {
            Object ip = settings.Values["ip"];
            if(ip != null)
            {
                return ip.ToString();
            }
            else
            {
                settings.Values["ip"] = "http://localhost:3000/";
                return settings.Values["ip"].ToString();
            }
        }

        public void saveIP(String ip)
        {
            settings.Values["ip"] = ip;
        }

    }
}
