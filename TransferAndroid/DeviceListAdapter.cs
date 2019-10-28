using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Support.V7.Widget;
using Android.Views;
using Android.Widget;

using Common;

namespace TransferAndroid.Resources.layout
{
    internal class DeviceListAdapter : RecyclerView.Adapter
    {
        List<Device> devices;

        public DeviceListAdapter(List<Device> devices)
        {
            this.devices = devices;
        }

        public override void OnBindViewHolder(RecyclerView.ViewHolder holder, int position)
        {
            DeviceListAdapterViewHolder vh = holder as DeviceListAdapterViewHolder;

            vh.DeviceName.Text = devices[position].Name;
            vh.DeviceLastAddr.Text = devices[position].Addr;
        }

        public override RecyclerView.ViewHolder OnCreateViewHolder(ViewGroup parent, int viewType)
        {
            View itemView = LayoutInflater.From(parent.Context).
                Inflate(Resource.Layout.view_device_item, parent, false);

            // Create a ViewHolder to hold view references inside the CardView:
            DeviceListAdapterViewHolder vh = new DeviceListAdapterViewHolder(itemView, OnLongClick);
            return vh;
        }

        public override int ItemCount => devices.Count;

        public event EventHandler<int> ItemLongClick;

        void OnLongClick(int position)
        {
            ItemLongClick?.Invoke(this, position);
        }
    }

    internal class DeviceListAdapterViewHolder : RecyclerView.ViewHolder
    {
        //Your adapter views to re-use
        //public TextView Title { get; set; }
        public TextView DeviceName { get; private set; }
        public TextView DeviceLastAddr { get; private set; }
        public DeviceListAdapterViewHolder(View view, Action<int> listener) : base(view)
        {
            DeviceName = view.FindViewById<TextView>(Resource.Id.device_item_name);
            DeviceLastAddr = view.FindViewById<TextView>(Resource.Id.device_item_addr);

            view.LongClick += (sender, e) => listener(base.LayoutPosition);
        }
    }
}