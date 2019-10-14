﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using Common;

namespace TransferAndroid
{
    [Service]
    public class SnRService : Service
    {
        private Sender sender;
        private Receiver receiver;

        bool receiverOccupied = false;
        readonly CancellationTokenSource receiverWorker = new CancellationTokenSource();
        public async void ActivateListening()
        {
            if (receiverOccupied)
            {
                receiverWorker.Cancel();
            }
            receiverOccupied = true;
            await Task.Run(receiver.ActivateListening,receiverWorker.Token);
        }
        public async void StartReceiving()
        {
            if (receiverOccupied)
            {
                receiverWorker.Cancel();
            }
            receiverOccupied = true;
            await Task.Run(receiver.ActivateReceiver,receiverWorker.Token);
        }
        public async void LookForDevice()
        {
            //sender.AddDeviceFromScanning();
#nullable enable
            Device? device = await Task.Run(sender.AddDeviceFromScanning);
#nullable disable

        }
        public async void TestService()
        {
            await Task.Delay(5000);
            //return "Hello from service.";
            Toast.MakeText(this, "wait ok", ToastLength.Long).Show();
        }

        public IBinder Binder { get; private set; }
        public override void OnCreate()
        {
            base.OnCreate();
            sender = new Sender();
            receiver = new Receiver();
            sender.OnAndroidDevice();
        }

        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            return StartCommandResult.Sticky;
        }
        public override IBinder OnBind(Intent intent)
        {
            Binder = new SnRBinder(this);
            return Binder;
            //return null;
        }
    }

    public class SnRBinder : Binder
    {
        public SnRService SnRService { get; private set; }

        public SnRBinder(SnRService snRService)
        {
            SnRService = snRService;
        }
    }
    public class SnRServiceConnection : Java.Lang.Object, IServiceConnection
    {
        IUsingSnRServiceActivity activity; // May modify when other activities needed
        public SnRServiceConnection(IUsingSnRServiceActivity activity)
        {
            this.activity = activity;
        }
        public void OnServiceConnected(ComponentName name, IBinder service)
        {
            activity.SnRBinder = service as SnRBinder;
        }

        public void OnServiceDisconnected(ComponentName name)
        {
            activity.SnRBinder = null;
        }
    }
}