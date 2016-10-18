﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Newtonsoft.Json.Linq;

// “空白页”项模板在 http://go.microsoft.com/fwlink/?LinkId=234238 上有介绍

namespace bilibili2.Pages
{
    enum ChatsType
    {
        New,
        Old
    }
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class ChatPage : Page,INotifyPropertyChanged
    {
        public delegate void GoBackHandler();
        public event GoBackHandler BackEvent;
        public event PropertyChangedEventHandler PropertyChanged;

        public ChatPage()
        {
            this.InitializeComponent();
            this.NavigationCacheMode = NavigationCacheMode.Required;
        }

        //private void btn_back_Click(object sender, RoutedEventArgs e)
        //{
        //    timer.Stop();
        //    if (this.Frame.CanGoBack)
        //    {
        //        this.Frame.GoBack();
              
        //    }
        //    else
        //    {
        //        BackEvent();
        //    }
        //}


        DispatcherTimer timer;
        protected async override void OnNavigatedTo(NavigationEventArgs e)
        {
            bg.Color = ((SolidColorBrush)this.Frame.Tag).Color;
            messages = new ObservableCollection<ChatModel>();
            timer = new DispatcherTimer();
            timer.Interval = new TimeSpan(0, 0, 3);
            timer.Tick += Timer_Tick;

            list_view.ItemsSource = messages;
            await Task.Delay(200);
            object[] ob = e.Parameter as object[];
            switch ((ChatsType)ob[1])
            {
                case ChatsType.New:
                    pr_Load.Visibility = Visibility.Visible;
                    await CreateRoom(ob[0].ToString());
                    if (room!=null)
                    {
                       await GetRoomMessage(room.rid);
                    }
                    timer.Start();
                    pr_Load.Visibility = Visibility.Collapsed;
                    break;
                case ChatsType.Old:
                    pr_Load.Visibility = Visibility.Visible;
                        room = new CreateRoomModel() { rid = ob[0].ToString() };
                        await GetRoomMessage(room.rid);
                    pr_Load.Visibility = Visibility.Collapsed;
                    timer.Start();
                    break;
                default:
                    break;
            }
        }

        private async void Timer_Tick(object sender, object e)
        {
            await GetRoomMessage(room.rid);
        }

        CreateRoomModel room;
        WebClientClass wc;
        private async Task CreateRoom(string mid)
        {
            try
            {
                wc = new WebClientClass();
                string url =string.Format("http://message.bilibili.com/api/msg/query.double.room.do?access_key={0}&actionKey=appkey&appkey={1}&build=422000&data_type=1&mobi_app=android&platform=android&mid={2}&ts={3}000",ApiHelper.access_key,ApiHelper._appKey_Android, mid,ApiHelper.GetTimeSpen);
                url += "&sign=" + ApiHelper.GetSign_Android(url);
                string results = await wc.GetResults(new Uri(url));
                CreateRoomModel m = JsonConvert.DeserializeObject<CreateRoomModel>(results);
                if (m.code==0)
                {
                    top_txt_Header.Text = m.data.room_name;
                    room = m.data;
                }
                else
                {
                    messShow.Show("创建聊天失败",2000);
                }
            }
            catch (Exception ex)
            {
                messShow.Show("创建聊天失败\r\n"+ex.Message, 2000);
                //throw;
            }
        }
        private void RaisePropertChanged(string pName)
        {
            if (PropertyChanged!=null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(pName));
            }
        }
        private ObservableCollection<ChatModel> _messages;
        public ObservableCollection<ChatModel> messages
        {
            get { return _messages; }
            set { _messages = value; RaisePropertChanged("messages"); }
        }
        private async Task GetRoomMessage(string rid)
        {
            try
            {
                wc = new WebClientClass();
                string url = string.Format("http://message.bilibili.com/api/msg/query.msg.list.do?access_key={0}&actionKey=appkey&appkey={1}&build=422000&data_type=1&mobi_app=android&platform=android&rid={2}&ts={3}000", ApiHelper.access_key, ApiHelper._appKey_Android, rid, ApiHelper.GetTimeSpen);
                url += "&sign=" + ApiHelper.GetSign_Android(url);
                string results = await wc.GetResults(new Uri(url));
                ChatModel m = JsonConvert.DeserializeObject<ChatModel>(results);
                m.data = m.data.OrderBy(x => x.send_time).ToList();
                foreach (var item in m.data)
                {
                    if (messages.Where(x=>x.id==item.id).ToList().Count==0)
                    {

                        messages.Add(item);
                        if (item.is_me==2)
                        {
                            top_txt_Header.Text = item.uname;
                        }
                      
                    }
                }

                //RaisePropertChanged("messages");
            }
            catch (Exception ex)
            {
                messShow.Show("读取信息失败\r\n" + ex.Message, 2000);
                //throw;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            txt_Content.Text += ((Button)sender).Content.ToString();
        }

        private void btn_Send_Click(object sender, RoutedEventArgs e)
        {
            if (txt_Content.Text.Length==0)
            {
                messShow.Show("内容不能为空",2000);
                return;
            }
            SendMessage(room.rid);
        }

        private async void SendMessage(string rid)
        {
            try
            {
                wc = new WebClientClass();
                string url = "http://message.bilibili.com/api/msg/send.msg.do";
               // url += "&sign=" + ApiHelper.GetSign_Android(url);
                string results = await wc.PostResults(new Uri(url), string.Format("access_key={0}&actionKey=appkey&appkey={1}&build=422000&data_type=1&mobi_app=android&platform=android&rid={2}&ts={3}000&msg={4}", ApiHelper.access_key, ApiHelper._appKey_Android, rid, ApiHelper.GetTimeSpen, Uri.EscapeDataString(txt_Content.Text)));
                JObject o = JObject.Parse(results);
                //ChatModel m = JsonConvert.DeserializeObject<ChatModel>(results);
                if ((int)o["code"]!=0)
                {
                    messShow.Show("发送失败,"+ o["message"].ToString(),2000);
                }
                else
                {
                    txt_Content.Text = "";
                }
                //RaisePropertChanged("messages");
            }
            catch (Exception ex)
            {
                messShow.Show("发送失败\r\n" + ex.Message, 2000);
                //throw;
            }
        }

        private void list_view_ItemClick(object sender, ItemClickEventArgs e)
        {
            Windows.ApplicationModel.DataTransfer.DataPackage pack = new Windows.ApplicationModel.DataTransfer.DataPackage();
            pack.SetText((e.ClickedItem as ChatModel).message);
            Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(pack); // 保存 DataPackage 对象到剪切板
            Windows.ApplicationModel.DataTransfer.Clipboard.Flush();
            messShow.Show("已将内容复制到剪切板", 3000);
        }
    }
    public class MessageItemDataTemplateSelector : DataTemplateSelector
    {
        protected override DataTemplate SelectTemplateCore(object item, DependencyObject container)
        {
            if ((item as ChatModel).is_me==2)
            {
                return App.Current.Resources["Chat1"] as DataTemplate;
            }
            else
            {
                return App.Current.Resources["Chat2"] as DataTemplate;
            }
        }
    }

