using Android;
using Android.Content;
using Android.OS;
using Android.Preferences;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.View;
using Android.Support.V7.App;
using Android.Util;
using Android.Views;
using Android.Widget;
using DescargaEnCaso.Enums;
using EnCasoShared;
using EnCasoShared.Model;
using FFImageLoading;
using MediaManager;
using MediaManager.Media;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using System;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace DescargaEnCaso.Views
{
    [Android.App.Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, LaunchMode = Android.Content.PM.LaunchMode.SingleTop)]
    public class MainActivity : AppCompatActivity
    {
        // When requested, this adapter returns a DemoObjectFragment,
        // representing an object in the collection.
        MainCollectionPagerAdapter mainCollectionPagerAdapter;
        Android.Support.V4.View.ViewPager viewPager;
        BottomNavigationView navigation;

        CancellationTokenSource cts;
        //TextView textMessage;
        int selectedNav = 0;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            AppCenter.Start("7e292b20-d832-43e0-b3a3-480443176e2a", typeof(Analytics), typeof(Crashes));
            CrossMediaManager.Current.Init();
            var config = new FFImageLoading.Config.Configuration()
            {
                DiskCacheDuration = new TimeSpan(3650, 0, 0, 0, 0),
                BitmapOptimizations = true,
                SchedulerMaxParallelTasks = 4
            };
            ImageService.Instance.Initialize(config);


            SetContentView(Resource.Layout.main_activity);
            navigation = FindViewById<BottomNavigationView>(Resource.Id.nav);

            // ViewPager and its adapters use support library fragments, so use SupportFragmentManager.
            mainCollectionPagerAdapter = new MainCollectionPagerAdapter(SupportFragmentManager);
            viewPager = (ViewPager)FindViewById(Resource.Id.pager);
            viewPager.Adapter = mainCollectionPagerAdapter;

            viewPager.PageSelected += ViewPager_PageSelected;
            navigation.NavigationItemSelected += Navigation_NavigationItemSelected;

            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this.ApplicationContext);
            if (prefs.GetBoolean(General.RETROCOMPATIBILIDAD, true))
            {
                Retrocompatibilidad();
                ISharedPreferencesEditor editor = prefs.Edit();
                editor.PutBoolean(General.RETROCOMPATIBILIDAD, false);
                editor.Apply();
            }

            if (Intent.HasExtra(General.ALARM_NOTIFICATION_EXTRA))
            {
                RetryDownload(Intent);
            }
        }

        ////////////////////////////////////////////////////////////////////// ELIMINAR ESTO DESPUES
        private void Retrocompatibilidad()
        {
            RssModel[] rssModelList = LocalDatabase<RssModel>.GetRssModelDb().GetAll();
            foreach (var rssModel in rssModelList)
            {
                LocalDatabase<EnCasoFile>.GetEnCasoFileDb().Save(
                    new EnCasoFile()
                    {
                        Title = rssModel.Title,
                        Description = rssModel.Description,
                        ImageUrl = rssModel.ImageUrl,
                        PubDate = rssModel.PubDate,
                        SavedFile = rssModel.SaveFile,
                        DownloadDateTime = rssModel.downloadDateTime
                    });
            }
        }
        
        
        private void Navigation_NavigationItemSelected(object sender, BottomNavigationView.NavigationItemSelectedEventArgs e)
        {
            viewPager.PageSelected -= ViewPager_PageSelected;
            if (cts != null)
                cts.Cancel();
            switch (e.Item.ItemId)
            {
                case Resource.Id.main_nav_list:
                    selectedNav = (int)MainTabsEnum.List;
                    ((ListFragment)mainCollectionPagerAdapter.GetItem((int)MainTabsEnum.List)).UpdateList();
                    break;
                case Resource.Id.main_nav_manual:
                    if (selectedNav != (int)MainTabsEnum.Manual)
                    {
                        var manualFragment = (ManualFragment)mainCollectionPagerAdapter.GetItem((int)MainTabsEnum.Manual);
                        manualFragment.Loading(true);
                        cts = new CancellationTokenSource();
                        Task.Run(async () => await manualFragment.UpdateRss(false, cts.Token))
                            .ContinueWith(t =>
                            {
                                if (t.IsFaulted)
                                {
                                    manualFragment.UpdateView(true);
                                    Toast.MakeText(this, Resource.String.download_error_no_idea, ToastLength.Short).Show();
                                }
                                else
                                {
                                    manualFragment.UpdateView(false);
                                }
                                _ = manualFragment.DownloadImages();
                            }, TaskScheduler.FromCurrentSynchronizationContext());
                    }
                    WritePermission();
                    selectedNav = (int)MainTabsEnum.Manual;
                    break;
                case Resource.Id.main_nav_auto:                    
                    WritePermission();
                    selectedNav = (int)MainTabsEnum.Auto;
                    break;
            }
            viewPager.SetCurrentItem(selectedNav, true);
            viewPager.PageSelected += ViewPager_PageSelected;
        }

        private void ViewPager_PageSelected(object sender, ViewPager.PageSelectedEventArgs e)
        {
            navigation.NavigationItemSelected -= Navigation_NavigationItemSelected;
            if (cts != null)
                cts.Cancel();
            int selected = 0;
            switch (e.Position)
            {
                case (int)MainTabsEnum.List:
                    selected = Resource.Id.main_nav_list;
                    ((ListFragment)mainCollectionPagerAdapter.GetItem((int)MainTabsEnum.List)).UpdateList();
                    break;
                case (int)MainTabsEnum.Manual:
                    var manualFragment = (ManualFragment)mainCollectionPagerAdapter.GetItem((int)MainTabsEnum.Manual);
                    manualFragment.Loading(true);
                    cts = new CancellationTokenSource();
                    Task.Run(async () => await manualFragment.UpdateRss(false, cts.Token))
                        .ContinueWith(t =>
                        {
                            if (t.IsFaulted)
                            {
                                manualFragment.UpdateView(true);
                                Toast.MakeText(this, Resource.String.download_error_no_idea, ToastLength.Short).Show();
                            }
                            else
                            {
                                manualFragment.UpdateView(false);
                            }
                            _ = manualFragment.DownloadImages();
                        }, TaskScheduler.FromCurrentSynchronizationContext());
                    WritePermission();
                    selected = Resource.Id.main_nav_manual;
                    selectedNav = (int)MainTabsEnum.Manual;
                    break;
                case (int)MainTabsEnum.Auto:
                    WritePermission();
                    selected = Resource.Id.main_nav_auto;
                    break;
            }
            navigation.SelectedItemId = selected;
            navigation.NavigationItemSelected += Navigation_NavigationItemSelected;
        }

        private void WritePermission()
        {
            if (!General.HavePermission(this))
            {
                General.GetExternalStoragePermission(this);
            }
        }

        protected override void OnNewIntent(Intent intent)
        {
            base.OnNewIntent(intent);
            RetryDownload(intent);
        }

        void RetryDownload(Intent intent)
        {
            if (intent.HasExtra(General.ALARM_NOTIFICATION_EXTRA))
            {
                DownloadReturns downloadReturns = (DownloadReturns)intent.GetIntExtra(General.ALARM_NOTIFICATION_EXTRA, 0);
                intent.RemoveExtra(General.ALARM_NOTIFICATION_EXTRA);
                string message = "";
                switch (downloadReturns)
                {
                    case DownloadReturns.NoInternetConection:
                        message = Resources.GetString(Resource.String.download_error_no_internet_conection);
                        break;
                    case DownloadReturns.NoWiFiFound:
                        message = Resources.GetString(Resource.String.download_error_no_wifi_found);
                        break;
                    case DownloadReturns.NoWritePermission:
                        message = Resources.GetString(Resource.String.download_error_no_write_permission);
                        break;
                    case DownloadReturns.OpenFile:
                        message = "";                        
                        break;
                }
                if (!string.IsNullOrEmpty(message))
                {
                    new AlertDialog.Builder(this)
                        .SetTitle(Resource.String.app_name)
                        .SetMessage(message + "\n" + Resources.GetString(Resource.String.download_error_retry))
                        .SetIcon(Resource.Drawable.baseline_warning_24)
                        .SetPositiveButton(Resource.String.download_error_ok_button, DialogPositiveButtonClick)
                        .SetNegativeButton(Resource.String.download_error_cancel_button, DialogNegativeButtonClick)
                        .Show();
                }
                else
                {   
                    if (intent.HasExtra(General.ALARM_NOTIFICATION_FILE))
                    {
                        Task.Run(async () => await Task.Delay(1000))
                        .ContinueWith(t =>
                        {
                            CrossMediaManager.Current.Play(new MediaItem(intent.GetStringExtra(General.ALARM_NOTIFICATION_FILE)));
                        }, TaskScheduler.FromCurrentSynchronizationContext());
                    }
                }
            }
        }

        private void DialogNegativeButtonClick(object sender, DialogClickEventArgs dialogClickEventArgs)
        {
            // nothing to do here
        }

        private async void DialogPositiveButtonClick(object sender, DialogClickEventArgs dialogClickEventArgs)
        {
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            bool isWiFi = prefs.GetBoolean(General.ALARM_ONLY_WIFI, true);
            try
            {
                CancellationTokenSource token = new CancellationTokenSource();                
                RssEnCaso[] rssEnCasos = await RssEnCaso.GetRssEnCasoAsync(true, token.Token);
                if (rssEnCasos.Length > 0)
                {
                    DownloadReturns downloadReturns = General.ExecuteDownload(this, rssEnCasos[0], isWiFi, true);
                    string notificationMessage = "";
                    switch (downloadReturns)
                    {
                        case DownloadReturns.NoInternetConection:
                            notificationMessage = this.GetString(Resource.String.download_error_no_internet_conection);
                            break;
                        case DownloadReturns.NoWiFiFound:
                            notificationMessage = this.GetString(Resource.String.download_error_no_wifi_found);
                            break;
                        case DownloadReturns.NoWritePermission:
                            notificationMessage = this.GetString(Resource.String.download_error_no_write_permission);
                            break;                        
                    }
                    if (!string.IsNullOrEmpty(notificationMessage))
                    {
                        Toast.MakeText(this, notificationMessage, ToastLength.Long).Show();
                    }
                }
            }
            catch (WebException we)
            {
                Analytics.TrackEvent(we.Message);
            }
            
        }
    }

    // Since this is an object collection, use a FragmentStatePagerAdapter,
    // and NOT a FragmentPagerAdapter.
    public class MainCollectionPagerAdapter : FragmentStatePagerAdapter
    {
        private ManualFragment manualFragment;
        private ListFragment listFragment;
        private AutoFragment autoFragment;

        public MainCollectionPagerAdapter(FragmentManager fm) : base(fm) { }

        public override int Count => System.Enum.GetNames(typeof(MainTabsEnum)).Length;

       public override Fragment GetItem(int position)
        {
            Fragment fragment;
            switch (position)
            {
                case (int)MainTabsEnum.List:
                    if (listFragment == null)
                        listFragment = new ListFragment();
                    fragment = listFragment;
                    break;
                case (int)MainTabsEnum.Manual:
                    if (manualFragment == null)
                        manualFragment = new ManualFragment();
                    fragment = manualFragment;
                    break;                
                case (int)MainTabsEnum.Auto:
                    if (autoFragment == null)
                        autoFragment = new AutoFragment();
                    fragment = autoFragment;
                    break;
                default:
                    fragment = new Fragment();                    
                    break;
            }
            return fragment;
        }
    }
}

