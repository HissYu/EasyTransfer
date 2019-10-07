using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        public void ActivateListening()
        {
            throw new NotImplementedException();
        }
        public string GetString()
        {
            return "Hello from service.";
        }

        public IBinder Binder { get; private set; }
        public override void OnCreate()
        {
            base.OnCreate();
            sender = new Sender();
            receiver = new Receiver();
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