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
            if (File.Exists("path.txt"))
            {
                FileStream upReader = new FileStream("path.txt", FileMode.Open, FileAccess.Read);
                StreamReader pbUpReader = new StreamReader(upReader);
                string upContent = pbUpReader.ReadLine();
                if (upContent != null)
                {
                    this.textBox1.Text = upContent.ToString();
                }
                pbUpReader.Close();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (this.folderBrowserDialog1.ShowDialog() == DialogResult.OK)
            {
                this.textBox1.Text = this.folderBrowserDialog1.SelectedPath;
                //把第一次选择的路径保存到文件中，方便下次打开
                FileStream upReader = new FileStream("path.txt", FileMode.Create, FileAccess.ReadWrite);
                StreamWriter requestWriter = new StreamWriter(upReader);
                requestWriter.WriteLine(this.textBox1.Text);
                requestWriter.Flush();
                requestWriter.Close();
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
            if (File.Exists(this.textBox1.Text + "/scripts/app/packhandles/" + this.textBox3.Text + ".lua"))
            {
                String requestCode = "--{0}\nfunction {1}:send{2}(data)\n\tNetwork.send2Socket({3}, data or {{}}, false)\nend\n\nfunction {4}:destroy()";
                FileStream request = new FileStream(this.textBox1.Text + "/scripts/app/packhandles/" + this.textBox3.Text + ".lua", FileMode.Open, FileAccess.Read);
                StreamReader requestReader = new StreamReader(request);
                string content1;
                
                while ((content1 = requestReader.ReadLine()) != null)
                {
                    if (content1.ToString().IndexOf(":destroy") >= 0)
                    {
                        requestReader.Close();
                        if (upPbName.CompareTo("None") == 0)
                        {
                            File.WriteAllText(this.textBox1.Text + "/scripts/app/packhandles/" + this.textBox3.Text + ".lua", File.ReadAllText(this.textBox1.Text + "/scripts/app/packhandles/" + this.textBox3.Text + ".lua").Replace(content1.ToString(), String.Format(requestCode, upFunName, this.textBox3.Text, upPbName + codeNum, codeNum, this.textBox3.Text)));
                        }
                        else
                        {
                            File.WriteAllText(this.textBox1.Text + "/scripts/app/packhandles/" + this.textBox3.Text + ".lua", File.ReadAllText(this.textBox1.Text + "/scripts/app/packhandles/" + this.textBox3.Text + ".lua").Replace(content1.ToString(), String.Format(requestCode, upFunName, this.textBox3.Text, upPbName, codeNum, this.textBox3.Text)));
                        }
                        break;
                    }
                }
                requestReader.Close();
            }
            else
            {
                String requestCode = "local {0} = class(\"{1}\",BasePackHandle)\n\nfunction {2}:getCodes()\n\treturn {{\n\n\t}}\nend\n\nfunction {3}:handlePack(code,packData)\n\nend\n\n--{4}\nfunction {5}:send{6}(data)\n\tNetwork.send2Socket({7}, data or {{}}, false)\nend\n\nfunction {8}:destroy()\nend\n\nreturn {9}";
                FileStream upReader = new FileStream(this.textBox1.Text + "/scripts/app/packhandles/" + this.textBox3.Text + ".lua", FileMode.Create, FileAccess.ReadWrite);
                StreamWriter requestWriter = new StreamWriter(upReader);
                if (upPbName.CompareTo("None") == 0)
                {
                    requestWriter.WriteLine(String.Format(requestCode, this.textBox3.Text, this.textBox3.Text, this.textBox3.Text, this.textBox3.Text, upFunName, this.textBox3.Text, upPbName + codeNum, codeNum, this.textBox3.Text, this.textBox3.Text));
                }
                else
                {
                    requestWriter.WriteLine(String.Format(requestCode, this.textBox3.Text, this.textBox3.Text, this.textBox3.Text, this.textBox3.Text, upFunName, this.textBox3.Text, upPbName, codeNum, this.textBox3.Text, this.textBox3.Text));
                }
                requestWriter.Flush();
                requestWriter.Close();
            }

            //修改protocols.lua
            String[] lineNums = new String[2];
            int arryNum = 0;
            FileStream protocolsR = new FileStream(this.textBox1.Text + "/scripts/app/utils/net/protocols.lua", FileMode.Open, FileAccess.Read);
            StreamReader protocolsReader = new StreamReader(protocolsR);
            string content;
            while ((content = protocolsReader.ReadLine()) != null)
            {
                if (content.ToString().IndexOf("--upEnd") >= 0)
                {
                    lineNums[arryNum] = content;
                    arryNum++;
                }

                if (content.ToString().IndexOf("--downEnd") >= 0)
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
                    File.WriteAllText(this.textBox1.Text + "/scripts/app/utils/net/protocols.lua", File.ReadAllText(this.textBox1.Text + "/scripts/app/utils/net/protocols.lua").Replace(line, String.Format("\t[{0}] =\"sango.packet.{1}\", --{2}\n}},--upEnd", codeNum, upPbName, upFunName)));
                }
                else
                {
                    File.WriteAllText(this.textBox1.Text + "/scripts/app/utils/net/protocols.lua", File.ReadAllText(this.textBox1.Text + "/scripts/app/utils/net/protocols.lua").Replace(line, String.Format("\t[{0}] =\"sango.packet.{1}\", --{2}\n}},--downEnd", int.Parse(codeNum) + 1, downPbName, downFunName)));
                }
                insertNum++;
            }
        }
    }
}
