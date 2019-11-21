using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace JavaEnv
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private string GetEnvUri()
        {
            return txtEnvUri.Text;
        }

        // mianly using https://www.cnblogs.com/enych/p/11084319.html
        private void btOK_Click(object sender, EventArgs e)
        {
            Dictionary<string, string> needAdds = new Dictionary<string, string>(10);
            StringBuilder needAppends = new StringBuilder();
            // try to open the directoty
            var dir = new DirectoryInfo(txtEnvUri.Text);
            var folders = dir.GetDirectories();
            var temp = folders.FirstOrDefault(d => d.Name == "JDK");
            if (temp != null)
            {
                needAdds.Add("JAVA_HOME", temp.FullName);
                needAdds.Add("CLASSPATH", @".;%JAVA_HOME%\lib\dt.jar;%JAVA_HOME%\lib\tools.jar;");
                needAppends.Append(@"%JAVA_HOME%\bin;%JAVA_HOME%\jre\bin;");
            }
            temp = folders.FirstOrDefault(d => d.Name == "Maven");
            if (temp != null)
            {
                needAdds.Add("MAVEN_HOME", temp.FullName);
                needAppends.Append(@"%MAVEN_HOME%\bin;");
            }
            temp = folders.FirstOrDefault(d => d.Name == "Go");
            if (temp != null)
            {
                needAdds.Add("GOPATH", temp.GetDirectories().First(d => d.Name == "GoPath").FullName);
                needAdds.Add("GOROOT", temp.GetDirectories().First(d => d.Name == "Go").FullName);
                needAppends.Append(@"%GOPATH%\bin;");
                needAppends.Append(@"%GOROOT%\bin;");
            }

            // append after the path ,this method will resolve the charactor ";"
            SysEnvironment.SetPathAfter(needAppends.ToString());
            MessageBox.Show(needAppends.ToString());
            foreach (var item in needAdds)
            {
                if (!SysEnvironment.CheckSysEnvironmentExist(item.Key))// if this has not been added
                {
                    SysEnvironment.SetSysEnvironment(item.Key, item.Value);// add it in to Env
                    MessageBox.Show("Successfully added "+item.Key);
                }
                else
                {
                    MessageBox.Show( item.Key+ " duplicated");
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ////RunCmd(@"C:\Windows\System32\","ipconfig");
            //System.Diagnostics.Process.Start(@"C:\Windows\System32\cmd.exe");
            //textBox2.Text += Cmd(@"CD C:\mysql  mysqld --defaults-file=my.ini --initialize-insecure");

        }
        ///<summary>
        /// cmd命令执行，在cmd上可以执行的语句，直接传到这里，调用grads画图实例如下：
        ///  Cmd("C:/OpenGrADS/Contents/Cygwin/Versions/2.0.1.oga.1/i686/grads.exe -lbcx 'D:/data_wrfchem/gs/d01_hour_pm25.gs D:/data_wrfchem 2016-04-18 d01'");
        ///  Cmd("grads启动程序exe路径 -lbcx 'gs脚本文件 参数1 参数2 参数3'");
        /// </summary>
        /// <param name="c">执行语句</param>
        public string Cmd(string c)
        {
            System.Diagnostics.Process process = new System.Diagnostics.Process();
            process.StartInfo.FileName = "cmd.exe";
            process.StartInfo.UseShellExecute = false;
            process.StartInfo.CreateNoWindow = true;
            process.StartInfo.RedirectStandardOutput = true;
            process.StartInfo.RedirectStandardInput = true;
            process.Start();
            process.StandardInput.WriteLine(c);
            process.StandardInput.AutoFlush = true;
            process.StandardInput.WriteLine("exit");
            StreamReader reader = process.StandardOutput;//截取输出流
                                                         //string output = reader.ReadLine();//每次读取一行
                                                         //while (!reader.EndOfStream)
                                                         //{
                                                         //    // PrintThrendInfo(output);
                                                         //    output = reader.ReadLine();
                                                         //}
            string output = reader.ReadToEnd();//每次读取一行
            process.WaitForExit();

            return output;
        }

    }


    public static class SysEnvironment
    {


        #region --获取注册表中的环境变量

        /// <summary>
        /// 获取系统环境变量
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static string GetSysEnvironmentByName(string name)
        {
            string result = string.Empty;
            try
            {
                result = OpenSysEnvironment().GetValue(name).ToString();//读取
            }
            catch (Exception)
            {

                return string.Empty;
            }
            return result;

        }

        /// <summary>
        /// 打开系统环境变量注册表
        /// </summary>
        /// <returns>RegistryKey</returns>
        private static RegistryKey OpenSysEnvironment()
        {
            RegistryKey regLocalMachine = Registry.LocalMachine;
            RegistryKey regSYSTEM = regLocalMachine.OpenSubKey("SYSTEM", true);//打开HKEY_LOCAL_MACHINE下的SYSTEM 
            RegistryKey regControlSet001 = regSYSTEM.OpenSubKey("ControlSet001", true);//打开ControlSet001 
            RegistryKey regControl = regControlSet001.OpenSubKey("Control", true);//打开Control 
            RegistryKey regManager = regControl.OpenSubKey("Session Manager", true);//打开Control 

            RegistryKey regEnvironment = regManager.OpenSubKey("Environment", true);
            return regEnvironment;
        }

        /// <summary>
        /// 设置系统环境变量
        /// </summary>
        /// <param name="name">变量名</param>
        /// <param name="strValue">值</param>
        public static void SetSysEnvironment(string name, string strValue)
        {
            OpenSysEnvironment().SetValue(name, strValue);
        }
        #endregion


        /// <summary>
        /// 检测系统环境变量是否存在
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool CheckSysEnvironmentExist(string name)
        {
            if (!string.IsNullOrEmpty(GetSysEnvironmentByName(name)))
                return true;
            else
                return false;
        }

        /// <summary>
        /// 添加到PATH环境变量（会检测路径是否存在，存在就不重复）
        /// </summary>
        /// <param name="strPath"></param>
        public static void SetPathAfter(string strHome)
        {
            string pathlist;
            pathlist = GetSysEnvironmentByName("PATH");

            bool isPathExist = false;
            if (pathlist.Length > 1)
            {
                //检测是否以;结尾
                if (pathlist.Substring(pathlist.Length - 1, 1) != ";") //截取最后一个字符,判断这个字符不等于;号,
                {
                    SetSysEnvironment("PATH", pathlist + ";");
                    pathlist = GetSysEnvironmentByName("PATH");
                }
                string[] list = pathlist.Split(';');//以;切割


                foreach (string item in list)
                {
                    if (item == strHome)
                        isPathExist = true;
                }
            }
            if (!isPathExist)
            {
                SetSysEnvironment("PATH", pathlist + strHome + ";");
            }
        }

        public static void SetPathBefore(string strHome)
        {

            string pathlist;
            pathlist = GetSysEnvironmentByName("PATH");
            string[] list = pathlist.Split(';');
            bool isPathExist = false;

            foreach (string item in list)
            {
                if (item == strHome)
                    isPathExist = true;
            }
            if (!isPathExist)
            {
                SetSysEnvironment("PATH", strHome + ";" + pathlist);
            }

        }

        public static void SetPath(string strHome)
        {
            string pathlist;
            pathlist = GetSysEnvironmentByName("PATH");
            string[] list = pathlist.Split(';');
            bool isPathExist = false;

            foreach (string item in list)
            {
                if (item == strHome)
                    isPathExist = true;
            }
            if (!isPathExist)
            {
                SetSysEnvironment("PATH", pathlist + strHome + ";");
            }
        }
    }

    //Kernel32.DLL内有SetEnvironmentVariable函数用于设置系统环境变量
    //C#调用要用DllImport，代码封装如下：
    class SetSysEnvironmentVariable
    {
        [DllImport("Kernel32.DLL ", SetLastError = true)]
        public static extern bool SetEnvironmentVariable(string lpName, string lpValue);

        public static void SetPath(string pathValue)
        {
            string pathlist;
            pathlist = SysEnvironment.GetSysEnvironmentByName("PATH");
            string[] list = pathlist.Split(';');
            bool isPathExist = false;

            foreach (string item in list)
            {
                if (item == pathValue)
                    isPathExist = true;
            }
            if (!isPathExist)
            {
                SetEnvironmentVariable("PATH", pathlist + pathValue + ";");

            }

        }
    }

}