    public class CreateRoomModel
    {
        public int code { get; set; }
        public CreateRoomModel data { get; set; }
        public string avatar_url { get; set; }
        public string mid { get; set; }
        public string rid { get; set; }
        public string room_name { get; set; }
        public int status { get; set; }

        public long ts { get; set; }
    }
    public class ChatModel
    {
        public int code { get; set; }
        public List<ChatModel> data { get; set; }
        public string message { get; set; }


        public int id { get; set; }
        public string mid { get; set; }
        public string uname { get; set; }
        public string avatar_url { get; set; }

        public int is_me { get; set; }
        public string cursor { get; set; }
        public long send_time { get; set; }

        public string Send_time
        {
            get
            {
                DateTime dtStart = new DateTime(1970, 1, 1);
                long lTime = long.Parse(send_time + "0000");
                //long lTime = long.Parse(textBox1.Text);
                TimeSpan toNow = new TimeSpan(lTime);
                DateTime dt = dtStart.Add(toNow).ToLocalTime();
                TimeSpan span = DateTime.Now - dt;
                return dt.ToString();
                //if (span.TotalDays > 7)
               // {
                    //return dt.ToString("MM-dd");
                //}
                //else
                //if (span.TotalDays > 1)
                //{
                //    return string.Format("{0}天前", (int)Math.Floor(span.TotalDays));
                //}
                //else
                //if (span.TotalHours > 1)
                //{
                //    return string.Format("{0}小时前", (int)Math.Floor(span.TotalHours));
                //}
                //else
                //if (span.TotalMinutes > 1)
                //{
                //    return string.Format("{0}分钟前", (int)Math.Floor(span.TotalMinutes));
                //}
                //else
                //if (span.TotalSeconds >= 1)
                //{
                //    return string.Format("{0}秒前", (int)Math.Floor(span.TotalSeconds));
                //}
                //else
                //{
                //    return "1秒前";
                //}

            }
        }
    }
}
