using System;
using System.Collections.Generic;
using System.IO;
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

namespace TransferAndroid
{
    [Activity(Label = "DeviceListActivity")]
    public class DeviceListActivity : Activity
    {
        RecyclerView devicelist;
        RecyclerView.LayoutManager dlLayoutManager;
        Resources.layout.DeviceListAdapter deviceListAdapter;
        List<Device> devices;
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Sender s = new Sender();
            Receiver r = new Receiver();
            
            r.OnAndroidDevice();
            devices = s.ReadDevices();
            devices.Add(new Device { Name = "test1", Addr = "127.0.0.1" });
            devices.Add(new Device { Name = "test2", Addr = "127.0.0.1" });
            devices.Add(new Device { Name = "test3", Addr = "127.0.0.1" });
            devices.Add(new Device { Name = "test4", Addr = "127.0.0.1" });
            devices.Add(new Device { Name = "test5", Addr = "127.0.0.1" });
            devices.Add(new Device { Name = "test6", Addr = "127.0.0.1" });
            devices.Add(new Device { Name = "test7", Addr = "127.0.0.1" });
            devices.Add(new Device { Name = "test8", Addr = "127.0.0.1" });
            devices.Add(new Device { Name = "test9", Addr = "127.0.0.1" });
            devices.Add(new Device { Name = "test10", Addr = "127.0.0.1" });
            devices.Add(new Device { Name = "test11", Addr = "127.0.0.1" });
            devices.Add(new Device { Name = "test12", Addr = "127.0.0.1" });
            devices.Add(new Device { Name = "test13", Addr = "127.0.0.1" });


            SetContentView(Resource.Layout.app_bar_device_list);
            
            devicelist = FindViewById<RecyclerView>(Resource.Id.device_list);
            dlLayoutManager = new LinearLayoutManager(this);
            devicelist.SetLayoutManager(dlLayoutManager);
            
            deviceListAdapter = new Resources.layout.DeviceListAdapter(devices);
            deviceListAdapter.ItemLongClick += OnItemLongClick;
            devicelist.SetAdapter(deviceListAdapter);
        }
        void OnItemLongClick(object sender, int position)
        {
            // Long press to manage device
            int photoNum = position + 1;
            Toast.MakeText(this, "This is device number " + photoNum, ToastLength.Short).Show();
        }
    }
}