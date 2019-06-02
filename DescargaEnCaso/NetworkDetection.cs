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
        public bool IsMobile => IsMobile;
        public bool IsWifi => isWifi;

        public static NetworkDetection DetectNetwork(Context context)
        {
            NetworkDetection networkDetection = new NetworkDetection();
            ConnectivityManager connectivityManager = (ConnectivityManager)context.GetSystemService(Context.ConnectivityService);
            NetworkInfo info = connectivityManager.ActiveNetworkInfo;
            if (info != null)
            {
                networkDetection.isOnline = info.IsConnected;
                if (networkDetection.isOnline)
                {
                    if (Android.OS.Build.VERSION.SdkInt >= BuildVersionCodes.M)
                    {
                        var network = connectivityManager.ActiveNetwork;
                        var capabilities = connectivityManager.GetNetworkCapabilities(network);
                        if (capabilities != null)
                        {
                            networkDetection.isWifi = capabilities.HasTransport(TransportType.Wifi);
                            networkDetection.isMobile = capabilities.HasTransport(TransportType.Cellular);                            
                        }        
                    }
                    else
                    {
                        networkDetection.isMobile = info.Type == ConnectivityType.Mobile;
                        networkDetection.isWifi = info.Type == ConnectivityType.Wifi;                        
                    }
                }
                else
                {
                    networkDetection.isMobile = false;
                    networkDetection.isWifi = false;
                }
            }
            else
            {
                networkDetection.isOnline = false;
                networkDetection.isMobile = false;
                networkDetection.isWifi = false;
            }
            return networkDetection;
        }
    }
}