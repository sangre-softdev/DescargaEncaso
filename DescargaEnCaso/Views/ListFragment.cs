using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.Media;
using Android.OS;
using Android.Provider;
using Android.Runtime;
using Android.Support.V4.Widget;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;
using EnCasoShared;
using EnCasoShared.Model;
using FFImageLoading;
using Java.IO;
using MediaManager;
using MediaManager.Media;
using Microsoft.AppCenter.Analytics;

namespace DescargaEnCaso.Views
{
    public class ListFragment : Android.Support.V4.App.Fragment
    {
        EnCasoFile[] enCasoFiles = { };
        SwipeRefreshLayout swipeRefreshLayout;
        ListRecyclerAdapter listRecyclerAdapter;
        RecyclerView recyclerView;
        
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);           
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.en_caso_list_fragment, container, false);

            swipeRefreshLayout = view.FindViewById<SwipeRefreshLayout>(Resource.Id.encaso_list_swipe);
            swipeRefreshLayout.Refresh += SwipeRefreshLayout_Refresh;

            recyclerView = view.FindViewById<RecyclerView>(Resource.Id.encaso_list_recyclerview);
            LinearLayoutManager linearLayoutManager = new LinearLayoutManager(Context);
            recyclerView.SetLayoutManager(linearLayoutManager);
            listRecyclerAdapter = new ListRecyclerAdapter(enCasoFiles);
            recyclerView.SetAdapter(listRecyclerAdapter);
            recyclerView.AddOnScrollListener(new CustomScrollListener());
            recyclerView.HasFixedSize = true;
            UpdateList();
            return view;
        }

        private void SwipeRefreshLayout_Refresh(object sender, EventArgs e)
        {
            UpdateList();
        }

        public void UpdateList()
        {
            ISaveAndLoadFiles saveFile = new SaveAndLoadFiles_Android();
            Loading(true);
            enCasoFiles = LocalDatabase<EnCasoFile>.GetEnCasoFileDb().GetAll();
            SaveAndLoadFiles_Android saveAndLoadFiles_Android = new SaveAndLoadFiles_Android();
            foreach (EnCasoFile enCasoFile in enCasoFiles)
            {
                if (!saveAndLoadFiles_Android.FileExists(enCasoFile.SavedFile))
                {
                    LocalDatabase<EnCasoFile>.GetEnCasoFileDb().Delete(enCasoFile);                    
                }
            }
            enCasoFiles = LocalDatabase<EnCasoFile>.GetEnCasoFileDb().GetAll().OrderByDescending(t => t.PubDate).ToArray();            
            listRecyclerAdapter.UpdateData(enCasoFiles);
            Loading(false);
        }

        public void Loading(bool loading)
        {
            if (swipeRefreshLayout != null)
                swipeRefreshLayout.Refreshing = loading;
        }
    }

    public class ListRecyclerAdapter : RecyclerView.Adapter
    {
        EnCasoFile[] items;
        bool cached = false;

        public ListRecyclerAdapter(EnCasoFile[] data)
        {
            items = data;
            _ = DownloadImages();
        }

        public override int ItemCount => items.Length;

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            //Setup your layout here
            View itemView = null;
            itemView = LayoutInflater.From(parent.Context).Inflate(Resource.Layout.manual_row_layout, parent, false);

            var vh = new ListRecyclerViewHolder(itemView, OnClick);
            return vh;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder viewHolder, int position)
        {
            var item = items[position];

            // Replace the contents of the view with that element
            var holder = viewHolder as ListRecyclerViewHolder;
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

        void OnClick(Context context, int position)
        {
            CrossMediaManager.Current.Play(new MediaItem(items[position].SavedFile));
        }

        public void UpdateData(EnCasoFile[] enCasoFiles)
        {
            items = enCasoFiles.OrderByDescending(t => t.PubDate).ToArray();
            NotifyDataSetChanged();
            _ = DownloadImages();
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
            NotifyDataSetChanged();
        }
    }

    public class ListRecyclerViewHolder : RecyclerView.ViewHolder
    {
        public TextView TvTitle { get; set; }
        public TextView TvDescription { get; set; }

        public ImageView IvImage { get; set; }

        public ListRecyclerViewHolder(View itemView, Action<Context, int> clickListener) : base(itemView)
        {
            TvTitle = itemView.FindViewById<TextView>(Resource.Id.manual_row_layout_title);
            TvDescription = itemView.FindViewById<TextView>(Resource.Id.manual_row_layout_description);
            IvImage = itemView.FindViewById<ImageView>(Resource.Id.manual_row_layout_image);

            itemView.Click += (sender, e) => clickListener(itemView.Context, AdapterPosition);
        }
    }
}