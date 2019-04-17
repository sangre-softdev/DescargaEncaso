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
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;
using EnCasoShared;
using EnCasoShared.Model;
using Microsoft.AppCenter.Analytics;

namespace DescargaEnCaso.Views
{
    public class ManualFragment : Android.Support.V4.App.Fragment
    {
        RecyclerView recyclerView;
        ManualRecyclerAdapter manualRecyclerAdapter;
        TextView tvNoInternet;
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
            
            recyclerView = view.FindViewById<RecyclerView>(Resource.Id.encaso_list_recyclerview);
            LinearLayoutManager linearLayoutManager = new LinearLayoutManager(Context);
            recyclerView.SetLayoutManager(linearLayoutManager);

            rssEnCaso = LocalDatabase<RssEnCaso>.getRssEnCasoDb().GetAll();

            manualRecyclerAdapter = new ManualRecyclerAdapter(rssEnCaso);
            recyclerView.SetAdapter(manualRecyclerAdapter);
            return view;
        }
        
        public async Task UpdateRss()
        {
            try
            {
                DateTime dateTime = DateTime.ParseExact(
                        prefs.GetString(General.MANUAL_LAST_SEARCH, "1984-10-12 14:00:00"),
                        "yyyy-MM-dd HH:mm:ss",
                        System.Globalization.CultureInfo.InvariantCulture);
                
                if (dateTime.Date < DateTime.Now)
                {
                    RssEnCaso[] rssEnCasoI = await RssEnCaso.GetRssEnCasoAsync(false);

                    ISharedPreferencesEditor editor = prefs.Edit();
                    editor.PutString(General.MANUAL_LAST_SEARCH, DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"));
                    editor.Apply();

                    RssEnCaso[] insertRecords = rssEnCasoI.Except(rssEnCaso, new RssEnCaso()).ToArray();
                    foreach (RssEnCaso ins in insertRecords)
                    {
                        LocalDatabase<RssEnCaso>.getRssEnCasoDb().Save(ins);
                    }

                    RssEnCaso[] deleteRecords = rssEnCaso.Except(rssEnCasoI, new RssEnCaso()).ToArray();
                    foreach (RssEnCaso del in deleteRecords)
                    {
                        LocalDatabase<RssEnCaso>.getRssEnCasoDb().Delete(del);
                    }

                    tvNoInternet.Visibility = ViewStates.Gone;
                    manualRecyclerAdapter.UpdateData(rssEnCasoI);
                }                
            }
            catch (WebException we)
            {
                tvNoInternet.Visibility = ViewStates.Visible;
                Analytics.TrackEvent(we.Message);
            }
        }
    }

    public class ManualRecyclerAdapter : RecyclerView.Adapter
    {
        //public event EventHandler<ManualRecylcerAdapterClickEventArgs> ItemClick;
        //public event EventHandler<ManualRecylcerAdapterClickEventArgs> ItemLongClick;
        RssEnCaso[] items;

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
        }

        public override int ItemCount => items.Length;

        async void OnClick(Context context, int position)
        {
            DownloadReturns downloadReturns = await General.ExecuteDownload(context, items[position], false);
            string notificationMessage = "";
            switch (downloadReturns)
            {
                case DownloadReturns.InsufficientSpace:
                    notificationMessage = context.GetString(Resource.String.download_error_insufficient_space);
                    break;
                case DownloadReturns.NoInternetConection:
                    notificationMessage = context.GetString(Resource.String.download_error_no_internet_conection);
                    break;
                case DownloadReturns.NoWiFiFound:
                    notificationMessage = context.GetString(Resource.String.download_error_no_wifi_found);
                    break;
                case DownloadReturns.NoWritePermission:
                    notificationMessage = context.GetString(Resource.String.download_error_no_write_permission);
                    break;
                case DownloadReturns.NoIdea:
                    notificationMessage = context.GetString(Resource.String.download_error_no_idea);
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
            items = rssEnCasos;            
            NotifyDataSetChanged();
        }       
    }

    public class ManualRecyclerViewHolder : RecyclerView.ViewHolder
    {
        public TextView TvTitle { get; set; }
        public TextView TvDescription { get; set; }

        public ManualRecyclerViewHolder(View itemView, Action<Context, int> clickListener/*,*/
                            /*Action<ManualRecylcerAdapterClickEventArgs> longClickListener*/) : base(itemView)
        {
            TvTitle = itemView.FindViewById<TextView>(Resource.Id.manual_row_layout_title);
            TvDescription = itemView.FindViewById<TextView>(Resource.Id.manual_row_layout_description);

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