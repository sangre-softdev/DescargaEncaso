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
        private bool isRoaming;

        public bool IsOnline { get { return isOnline; } }
        public bool IsMobile { get { return IsMobile; } }
        public bool IsWifi { get { return isWifi; } }
        public bool IsRoaming { get { return isRoaming; } }

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
                    networkDetection.isMobile = info.Type == ConnectivityType.Mobile;
                    networkDetection.isWifi = info.Type == ConnectivityType.Wifi;
                    networkDetection.isRoaming = info.IsRoaming;
                }
                else
                {
                    networkDetection.isMobile = false;
                    networkDetection.isWifi = false;
                    networkDetection.isRoaming = false;
                }
            }
            else
            {
                networkDetection.isOnline = false;
                networkDetection.isMobile = false;
                networkDetection.isWifi = false;
                networkDetection.isRoaming = false;
            }
            return networkDetection;
        }
    }
}