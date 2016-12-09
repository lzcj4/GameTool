﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Windows.Forms;
using PublicUtilities;

namespace PwdReset
{
    public partial class FrmPwdReset : Form
    {
        private IList<GameServer> gameServerList = new List<GameServer>();
        private PwdResetHttperManager httperManager = null;

        public FrmPwdReset()
        {
            InitializeComponent();
            this.Load += FrmWebDetection_Load;
            this.FormClosing += FrmMain_FormClosing;
            WowLogManager.Instance.LogEvent += LogManager_LogEvent;
        }

        UcAdsl ucAdsl = null;
        UcRouter ucRouter = null;
        UcVpnList ucVpnList = null;
        UcVpnItem ucVpn = null;

        private void LoadUcNet()
        {
            ADSLItem adsl = new ADSLItem();
            adsl.EntryName = PwdSetConfig.Instance.ADSLName;
            adsl.Password = PwdSetConfig.Instance.ADSLPwd;
            adsl.User = PwdSetConfig.Instance.ADSLUser;
            ucAdsl = new UcAdsl(adsl, WowLogManager.Instance);
            ucAdsl.TestChanged += ucNetwork_TestChanged;

            RouterItem router = new RouterItem();
            router.RouterType = PwdSetConfig.Instance.RouterType;
            router.IP = PwdSetConfig.Instance.RouterIP;
            router.Password = PwdSetConfig.Instance.RouterPwd;
            router.User = PwdSetConfig.Instance.RouterUser;
            ucRouter = new UcRouter(router, WowLogManager.Instance);
            ucRouter.TestChanged += ucNetwork_TestChanged;

            VPNFile vpn = new VPNFile();
            vpn.EntryName = PwdSetConfig.Instance.VpnEntryName;
            vpn.File = PwdSetConfig.Instance.VpnFile;
            ucVpnList = new UcVpnList(vpn, WowLogManager.Instance);
            ucVpnList.TestChanged += ucNetwork_TestChanged;

            VPNItem vpnItem = new VPNItem();
            vpnItem.EntryName = WowSetConfig.Instance.VpnEntryName;
            vpnItem.IP = WowSetConfig.Instance.VpnIP;
            vpnItem.User = WowSetConfig.Instance.VpnUser;
            vpnItem.Password = WowSetConfig.Instance.VpnPwd;
            ucVpn = new UcVpnItem(vpnItem, WowLogManager.Instance);
            ucVpn.TestChanged += ucNetwork_TestChanged;
        }

        #region UI events

        private void FrmWebDetection_Load(object sender, EventArgs e)
        {
            this.LoadParmas();
            this.LoadUcNet();
            this.chkRestart_CheckedChanged(null, null);
            this.radioCustomRange_CheckedChanged(null, null);
        }

        private bool isClosing = false;

        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            this.isClosing = true;
            this.StopDetectionManager(true);

            WowLogManager.Instance.LogEvent -= LogManager_LogEvent;
            ucAdsl.TestChanged -= ucNetwork_TestChanged;
            ucVpnList.TestChanged -= ucNetwork_TestChanged;
            ucRouter.TestChanged -= ucNetwork_TestChanged;
            ucVpn.TestChanged -= ucNetwork_TestChanged;
            this.Load -= FrmWebDetection_Load;
            this.FormClosing -= FrmMain_FormClosing;

            //FileExportManager.Instance.Save();
            PwdSetConfig.Instance.Save();
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            this.IniHttperManamger();
            DetectionParamsItem detectionItem = GetDetectionParamsItem();
            if (null != detectionItem)
            {
                this.ClearLog();
                this.SetEnableForButton(false);
                this.ShowProcessCount(true);
                this.StartSpendTimer();
                httperManager.Start(detectionItem);
            }
            SoundPlayer.PlayAlter();
        }

        private void btnFile_Click(object sender, EventArgs e)
        {
            OpenFileDialog openDialog = new OpenFileDialog();
            openDialog.Filter = "文本文件(*.txt)|*.txt|所有文件(*.*)|*.*";
            openDialog.Multiselect = false;
            openDialog.ShowDialog();
            if (!string.IsNullOrEmpty(openDialog.FileName))
            {
                this.txtFilePath.Text = openDialog.FileName;
            }
            else
            {
                MessageBox.Show("请选择一个（破宝）帐户数据文件", "（破宝）提醒");
            }
        }

        private bool isManualStopped = false;

        private void btnStop_Click(object sender, EventArgs e)
        {
            this.isManualStopped = true;
            this.StopDetectionManager(false);
            this.SetEnableForButton(true);
            SoundPlayer.PlayAlter();
        }

        private void btnPaused_Click(object sender, EventArgs e)
        {
            if (null != this.httperManager)
            {
                if (!this.httperManager.IsPaused)
                {
                    this.httperManager.PauseProcess();
                    this.btnPaused.Text = "继续";
                    SoundPlayer.PlayAlter();
                    this.StopSpendTimer();
                }
                else
                {
                    this.httperManager.RestartProcess();
                    this.btnPaused.Text = "暂停";
                    this.StartSpendTimer();
                }
            }
        }

