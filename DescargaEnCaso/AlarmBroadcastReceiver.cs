using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.App.Job;
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
        public async override void OnReceive(Context context, Intent intent)
        {
            General.ProgramNextAlarm(context);

            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context);
            bool isWiFi = prefs.GetBoolean(General.ALARM_ONLY_WIFI, true);

            RssEnCaso rssEnCaso = null;
            try
            {
                CancellationTokenSource cts = new CancellationTokenSource();
                RssEnCaso[] rssEnCasos = await RssEnCaso.GetRssEnCasoAsync(true, cts.Token);
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
                DownloadReturns downloadReturns = General.ExecuteDownload(context, rssEnCaso, isWiFi, false);
                string notificationMessage = "";
                Intent callIntent = new Intent(context, typeof(MainActivity));
                switch (downloadReturns)
                {
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
                    case DownloadReturns.ServiceStarted:
                        callIntent = null;
                        break;
                }
                if (callIntent != null)
                {
                    PendingIntent pendingIntent = PendingIntent.GetActivity(context, 0, callIntent, PendingIntentFlags.UpdateCurrent);
                    General.ShowNotification(context, notificationMessage, Resource.Drawable.main_nav_list, pendingIntent);
                }
            }

            //var jobScheduler = (JobScheduler)context.GetSystemService(Context.JobSchedulerService);

            //var javaClass = Java.Lang.Class.FromType(typeof(DownloadJobService));
            //var componentName = new ComponentName(context, javaClass);
            //var jobInfo = new JobInfo
            //    .Builder(1, componentName)
            //    .SetMinimumLatency(1000)
            //    .SetOverrideDeadline(2000)
            //    .SetRequiredNetworkType(NetworkType.Any)
            //    .SetRequiresDeviceIdle(false)
            //    .SetRequiresCharging(false)
            //    .Build();

            //var scheduleResult = jobScheduler.Schedule(jobInfo);
            //if (JobScheduler.ResultSuccess == scheduleResult)
            //{

            //}
            //else
            //{

            //}
        }
    }

    [Service(Permission = "android.permission.BIND_JOB_SERVICE")]
    public class DownloadJobService : JobService
    {
        public override bool OnStartJob(JobParameters @params)
        {            
            Task.Run(async () =>
            {
                ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this.ApplicationContext);
                bool isWiFi = prefs.GetBoolean(General.ALARM_ONLY_WIFI, true);

                RssEnCaso rssEnCaso = null;
                try
                {
                    CancellationTokenSource cts = new CancellationTokenSource();
                    RssEnCaso[] rssEnCasos = await RssEnCaso.GetRssEnCasoAsync(true, cts.Token);
                    rssEnCaso = rssEnCasos[0];
                }
                catch (WebException we)
                {
                    Intent callIntent = new Intent(this.ApplicationContext, typeof(MainActivity));
                    callIntent.PutExtra(General.ALARM_NOTIFICATION_EXTRA, (int)DownloadReturns.NoInternetConection);
                    PendingIntent pendingIntent = PendingIntent.GetActivity(this.ApplicationContext, 0, callIntent, PendingIntentFlags.UpdateCurrent);
                    string notificationMessage = this.ApplicationContext.GetString(Resource.String.download_error_no_internet_conection);
                    General.ShowNotification(this.ApplicationContext, notificationMessage, Resource.Drawable.main_nav_list, pendingIntent);

                    Analytics.TrackEvent(we.Message);
                }

                if (rssEnCaso != null)
                {
                    DownloadReturns downloadReturns = General.ExecuteDownload(this.ApplicationContext, rssEnCaso, isWiFi, false);
                    string notificationMessage = "";
                    Intent callIntent = new Intent(this.ApplicationContext, typeof(MainActivity));
                    switch (downloadReturns)
                    {
                        case DownloadReturns.NoInternetConection:
                            callIntent.PutExtra(General.ALARM_NOTIFICATION_EXTRA, (int)DownloadReturns.NoInternetConection);
                            notificationMessage = this.ApplicationContext.GetString(Resource.String.download_error_no_internet_conection);
                            break;
                        case DownloadReturns.NoWiFiFound:
                            callIntent.PutExtra(General.ALARM_NOTIFICATION_EXTRA, (int)DownloadReturns.NoWiFiFound);
                            notificationMessage = this.ApplicationContext.GetString(Resource.String.download_error_no_wifi_found);
                            break;
                        case DownloadReturns.NoWritePermission:
                            callIntent.PutExtra(General.ALARM_NOTIFICATION_EXTRA, (int)DownloadReturns.NoWritePermission);
                            notificationMessage = this.ApplicationContext.GetString(Resource.String.download_error_no_write_permission);
                            break;
                        case DownloadReturns.ServiceStarted:
                            callIntent = null;
                            break;
                    }
                    if (callIntent != null)
                    {
                        PendingIntent pendingIntent = PendingIntent.GetActivity(this.ApplicationContext, 0, callIntent, PendingIntentFlags.UpdateCurrent);
                        General.ShowNotification(this.ApplicationContext, notificationMessage, Resource.Drawable.main_nav_list, pendingIntent);
                    }
                }

                // Have to tell the JobScheduler the work is done.                 
                JobFinished(@params, false);
            });
            return true;
        }

        public override bool OnStopJob(JobParameters @params)
        {
            return false;
        }
    }
}