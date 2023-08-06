using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace _1xbetVilka
{
    public partial class Form1 : Form
    {
        private Thread thr;
        //private WebClient wb;
        private bool f = true;
        //private List<string> links = new List<string>();
        private string linksold = "";
        //private string uhash = "";
        //private string ssid = "";
        //private string usid = "";

        const string path = "links.txt";
        const string usersets = "usersets.txt";

        public Form1()
        {
            InitializeComponent();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            if (File.Exists(usersets))
            { 
                var usets = File.ReadAllText(usersets).Split(';');
                textBox1.Text = usets[0];
                textBox12.Text = usets[1];
                textBox5.Text = usets[2];
            }
        }

       


        private void button1_Click(object sender, EventArgs e)
        {
            if (thr != null)
            { thr.Abort(); thr.Join(); }
            f = true;
            button1.Enabled = false;
            thr = new Thread(StartStavka);
            thr.IsBackground = true;
            thr.Start();
        }

        private void StartStavka()
        {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            WebClient wb = new WebClient();
            wb.Encoding = Encoding.UTF8;
  
            

            //var txttest = wb.DownloadString("https://2ip.ru");
            //txttest = Substring("d_clip_button\">", txttest, "<");
            //MessageBox.Show(txttest);
            wb.Headers.Add("Cookie", "lng=ru; SESSION=" + textBox8.Text);
            //wb.Headers.Add("Content-Type", "application/json; charset=UTF-8");
            wb.Headers.Add("X-Requested-With", "XMLHttpRequest");

           
            // wb2.Headers.Add("X-Requested-With", "XMLHttpRequest");
            // wb.Headers.Add("X-Requested-With", "XMLHttpRequest");

            var usid1 = textBox1.Text.Split(':')[0];

            if (!File.Exists(path))
            { File.Create(path).Close(); }

            //if (!File.Exists(corpath))
            //{ File.Create(corpath).Close(); }

            linksold = File.ReadAllText(path);

            File.WriteAllText(usersets, usid1 + ";" + textBox12.Text + ";" + textBox5.Text);

            var ligas_set_inc = textBox4.Text.Split(',');
            var ligas_set_ex = textBox2.Text.Split(',');


            while (f)
            {
                try
                {
                    Invoke((MethodInvoker)delegate () { textBox3.Text = "Работаю..."; });

                    var domain = textBox5.Text;
                    var api = "service-api";
                    
                    var txt = wb.DownloadString(
                        $"https://{domain}/{api}/LiveFeed/Get1x2_VZip?sports=3&count=50&mode=4&country=2&partner=51");

                    Console.WriteLine(txt);
                    var s = txt.Split(new string[] { "\"AE\"" }, StringSplitOptions.None); //AE
     
                    for (int i = 1; i < s.Length; i++)
                    {
                        if (!f) { break; }
                        var block = s[i];
                        //MessageBox.Show(s.Length.ToString());

                        var cod = Substring("\"I\":", block, ",");
                        var liga = Substring("\"L\":", block, ",");
                        //MessageBox.Show(liga);
                        var comm1 = Substring("\"O1\":", block, ",");
                        var comm2 = Substring("\"O2\":", block, ",");

                        var game_time = Substring("\"TS\":", block, "}");

                        var obsc = 0;
                        if (block.Contains(",\"FS\":{"))
                        {
                            var obs = Substring(",\"FS\":{", block, "}"); //"""FS"":{", "}"
                            var sc1 = "0"; var sc2 = "0";
                            if (obs != "")
                            {
                                var sc = obs.Split(',');

                                for (var j = 0; j < sc.Length; j++)
                                {
                                    var blocksc = sc[j];
                                    if (!blocksc.Contains("S1")) { sc1 = blocksc.Split(':')[1]; }
                                    if (!blocksc.Contains("S2")) { sc2 = blocksc.Split(':')[1]; }
                                }
                            }

                            obsc = Math.Abs(Convert.ToInt32(sc1) - Convert.ToInt32(sc2));
                        }

                        var kfs = block.Split('{');
                        var tm = ""; var par = "";

                        foreach (var elem in kfs)
                        {
                            if (elem.Contains("\"T\":10}") && elem.Contains("\"CE\":1,\"G\":17,"))
                            {
                                par= SubstringRev(":", elem, ",\"T\":10}"); //if (p1 == "") { p1 = "0"; }:" + tm + ",\"T\":10}
                                tm = SubstringRev("\"C\":", elem, ",\"CE\":1,\"G\":17,\"P\""); //if (p1 == "") { p1 = "0"; }
                            }
                        }

                        var link =  comm1 + "-" + comm2 + " "; //liga + " " +

                        //MessageBox.Show(link + tm + " " + par);
                        //Short&& checkBox1.Checked==true  && checkBox2.Checked==true
                        if ((obsc >= (int)numericUpDown1.Value && obsc <= (int)numericUpDown3.Value) && !linksold.Contains(cod))
                        {
                            var liga_bool = false;

                            if (ligas_set_inc[0] !="")
                            { 
                                foreach (var liga_elem in ligas_set_inc)
                                {
                                    if (liga.Contains(liga_elem))
                                    {
                                        liga_bool = true;
                                        break;
                                    }
                                }
                            }

                            
                            if (ligas_set_ex[0] != "")
                            {
                                foreach (var liga_elem in ligas_set_ex)
                                {
                                    if (liga.Contains(liga_elem))
                                    {
                                        liga_bool = false;
                                        break;
                                    }
                                }
                            }   // }

                           
                            if (liga_bool && par != "" && Convert.ToInt32(game_time) >= (int)numericUpDown4.Value)
                            {
                                //MessageBox.Show(comm1 + " " + liga_bool + " " + game_time);
                                //private void MakeStavka(string kf, string link, string type,
                                //string game_id, string domen, string SSID, string usid, string par = "0")
                                var summ = numericUpDown2.Text;
                                var otchet_str = "ТМ" + par + " " + tm + " " + link + " " + summ;
                                var stavka = MakeStavka(wb, tm, otchet_str, "10", cod, domain, textBox12.Text, usid1, summ, par);
                            }

                        }

                        //MessageBox.Show("ok");

                       

                    }

                    //File.WriteAllText(path, linksold);
                    if (!f) { break; }
                    //File.AppendAllText(path, link);


                    for (int j = 5; j >= 1; j--)//2
                    {
                        this.Invoke((MethodInvoker)delegate () { textBox3.Text = "Пауза " + j + " сек"; });
                        if (!f) { break; }
                        Thread.Sleep(1000);//60
                        if (!f) { break; }
                    }

                    Thread.Sleep(1000);
                }
                catch (Exception)
                {

                }
            }
        }


        private static string Substring(string T_, string ForS, string _T)
        {
            //Begin:
            try
            {
                if (T_.Length == 0 || ForS.Length == 0 || _T.Length == 0) return "";
                if (!ForS.Contains(T_) || !ForS.Contains(_T)) return "";

                string s = ForS;
                //int istart = s.IndexOf(T_) + T_.Length;
                //return s.Substring(istart, s.IndexOf(_T) - istart);
                string str1 = s.Split(new[] { T_ }, StringSplitOptions.None)[1];
                return str1.Split(new[] { _T }, StringSplitOptions.None)[0];

            }
            catch (Exception e)
            {
                return "";
            }
        }
        private Boolean MakeStavka(WebClient wb, string kf,  string link, string type,
                            string game_id, string domen, string uhash, string usid, string summ, string par="0")
        {
            //Begin:
            try
            {
                if (Convert.ToInt32(summ)<20) { summ = "20"; }
                //if (!domen.Contains("1x") || !domen.Contains("bet")) return false; //bit
                //var k = "";
                //var param = "0";
                //var summ = "10";
                
                //MessageBox.Show(start_cod + link);
                /*
            switch (flag)
                {
                    case "105Б ":
                        k = "9";
                        param = "0.5";
                        summ = numericUpDown2.Text;
                        if (summ.Trim() == "") { return; }
                        break;
                    case "205Б ":
                        k = "9";
                        param = "0.5";
                        //summ = numericUpDown4.Text;
                        break;
                    
                    case "15Б ":
                        k = "9";
                        param = "1.5";
                        break;
                    
                    case "G30 ":
                        k = "812";
                        //param = "30." + par;//0
                        //summ = numericUpDown1.Text;
                        if (summ.Trim() == "") { return; }
                        break;

                }
                
                wb.Headers.Add("Content-Type", "application/json; charset=UTF-8");
                var resp_balance = wb.UploadString("https://1xstavka.ru/user/balance","{}");
                MessageBox.Show(resp_balance);
                */
                var betGUID = "";

                var txtlink = link + "  TRY "; //" + cod + "

                Invoke((MethodInvoker)delegate ()
                { textBox3.Text = "Ставлю..."; });

                this.Invoke((MethodInvoker)delegate ()
                {
                    textBox9.AppendText(txtlink);
                });

                StartStavka:
                for (var c = 0; c <= 3; c++)
                {
                    
                    wb.Headers.Add("Content-Type", "application/json; charset=UTF-8");
                    wb.Headers.Add("Accept-Language", "ru-RU,ru;q=0.8,en-US;q=0.5,en;q=0.3");

                    var data = "{\"coupon\":{\"Live\":true,\"Events\":[{\"GameId\":\"" + game_id + "\"," +
                               "\"Type\":" + type + ",\"Coef\":" + kf + ",\"Param\":" + par + ",\"PV\":null,\"PlayerId\":0," +
                               "\"Kind\":1,\"InstrumentId\":0,\"Seconds\":0,\"Price\":0,\"Expired\":0}],\"Summ\"" +
                               ":\"" + summ + "\",\"Lng\":\"ru\",\"UserId\":" + usid + ",\"Vid\":0,\"hash\":\"" + uhash + "\"" +
                               ",\"CfView\":0,\"notWait\":true,\"CheckCf\":1,\"partner\":25" + betGUID + "}}";
                    //
                    //{"coupon":{"Live":true,"Events":[{"GameId":"280168089","Type":9,"Coef":1.27,"Param":0.5,"PV":null,"PlayerId":0,"Kind":1,"InstrumentId":0,"Seconds":0,"Price":0,"Expired":0}],"Summ":"10",
                    //"Lng":"ru","UserId":19996591,"Vid":0,"hash":"a25f583980869fac0504b2098d39df14","CfView":0,"notWait":true,"CheckCf":1,"partner":25}}
                    //{"coupon":{"Live":true,"Events":[{"GameId":"311388797","Type":9,"Coef":1.8,"Param":26.5,"PV":null,"PlayerId":0,"Kind":1,"InstrumentId":0,"Seconds":0,"Price":0,"Expired":0}],"Summ":"10","Lng":"ru","UserId":11942217,"Vid":0,"hash":"8d8d63f48d771b73b0ce9fab7f6d06a0","CfView":0,"notWait":true,"CheckCf":1,"partner":25}}
                    //MessageBox.Show(data);
                    if (!f) { break; }

                    var response = wb.UploadString($"https://{domen}/web-api/datalinelive/putbetscommon", "POST", data);
                    Console.WriteLine("response");
                    if (!f) { break; }

                    File.WriteAllText("txt.txt", data);
                    //MessageBox.Show(response);

                    if (response == string.Empty)
                    {
                        //thr.Abort();
                        Invoke((MethodInvoker)delegate ()
                        { textBox9.AppendText("Ошибка авторизации!" + Environment.NewLine); });
                        Invoke(new MethodInvoker(Stop)); //Stop(); 
                        //MessageBox.Show("Ошибка авторизации!");//Неверный код
                        return false;
                    }
                    /*
                    if (response.Contains("\"Balance\":") || response.Contains("\"ErrorCode\":104,"))//1
                    {
                        //thr.Abort();
                        var balan = false;
                        if (response.Contains("\"Balance\":"))
                        {
                            var balance = Substring("\"Balance\":", response, ",");
                            if (Convert.ToDouble(summ) > Convert.ToDouble(balance)) { balan = true; }
                        }
                        if (balan || response.Contains("\"ErrorCode\":104,"))
                        {
                            Invoke((MethodInvoker)delegate ()
                            { textBox7.AppendText("Сумма ставки больше суммы на счету!" + Environment.NewLine); });
                            Invoke(new MethodInvoker(Stop)); //Stop(); 
                            //MessageBox.Show("Ошибка авторизации!");//Неверный код
                            return;
                        }
                    }
                    */

                    if (response.Contains("betGUID")) { betGUID = ",\"betGUID\":\"" + Substring("\"betGUID\":\"", response, "\"") + "\""; }
                    if (response.Contains("\"waitTime\""))
                    {
                        var waitTime = Substring("\"waitTime\":", response, "}");
                        if (waitTime != "") { Thread.Sleep(Convert.ToInt32(waitTime) + 100); }
                    }

                    if (response.Contains("\"Success\":true") & response.Contains("\"waitTime\":0}"))//
                    {

                        this.Invoke((MethodInvoker)delegate ()
                        {
                            textBox9.AppendText(" OK" + Environment.NewLine);
                        });

                        linksold += game_id + " ";
                        /*
                        if (type == "10")
                        { 
                            var pssec = textBox7.Text.Split('-');
                            Random rnd = new Random();
                            var ps = rnd.Next(Convert.ToInt32(pssec[0].Trim()), Convert.ToInt32(pssec[1].Trim()));
                            for (int j = ps; j >= 1; j--)
                            //for (int j = (int)numericUpDown1.Value; j >= 1; j--) 5
                            {
                                if (!f) { break; }
                                this.Invoke((MethodInvoker)delegate () { textBox3.Text = "Пауза " + j + " сек после ставки"; });
                                Thread.Sleep(1000); //60
                            }
                        }
                        */
                        //MessageBox.Show(response);
                        return true;
                    }
                    else if (!response.Contains("Error\":\"\""))
                    {
                        var err = Substring("Error\":\"", response, "\"");
                        if (!textBox9.Text.Contains(txtlink + " " + err))
                        {
                            Invoke((MethodInvoker)delegate ()
                            { textBox9.AppendText(" " + err + Environment.NewLine); });

                            //if (response.Contains("\"ErrorCode\":129,") || response.Contains("\"ErrorCode\":104,"))
                            // linksold += cod + kf + " "; 
                            /*
                            if (response.Contains("временно заблокирован") &&  type == "10")//заблокирована
                            {
                                //linksold += game_id + par + " ";
                                Invoke((MethodInvoker)delegate ()
                                { textBox3.Text = "временно недоступна"; });
                                Thread.Sleep(5000);
                                goto StartStavka;
                            }
                            */
                            return false;

                        }
                        else
                        {
                            string[] result = textBox9.Lines.Where((x, y) => y != textBox9.Lines.Length - 1).ToArray();
                            Invoke((MethodInvoker)delegate () { textBox9.Text = string.Join(Environment.NewLine, result) + Environment.NewLine; });
                        }
                        return false;
                        //MessageBox.Show(err);
                    }
                }
            }
            catch (Exception er)
            {
                //File.AppendAllText("error.txt", er + "\n\n");
                Invoke((MethodInvoker)delegate ()
                { textBox9.AppendText(" Fail" + Environment.NewLine); });
                //goto Begin;
                return false;
            }

            return false;
        }

       
        private static string SubstringRev(string T_, string forS, string _T)
        {
            try
            {
                if (T_.Length == 0 || forS.Length == 0 || _T.Length == 0) return "";
                if (!forS.Contains(T_) || !forS.Contains(_T)) return "";

                string str1 = forS.Split(new[] { _T }, StringSplitOptions.None)[0];
                var s = str1.Split(new[] { T_ }, StringSplitOptions.None);
                return s[s.Length - 1];


            }
            catch (Exception)
            {
                return "";
            }
        }


        

        private string GetScoreSumm(string txtmatch)
        {
            var obsc = "0";
            if (txtmatch.Contains(",\"FS\":{"))
            {
                var obs = Substring(",\"FS\":{", txtmatch, "}"); //"""FS"":{", "}"
                var sc1 = "0"; var sc2 = "0";
                if (obs != "")
                {
                    var sc = obs.Split(',');

                    for (var j = 0; j < sc.Length; j++)
                    {
                        var blocksc = sc[j];
                        if (blocksc.Contains("S1")) { sc1 = blocksc.Split(':')[1]; }
                        if (blocksc.Contains("S2")) { sc2 = blocksc.Split(':')[1]; }
                    }
                }
               
                obsc = sc1 + sc2;
            }

            return obsc;
        }

        


        private void Stop()
        {
            //if (thr != null) { thr.Abort(); }
            f = false;
            //wb.Dispose();
            Invoke((MethodInvoker)delegate { textBox3.Text = @"Остановлен."; });
            File.WriteAllText(path, linksold);
            button1.Enabled = true;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            Stop();
        }
    }
}
