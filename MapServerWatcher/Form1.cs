using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Diagnostics;
using System.IO;
using System.Net.Mail;

namespace MapServerWatcher
{
    public partial class Form1 : Form
    {
        Timer m_Timer;
        DateTime m_LastSentMailTime;
        public Form1()
        {
            InitializeComponent();
            m_LastSentMailTime = DateTime.Now.AddDays(-1);
        }

        private void Form1_Shown(object sender, EventArgs e)
        {
            m_Timer = new Timer();
            m_Timer.Interval = 600000;
            //m_Timer.Interval = 10000;
            m_Timer.Tick += M_Timer_Tick;
            m_Timer.Start();
        }

        private void M_Timer_Tick(object sender, EventArgs e)
        {
            string onelinelog = "";
            //判斷要wmts是否正常
            bool bOK = false;
            string Url = "http://140.109.161.39:8082/wmts";
            //string Url = "http://127.0.0.1:8080/wmts";
            try
            {
                HttpWebRequest req = WebRequest.Create(Url) as HttpWebRequest;
                if (req != null)
                {
                    req.CachePolicy = new System.Net.Cache.RequestCachePolicy(System.Net.Cache.RequestCacheLevel.NoCacheNoStore);
                    HttpWebResponse res = req.GetResponse() as HttpWebResponse;
                    if (res != null)
                    {
                        long len = res.ContentLength;
                        if (len > 0)
                        {
                            bOK = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                WriteLOG("異常>>" + ex.Message, ref onelinelog);
            }

            //不正常就重啟
            if (!bOK)
            {
                WriteLOG("伺服器無回應。", ref onelinelog);
                //先檢查屍體還在不在
                Process [] pss = Process.GetProcessesByName("PGMapServer");
                if (pss.Length > 0)
                {
                    //在就毀屍滅跡
                    foreach (Process p in pss)
                    {
                        WriteLOG("殺死 " + p.MainWindowTitle, ref onelinelog);
                        p.Kill();
                    }
                }
                //重啟伺服器
                Process ms = new Process();
                ms.StartInfo.FileName = @"C:\Program Files\PilotGaea\TileMap\PGMapServer.exe";
                ms.StartInfo.Arguments = "-s 1";
                bool r = ms.Start();
                WriteLOG("重啟" + (r ? "成功":"失敗") + "。", ref onelinelog);

                //準備寄信
                if ((DateTime.Now - m_LastSentMailTime).TotalDays > 0.5)//最多半天寄一次
                {
                    try
                    {
                        SmtpClient client = new SmtpClient("smtp.gmail.com", 587)
                        {
                            EnableSsl = true,
                            DeliveryMethod = System.Net.Mail.SmtpDeliveryMethod.Network,
                            UseDefaultCredentials = false,
                            Credentials = new NetworkCredential("pilotgaeaftp@gmail.com", "pg2017ftp")
                        };
                        //client.Send("中研院伺服器監測中心<pilotgaeaftp@gmail.com>", "tech.sales@pilotgaea.com.tw", "您關注的伺服器已經崩潰。", onelinelog + "\r\nEnjoy your bugs.");
                        client.Send("中研院伺服器監測中心<pilotgaeaftp@gmail.com>", "lag945@hotmail.com", "您關注的伺服器已經崩潰。", onelinelog + "\r\nEnjoy your bugs.");
                    }
                    catch (Exception ex)
                    {
                        WriteLOG("寄信異常>>" + ex.Message, ref onelinelog);
                    }
                    m_LastSentMailTime = DateTime.Now;
                }
            }


            //PGMapServer
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (m_Timer != null)
            {
                m_Timer.Stop();
            }
        }

        private void WriteLOG(string log, ref string onelinelog)
        {
            string filename = @"C:\ProgramData\PilotGaea\crashlog.txt";
            StreamWriter sw = new StreamWriter(filename, true, Encoding.UTF8);
            string _log = DateTime.Now.ToString("u") + " : " + log;
            sw.WriteLine(_log);
            sw.Close();
            onelinelog += _log + "\r\n";
        }

    }
}
