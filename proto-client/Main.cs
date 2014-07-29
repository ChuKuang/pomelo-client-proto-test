using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Pomelo.DotNetClient;
using SimpleJson;
using System;
using System.IO;
using System.Threading;
using System.Windows.Forms;

public delegate string GetStringDelegate();

namespace Pomelo_NativeSocket
{

    public partial class Main : Form
    {

        private string _gate_server_ip;
        private int _gate_server_port;

        public static JsonObject _users = null;
        public static PomeloClient _pomelo = null;

        public Main()
        {
             InitializeComponent();
            //AppendLog("Main Thread:" + Thread.CurrentThread.ManagedThreadId);
        }


        /// <summary>
        /// 请求服务器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_send_Click(object sender, EventArgs e)
        {
            request();
        }

        /// <summary>
        /// 登陆服务器
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void btn_connect_Click(object sender, EventArgs e)
        {
            _gate_server_ip = tb_ip.Text;
            if (string.IsNullOrEmpty(_gate_server_ip))
            {
                _gate_server_ip = "192.168.0.156";
            }
            string port = tb_port.Text;
            if (string.IsNullOrEmpty(port))
            {
                port = "3014";
            }
            _gate_server_port = Convert.ToInt32(port);
            LoginGateServer(tb_name.Text);
        }

        /// <summary>
        /// 显示日志到界面上
        /// </summary>
        /// <param name="log"></param>
        private void AppendLog(string log)
        {
            if (tb_info.InvokeRequired)
            {
                Action<string> d = AppendLog;
                this.Invoke(d, log);
            }
            else
            {
                tb_info.AppendText(log + "\n");
                tb_info.Focus();
               // Console.WriteLine(log);
            }
        }
        
        /// <summary>
        /// 格式化Json
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        private string FormatJson(string json)
        {
            try
            {
                dynamic parsedJson = JsonConvert.DeserializeObject(json);
                return JsonConvert.SerializeObject(parsedJson, Formatting.Indented);
            }
            catch (Exception e)
            {
                MessageBox.Show("非法json格式：" + json);
                return "";
            }
        }

        /// <summary>
        /// 连接gate服务器
        /// </summary>
        /// <param name="userName"></param>
        void LoginGateServer(string userName)
        {
            Console.WriteLine("开始连接 gate server  " + _gate_server_ip + ":" + _gate_server_port);
            _pomelo = new PomeloClient(_gate_server_ip, _gate_server_port);

            _pomelo.connect(null, (data) =>
            {
                if (Convert.ToInt32(data["code"]) == 200)
                {
                    Console.WriteLine("成功连接 gate server :\n" + FormatJson(data.ToString()));
                    JsonObject msg = new JsonObject();
                    msg["uid"] = userName;
                    _pomelo.request("gate.gateHandler.queryEntry", msg, LoginGateServerCallback);
                }
                else
                {
                    AppendLog("oh shit...连接 gate 出错..");
                }
            });
        }

        void LoginGateServerCallback(JsonObject result)
        {
            if (Convert.ToInt32(result["code"]) == 200)
            {
                _pomelo.disconnect();
                LoginConnectorServer(result);
            }
            else
            {
                AppendLog("oh shit... 请求 connector 出错");
            }
        }

        /// <summary>
        /// 连接connector服务器
        /// </summary>
        /// <param name="result"></param>
        void LoginConnectorServer(JsonObject result)
        {

            string host = (string)result["host"];
            int port = Convert.ToInt32(result["port"]);

            Console.WriteLine("Connector Server 分配成功,开始连接：" + host + ":" + port);

            _pomelo = new PomeloClient(host, port);

            _pomelo.connect(null, (data) =>
            {
                Console.WriteLine("成功连接 connector server:\n " + FormatJson(data.ToString()));
                JoinChannel(tb_name.Text, tb_channel.Text);
            });
        }

        /// <summary>
        /// 加入频道
        /// </summary>
        /// <param name="userName"></param>
        /// <param name="channel"></param>
        void JoinChannel(string userName, string channel)
        {
            JsonObject userMessage = new JsonObject();
            userMessage.Add("uid", userName);
            //userMessage.Add("rid", channel);

            if (_pomelo != null)
            {
                //请求加入聊天室
                _pomelo.request("connector.entryHandler.entry", userMessage, (data) =>
                {
                    AppendLog("进入 channel:\n" + FormatJson(data.ToString()));
                });
            }
        }

        /// <summary>
        /// 发送聊天请求
        /// </summary>
        /// <param name="target"></param>
        /// <param name="content"></param>
        void request()
        {
            string route = tb_route.Text;

            string data_json = tb_data.Text;
            JsonObject msg = null;

            if (!String.IsNullOrEmpty(data_json))
            {
                msg = SimpleJson.SimpleJson.DeserializeObject<JsonObject>(data_json.Trim());
            }

            if (msg == null)
            {
                msg = new JsonObject();
            }

            _pomelo.request(route, msg, (data) =>
            {
                AppendLog(route + " " + msg + ":\n" + FormatJson(data.ToString()));
            });
        }

        /// <summary>
        /// 退出
        /// </summary>
        public static void Logout()
        {
            _pomelo.request("connector.entryHandler.onUserLeave", delegate(JsonObject data)
            {
                Console.WriteLine("userLeave " + data);
            });
        }

        private void tb_data_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files != null && files.Length == 1)
                {
                    string text = File.ReadAllText(files[0]);
                    tb_data.Text = FormatJson(text);
                }
                else
                {
                    MessageBox.Show("只能拖拽json文本和json文件到这里");
                }
            }
            else if (e.Data.GetDataPresent(DataFormats.Text))
            {
                string text = e.Data.GetData(DataFormats.Text).ToString();
                tb_data.Text = FormatJson(text);
            }
            else
            {
                MessageBox.Show("只能拖拽json文本和json文件到这里");
            }
        }

        private void tb_data_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.Text) || e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }


        private int _toolTipCount = 0;
        private void tb_data_MouseHover(object sender, EventArgs e)
        {
            if (_toolTipCount++ > 2)
            {
                return;
            }
            TextBox TB = (TextBox)sender;
            int VisibleTime = 2000;  //in milliseconds
            ToolTip tt = new ToolTip();
            tt.Show("可以拖拽json文本或者json文件到这里哦", TB, 0, tb_data.Height / 2, VisibleTime);
        }
    }
}