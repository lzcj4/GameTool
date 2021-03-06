﻿using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using PublicUtilities;
using XBOX.Http;

namespace XBOX
{
    //public class HttpProcessor : HttpProcessorBase<AccountItem, XBoxHttperLogin>
    //{
    //    public HttpProcessor(HttperManagerBase<AccountItem, XBoxHttperLogin> hm)
    //        : base(hm,XBOXLogManager.Instance)
    //    {
    //    }

    //    protected override void StartDetection()
    //    {
    //        while (this.isRunning)
    //        {
    //            AccountItem userItem = httperManager.GetNextItem();
    //            XBoxHttperLogin httpHelper = httperManager.GetHttper();
    //            this.cuttentHttpHelper = httpHelper;
    //            if ((null == userItem) || (null == httpHelper))
    //            {
    //                break;
    //            }
    //            this.stopwatch.Start();
    //            XBOXLogManager.Instance.Info(string.Format("-----------  start detecting new account,email={0},pwd={1}", userItem.EMail, userItem.Password));

    //            try
    //            {
    //                using (httpHelper)
    //                {
    //                    httpHelper.GetState(userItem);
    //                    this.RaiseProcessedItemChanged(userItem);
    //                    this.RaiseReconnectChanged(userItem);
    //                }
    //                this.stopwatch.Stop();
    //                XBOXLogManager.Instance.Info(string.Format("***********  end detecting account,email={0},pwd={1}, state={2}, total spend:{3} s",
    //                                              userItem.EMail, userItem.Password, userItem.State, this.stopwatch.Elapsed.ToString()));
    //                //Thread.Sleep(500);
    //            }
    //            catch (System.Exception ex)
    //            {
    //                XBOXLogManager.Instance.Error(string.Format("DectectionProcess.GetAccountState() failed:{0}", ex.Message));
    //            }
    //        }
    //        this.RaiseProcessCompleted();
    //    }
    //}

    public class HttpProcessor : HttpProcessorBase<AccountItem, XBoxHttperLogin>
    {
        public HttpProcessor(HttperManagerBase<AccountItem, XBoxHttperLogin> hm)
            : base(hm, XBOXLogManager.Instance)
        {
        }

        protected override void StartDetection()
        {
            AutoResetEvent autoResetEvent = new AutoResetEvent(false);
            while (this.isRunning)
            {
                AccountItem userItem = httperManager.GetNextItem();
                if ((null == userItem))
                {
                    break;
                }
                this.stopwatch.Start();
                XBOXLogManager.Instance.Info(string.Format("-----------  start detecting new account,email={0},pwd={1}", userItem.EMail, userItem.Password));

                try
                {
                    //UCXBoxLogin ucLogin = new UCXBoxLogin(httperManager.HttperParamsItem, XBOXLogManager.Instance);
                    //using (ucLogin)
                    //{
                    //    ucLogin.GetState(userItem, autoResetEvent);
                    //}
                    FrmMain.MainFrom.BeginInvoke(new Action(delegate()
                      {
                          FrmMain.MainFrom.GetAccountStateFromUCLogin(userItem, autoResetEvent);
                      }));

                    autoResetEvent.WaitOne();

                    FrmMain.MainFrom.Invoke(new Action(delegate()
                     {
                         FrmMain.MainFrom.StopUCLogin();
                     }));

                    this.RaiseProcessedItemChanged(userItem);
                    this.RaiseReconnectChanged(userItem);
                    this.stopwatch.Stop();
                    XBOXLogManager.Instance.Info(string.Format("***********  end detecting account,email={0},pwd={1}, state={2}, total spend:{3} s",
                                                  userItem.EMail, userItem.Password, userItem.State, this.stopwatch.Elapsed.ToString()));
                    Thread.Sleep(500);
                }
                catch (System.Exception ex)
                {
                    XBOXLogManager.Instance.Error(string.Format("DectectionProcess.GetAccountState() failed:{0}", ex.Message));
                }
            }
            this.RaiseProcessCompleted();
        }
    }
}
