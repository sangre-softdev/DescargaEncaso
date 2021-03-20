using Android.Content;
using Android.OS;
using Android.Support.Design.Widget;
using Android.Support.V4.App;
using Android.Support.V4.View;
using Android.Support.V7.App;
using Android.Views;
using Android.Widget;
using AndroidX.Preference;
using DescargaEnCaso.Enums;
using EnCasoShared.Model;
using FFImageLoading;
using MediaManager;
using MediaManager.Media;
using MediaManager.Player;
using Microsoft.AppCenter;
using Microsoft.AppCenter.Analytics;
using Microsoft.AppCenter.Crashes;
using System;
using System.Collections.Generic;
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

        // Media Player Controls
        LinearLayout customMediaPlayer;
        SeekBar playerSeekBar;
        TextView playerTitle;
        ImageView playerImage;
        ImageButton playerPlayPause;
        ImageButton playerStop;
        string current_title = string.Empty;
        string current_image = string.Empty;

        CancellationTokenSource cts;
        //TextView textMessage;
        int selectedNav = 0;

        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            AppCenter.Start("7e292b20-d832-43e0-b3a3-480443176e2a", typeof(Analytics), typeof(Crashes));
            // Media reproduction configurations
            CrossMediaManager.Current.Init();
            CrossMediaManager.Current.Notification.ShowNavigationControls = false;

            var config = new FFImageLoading.Config.Configuration()
            {
                DiskCacheDuration = new TimeSpan(3650, 0, 0, 0, 0),
                BitmapOptimizations = true,
                SchedulerMaxParallelTasks = 4
            };
            ImageService.Instance.Initialize(config);

            SetContentView(Resource.Layout.main_activity);

            // Media Player
            customMediaPlayer = FindViewById<LinearLayout>(Resource.Id.customMediaPlayer);
            playerSeekBar = FindViewById<SeekBar>(Resource.Id.playerSeekBar);
            playerSeekBar.StopTrackingTouch += CustomSeekBar_StopTrackingTouch;
            playerSeekBar.StartTrackingTouch += CustomSeekBar_StartTrackingTouch;
            playerTitle = FindViewById<TextView>(Resource.Id.playerTitle);
            playerImage = FindViewById<ImageView>(Resource.Id.playerImage);
            playerPlayPause = FindViewById<ImageButton>(Resource.Id.playerPlayPause);
            playerPlayPause.Click += PlayerPlayPause_Click;
            playerStop = FindViewById<ImageButton>(Resource.Id.playerStop);
            playerStop.Click += PlayerStop_Click;

            navigation = FindViewById<BottomNavigationView>(Resource.Id.nav);
            // ViewPager and its adapters use support library fragments, so use SupportFragmentManager.
            mainCollectionPagerAdapter = new MainCollectionPagerAdapter(SupportFragmentManager);
            viewPager = (ViewPager)FindViewById(Resource.Id.pager);
            viewPager.Adapter = mainCollectionPagerAdapter;

            viewPager.PageSelected += ViewPager_PageSelected;
            navigation.NavigationItemSelected += Navigation_NavigationItemSelected;

            if (Intent.HasExtra(General.ALARM_NOTIFICATION_EXTRA))
            {
                RetryDownload(Intent);
            }
        }

        protected override void OnPause()
        {
            base.OnPause();
            CrossMediaManager.Current.StateChanged -= Current_StateChanged;
            CrossMediaManager.Current.PositionChanged -= Current_PositionChanged;
            CrossMediaManager.Current.MediaItemFinished -= Current_MediaItemFinished;
            SaveCurrentPlayerData(null);
        }

        protected override void OnSaveInstanceState(Bundle outState)
        {
            base.OnSaveInstanceState(outState);
            SaveCurrentPlayerData(outState);            
        }

        protected override void OnResume()
        {
            base.OnResume();
            CrossMediaManager.Current.StateChanged += Current_StateChanged;
            CrossMediaManager.Current.PositionChanged += Current_PositionChanged;
            CrossMediaManager.Current.MediaItemFinished += Current_MediaItemFinished;
            if (GetCurrentPlayerDataFromBundlePrefs(null))
            {
                SetPlayerDataPlaying();
                customMediaPlayer.Visibility = ViewStates.Visible;
                SetCrossMediaByState(CrossMediaManager.Current.State);
            }
            else
            {
                customMediaPlayer.Visibility = ViewStates.Gone;
            }
        }
        protected override void OnStop()
        {
            base.OnStop();
        }

        private bool GetCurrentPlayerDataFromBundlePrefs(Bundle SavedInstance)
        {
            if (SavedInstance != null)
            {
                current_title = SavedInstance.GetString(General.PLAYER_TITLE_PLAYING);
                current_image = SavedInstance.GetString(General.PLAYER_IMAGE_SRC_PLAYING);
            }
            if (string.IsNullOrEmpty(current_title))
            {
                ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
                current_title = prefs.GetString(General.PLAYER_TITLE_PLAYING, string.Empty);
                current_image = prefs.GetString(General.PLAYER_IMAGE_SRC_PLAYING, string.Empty);
            }
            return !string.IsNullOrEmpty(current_title) && !string.IsNullOrEmpty(current_image);
        }

        private void SaveCurrentPlayerData(Bundle state)
        {
            state?.PutString(General.PLAYER_TITLE_PLAYING, current_title);
            state?.PutString(General.PLAYER_IMAGE_SRC_PLAYING, current_image);
            ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(this);
            var edits = prefs.Edit();
            edits.PutString(General.PLAYER_TITLE_PLAYING, current_title);
            edits.PutString(General.PLAYER_IMAGE_SRC_PLAYING, current_image);
            edits.Commit();
        }

        private async void PlayerStop_Click(object sender, EventArgs e)
        {
            await CrossMediaManager.Current.Stop();
        }

        private async void PlayerPlayPause_Click(object sender, EventArgs e)
        {
            switch (CrossMediaManager.Current.State)
            {
                case MediaPlayerState.Playing:
                    await CrossMediaManager.Current.Pause();
                    break;
                case MediaPlayerState.Paused:
                case MediaPlayerState.Buffering:
                case MediaPlayerState.Loading:
                    await CrossMediaManager.Current.Play();
                    break;                    
            }
        }

        private void CustomSeekBar_StartTrackingTouch(object sender, SeekBar.StartTrackingTouchEventArgs e)
        {
            CrossMediaManager.Current.PositionChanged -= Current_PositionChanged;
        }

        private void CustomSeekBar_StopTrackingTouch(object sender, SeekBar.StopTrackingTouchEventArgs e)
        {
            CrossMediaManager.Current.SeekTo(TimeSpan.FromSeconds(e.SeekBar.Progress));
            CrossMediaManager.Current.PositionChanged += Current_PositionChanged;
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
                        var title = intent.HasExtra(General.ALARM_NOTIFICATION_TITLE) ? intent.GetStringExtra(General.ALARM_NOTIFICATION_TITLE) : string.Empty;
                        var image = intent.HasExtra(General.ALARM_NOTIFICATION_IMAGE) ? intent.GetStringExtra(General.ALARM_NOTIFICATION_IMAGE) : string.Empty;
                        Task.Run(async () => await Task.Delay(1000))
                        .ContinueWith(async t =>
                        {
                            
                            await Play(intent.GetStringExtra(General.ALARM_NOTIFICATION_FILE), title, image);
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

        public async Task Play(string uri, string title, string image)
        {
            current_image = image;
            current_title = title;
            await CrossMediaManager.Current.Play(uri);
            SetPlayerDataPlaying();
        }

        private void SetPlayerDataPlaying()
        {
            playerTitle.Text = current_title;
            ImageService.Instance
                    .LoadUrl(current_image)
                    .DownSampleInDip(80)
                    .FadeAnimation(false)
                    .Delay(0)
                    .WithCache(FFImageLoading.Cache.CacheType.All)
                    .LoadingPlaceholder("drawable/loading.png", FFImageLoading.Work.ImageSource.CompiledResource)
                    .Error((Exception ex) => { Analytics.TrackEvent("Image caching error", new Dictionary<string, string>() { { "exception", ex.Message }, { "Data", ex.Data.ToString() } }); })
                    .Into(playerImage);
        }

        private void SetCrossMediaByState(MediaPlayerState state)
        {
            switch (state)
            {
                case MediaPlayerState.Stopped:
                    customMediaPlayer.Visibility = ViewStates.Gone;
                    current_title = current_image = string.Empty;
                    SaveCurrentPlayerData(null);
                    break;
                case MediaPlayerState.Playing:
                    customMediaPlayer.Visibility = ViewStates.Visible;
                    SetSeekBarMaxProgress();
                    playerPlayPause.SetImageResource(Resource.Drawable.pause_24px);
                    break;
                case MediaPlayerState.Paused:
                    customMediaPlayer.Visibility = ViewStates.Visible;
                    SetSeekBarMaxProgress();
                    playerPlayPause.SetImageResource(Resource.Drawable.play_24);
                    break;
                case MediaPlayerState.Buffering:
                case MediaPlayerState.Loading:
                    break;
            }
        }

        private void SetSeekBarMaxProgress()
        {
            if (CrossMediaManager.Current.Duration != null)
            {
                playerSeekBar.Max = (int)CrossMediaManager.Current.Duration.TotalSeconds;
            }
            if (CrossMediaManager.Current.Position != null)
            {
                playerSeekBar.Progress = (int)CrossMediaManager.Current.Position.TotalSeconds;
            }
        }

        private void Current_StateChanged(object sender, MediaManager.Playback.StateChangedEventArgs e)
        {
            SetCrossMediaByState(e.State);
        }

        private void Current_PositionChanged(object sender, MediaManager.Playback.PositionChangedEventArgs e)
        {
            playerSeekBar.Progress = (int)e.Position.TotalSeconds;
        }

        private void Current_MediaItemFinished(object sender, MediaItemEventArgs e)
        {
            CrossMediaManager.Current.Stop();
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

