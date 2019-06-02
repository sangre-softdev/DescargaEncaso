using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Graphics;
using Android.Media;
using Android.OS;
using Android.Preferences;
using Android.Runtime;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using Android.Util;
using Android.Views;
using Android.Widget;
using DescargaEnCaso.Views;
using EnCasoShared;
using EnCasoShared.Model;
using Java.IO;

namespace DescargaEnCaso
{
    [Service]
    public class DownloadService : Service
    {
        private readonly int id = new Random().Next();
        private readonly string channel_id = "All notifications";
        private readonly string channel_name = "All notifications";
        private readonly string channel_description = "All notifications";

        private bool isRunning = false;
        private readonly Queue<RssEnCaso> downloadList = new Queue<RssEnCaso>();

        //[return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            string extraDate = intent.GetStringExtra("pubdate");
            DateTime.TryParse(extraDate, out DateTime pubDate);

            var rssEnCaso = new RssEnCaso()
            {
                Url = intent.GetStringExtra("file"),
                Title = intent.GetStringExtra("title"),
                Description = intent.GetStringExtra("description"),
                ImageUrl = intent.GetStringExtra("imageurl"),
                AudioBoomUrl = intent.GetStringExtra("audioboomurl"),
                PubDate = pubDate
                //        downloadDateTime = DateTime.Now
            };

            if (intent.Action.Equals(General.DOWNLOAD_START_AUTO))
            {
                downloadList.Enqueue(rssEnCaso);
                if (!isRunning)
                {
                    RegisterForegroundService();
                    isRunning = true;
                    DownloadList(false);
                }
            }
            else if (intent.Action.Equals(General.DOWNLOAD_START_SERVICE))
            {
                downloadList.Enqueue(rssEnCaso);
                PendingDownloads();
                if (!isRunning)
                {
                    RegisterForegroundService();
                    isRunning = true;
                    DownloadList(true);
                }
            }
            else if (intent.Action.Equals(General.DOWNLOAD_STOP_SERVICE))
            {
                StopMe();
            }
            return StartCommandResult.Sticky;
        }

        private void PendingDownloads()
        {
            NotificationManager notificationManager = (NotificationManager)this.ApplicationContext.GetSystemService(Context.NotificationService);

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

            var notification = new NotificationCompat.Builder(this, channel_id)
               .SetContentTitle(Resources.GetString(Resource.String.app_name))
               .SetContentText((downloadList.Count + 1) + " " + Resources.GetString(Resource.String.download_service_pending_downloads))
               .SetSmallIcon(Resource.Mipmap.ic_launcher)
               .SetOngoing(true)
               .Build();

            // Enlist this instance of the service as a foreground service
            StartForeground(id, notification);
        }

        private async void DownloadList(bool isManual)
        {
            while (downloadList.TryDequeue(out RssEnCaso link))
            {
                if (isManual)
                {
                    PendingDownloads();
                }
                await Download(link, isManual);
            }
            StopMe();
        }

        private void StopMe()
        {
            StopForeground(true);
            StopSelf();
            isRunning = false;
        }

