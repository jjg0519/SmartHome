﻿using System;
using MySql.Data.MySqlClient;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace 智能家居系统
{

    public partial class MainForm : Form
    {
        /// <summary>
        /// 数据库的长连接
        /// </summary>
        MySQLDBModel DataBaseController = new MySQLDBModel();

        /// <summary>
        /// 页面状态枚举
        /// </summary>
        private enum PanelState
        {
            /// <summary>
            /// 显示家电控制界面
            /// </summary>
            Control,
            /// <summary>
            /// 显示家电信息界面
            /// </summary>
            Info,
            /// <summary>
            /// 家电状态展示视图
            /// </summary>
            Card
        }
        /// <summary>
        /// 页面状态状态
        /// </summary>
        PanelState PanelStatenow = PanelState.Control;
        /// <summary>
        /// 获取或设置页面状态
        /// </summary>
        private PanelState PanelStateNow
        {
            get => PanelStatenow;
            set
            {
                PanelStatenow = value;
                UnityModule.DebugPrint("正在切换界面到 " + value.ToString());
                switch (value)
                {
                    case PanelState.Control:
                        {
                            ControlPanel.Show();
                            if (InfoPanel.Visible) InfoPanel.Hide();
                            if (CardPanel.Visible) CardPanel.Hide();
                            InfoLabel.Image = UnityResource.Info_;
                            CardLabel.Image = UnityResource.Card_;
                            break;
                        }
                    case PanelState.Info:
                        {
                            if (ControlPanel.Visible)
                            {
                                ControlPanel.Hide();
                                ControlLabel.Image = UnityResource.Control_;
                            }
                            InfoPanel.Show();
                            if (CardPanel.Visible)
                            {
                                CardPanel.Hide();
                                CardLabel.Image = UnityResource.Card_;
                            }
                            this.Invalidate();
                            if (!string.IsNullOrEmpty(DomesticApplianceItem.ActiveItem?.MAC))
                            {
                                ShowDomesticApplianceInfo(DomesticApplianceItem.ActiveItem.MAC);
                                ShowDomesticApplianceEventLog(DomesticApplianceItem.ActiveItem.MAC);
                            }
                            break;
                        }
                    case PanelState.Card:
                        {
                            if (ControlPanel.Visible)
                            {
                                ControlPanel.Hide();
                                ControlLabel.Image = UnityResource.Control_;
                            }
                            if (InfoPanel.Visible)
                            {
                                InfoPanel.Hide();
                                InfoLabel.Image = UnityResource.Info_;
                            }
                            CardPanel.Show();
                            break;
                        }
                }
                UnityModule.DebugPrint("界面切换完成！当前状态：" + value.ToString());
            }
        }

        #region "窗体事件"

        public MainForm()
        {
            InitializeComponent();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            this.Icon = UnityResource.LogoIcon;
            TimeLabel.Text = DateTime.Now.ToString("yyyy/MM/dd hh:mm");
            DomesticApplianceItem.ParentPanel = DomesticAppliancePanel;

            //注册鼠标拖动功能
            LogoLabel.MouseDown += new MouseEventHandler(UnityModule.MoveFormViaMouse);
            TopPanel.MouseDown += new MouseEventHandler(UnityModule.MoveFormViaMouse);


            CheckForIllegalCrossThreadCalls = false;
            ControlPanel.Dock = DockStyle.Fill;
            InfoPanel.Dock = DockStyle.Fill;
            CardPanel.Dock = DockStyle.Fill;

            InfoLabel.MouseEnter += new EventHandler(Button_MouseEnter);
            InfoLabel.MouseLeave += new EventHandler(TitleButton_MouseLeave);
            InfoLabel.MouseDown += new MouseEventHandler(Button_MouseDown);
            InfoLabel.MouseUp += new MouseEventHandler(Button_MouseUp);

            ControlLabel.MouseEnter += new EventHandler(Button_MouseEnter);
            ControlLabel.MouseLeave += new EventHandler(TitleButton_MouseLeave);
            ControlLabel.MouseDown += new MouseEventHandler(Button_MouseDown);
            ControlLabel.MouseUp += new MouseEventHandler(Button_MouseUp);

            CardLabel.MouseEnter += new EventHandler(Button_MouseEnter);
            CardLabel.MouseLeave += new EventHandler(TitleButton_MouseLeave);
            CardLabel.MouseDown += new MouseEventHandler(Button_MouseDown);
            CardLabel.MouseUp += new MouseEventHandler(Button_MouseUp);

            ExitButton.MouseEnter += new EventHandler(Button_MouseEnter);
            ExitButton.MouseLeave += new EventHandler(Button_MouseLeave);
            ExitButton.MouseDown += new MouseEventHandler(Button_MouseDown);
            ExitButton.MouseUp += new MouseEventHandler(Button_MouseUp);


            //家电名称和描述显示控件鼠标进入或离开时改变编辑按钮的可见性
            Action<object, EventArgs> ShowEditButton = new Action<object, EventArgs>(delegate (object x,EventArgs y){ if (MACValueLabel.Text == "(unknown)") return; (x as Label).Image = UnityResource.Edit_0;(x as Label).ForeColor = Color.DeepSkyBlue; });
            Action<object, EventArgs> HideEditButton = new Action<object, EventArgs>(delegate (object x, EventArgs y) { if (MACValueLabel.Text == "(unknown)") return; (x as Label).Image = null; (x as Label).ForeColor = Color.Black; });
            DeviceNameValueLabel.MouseEnter += new EventHandler(ShowEditButton);
            DescriptionValueLabel.MouseEnter += new EventHandler(ShowEditButton);
            DeviceNameValueLabel.MouseLeave += new EventHandler(HideEditButton);
            DescriptionValueLabel.MouseLeave += new EventHandler(HideEditButton);
            DeviceNameValueLabel.Click += new EventHandler(EditDAInfo);
            DescriptionValueLabel.Click += new EventHandler(EditDAInfo);

            LogoLabel.Text = "界面初始化完毕！";
            UnityModule.DebugPrint("界面初始化完毕！");
            this.Invalidate();
        }

        private void MainForm_Shown(object sender, EventArgs e)
        {
            UnityModule.DebugPrint("界面显示完成。_Shown()");
            //连接数据库
            if (DataBaseController.CreateConnection())
            {
                LogoLabel.Text = "数据库连接成功！";
                UnityModule.DebugPrint("数据库长连接创建成功！");
            }
            else
            {
                LogoLabel.Text = "数据库连接失败！";
                new MyMessageBox("无法连接数据库！请退出系统并检查连接！", "数据库连接失败：", MyMessageBox.IconType.Error).ShowDialog(this);
                ExitApplication();
            }

            //稳定界面
            this.Invalidate();
            UnityModule.DebugPrint("界面稳定！");

            //读取数据库里的家用电器
            LoadDomesticAppliance();

            LogoLabel.Text = "家电读取完毕！\n欢迎使用智能家居系统！";
        }

        static bool AllowToClose = false;
        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (!AllowToClose)
            {
                e.Cancel = true;
                if (new MyMessageBox("您真的要退出智能家居系统吗？", MyMessageBox.IconType.Question).ShowDialog(this) != DialogResult.OK) return;
                ExitApplication();
            }
        }

        #endregion

        #region "页面大小改变自适应布局"
        private void MainForm_Resize(object sender, EventArgs e)
        {
            if (this.Width == 0 || this.Height == 0) return;
            UnityModule.DebugPrint("开始自适应调整界面...");

            //Left:
            LeftPanel.Width = Math.Min((int)(this.Width * 0.3), 300);

            //Top
            TopPanel.Height = Math.Max((int)(this.Height * 0.10), 80);

            UnityModule.DebugPrint("界面调整完毕！");
        }

        private void LeftPanel_Resize(object sender, EventArgs e)
        {
            LogoLabel.Width = LeftPanel.Width;
            DomesticAppliancePanel.Height = LeftPanel.Height - LogoLabel.Bottom;
        }

        private void TopPanel_Resize(object sender, EventArgs e)
        {
            ExitButton.Width = TopPanel.Height;
            ControlLabel.Left = (int)((TimeLabel.Left - 240) / 2);
            InfoLabel.Left = ControlLabel.Right + 20;
            CardLabel.Left = InfoLabel.Right + 20;
        }
        #endregion

        #region "功能函数"

        /// <summary>
        /// 关闭数据库连接，退出系统
        /// </summary>
        private void ExitApplication()
        {
            UnityModule.DebugPrint("正在退出系统...");
            AllowToClose = true;
            if (DataBaseController != null) DataBaseController.CloseConnection();
            ThreadPool.QueueUserWorkItem(delegate
            {
                while (this.Opacity > 0)
                {
                    this.Opacity -= 0.1;
                    Thread.Sleep(20);
                }
                UnityModule.DebugPrint("欢迎下次使用！再见！");
                Application.Exit();
            });
        }

        /// <summary>
        /// 从数据库加载家电列表
        /// </summary>
        private void LoadDomesticAppliance()
        {
            //todo:遍历在线的家电，新家电加入字典和控件
            //todo:遍历在线的家电，更新家电数据
            //todo:遍历FD<0的家电（已经掉线），移除字典和控件(如果被移除的空间被激活，优先激活下一个，如果下一个不存在，激活上一个，如不存在不激活)

            UnityModule.DebugPrint("开始更新已连接家电信息...");
            using (MySqlDataReader DataReader = DataBaseController.ExecuteReader("SELECT * FROM devicebase WHERE FD>-1"))
            {
                if (DataReader == null) return;
                if (DataReader.HasRows)
                {
                    while (DataReader.Read())
                    {
                        string DeviceName = DataReader["DeviceName"] as string;
                        string Model = DataReader["Model"] as string;
                        string Description = DataReader["Description"] as string;
                        string Manufactor = DataReader["Manufactor"] as string;
                        string MAC;
                        try
                        {
                            DeviceName = DataReader["DeviceName"] as string;
                            Model = DataReader["Model"] as string;
                            Description = DataReader["Description"] as string;
                            Manufactor = DataReader["Manufactor"] as string;
                            MAC = DataReader["MAC"] as string;
                            if (DomesticApplianceItem.DAExists(MAC))
                            {
                                //家电已经存在了，仅更新数据
                                UnityModule.DebugPrint("更新家电数据 : {0}",MAC);

                            }
                            else
                            {
                                //家电不存在，添加新家电
                                DomesticApplianceItem newDAItem;
                                newDAItem = new DomesticApplianceItem(MAC, Manufactor,Model);
                                newDAItem.SetDeviceNameAndDescription(DeviceName,Description);
                                newDAItem.Width = DomesticAppliancePanel.Width-25;
                                newDAItem.ItemClick += new EventHandler(DomesticApplianceItem_ItemClick);

                                UnityModule.DebugPrint("增加新家电：{0}({1})\n\t\t\t\tMAC地址：{2}",DeviceName , Model, MAC);
                                this.Invoke(new Action(() =>
                                {
                                    DomesticAppliancePanel.Controls.Add(newDAItem);
                                    UnityModule.DebugPrint("成功加入列表控件");
                                }));
                            }
                        }
                        catch (Exception ex)
                        {
                            UnityModule.DebugPrint("读取家电列表时出错：\n\t\t\t" + DeviceName ?? "未知家电" + " (" + Model ?? "未知型号" + ")" + ex.Message);
                        }
                    }
                }
                DataReader.Close();
            }
            UnityModule.DebugPrint("在线家电读取完毕！");
        }

        #endregion

        private void DomesticApplianceItem_ItemClick(object sender, EventArgs e)
        {
            if (-1 < (int)DataBaseController.ExecuteScalar("SELECT FD FROM devicebase WHERE MAC='{0}'", (sender as DomesticApplianceItem).MAC))
            {
                //如果用户点击的家电仍在线，更新家电信息
                UnityModule.DebugPrint("点击家电项目，加载家电信息和事件记录...");
                ShowDomesticApplianceInfo((sender as DomesticApplianceItem).MAC);
                ShowDomesticApplianceEventLog((sender as DomesticApplianceItem).MAC);
            }
            else
            {
                ResetDAInfoPanel();
                (sender as IDisposable).Dispose();
            }
        }

        #region "按钮动态效果"
        private void Button_MouseDown(object sender, MouseEventArgs e)
        {
            (sender as Label).Image = UnityResource.ResourceManager.GetObject((sender as Label).Tag + "_2") as Image;
        }

        private void Button_MouseEnter(object sender, EventArgs e)
        {
            (sender as Label).Image = UnityResource.ResourceManager.GetObject((sender as Label).Tag + "_1") as Image;
        }

        private void TitleButton_MouseLeave(object sender, EventArgs e)
        {
            if (((sender as Label).Tag as string) == PanelStateNow.ToString())
                (sender as Label).Image = UnityResource.ResourceManager.GetObject((sender as Label).Tag + "_0") as Image;
            else
                (sender as Label).Image = UnityResource.ResourceManager.GetObject((sender as Label).Tag + "_") as Image;
        }

        private void Button_MouseLeave(object sender, EventArgs e)
        {
            (sender as Label).Image = UnityResource.ResourceManager.GetObject((sender as Label).Tag + "_0") as Image;
        }

        private void Button_MouseUp(object sender, MouseEventArgs e)
        {
            (sender as Label).Image = UnityResource.ResourceManager.GetObject((sender as Label).Tag + "_1") as Image;
        }
        #endregion

        #region "顶部按钮点击事件"
        private void ControlLabel_Click(object sender, EventArgs e)
        {
            PanelStateNow = PanelState.Control;
        }

        private void InfoLabel_Click(object sender, EventArgs e)
        {
            PanelStateNow = PanelState.Info;
        }

        private void CardLabel_Click(object sender, EventArgs e)
        {
            PanelStateNow = PanelState.Card;
        }

        private void ExitButton_Click(object sender, EventArgs e)
        {
            if (!AllowToClose)
            {
                if (new MyMessageBox("您真的要退出智能家居系统吗？", MyMessageBox.IconType.Question).ShowDialog(this) != DialogResult.OK) return;
                ExitApplication();
            }
        }

        #endregion

        #region "家电信息页面事件"

        /// <summary>
        /// 编辑家电信息并储存进数据库
        /// </summary>
        private void EditDAInfo(object sender, EventArgs e)
        {
            if (MACValueLabel.Text == "(unknown)") return;
            string LabelName= (sender as Label).Name.Remove((sender as Label).Name.Length - "ValueLabel".Length),UserInput = "";
            if( MyMessageBox.ShowInputBox(string.Format("请输入 {0} 信息：",LabelName),ref UserInput, (sender as Label).Text,50) != DialogResult.OK) return;
            if (DataBaseController.ExecuteNonQuery("UPDATE devicebase SET {0} = '{1}' WHERE MAC='{2}'", LabelName, UserInput,MACValueLabel.Text))
            {
                (sender as Label).Text = UserInput;
                DomesticApplianceItem.GetDAByMAC(MACValueLabel.Text)?.SetDeviceNameAndDescription(DeviceNameValueLabel.Text,DescriptionValueLabel.Text);
                UnityModule.DebugPrint("{0} 值更新成功！",LabelName);
            }
            else
            {
                new MyMessageBox(string.Format("{0} 值编辑失败，请重试！",LabelName),MyMessageBox.IconType.Error).ShowDialog(this);
                UnityModule.DebugPrint("{0} 值更新失败！", LabelName);
            }

        }

        /// <summary>
        /// 初始化家电信息页面
        /// </summary>
        private void ResetDAInfoPanel()
        {
            foreach (Label ValueLabel in InfoTablePanel.Controls)
            {
                if (ValueLabel.Name.EndsWith("ValueLabel"))ValueLabel.Text = "(unknown)";
            }
            EventListView.Items.Clear();
        }

        /// <summary>
        /// 点击家电事件列表表头进行排序
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void EventView_ColumnClick(object sender, ColumnClickEventArgs e)
        {
            UnityModule.DebugPrint("事件列表按第 " + e.Column + " 列排序。");
            EventListView.ListViewItemSorter = new ListViewItemComparer(e.Column);
        }

        /// <summary>
        /// 把上次读取的数据里读取家电信息显示在信息页面
        /// </summary>
        /// <param name="mac">家电的MAC地址</param>
        private void ShowDomesticApplianceInfo(string mac)
        {
            if (string.IsNullOrEmpty(mac)) return;
            using (MySqlDataReader DataReader = DataBaseController.ExecuteReader(String.Format("SELECT * FROM devicebase WHERE MAC= '{0}'", mac)))
            {
                EventListView.Items.Clear();
                if (DataReader == null) return;

                if (DataReader.HasRows)
                {
                    while (DataReader.Read())
                    {
                        try
                        {
                            DeviceNameValueLabel.Text = DataReader["DeviceName"] as string;
                            ManufactorValueLabel.Text = DataReader["Manufactor"] as string;
                            ModelValueLabel.Text = DataReader["Model"] as string;
                            MACValueLabel.Text = DataReader["MAC"] as string;
                            TypeValueLabel.Text = DataReader["Type"] as string;
                            DescriptionValueLabel.Text = DataReader["Description"] as string;
                            FDValueLabel.Text = DataReader["FD"].ToString();
                        }
                        catch (Exception ex)
                        {
                            UnityModule.DebugPrint("读取家电信息时遇到错误：" + ex.Message);
                        }
                    }
                }
                DataReader.Close();
            }
            UnityModule.DebugPrint("显示家电信息完成。");
        }

        /// <summary>
        /// 从数据库读取家电的事件记录
        /// </summary>
        /// <param name="mac">家电设备的MAC地址</param>
        private void ShowDomesticApplianceEventLog(string mac)
        {
            if (string.IsNullOrEmpty(mac)) return;
            using (MySqlDataReader DataReader = DataBaseController.ExecuteReader(String.Format("SELECT * FROM eventbase WHERE MAC= '{0}'",mac)))
            {
                EventListView.Items.Clear();
                if (DataReader == null) return;

                if (DataReader.HasRows)
                {
                    while (DataReader.Read())
                    {
                        try
                        {
                            UnityModule.DebugPrint("读取到事件：" + DataReader["EventName"].ToString());
                            EventListView.Items.Add(new ListViewItem(new string[] {UnixTimeToString((long)(int)DataReader["EventTime"]), DataReader["EventName"].ToString(), DataReader["EventDescription"].ToString() }));
                        }
                        catch (Exception ex)
                        {
                            UnityModule.DebugPrint("读取事件时遇到错误："+ ex.Message);
                        }
                    }
                }
                DataReader.Close();
            }
            UnityModule.DebugPrint("读取家电事件列表完成。");
        }

        /// <summary>
        /// 把Unix时间戳转换为本地格式化时间
        /// </summary>
        /// <param name="UnixTime"></param>
        /// <returns></returns>
        private string UnixTimeToString(long UnixTime)
        {
            //神奇勿动！
            /*
             * 首先创建等于Unix时间戳的起始日期(1970/1/1 0:0:0)的时间对象
             * 为新的时间对象加上时间戳表示的时间
             * 把上述UTC时间转换为本地时间
             * 最后格式化输出（HH：24小时制；hh：12小时制）！
             */
            return new DateTime(1970, 1, 1, 0, 0, 0,DateTimeKind.Utc).AddSeconds(Convert.ToDouble(UnixTime)).ToLocalTime().ToString("yyyy/MM/dd HH:mm:ss");
        }

        #endregion

        private void LogoLabel_Click(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Maximized)
                this.WindowState = FormWindowState.Normal;
            else if (this.WindowState == FormWindowState.Normal)
                this.WindowState = FormWindowState.Maximized;
        }

        private void DateAndTimeTimer_Tick(object sender, EventArgs e)
        {
            UnityModule.DebugPrint("心跳更新数据...");
            TimeLabel.Text = DateTime.Now.ToString("yyyy-MM-dd\nhh:mm");

            LoadDomesticAppliance();

            if (PanelStatenow == PanelState.Info)
            {
                if (DomesticApplianceItem.ActiveItem != null && !string.IsNullOrEmpty(DomesticApplianceItem.ActiveItem.MAC))
                {
                    ShowDomesticApplianceInfo(DomesticApplianceItem.ActiveItem.MAC);
                    ShowDomesticApplianceEventLog(DomesticApplianceItem.ActiveItem.MAC);
                }
            }
        }

    }
}
