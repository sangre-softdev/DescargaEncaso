using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Preferences;
using Android.Runtime;
using Android.Support.V4.Widget;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;
using EnCasoShared;
using EnCasoShared.Model;
using FFImageLoading;
using Microsoft.AppCenter.Analytics;

namespace DescargaEnCaso.Views
{
    public class ManualFragment : Android.Support.V4.App.Fragment
    {
        RecyclerView recyclerView;
        ManualRecyclerAdapter manualRecyclerAdapter;
        TextView tvNoInternet;
        SwipeRefreshLayout swipeRefreshLayout;
        RssEnCaso[] rssEnCaso;
        ISharedPreferences prefs;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);
            prefs = PreferenceManager.GetDefaultSharedPreferences(Context);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            // Use this to return your custom view for this Fragment
            View view = inflater.Inflate(Resource.Layout.en_caso_list_fragment, container, false);

            tvNoInternet = view.FindViewById<TextView>(Resource.Id.encaso_list_nointernet);

            swipeRefreshLayout = view.FindViewById<SwipeRefreshLayout>(Resource.Id.encaso_list_swipe);
            swipeRefreshLayout.Refresh += SwipeRefreshLayout_Refresh;

            recyclerView = view.FindViewById<RecyclerView>(Resource.Id.encaso_list_recyclerview);

            LinearLayoutManager linearLayoutManager = new LinearLayoutManager(Context);
            recyclerView.SetLayoutManager(linearLayoutManager);
            rssEnCaso = LocalDatabase<RssEnCaso>.GetRssEnCasoDb().GetAll();