        private async Task Download(RssEnCaso download, bool isManual)
        {   
            string filename = download.Url.Substring(download.Url.LastIndexOf("/") + 1);
            int notificationId = new Random().Next();


            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            ISaveAndLoadFiles saveAndLoad = new SaveAndLoadFiles_Android();
            Bitmap bm = null;
            //    var pathString = prefs.GetString(download.ImageUrl, "");
            //    if (pathString != "")
            //    {
            //        if (saveAndLoad.FileExists(pathString))
            //        {
            //            var imageBytes = saveAndLoad.LoadByte(pathString);
            //            bm = BitmapFactory.DecodeByteArray(imageBytes, 0, imageBytes.Length);
            //        }
            //    }


            NotificationManager notificationManager = (NotificationManager)this.ApplicationContext.GetSystemService(Context.NotificationService);

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

            NotificationCompat.Builder builder = new NotificationCompat.Builder(this.ApplicationContext, channel_id)
                .SetAutoCancel(true)                    // Dismiss from the notif. area when clicked                
                .SetContentTitle(download.Title)      // Set its title                
                .SetSmallIcon(Resource.Mipmap.ic_launcher)
                .SetProgress(100, 0, false)
                .SetOnlyAlertOnce(true)
                .SetContentText(this.ApplicationContext.GetString(Resource.String.download_service_downloading));

            if (bm != null)
            {
                builder.SetLargeIcon(bm);
            }

            if (isManual)
            {
                notificationManager.Notify(notificationId, builder.Build());
            }
            else
            {
                StartForeground(id, builder.Build());
            }

            int ant = 1;
            Progress<DownloadBytesProgress> progressReporter = new Progress<DownloadBytesProgress>();
            progressReporter.ProgressChanged += (s, args) =>
            {
                int perc = (int)(100 * args.PercentComplete);
                if (perc > ant)
                {
                    ant += 1;
                    builder.SetProgress(100, (int)(100 * args.PercentComplete), false);
                    if (isManual)
                    {
                        notificationManager.Notify(notificationId, builder.Build());
                    }
                    else
                    {
                        StartForeground(id, builder.Build());
                    }
                }
            };

            try
            {
                if (Android.OS.Build.VERSION.SdkInt >= BuildVersionCodes.M)
                {
                    Permission permissionCheck = this.ApplicationContext.CheckSelfPermission(Manifest.Permission.WriteExternalStorage);
                    if (permissionCheck != Permission.Granted)
                    {
                        throw new UnauthorizedAccessException();
                    }
                }
                Task<byte[]> downloadTask = DownloadHelper.CreateDownloadTask(download.Url, progressReporter);
                byte[] bytesDownloaded = null;
                try
                {
                    bytesDownloaded = await downloadTask;
                }
                catch (Exception ex1)
                {
                    if (ex1.Message.Contains("(404) Not Found"))
                    {
                        downloadTask = DownloadHelper.CreateDownloadTask(download.AudioBoomUrl, progressReporter);
                        bytesDownloaded = await downloadTask;
                    }
                }

                ISaveAndLoadFiles saveFile = new SaveAndLoadFiles_Android();
                var path = saveFile.SaveByteAsync(filename, bytesDownloaded);
                
                MediaScannerConnection
                    .ScanFile(this.ApplicationContext,
                    new string[] { path },
                    new string[] { "audio/*" }, null);

                var enCasoFile = LocalDatabase<EnCasoFile>.GetEnCasoFileDb().GetFirstUsingString("SavedFile", path);
                if (enCasoFile != null)
                {
                    LocalDatabase<EnCasoFile>.GetEnCasoFileDb().Delete(enCasoFile);
                }                
                LocalDatabase<EnCasoFile>.GetEnCasoFileDb().Save(
                        new EnCasoFile()
                        {
                            Title = download.Title,
                            Description = download.Description,
                            PubDate = download.PubDate,
                            ImageUrl = download.ImageUrl,
                            SavedFile = path,
                            DownloadDateTime = DateTime.Now
                        }
                    );

                Intent openIntent = new Intent(this.ApplicationContext, typeof(MainActivity));
                openIntent.PutExtra(General.ALARM_NOTIFICATION_EXTRA, (int)DownloadReturns.OpenFile);
                openIntent.PutExtra(General.ALARM_NOTIFICATION_FILE, path);                
                PendingIntent pIntent = PendingIntent.GetActivity(this.ApplicationContext, 0, openIntent, PendingIntentFlags.UpdateCurrent);

                builder
                    .SetProgress(0, 0, false)
                    .SetContentText(download.Description)
                    .SetContentIntent(pIntent);

                notificationManager.Notify(notificationId, builder.Build());
            }
            catch (UnauthorizedAccessException)
            {
                builder
                    .SetProgress(0, 0, false)
                    .SetContentText(Resources.GetString(Resource.String.download_service_unauthorized_file_exception));
                notificationManager.Notify(notificationId, builder.Build());
            }
            catch (IOException)
            {
                builder
                    .SetProgress(0, 0, false)
                    .SetContentText(Resources.GetString(Resource.String.download_service_io_file_exception));
                notificationManager.Notify(notificationId, builder.Build());
            }            
            catch (Exception ex)
            {
                if (ex.Message.Contains("full"))
                {
                    builder
                        .SetProgress(0, 0, false)
                        .SetContentText(Resources.GetString(Resource.String.download_service_disk_full));
                }
                else
                {
                    builder
                        .SetProgress(0, 0, false)
                        .SetContentText(Resources.GetString(Resource.String.download_service_lost_internet_connection));
                }
                if (isManual)
                {
                    notificationManager.Notify(notificationId, builder.Build());
                }
                else
                {
                    StartForeground(id, builder.Build());
                }
            }
        }


        //public override void OnDestroy()
        //{
        //    // Remove the notification from the status bar.
        //    //var notificationManager = NotificationManagerCompat.From(this.ApplicationContext);
        //    NotificationManager notificationManager = (NotificationManager)this.ApplicationContext.GetSystemService(Context.NotificationService);
        //    notificationManager.Cancel(id);
        //    base.OnDestroy();
        //}

        public override IBinder OnBind(Intent intent)
        {
            return null;
        }

        void RegisterForegroundService()
        {
            NotificationManager notificationManager = (NotificationManager)this.ApplicationContext.GetSystemService(Context.NotificationService);

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

            var notification = new NotificationCompat.Builder(this, channel_id)
                .SetContentTitle(Resources.GetString(Resource.String.app_name))
                .SetContentText(Resources.GetString(Resource.String.download_service_downloading))
                .SetSmallIcon(Resource.Mipmap.ic_launcher)
                .SetOngoing(true)
                //.AddAction(BuildStopServiceAction())
                .Build();

            // Enlist this instance of the service as a foreground service
            StartForeground(id, notification);
        }

        //      /// <summary>
        ///// Builds the Notification.Action that will allow the user to stop the service via the
        ///// notification in the status bar
        ///// </summary>
        ///// <returns>The stop service action.</returns>
        //NotificationCompat.Action BuildStopServiceAction()
        //      {
        //          var stopServiceIntent = new Intent(this, GetType());
        //          stopServiceIntent.SetAction(Consts.DOWNLOAD_STOP_SERVICE);
        //          var stopServicePendingIntent = PendingIntent.GetService(this, 0, stopServiceIntent, 0);
        //          var builder = new NotificationCompat.Action.Builder(Resource.Mipmap.iconoapp,
        //                                                        "Stop Service",
        //                                                        stopServicePendingIntent);
        //          return builder.Build();
        //      }
    }
}