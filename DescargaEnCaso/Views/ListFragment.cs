using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Util;
using Android.Views;
using Android.Widget;
using EnCasoShared;
using EnCasoShared.Model;

namespace DescargaEnCaso.Views
{
    public class ListFragment : Android.Support.V4.App.Fragment
    {
        EnCasoFile[] enCasoFiles = { };

        ListRecyclerAdapter listRecyclerAdapter;
        RecyclerView recyclerView;
        
        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            //enCasoFiles = DataAccesObject.GetAllEnCasoFile();

        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            View view = inflater.Inflate(Resource.Layout.en_caso_list_fragment, container, false);

            recyclerView = view.FindViewById<RecyclerView>(Resource.Id.encaso_list_recyclerview);
            LinearLayoutManager linearLayoutManager = new LinearLayoutManager(Context);
            recyclerView.SetLayoutManager(linearLayoutManager);

            listRecyclerAdapter = new ListRecyclerAdapter(enCasoFiles);
            recyclerView.SetAdapter(listRecyclerAdapter);

            return view;
        }
    }

    public class ListRecyclerAdapter : RecyclerView.Adapter
    {
        EnCasoFile[] items;

        public ListRecyclerAdapter(EnCasoFile[] data)
        {
            items = data;
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
        }

        void OnClick(Context context, int position)
        {
            Toast.MakeText(context, items[position].Title, ToastLength.Long).Show();            
        }

        public void UpdateData(EnCasoFile[] enCasoFiles)
        {
            items = enCasoFiles;
            NotifyDataSetChanged();
        }
    }

    public class ListRecyclerViewHolder : RecyclerView.ViewHolder
    {
        public TextView TvTitle { get; set; }
        public TextView TvDescription { get; set; }

        public ListRecyclerViewHolder(View itemView, Action<Context, int> clickListener) : base(itemView)
        {
            TvTitle = itemView.FindViewById<TextView>(Resource.Id.manual_row_layout_title);
            TvDescription = itemView.FindViewById<TextView>(Resource.Id.manual_row_layout_description);

            itemView.Click += (sender, e) => clickListener(itemView.Context, AdapterPosition);
        }
    }
}