        private void ClearLog()
        {
            itemCount = 0;
            txtLog.Text = string.Empty;
            progressProcess.Value = 0;
        }

        /// <summary>
        /// If btnStart is true ,btnStop is false
        /// </summary>
        /// <param name="isEnabled"></param>
        private void SetEnableForButton(bool isEnabled)
        {
            this.btnStart.Enabled = isEnabled;
            this.btnClear.Enabled = isEnabled;
            this.btnPaused.Enabled = !isEnabled;
            this.btnStop.Enabled = this.btnPaused.Enabled;

            if (!isEnabled)
            {
                this.tabWebDetection.SelectedTab = pageDisplay;
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            CaptchaHelper.Instance.ClearCaptchaFolder();
        }

        private void radio_CheckedChanged(object sender, EventArgs e)
        {
            this.CheckNetworkPanel();
        }

        private void ucNetwork_TestChanged(object sender, BoolEventArgs e)
        {
            this.BeginInvoke(new ThreadStart(delegate()
            {
                if (e.IsTrue)
                {
                    this.tabWebDetection.SelectTab(pageDisplay);
                    txtLog.Clear();
                    this.StartReconnectTimer();
                }
                else
                {
                    this.StopReconnectTimer();
                }
            }));
        }

        private void CheckNetworkPanel()
        {
            if (radioADSL.Checked)
            {
                panelNet.Controls.Clear();
                panelNet.Controls.Add(ucAdsl);
            }

            if (radioRouter.Checked)
            {
                panelNet.Controls.Clear();
                panelNet.Controls.Add(ucRouter);
            }

            if (radioVpn.Checked)
            {
                panelNet.Controls.Clear();
                panelNet.Controls.Add(ucVpn);
                //panelNet.Controls.Add(ucVpnList);
            }
        }

        private void chkRestart_CheckedChanged(object sender, EventArgs e)
        {
            groupRestartMode.Enabled = chkReconnect.Checked;
            if (!chkReconnect.Checked)
            {
                radioADSL.Checked = chkReconnect.Checked;
                radioRouter.Checked = chkReconnect.Checked;
                radioVpn.Checked = chkReconnect.Checked;
                panelNet.Controls.Clear();
            }

            this.CheckNetworkPanel();
        }

        private void radioCustomRange_CheckedChanged(object sender, EventArgs e)
        {
            this.txtLower.Enabled = this.radioCustomRange.Checked;
            this.txtUpper.Enabled = this.radioCustomRange.Checked;
        }

        private void SetBtnPausedStateInReconnect(bool isEnabled)
        {
            if (btnPaused.Enabled != isEnabled)
            {
                btnPaused.Enabled = isEnabled;
            }
        }

        #endregion UI events

        #region Business events

        private void LogManager_LogEvent(string log)
        {
            if (this.isClosing) { return; }

            this.BeginInvoke(new ThreadStart(delegate()
                            {
                                txtLog.AppendText("\r\n" + log);
                                SetUIByLog(log);
                            }));
        }

        private void SetUIByLog(string log)
        {
            if (TextHelper.IsContains(log, "角色"))
            {
                this.ShowProcessCount(false);
            }
            else if (TextHelper.IsContains(log, "正在进行网络重连"))
            {
                SetBtnPausedStateInReconnect(false);
            }
            else if (TextHelper.IsContains(log, "程序就绪，正在重新启动查询功能"))
            {
                SetBtnPausedStateInReconnect(true);
            }
        }

        private const int MaxLine = 200;
        private int itemCount = 0;

        private void detectionManager_ProcessItemChanged(object sender, ProcessEventArgs<PwdResetItem> e)
        {
            lock (this)
            {
                if (this.isClosing) { return; }

                try
                {
                    this.BeginInvoke(new ThreadStart(delegate()
                              {
                                  if (this.httperManager.TotalCount != this.progressProcess.Maximum)
                                  {
                                      this.progressProcess.Maximum = this.httperManager.TotalCount;
                                      this.progressProcess.Minimum = 0;
                                  }

                                  //string line = "\r\n" + e.Item.UserDetail;
                                  //txtLog.AppendText(line);
                                  //if IsTooManyAttempt,don't export to file and update progress
                                  //if (e.Item.IsTooManyAttempt)
                                  //{
                                  //    return;
                                  //}


                                  if (itemCount < this.httperManager.TotalCount)
                                  {
                                      this.progressProcess.Value = ++itemCount;
                                  }

                                  //FileExportManager.Instance.Output(e.Item, httperManager.HttperParamsItem);
                                  this.ShowProcessCount(false);

                                  if (txtLog.Lines.Length >= MaxLine)
                                  {
                                      txtLog.Clear();
                                  }
                                  else
                                  {
                                      //txtLog.SelectedText = line;
                                      txtLog.ScrollToCaret();
                                  }
                              }));
                }
                catch (Exception ex)
                {
                    WowLogManager.Instance.Info("Update UI error:" + ex.Message);
                }
            }
        }

        private void ShowProcessCount(bool isRestart)
        {
            if (isRestart)
            {
                this.txtTotalCount.Text = "0";
                this.txtRemainCount.Text = "0";
                this.txtProcessedCount.Text = "0";
                this.txtSuccessCount.Text = "0";
                this.txtFailedCount.Text = "0";
                this.txtRetryCount.Text = "0";
                this.txtUselessCount.Text = "0";
            }
            else
            {
                if (null != this.httperManager)
                {
                    this.txtTotalCount.Text = this.httperManager.TotalCount.ToString();
                    int remainCount = this.httperManager.TotalCount;
                    this.txtRemainCount.Text = (remainCount < 0 ? 0 : remainCount).ToString();
                }
                this.txtProcessedCount.Text = "0";
                this.txtSuccessCount.Text = "0";
                this.txtFailedCount.Text = "0";
                this.txtRetryCount.Text = "0";
                this.txtUselessCount.Text = "0";
            }
        }

        private void ProcessCompleted()
        {
            if (this.isClosing) { return; }

            this.BeginInvoke(new ThreadStart(delegate()
             {
                 //BattleOutptMgt.Instance.LoadLastStopCount();
                 //BattleOutptMgt.Instance.Save();
                 this.ShowProcessCount(false);
                 this.SetEnableForButton(true);
                 this.StopSpendTimer();
                 PlayProcessCompletedSound();
                 MessageBox.Show(string.Format("完成（破宝）网络对号 ！", "（破宝）提醒"));
                 SoundPlayer.StopWarn();
             }));
        }

        private void PlayProcessCompletedSound()
        {
            if (!this.isManualStopped)
            {
                SoundPlayer.PlayWarn();
            }
            isManualStopped = false;
        }

        private void IniHttperManamger()
        {
            if (null == httperManager)
            {
                httperManager = new PwdResetHttperManager();
                httperManager.ProcessItemChanged += detectionManager_ProcessItemChanged;
                httperManager.ProcessCompleted += detectionManager_ProcessCompleted;
                httperManager.ReconnectChanged += detectionManager_ReconnectChanged;
            }
        }

        private void detectionManager_ReconnectChanged(object sender, BoolEventArgs e)
        {
            if (this.isClosing) { return; }

            this.BeginInvoke(new ThreadStart(delegate()
            {
                if (e.IsTrue)
                {
                    SoundPlayer.PlayWarn();
                    this.StartReconnectTimer();
                }
                else
                {
                    this.StopReconnectTimer();
                }
            }));
        }

        private void detectionManager_ProcessCompleted(object sender, BoolEventArgs e)
        {
            this.ProcessCompleted();
        }

        private void StopDetectionManager(bool isDisposed)
        {
            if (null != httperManager)
            {
                if (isDisposed)
                {
                    httperManager.ProcessItemChanged -= detectionManager_ProcessItemChanged;
                    httperManager.ProcessCompleted -= detectionManager_ProcessCompleted;
                    httperManager.ReconnectChanged -= detectionManager_ReconnectChanged;
                    httperManager.Dispose();
                }
                else
                {
                    httperManager.Stop();
                }
            }
            this.StopReconnectTimer();
            this.StopSpendTimer();
        }

        #endregion Business events

        #region Net reconnect Timer

        System.Windows.Forms.Timer reconnectTimer = null;
        int timerCount = 0;

        private void StartReconnectTimer()
        {
            if (null == this.reconnectTimer)
            {
                this.reconnectTimer = new System.Windows.Forms.Timer();
                // 1 second
                this.reconnectTimer.Interval = 1 * 1000;
                this.reconnectTimer.Tick += reconnectTimer_Tick;
            }
            this.StopReconnectTimer();
            this.reconnectTimer.Start();
        }

        private void reconnectTimer_Tick(object sender, EventArgs e)
        {
            this.labSeconds.Text = (++timerCount).ToString();
        }

        private void StopReconnectTimer()
        {
            if ((null != this.reconnectTimer) && this.reconnectTimer.Enabled)
            {
                this.reconnectTimer.Stop();
            }
            timerCount = 0;
            this.labSeconds.Text = timerCount.ToString();
        }

        System.Windows.Forms.Timer spendTimer = null;
        long spendCount = 0;

        private void StartSpendTimer()
        {
            if (null == this.spendTimer)
            {
                this.spendTimer = new System.Windows.Forms.Timer();
                // 1 second
                this.spendTimer.Interval = 1 * 1000;
                this.spendTimer.Tick += spendTimer_Tick;
            }
            this.StopSpendTimer();
            spendCount = 0;
            this.spendTimer.Start();
        }

        private void spendTimer_Tick(object sender, EventArgs e)
        {
            spendCount++;
            this.txtSpend.Text = TimeSpan.FromSeconds(spendCount).ToString();
        }

        private void StopSpendTimer()
        {
            if ((null != this.spendTimer) && this.spendTimer.Enabled)
            {
                this.spendTimer.Stop();
            }
            this.txtSpend.Text = TimeSpan.FromSeconds(spendCount).ToString();
        }

        #endregion Net reconnect Timer

        private void btnStopSound_Click(object sender, EventArgs e)
        {
            SoundPlayer.StopWarn();
        }
    }
}