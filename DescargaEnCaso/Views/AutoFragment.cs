using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using Android.App;
using Android.Content;
using Android.OS;
using Android.Preferences;
using Android.Runtime;
using Android.Util;
using Android.Views;
using Android.Widget;
using EnCasoShared;

namespace DescargaEnCaso.Views
{
    public class AutoFragment : Android.Support.V4.App.Fragment
    {
        private TimePicker tp;
        private CheckBox onlyWiFi;
        private Button setAlarm;
        ISharedPreferences prefs;

        public override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);            
            prefs = PreferenceManager.GetDefaultSharedPreferences(Context);
        }

        public override View OnCreateView(LayoutInflater inflater, ViewGroup container, Bundle savedInstanceState)
        {
            bool alarmActive = prefs.GetBoolean(General.ALARM_ACTIVE, false);
            int hour = prefs.GetInt(General.ALARM_HOUR, DateTime.Now.Hour);
            int minute = prefs.GetInt(General.ALARM_MINUTE, DateTime.Now.Minute);
            bool isWiFi = prefs.GetBoolean(General.ALARM_ONLY_WIFI, true);
            
            View vw = inflater.Inflate(Resource.Layout.auto_fragment, container, false);
            setAlarm = vw.FindViewById<Button>(Resource.Id.auto_btn_set_alarm);
            setAlarm.Click += SetAlarm_Click; ;

            tp = vw.FindViewById<TimePicker>(Resource.Id.auto_time_picker);
            onlyWiFi = vw.FindViewById<CheckBox>(Resource.Id.auto_check_only_wiFi);
            onlyWiFi.Checked = isWiFi;
            onlyWiFi.CheckedChange += OnlyWiFi_CheckedChange;

            if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
            {
                tp.Hour = hour;
                tp.Minute = minute;
            }
            else
            {
                tp.CurrentHour = new Java.Lang.Integer(hour);
                tp.CurrentMinute = new Java.Lang.Integer(minute);
            }

            SetActiveAlarmText(alarmActive);

            return vw;
        }

        private void SetAlarm_Click(object sender, EventArgs e)
        {
            Button setButton = (Button)sender;
            if (setButton.Text == Resources.GetString(Resource.String.auto_btn_set_alarm))
            {
                int hour, minute;
                if (Build.VERSION.SdkInt >= BuildVersionCodes.M)
                {
                    hour = tp.Hour;
                    minute = tp.Minute;
                }
                else
                {
                    hour = (int)tp.CurrentHour;
                    minute = (int)tp.CurrentMinute;
                }

                ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(Context);
                ISharedPreferencesEditor editor = prefs.Edit();
                editor.PutInt(General.ALARM_HOUR, hour);
                editor.PutInt(General.ALARM_MINUTE, minute);
                editor.PutBoolean(General.ALARM_ACTIVE, true);
                editor.Apply();

                SetActiveAlarmText(true);

                General.ProgramNextAlarm(Context);

                Toast.MakeText(Context, Resource.String.auto_toast_alarm_set, ToastLength.Short).Show();
            }
            else
            {
                Intent i = new Intent(this.Context, typeof(AlarmBroadcastReceiver));                
                Android.App.PendingIntent pi = Android.App.PendingIntent.GetBroadcast(Context, 1, i, 0);
                Android.App.AlarmManager alarmManager = (Android.App.AlarmManager)Context.GetSystemService(Context.AlarmService);

                // La cancela
                alarmManager.Cancel(pi);
                pi.Cancel();
                
                ISharedPreferences prefs = PreferenceManager.GetDefaultSharedPreferences(Context);
                ISharedPreferencesEditor editor = prefs.Edit();
                editor.PutBoolean(General.ALARM_ACTIVE, false);
                editor.Apply();
                SetActiveAlarmText(false);
            }
        }

        private void OnlyWiFi_CheckedChange(object sender, CompoundButton.CheckedChangeEventArgs e)
        {
            var editor = prefs.Edit();
            editor.PutBoolean(General.ALARM_ONLY_WIFI, onlyWiFi.Checked);
            editor.Apply();
        }

        private void SetActiveAlarmText(bool isAlarmActive)
        {
            setAlarm.Text = isAlarmActive ? Resources.GetString(Resource.String.auto_btn_remove_alarm) : Resources.GetString(Resource.String.auto_btn_set_alarm);
            tp.Enabled = !isAlarmActive;
        }
    }
}