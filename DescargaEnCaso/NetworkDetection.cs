using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.Net;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;

namespace DescargaEnCaso
{
    public class NetworkDetection
    {
        private bool isOnline;
        private bool isMobile;
        private bool isWifi;

        public bool IsOnline => isOnline;
        public bool IsMobile => isMobile;
        public bool IsWifi => isWifi;

        public static NetworkDetection DetectNetwork(Context context)
        {
            NetworkDetection networkDetection = new NetworkDetection();
            ConnectivityManager connectivityManager = (ConnectivityManager)context.GetSystemService(Context.ConnectivityService);

            var networkCapabilities = connectivityManager.GetNetworkCapabilities(connectivityManager.ActiveNetwork);
            if (networkCapabilities.HasCapability(NetCapability.Internet))
            {
                networkDetection.isOnline = true;

                if (networkCapabilities.HasTransport(TransportType.Wifi))
                {
                    networkDetection.isWifi = true;
                }
                if (networkCapabilities.HasTransport(TransportType.Cellular))
                {
                    networkDetection.isMobile = true;
                }
            }

            return networkDetection;
        }
    }
}