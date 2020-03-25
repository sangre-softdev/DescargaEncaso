using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Database;
using Android.Graphics;
using Android.OS;
using Android.Preferences;
using Android.Support.V4.App;
using Android.Util;
using EnCasoShared;
using EnCasoShared.Model;
using Java.IO;
using System;
using System.IO;
using System.Threading.Tasks;

namespace DescargaEnCaso
{
    public static class General
    {
        public static readonly string LOCAL_DATABASE_ENCASO = "EnCasoRssDB.db3";

        public static readonly string ALARM_HOUR = "HoraSeleccionada";
        public static readonly string ALARM_MINUTE = "MinutoSeleccionado";
        public static readonly string ALARM_ACTIVE = "AlarmaActivada";
        public static readonly string ALARM_ONLY_WIFI = "UnicamenteWifi";
        public static readonly string ALARM_NOTIFICATION_EXTRA = "NotificationExtra";
        public static readonly string ALARM_NOTIFICATION_FILE = "NotificationFile";
        public static readonly string ALARM_NOTIFICATION_TITLE = "NotificationFileTitle";
        public static readonly string ALARM_NOTIFICATION_IMAGE = "NotificationFileImage";

        public static readonly string PLAYER_TITLE_PLAYING = "PlayerTitle";
        public static readonly string PLAYER_IMAGE_SRC_PLAYING = "PlayerImageSrc";        

        public static readonly string RETROCOMPATIBILIDAD = "Retrocompatibilidad";

        public static readonly string MANUAL_LAST_SEARCH = "LastSearch";


        public static readonly string DOWNLOAD_START_SERVICE = "Start_Service";
        public static readonly string DOWNLOAD_STOP_SERVICE = "Stop_Service";
        public static readonly string DOWNLOAD_START_AUTO = "Start_Auto";
        
        public static void ProgramNextAlarm(Context context)
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(context);

            int hour = prefs.GetInt(General.ALARM_HOUR, DateTime.Now.Hour);
            int minute = prefs.GetInt(General.ALARM_MINUTE, DateTime.Now.Minute);

            int seconds = TimeCalculator.GetSecondsUntilNextCheck(new DateTime(1984, 10, 12, hour, minute, 00));
            //GET TIME IN SECONDS AND INITIALIZE INTENT                
            Intent i = new Intent(context, typeof(AlarmBroadcastReceiver));
            //PASS CONTEXT,YOUR PRIVATE REQUEST CODE,INTENT OBJECT AND FLAG
            Android.App.PendingIntent pi = Android.App.PendingIntent.GetBroadcast(context, 1, i, 0);
            //INITIALIZE ALARM MANAGER
            Android.App.AlarmManager alarmManager = (Android.App.AlarmManager)context.GetSystemService(Context.AlarmService);

            //SET THE ALARM
            long triggermillis = Java.Lang.JavaSystem.CurrentTimeMillis() + (seconds * 1000);

            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                alarmManager.SetAndAllowWhileIdle(Android.App.AlarmType.RtcWakeup, triggermillis, pi);
            else //if (Build.VERSION.SdkInt >= BuildVersionCodes.Kitkat)
                alarmManager.SetExact(Android.App.AlarmType.RtcWakeup, triggermillis, pi);
            //else
            //    alarmManager.Set(Android.App.AlarmType.RtcWakeup, triggermillis, pi);
        }

        public static DownloadReturns ExecuteDownload(Context context, RssEnCaso rssEnCaso, bool onlyWiFi, bool isManual)
        {
            if (Android.OS.Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                Permission permissionCheck = context.CheckSelfPermission(Manifest.Permission.WriteExternalStorage);
                if (permissionCheck != Permission.Granted)
                {
                    return DownloadReturns.NoWritePermission;
                }
            }

            NetworkDetection networkDetection = NetworkDetection.DetectNetwork(context);
            if (!networkDetection.IsOnline) 
                return DownloadReturns.NoInternetConection;

            if (onlyWiFi && !networkDetection.IsWifi)
                return DownloadReturns.NoWiFiFound;

            Intent downloadIntent = new Intent(context, typeof(DownloadService));
            // Flag to start service
            if (isManual)
            {
                downloadIntent.SetAction(General.DOWNLOAD_START_SERVICE);
            }
            else
            {
                downloadIntent.SetAction(General.DOWNLOAD_START_AUTO);
            }
            // Passing values to intent
            downloadIntent.PutExtra("file", rssEnCaso.Url);
            downloadIntent.PutExtra("audioboomurl", rssEnCaso.AudioBoomUrl);
            downloadIntent.PutExtra("title", rssEnCaso.Title);
            downloadIntent.PutExtra("description", rssEnCaso.Description);
            downloadIntent.PutExtra("imageurl", rssEnCaso.ImageUrl);
            downloadIntent.PutExtra("pubdate", rssEnCaso.PubDate.ToString());
            // Starting service
            context.StartService(downloadIntent);                  
            return DownloadReturns.ServiceStarted;
        }

        public static void ShowNotification(Context context, string message, int icon, PendingIntent pendingIntent)
        {
            string channel_id = "All notifications";
            string channel_name = "All notifications";
            string channel_description = "All notifications";

            int notificationId = new Random().Next();

            NotificationManager notificationManager = (NotificationManager)context.GetSystemService(Context.NotificationService);

            if (Android.OS.Build.VERSION.SdkInt >= Android.OS.BuildVersionCodes.O)
            {
                NotificationChannel mChannel = new NotificationChannel(channel_id, channel_name, NotificationImportance.Default)
                {
                    Description = channel_description,
                    LightColor = Color.Red
                };
                mChannel.EnableLights(true);                
                mChannel.SetShowBadge(true);
                notificationManager.CreateNotificationChannel(mChannel);
            }

            var notification = new NotificationCompat.Builder(context, channel_id)
                .SetAutoCancel(true)
                .SetContentTitle(context.Resources.GetString(Resource.String.app_name))
                .SetContentText(message)
                .SetSmallIcon(icon);

            if (pendingIntent != null)
                notification.SetContentIntent(pendingIntent);

            notificationManager.Notify(notificationId, notification.Build());
        }

        public static bool HavePermission(Context context)
        {
            if (Android.OS.Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                Permission permissionCheck = context.CheckSelfPermission(Manifest.Permission.WriteExternalStorage);

                if (permissionCheck != Permission.Granted)
                {
                    return false;
                }
            }
            return true;
        }


        public static void GetExternalStoragePermission(Activity context)
        {
            string[] permissionsStorage =
            {
                    Manifest.Permission.ReadExternalStorage,
                    Manifest.Permission.WriteExternalStorage
                };
            int requestStorageId = 0;
            context.RequestPermissions(permissionsStorage, requestStorageId);
        }

    }

    public enum DownloadReturns
    {
        NoWritePermission,
        NoInternetConection,
        NoWiFiFound,
        //InsufficientSpace,
        //NoIdea,
        ServiceStarted,
        OpenFile
    }
}