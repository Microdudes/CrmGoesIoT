using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Newtonsoft.Json;

// The Blank Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace ArtikelCRMgoesIoT
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        private bool activated = false;
        MFRC522.Init mfrc522 = new MFRC522.Init();
        private DispatcherTimer commandsTimer;
        CultureInfo ci = new CultureInfo("de-DE");

        public MainPage()
        {
            this.InitializeComponent();          
            BusyTxt.Text = "Warte auf Teilnehmer...";
        }             

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await RegisterDevice();
            await mfrc522.ConfigureSPI();          
            mfrc522.OnCardRead += Mfrc522_OnCardRead;
            mfrc522.ConfigureTimer(!activated);            
        }

        private async Task RegisterDevice()
        {
            dynamic requestObj = new ExpandoObject();
            requestObj.room = "Kepler";
            dynamic dynObj = await SendRequest("http://crm2016iotdemo.cloudapp.net/Training/Room", requestObj);
            TrainingHeader.Text = dynObj.Name;
            TrainerTxt.Text = $"Trainer: {dynObj.Trainer[0]}";
            DateTime startTime = dynObj.Start;
            DateTime endTime = dynObj.End;
            StartTxt.Text = $"Start: {startTime.ToString("d", ci)}";
            EndTxt.Text = $"Ende: {endTime.ToString("d", ci)}";
            DescriptionTxt.Text = dynObj.Description;
        }

        private async Task<dynamic> SendRequest(string url, dynamic dynJson)
        {
            HttpClient client = new HttpClient();
            client.BaseAddress = new Uri(url);
            string json = "";
            json = JsonConvert.SerializeObject(dynJson);
            HttpResponseMessage respon = await client.PostAsync(client.BaseAddress, new StringContent(json, Encoding.UTF8, "application/json"));
            string responJsonText = await respon.Content.ReadAsStringAsync();
            dynamic dynObj = JsonConvert.DeserializeObject(responJsonText);
            return dynObj;
        }

        private async Task RegisterUser(string serial)
        {
            dynamic requestObj = new ExpandoObject();
            requestObj.room = "Kepler";
            requestObj.tagId = serial;
            dynamic dynObj = await SendRequest("http://crm2016iotdemo.cloudapp.net/Training/Attend", requestObj);
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
            () =>
            {
                UserNameTxt.Text = $"{dynObj.Attendee.Firstname} {dynObj.Attendee.Lastname}";
                bool success = dynObj.Success;
                if (success)
                {
                    StatusTxt.Text = "Nimmt teil";
                }
            });
        }

        private async void Mfrc522_OnCardRead(object myObject, MFRC522.MFRArgs myArgs)
        {
            mfrc522.ConfigureTimer(false);         
            try
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    BusyTxt.Text = "Prüfe Teilnehmer...";
                });
                await RegisterUser(myArgs.Message);
                        
                mfrc522.ConfigureTimer(true);
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    BusyTxt.Text = "Warte auf Teilnehmer...";
                });
            }
            catch (Exception k)
            {
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(CoreDispatcherPriority.Normal,
                () =>
                {
                    BusyTxt.Text = "Fehler bei der Erkennung";
                });
            }

        }
    }
}
