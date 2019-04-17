using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Preferences;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Views;
using Android.Widget;
using DescargaEnCaso.Views;
using EnCasoShared.Model;
using Microsoft.AppCenter.Analytics;

namespace DescargaEnCaso
{
    [BroadcastReceiver]
    public class AlarmBroadcastReceiver : BroadcastReceiver
    {
        public override async void OnReceive(Context context, Intent intent)
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context);
            bool isWiFi = prefs.GetBoolean(General.ALARM_ONLY_WIFI, true);

            General.ProgramNextAlarm(context);
            RssEnCaso rssEnCaso = null;
            try
            {
                RssEnCaso[] rssEnCasos = await RssEnCaso.GetRssEnCasoAsync(true);
                rssEnCaso = rssEnCasos[0];
            }
            catch (WebException we)
            {
                Intent callIntent = new Intent(context, typeof(MainActivity));
                callIntent.PutExtra(General.ALARM_NOTIFICATION_EXTRA, (int)DownloadReturns.NoInternetConection);
                PendingIntent pendingIntent = PendingIntent.GetActivity(context, 0, callIntent, PendingIntentFlags.UpdateCurrent);
                string notificationMessage = context.GetString(Resource.String.download_error_no_internet_conection);
                General.ShowNotification(context, notificationMessage, Resource.Drawable.main_nav_list, pendingIntent);

                Analytics.TrackEvent(we.Message);
            }
            
            if (rssEnCaso != null)
            {
                DownloadReturns downloadReturns = await General.ExecuteDownload(context, rssEnCaso, isWiFi);
                string notificationMessage = "";
                Intent callIntent = new Intent(context, typeof(MainActivity));
                switch (downloadReturns)
                {
                    case DownloadReturns.InsufficientSpace:
                        callIntent.PutExtra(General.ALARM_NOTIFICATION_EXTRA, (int)DownloadReturns.InsufficientSpace);
                        notificationMessage = context.GetString(Resource.String.download_error_insufficient_space);
                        break;
                    case DownloadReturns.NoInternetConection:
                        callIntent.PutExtra(General.ALARM_NOTIFICATION_EXTRA, (int)DownloadReturns.NoInternetConection);
                        notificationMessage = context.GetString(Resource.String.download_error_no_internet_conection);
                        break;
                    case DownloadReturns.NoWiFiFound:
                        callIntent.PutExtra(General.ALARM_NOTIFICATION_EXTRA, (int)DownloadReturns.NoWiFiFound);
                        notificationMessage = context.GetString(Resource.String.download_error_no_wifi_found);
                        break;
                    case DownloadReturns.NoWritePermission:
                        callIntent.PutExtra(General.ALARM_NOTIFICATION_EXTRA, (int)DownloadReturns.NoWritePermission);
                        notificationMessage = context.GetString(Resource.String.download_error_no_write_permission);
                        break;
                    case DownloadReturns.NoIdea:
                        callIntent.PutExtra(General.ALARM_NOTIFICATION_EXTRA, (int)DownloadReturns.NoIdea);
                        notificationMessage = context.GetString(Resource.String.download_error_no_idea);
                        break;
                    case DownloadReturns.Success:
                        callIntent = null;
                        break;
                }
                if (callIntent != null)
                {
                    PendingIntent pendingIntent = PendingIntent.GetActivity(context, 0, callIntent, PendingIntentFlags.UpdateCurrent);
                    General.ShowNotification(context, notificationMessage, Resource.Drawable.main_nav_list, pendingIntent);
                }
            }
        }
    }
}