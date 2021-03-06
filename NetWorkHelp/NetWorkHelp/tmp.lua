using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.IO;

namespace NetWorkHelp
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (this.folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                this.textBox1.Text = this.folderBrowserDialog1.SelectedPath;
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            //调用系统命令生成main.pb，并且移动到res/pb目录下
            Process proc = null;
            proc = new Process();
            proc.StartInfo.FileName = "proto2mainpb.bat";
            proc.Start();
            proc.WaitForExit();
            //拷贝main.pb到指定的文件夹
            FileInfo fi = new FileInfo("main.pb");
            fi.CopyTo(this.textBox1.Text + "/res/pb/main.pb", true);

            String[] codeArray = Regex.Split(this.textBox2.Text, ",", RegexOptions.IgnoreCase);
            foreach (string codeNum in codeArray)
            {
                //分析协议文件，找到需要的参数
                this.creatMainPb(codeNum);
            }
            MessageBox.Show("操作成功");
        }

        //分析协议文件，找到需要的参数
        private void creatMainPb(String codeNum)
        {
            //根据上行协议号分析出来pb名字
            String upPbName = "";
            String upFunName = "";
            String downPbName = "";
            String downFunName = "";
            bool findPb = false;
            bool findMessage = false;
            bool findZhushi = false;
            FileStream upReader = new FileStream("up.proto", FileMode.Open, FileAccess.Read);
            StreamReader pbUpReader = new StreamReader(upReader);
            string upContent;
            while ((upContent = pbUpReader.ReadLine()) != null)
            {
                if (upContent.ToString().IndexOf(codeNum) >= 0)
                {
                    //上行协议注释内容
                    upFunName = upContent.ToString();
                    findPb = true;
                }
                else if (findPb)
                {
                    if (upContent.ToString().IndexOf("message") >= 0 && findZhushi == false)
                    {
                        //上行协议pb名称
                        upPbName = upContent.ToString().Substring(upContent.ToString().IndexOf("message") + 8, upContent.ToString().IndexOf("{") - upContent.ToString().IndexOf("message") - 8);
                        findMessage = true;
                        break;
                    }
                    if (upContent.ToString().IndexOf("//") >= 0 && findMessage == false)
                    {
                        //上行协议pb名称
                        upPbName = "None";
                        findZhushi = true;
                        break;
                    }
                }
            }
            if (findMessage == false && findZhushi == false)
            {
                upPbName = "None";
            }
            pbUpReader.Close();

            findPb = false;
            findMessage = false;
            findZhushi = false;
            FileStream downReader = new FileStream("down.proto", FileMode.Open, FileAccess.Read);
            StreamReader pbDownReader = new StreamReader(downReader);
            string downContent;
            while ((downContent = pbDownReader.ReadLine()) != null)
            {
                if (downContent.ToString().IndexOf((int.Parse(codeNum) + 1).ToString()) >= 0)
                {
                    //下行协议注释内容
                    downFunName = downContent.ToString();
                    findPb = true;
                }
                else if (findPb)
                {
                    if (downContent.ToString().IndexOf("message") >= 0 && findZhushi == false)
                    {
                        //上行协议pb名称
                        downPbName = downContent.ToString().Substring(downContent.ToString().IndexOf("message") + 8, downContent.ToString().IndexOf("{") - downContent.ToString().IndexOf("message") - 8);
                        findMessage = true;
                        break;
                    }
                }
            }
            pbDownReader.Close();

            this.creatCode(codeNum, upPbName, upFunName, downPbName, downFunName);
        }

        //向代码中添加网络协议相关内容
        private void creatCode(String codeNum, String upPbName, String upFunName, String downPbName, String downFunName)
        {
            //修改NetRequest.lua
            String requestCode = "\n--{0}\nfunction NetRequest.send{1}(data)\n\tNetwork.send2Socket({2}, data or {{}}, false)\nend";
            FileStream request = new FileStream(this.textBox1.Text + "/scripts/app/utils/net/NetRequest.lua", FileMode.Append, FileAccess.Write);
            StreamWriter requestWriter = new StreamWriter(request);
            requestWriter.BaseStream.Seek(0, SeekOrigin.End);
            if (upPbName.CompareTo("None") == 0)
            {
                requestWriter.WriteLine(String.Format(requestCode, upFunName, upPbName + codeNum, codeNum));
            }
            else
            {
                requestWriter.WriteLine(String.Format(requestCode, upFunName, upPbName, codeNum));
            }
            
            requestWriter.Flush();
            requestWriter.Close();

            //修改protocols.lua
            String[] lineNums = new String[2];
            int arryNum = 0;
            FileStream protocolsR = new FileStream(this.textBox1.Text + "/scripts/app/utils/net/protocols.lua", FileMode.Open, FileAccess.Read);
            StreamReader protocolsReader = new StreamReader(protocolsR);
            string content;
            while ((content = protocolsReader.ReadLine()) != null)
            {
                if (content.ToString().IndexOf("send = {") >= 0)
                {
                    lineNums[arryNum] = content;
                    arryNum++;
                }

                if (content.ToString().IndexOf("receive = {") >= 0)
                {
                    lineNums[arryNum] = content;
                    arryNum++;
                }
            }
            protocolsReader.Close();
            int insertNum = 0;
            foreach (String line in lineNums)
            {
                if (insertNum == 0)
                {
                    File.WriteAllText(this.textBox1.Text + "/scripts/app/utils/net/protocols.lua", File.ReadAllText(this.textBox1.Text + "/scripts/app/utils/net/protocols.lua").Replace(line, String.Format("send = {{\n\t[{0}] =\"sango.packet.{1}\", --{2}", codeNum, upPbName, upFunName)));
                }
                else
                {
                    File.WriteAllText(this.textBox1.Text + "/scripts/app/utils/net/protocols.lua", File.ReadAllText(this.textBox1.Text + "/scripts/app/utils/net/protocols.lua").Replace(line, String.Format("receive = {{\n\t[{0}] =\"sango.packet.{1}\", --{2}", int.Parse(codeNum) + 1, downPbName, downFunName)));
                }
                insertNum++;
            }
        }
    }
}