            manualRecyclerAdapter = new ManualRecyclerAdapter(rssEnCaso);
            recyclerView.SetAdapter(manualRecyclerAdapter);
            recyclerView.AddOnScrollListener(new CustomScrollListener());
            recyclerView.HasFixedSize = true;            
            return view;
        }

        private async void SwipeRefreshLayout_Refresh(object sender, EventArgs e)
        {
            var cts = new CancellationTokenSource();
            await this.UpdateRss(true, cts.Token);
            Loading(false);
        }
        
        public async Task UpdateRss(bool forceUpdate, CancellationToken ct)
        {
            try
            {
                if (ct.IsCancellationRequested)
                    ct.ThrowIfCancellationRequested();
                DateTime dateTime = DateTime.ParseExact(
                        prefs.GetString(General.MANUAL_LAST_SEARCH, "1984-10-12 14:00:00"),
                        "yyyy-MM-dd HH:mm:ss",
                        System.Globalization.CultureInfo.InvariantCulture);

                if (int.Parse(dateTime.Date.ToString("yyyyMMdd")) < int.Parse(DateTime.Now.ToString("yyyyMMdd"))
                    || forceUpdate)
                {
                    rssEnCaso = LocalDatabase<RssEnCaso>.GetRssEnCasoDb().GetAll();
                    RssEnCaso[] rssEnCasoI = await RssEnCaso.GetRssEnCasoAsync(false, ct);

                    ISharedPreferencesEditor editor = prefs.Edit();
                    editor.PutString(General.MANUAL_LAST_SEARCH, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    editor.Apply();

                    RssEnCaso[] insertRecords = rssEnCasoI.Except(rssEnCaso, new RssEnCaso()).ToArray();
                    foreach (RssEnCaso ins in insertRecords)
                    {
                        if (ct.IsCancellationRequested)
                            ct.ThrowIfCancellationRequested();
                        LocalDatabase<RssEnCaso>.GetRssEnCasoDb().Save(ins);
                    }

                    RssEnCaso[] deleteRecords = rssEnCaso.Except(rssEnCasoI, new RssEnCaso()).ToArray();
                    foreach (RssEnCaso del in deleteRecords)
                    {
                        if (ct.IsCancellationRequested)
                            ct.ThrowIfCancellationRequested();
                        LocalDatabase<RssEnCaso>.GetRssEnCasoDb().Delete(del);
                    }
                    manualRecyclerAdapter.UpdateData(rssEnCasoI);
                }                
            }
            catch (WebException we)
            {
                Analytics.TrackEvent(we.Message);
                throw we;
            }
            catch (Exception ex)
            {
                Analytics.TrackEvent(ex.Message);
                throw ex;
            }
        }

        public void UpdateView(bool exception) {
            if (exception)
            {
                tvNoInternet.Visibility = ViewStates.Visible;
            }
            else
            {
                tvNoInternet.Visibility = ViewStates.Gone;
                Loading(false);
                manualRecyclerAdapter.NotifyChanges();
            }
        }

        public void Loading(bool loading)
        {
            if (swipeRefreshLayout != null)
            {
                swipeRefreshLayout.Refreshing = loading;
            }
        }

        public async Task DownloadImages()
        {
            await manualRecyclerAdapter.DownloadImages();
        }
    }

    public class CustomScrollListener : RecyclerView.OnScrollListener
    {
        public override void OnScrollStateChanged(RecyclerView recyclerView, int newState)
        {            
            base.OnScrollStateChanged(recyclerView, newState);
            switch (newState)
            {
                case RecyclerView.ScrollStateDragging:
                    ImageService.Instance.SetPauseWorkAndCancelExisting(true);
                    break;
                case RecyclerView.ScrollStateIdle:
                    ImageService.Instance.SetPauseWork(false);
                    break;
            }
        }
    }

    public class ManualRecyclerAdapter : RecyclerView.Adapter
    {
        //public event EventHandler<ManualRecylcerAdapterClickEventArgs> ItemClick;
        //public event EventHandler<ManualRecylcerAdapterClickEventArgs> ItemLongClick;
        RssEnCaso[] items;        
        Context context;
        int position;
        bool cached = false;

        public ManualRecyclerAdapter(RssEnCaso[] data)
        {
            items = data;
        }

        // Create new views (invoked by the layout manager)
        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            //Setup your layout here
            View itemView = null;
            itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.manual_row_layout, parent, false);

            var vh = new ManualRecyclerViewHolder(itemView, OnClick/*, OnLongClick*/);
            return vh;
        }

        // Replace the contents of a view (invoked by the layout manager)
        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var item = items[position];

            // Replace the contents of the view with that element
            var holder = viewHolder as ManualRecyclerViewHolder;
            holder.TvTitle.Text = item.Title;
            holder.TvDescription.Text = item.Description;
            if (cached)
            {
                ImageService.Instance
                    .LoadUrl(item.ImageUrl)
                    .DownSampleInDip(80)
                    .FadeAnimation(false)
                    .Delay(0)
                    .WithCache(FFImageLoading.Cache.CacheType.All)
                    .LoadingPlaceholder("drawable/loading.png", FFImageLoading.Work.ImageSource.CompiledResource)
                    .Error((Exception ex) => { Analytics.TrackEvent("Image caching error", new Dictionary<string, string>() { { "exception", ex.Message }, { "Data", ex.Data.ToString() } }); })
                    .Into(holder.IvImage);
            }
        }

        public override int ItemCount => items.Length;        

        void OnClick(Context context, int position)
        {
            if (position >= 0)
            {
                this.context = context;
                this.position = position;
                var urlDownload = items[position].Url;
                string filename = urlDownload.Substring(urlDownload.LastIndexOf("/") + 1);
                ISaveAndLoadFiles saveFile = new SaveAndLoadFiles_Android();

                if (saveFile.FileExists(filename))
                {
                    new AlertDialog.Builder(context)
                            .SetTitle(Resource.String.app_name)
                            .SetMessage(context.GetString(Resource.String.download_manual_file_exists))
                            .SetIcon(Resource.Drawable.baseline_warning_24)
                            .SetPositiveButton(Resource.String.download_error_ok_button, DialogPositiveButtonClick)
                            .SetNegativeButton(Resource.String.download_error_cancel_button, DialogNegativeButtonClick)
                            .Show();
                }
                else
                {
                    StartDownload();
                }
            }
        }

        private void DialogNegativeButtonClick(object sender, DialogClickEventArgs dialogClickEventArgs)
        {
            // nothing to do here
        }

        private void DialogPositiveButtonClick(object sender, DialogClickEventArgs dialogClickEventArgs)
        {
            StartDownload();
        }

        private void StartDownload()
        {
            DownloadReturns downloadReturns = General.ExecuteDownload(context, items[position], false, true);
            string notificationMessage = "";
            switch (downloadReturns)
            {
                case DownloadReturns.NoInternetConection:
                    notificationMessage = context.GetString(Resource.String.download_error_no_internet_conection);
                    break;
                case DownloadReturns.NoWiFiFound:
                    notificationMessage = context.GetString(Resource.String.download_error_no_wifi_found);
                    break;
                case DownloadReturns.NoWritePermission:
                    notificationMessage = context.GetString(Resource.String.download_error_no_write_permission);
                    break;
            }
            if (!string.IsNullOrEmpty(notificationMessage))
            {
                Toast.MakeText(context, notificationMessage, ToastLength.Long).Show();
            }
        }

        //void OnLongClick(ManualRecylcerAdapterClickEventArgs args) => ItemLongClick?.Invoke(this, args);

        public void UpdateData(RssEnCaso[] rssEnCasos)
        {
            items = rssEnCasos.OrderByDescending(t => t.PubDate).ToArray();            
        }

        public void NotifyChanges()
        {
            NotifyDataSetChanged();
        }

        public async Task DownloadImages()
        {
            foreach (var imUrl in items.Select(x => x.ImageUrl).Distinct())
            {
                await ImageService.Instance
                    .LoadUrl(imUrl)
                    .DownSampleInDip(80)
                    .FadeAnimation(false)
                    .Delay(0)
                    .WithCache(FFImageLoading.Cache.CacheType.All)
                    .LoadingPlaceholder("drawable/loading.png", FFImageLoading.Work.ImageSource.CompiledResource)
                    .Error((Exception ex) => { Analytics.TrackEvent("Image caching error", new Dictionary<string, string>() { { "exception", ex.Message }, { "Data", ex.Data.ToString() } }); })
                    .DownloadOnlyAsync();
            }
            cached = true;
            NotifyChanges();
        }
    }

    public class ManualRecyclerViewHolder : RecyclerView.ViewHolder
    {
        public TextView TvTitle { get; set; }
        public TextView TvDescription { get; set; }

        public ImageView IvImage { get; set; }

        public ManualRecyclerViewHolder(View itemView, Action<Context, int> clickListener/*,*/
                            /*Action<ManualRecylcerAdapterClickEventArgs> longClickListener*/) : base(itemView)
        {
            TvTitle = itemView.FindViewById<TextView>(Resource.Id.manual_row_layout_title);
            TvDescription = itemView.FindViewById<TextView>(Resource.Id.manual_row_layout_description);
            IvImage = itemView.FindViewById<ImageView>(Resource.Id.manual_row_layout_image);

            itemView.Click += (sender, e) => clickListener(itemView.Context, AdapterPosition);
            //itemView.Click += (sender, e) => clickListener(new ManualRecylcerAdapterClickEventArgs { View = itemView, Position = AdapterPosition }, 1);
            //itemView.LongClick += (sender, e) => longClickListener(new ManualRecylcerAdapterClickEventArgs { View = itemView, Position = AdapterPosition });
        }
    }

    //public class ManualRecylcerAdapterClickEventArgs : EventArgs
    //{
    //    public View View { get; set; }
    //    public int Position { get; set; }
    //}

}