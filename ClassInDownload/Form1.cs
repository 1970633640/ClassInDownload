using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace ClassInDownload
{

    public partial class Form1 : Form
    {
        private static readonly HttpClient client = new HttpClient();
        ClassInObject item;
        ClassInObject2 item2;
        String downloadFolder="D:\\ClassIn";
        String folder;
        int downloadTaskCount;
        int currentTaskNum;
        int currentVideoNum;

        public Form1()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
        }

        public class ClassInObject
        {
            public Data data { get; set; }
        }

        public class ClassInObject2
        {
            public Data2 data { get; set; }
        }

        public class Data
        {
            public long lessonId { get; set; }
            public string lessonName { get; set; }
            public long lessonStarttime { get; set; }
            public long lessonEndtime { get; set; }
            public string courseName { get; set; }
            public string schoolName { get; set; }
            public string teacherName { get; set; }
        }
        public class Data2
        {
            public Lessondata lessonData { get; set; }
        }
        public class Lessondata
        {
            public int lessonStatus { get; set; }
            public Filelist[] fileList { get; set; }
        }


        public class Filelist
        {
            public string CreateTime { get; set; }
            public int Duration { get; set; }
            public string EndTime { get; set; }
            public string FileId { get; set; }
            public string FileName { get; set; }
            public string Message { get; set; }
            public Playset[] Playset { get; set; }
            public string Size { get; set; }
            public int SourceType { get; set; }
            public string StartTime { get; set; }
            public string Status { get; set; }
        }

        public class Playset
        {
            public string Definition { get; set; }
            public string Url { get; set; }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            textBox1.Text = Clipboard.GetText();
        }

        private async void button1_ClickAsync(object sender, EventArgs e)
        {
            string s = textBox1.Text;
            if(s.IndexOf("lessonKey=")==-1)
            {
                return;
            }
            string lessonkey = s.Substring(s.IndexOf("lessonKey=") + 10);
            label1.Text = "LessonKey: " + lessonkey;

            //net
            var values = new Dictionary<string, string>
            {
                { "lessonKey", lessonkey },
            };
            var content = new FormUrlEncodedContent(values);
            var response = await client.PostAsync("https://www.eeo.cn/saasajax/webcast.ajax.php?action=getLessonLiveInfo", content);
            var responseString = await response.Content.ReadAsStringAsync();
            var response2 = await client.PostAsync("https://www.eeo.cn/saasajax/webcast.ajax.php?action=getLessonWebcastData", content);
            var responseString2 = await response2.Content.ReadAsStringAsync();
            //json
            item = JsonSerializer.Deserialize<ClassInObject>(responseString);

            //ui
            System.DateTime dateTime = new System.DateTime(1970, 1, 1, 8, 0, 0, 0);
            var startTime = dateTime.AddSeconds(double.Parse(item.data.lessonStarttime.ToString()));
            var endTime = dateTime.AddSeconds(double.Parse(item.data.lessonEndtime.ToString()));
            label2.Text = "School: " + item.data.schoolName + "    Teacher: " + item.data.teacherName + "    LessonName: " + item.data.lessonName
                + "\n(GMT+8) Start: " + startTime.ToShortDateString() + " " + startTime.ToLongTimeString()
                + "    End: " + endTime.ToShortDateString() + " " + endTime.ToLongTimeString();
            folder = item.data.lessonName;
            refreshText2();
            //json
            item2 = JsonSerializer.Deserialize<ClassInObject2>(responseString2);
            //file data grid
            dataGridView1.Rows.Clear();
            for (int i = 0; i < item2.data.lessonData.fileList.Length; i++)
            {
                var it = item2.data.lessonData.fileList[i];
                dataGridView1.Rows.Add((it.Duration>600||(!checkBox1.Checked)),it.StartTime, it.Duration/3600+"hr"+(it.Duration%3600)/60+"min", long.Parse(it.Size)/1024/1024+"MB", it.Playset[0].Url);
            }
        }
        

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if(checkBox1.Checked)
            {
                if(dataGridView1.Rows.Count>0)
                {
                    for (int i = 0; i < dataGridView1.Rows.Count; i++)
                    {
                        if (item2.data.lessonData.fileList[i].Duration > 600)
                            dataGridView1.Rows[i].Cells[0].Value = true;
                        else
                            dataGridView1.Rows[i].Cells[0].Value = false;
                    }
                }
            }
            else
            {
                for(int i=0;i<dataGridView1.Rows.Count;i++)
                {
                    dataGridView1.Rows[i].Cells[0].Value = true;
                }
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            System.Windows.Forms.FolderBrowserDialog dialog = new System.Windows.Forms.FolderBrowserDialog();
            dialog.Description = "Download Root Folder";
            if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (string.IsNullOrEmpty(dialog.SelectedPath))
                {
                    return;
                }
                downloadFolder = dialog.SelectedPath;
                refreshText2();
            }
        }
        private void refreshText2()
        {
            textBox2.Text = downloadFolder+"\\"+folder;
        }

        private void refreshStatus(string x,string a)
        {
            label4.Text = a;
            label3.Text = (currentTaskNum+1).ToString() + "/" + downloadTaskCount + "    " + x + "%";
            int x2=0;
            int.TryParse(x,out x2);
            progressBar1.Value = x2;
            if (currentVideoNum >= item2.data.lessonData.fileList.Length)
                label3.Text = "成功！ Complete!";
        }

        private void button4_Click(object sender, EventArgs e)
        {
            downloadTaskCount = 0;
            for (int i = 0; i < dataGridView1.Rows.Count; i++)
            {
                if (Convert.ToBoolean(dataGridView1.Rows[i].Cells[0].Value)==true)
                    downloadTaskCount += 1;
            }
            currentTaskNum = -1;
            currentVideoNum = -1;
            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;
            button4.Enabled = false;
            checkBox1.Enabled = false;
            textBox1.Enabled = false;
            textBox2.Enabled = false;
            down();
        }
        private void success()
        {
            if (currentVideoNum >= item2.data.lessonData.fileList.Length)
                label3.Text = "成功！ Complete!";
            button1.Enabled = true;
            button2.Enabled = true;
            button3.Enabled = true;
            button4.Enabled = true;
            checkBox1.Enabled = true;
            textBox1.Enabled = true;
            textBox2.Enabled = true;
        }
        private void down()
        {
            currentTaskNum += 1;
            currentVideoNum += 1;
            if (currentVideoNum >= item2.data.lessonData.fileList.Length)
            {
                success();
                return;
            }
            while (Convert.ToBoolean(dataGridView1.Rows[currentVideoNum].Cells[0].Value) == false)
                currentVideoNum += 1;
            if (currentVideoNum >= item2.data.lessonData.fileList.Length)
            {
                success();
                return;
            }
            refreshStatus("0","");
            DownloadFileByAria2(item2.data.lessonData.fileList[currentVideoNum].Playset[0].Url, textBox2.Text, item2.data.lessonData.fileList[currentVideoNum].StartTime.Replace(':','-')+".mp4");
        }

        //https://www.cnblogs.com/littlehb/p/5782714.html
        public  bool DownloadFileByAria2(string url, string strFileName1,string strFileName2)
        {
            var tool = System.AppDomain.CurrentDomain.BaseDirectory+"data\\aria2c\\aria2c.exe";
            var command = " -c -x 8  --file-allocation=falloc -d \"" +strFileName1 + "\" -o \"" + strFileName2 + "\" " + url;
            using (var p = new Process())
            {
                RedirectExcuteProcess(p, tool, command, (s, e) => ShowInfo(url, e.Data));
            }
            return true;
        }
        private  void ShowInfo(string url, string a)
        {
            if (a == null) return;

            const string re1 = ".*?"; // Non-greedy match on filler
            const string re2 = "(\\(.*\\))"; // Round Braces 1

            var r = new Regex(re1 + re2, RegexOptions.IgnoreCase | RegexOptions.Singleline);
            var m = r.Match(a);
            if (m.Success)
            {
                var rbraces1 = m.Groups[1].ToString().Replace("(", "").Replace(")", "").Replace("%", "").Replace("s", "0");
                //Console.WriteLine("原始" + a);
                //Console.WriteLine(rbraces1);
                if (rbraces1 == "OK")
                {
                    rbraces1 = "100";
                    refreshStatus(rbraces1,a);
                    down();
                }
                refreshStatus(rbraces1,a);
            }
        }

        /// <summary>
        /// 功能：重定向执行
        /// </summary>
        /// <param name="p"></param>
        /// <param name="exe"></param>
        /// <param name="arg"></param>
        /// <param name="output"></param>
        private  void RedirectExcuteProcess(Process p, string exe, string arg, DataReceivedEventHandler output)
        {
            p.StartInfo.FileName = exe;
            p.StartInfo.Arguments = arg;

            p.StartInfo.UseShellExecute = false;    //输出信息重定向
            p.StartInfo.CreateNoWindow = true;
            p.StartInfo.RedirectStandardError = true;
            p.StartInfo.RedirectStandardOutput = true;

            p.OutputDataReceived += output;
            p.ErrorDataReceived += output;

            p.Start();                    //启动线程
            p.BeginOutputReadLine();
            p.BeginErrorReadLine();
            p.WaitForExit();            //等待进程结束
        }

        private void dataGridView1_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }
    }
}
