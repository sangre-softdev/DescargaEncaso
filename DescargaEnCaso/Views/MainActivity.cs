using Android;
using Android.Content;
using Android.OS;
using Android.Preferences;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.View;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using DescargaEnCaso.Enums;
using EnCasoShared.Model;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using System;
using System.Net;

namespace DescargaEnCaso.Views
{
    [Android.App.Activity(Label = "@string/app_name", Theme = "@style/AppTheme", MainLauncher = true, LaunchMode = Android.Content.PM.LaunchMode.SingleTop)]
    public class MainActivity : AppCompatActivity
    {
        // When requested, this adapter returns a DemoObjectFragment,
        // representing an object in the collection.
        DemoCollectionPagerAdapter demoCollectionPagerAdapter;
        Android.Support.V4.View.ViewPager viewPager;
        BottomNavigationView navigation;

        //TextView textMessage;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            AppCenter.Start("7e292b20-d832-43e0-b3a3-480443176e2a", typeof(Analytics), typeof(Crashes));

            SetContentView(Resource.Layout.main_activity);
            navigation = FindViewById<BottomNavigationView>(Resource.Id.nav);

            // ViewPager and its adapters use support library fragments, so use SupportFragmentManager.
            demoCollectionPagerAdapter = new DemoCollectionPagerAdapter(SupportFragmentManager);
            viewPager = (ViewPager)FindViewById(Resource.Id.pager);
            viewPager.Adapter = demoCollectionPagerAdapter;

            viewPager.PageSelected += ViewPager_PageSelected;
            navigation.NavigationItemSelected += Navigation_NavigationItemSelected;

            if (Intent.HasExtra(General.ALARM_NOTIFICATION_EXTRA))
            {
                RetryDownload(Intent);
            }
        }

        private void Navigation_NavigationItemSelected(object sender, BottomNavigationView.NavigationItemSelectedEventArgs e)
        {
            viewPager.PageSelected -= ViewPager_PageSelected;
            int selected = 0;
            switch (e.Item.ItemId)
            {
                case Resource.Id.main_nav_list:
                    selected = (int)MainTabsEnum.List;
                    break;
                case Resource.Id.main_nav_manual:
                    selected = (int)MainTabsEnum.Manual;
                    _ = ((ManualFragment)demoCollectionPagerAdapter.GetItem((int)MainTabsEnum.Manual)).UpdateRss();
                    WritePermission();
                    break;
                case Resource.Id.main_nav_auto:
                    selected = (int)MainTabsEnum.Auto;
                    WritePermission();
                    break;
            }
            viewPager.SetCurrentItem(selected, true);
            viewPager.PageSelected += ViewPager_PageSelected;
        }

        private void ViewPager_PageSelected(object sender, ViewPager.PageSelectedEventArgs e)
        {
            navigation.NavigationItemSelected -= Navigation_NavigationItemSelected;
            int selected = 0;
            switch (e.Position)
            {
                case (int)MainTabsEnum.List:
                    selected = Resource.Id.main_nav_list;
                    break;
                case (int)MainTabsEnum.Manual:
                    selected = Resource.Id.main_nav_manual;
                    _ = ((ManualFragment)demoCollectionPagerAdapter.GetItem((int)MainTabsEnum.Manual)).UpdateRss();
                    WritePermission();
                    break;
                case (int)MainTabsEnum.Auto:
                    selected = Resource.Id.main_nav_auto;
                    WritePermission();
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
                    case DownloadReturns.InsufficientSpace:
                        message = Resources.GetString(Resource.String.download_error_insufficient_space);
                        break;
                    case DownloadReturns.NoIdea:
                        message = Resources.GetString(Resource.String.download_error_no_idea);
                        break;
                    case DownloadReturns.NoInternetConection:
                        message = Resources.GetString(Resource.String.download_error_no_internet_conection);
                        break;
                    case DownloadReturns.NoWiFiFound:
                        message = Resources.GetString(Resource.String.download_error_no_wifi_found);
                        break;
                    case DownloadReturns.NoWritePermission:
                        message = Resources.GetString(Resource.String.download_error_no_write_permission);
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
                RssEnCaso[] rssEnCasos = await RssEnCaso.GetRssEnCasoAsync(true);
                if (rssEnCasos.Length > 0)
                {
                    DownloadReturns downloadReturns = await General.ExecuteDownload(this, rssEnCasos[0], isWiFi);
                    string notificationMessage = "";
                    switch (downloadReturns)
                    {
                        case DownloadReturns.InsufficientSpace:
                            notificationMessage = this.GetString(Resource.String.download_error_insufficient_space);
                            break;
                        case DownloadReturns.NoInternetConection:
                            notificationMessage = this.GetString(Resource.String.download_error_no_internet_conection);
                            break;
                        case DownloadReturns.NoWiFiFound:
                            notificationMessage = this.GetString(Resource.String.download_error_no_wifi_found);
                            break;
                        case DownloadReturns.NoWritePermission:
                            notificationMessage = this.GetString(Resource.String.download_error_no_write_permission);
                            break;
                        case DownloadReturns.NoIdea:
                            notificationMessage = this.GetString(Resource.String.download_error_no_idea);
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
    public class DemoCollectionPagerAdapter : FragmentStatePagerAdapter
    {
        private ManualFragment manualFragment;

        public DemoCollectionPagerAdapter(FragmentManager fm) : base(fm) { }

        public override int Count => System.Enum.GetNames(typeof(MainTabsEnum)).Length;

       public override Fragment GetItem(int position)
        {
            Fragment fragment;
            switch (position)
            {
                case (int)MainTabsEnum.List:
                    fragment = new ListFragment();
                    break;
                case (int)MainTabsEnum.Manual:
                    if (manualFragment == null)
                        manualFragment = new ManualFragment();
                    fragment = manualFragment;
                    break;                
                case (int)MainTabsEnum.Auto:
                    fragment = new AutoFragment();
                    break;
                default:
                    fragment = new Fragment();                    
                    break;
            }
            return fragment;
        }
    }
}

