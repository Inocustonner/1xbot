using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace _1xbetVilka {
    public partial class Form1 : Form {
        private Thread thr;
        private bool f = true;
        private string linksold = "";
        const string path = "links.txt";
        const string usersets = "usersets.txt";


        public Form1() {
            InitializeComponent();
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;
            if (File.Exists(usersets)) {
                var usets = File.ReadAllText(usersets).Split(';');
                textBox1.Text = usets[0];
                textBox12.Text = usets[1];
                textBox5.Text = usets[2];
            }
        }


        private void button1_Click(object sender, EventArgs e) {
            if (thr != null) { thr.Abort(); thr.Join(); }
            f = true;
            button1.Enabled = false;
            thr = new Thread(StartStavka);
            thr.IsBackground = true;
            thr.Start();
        }


        private void StartStavka() {
            Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("en-US");
            WebClient wb = new WebClient();
            wb.Encoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            wb.Headers.Add("Cookie", "lng=ru; SESSION=" + textBox8.Text);
            wb.Headers.Add("X-Requested-With", "XMLHttpRequest");

            var usid1 = textBox1.Text.Split(':')[0];

            if (!File.Exists(path)) {
                File.Create(path).Close();
            }

            linksold = File.ReadAllText(path);

            File.WriteAllText(usersets, usid1 + ";" + textBox12.Text + ";" + textBox5.Text);

            var ligas_set_inc = textBox4.Text.Split(',');
            var ligas_set_ex = textBox2.Text.Split(',');


            while (f) {
                try {
                    Invoke((MethodInvoker)delegate () { textBox3.Text = "Работаю..."; });

                    var domain = textBox5.Text;
                    var txt = wb.DownloadString($"https://{domain}/service-api/LiveFeed/Get1x2_VZip?sports=3&count=50&mode=4&country=2&partner=51");
                    var s = txt.Split(new string[] { "\"AE\"" }, StringSplitOptions.None);

                    for (int i = 1; i < s.Length; i++) {
                        if (!f) { 
                            break; 
                        }

                        var block = s[i];
                        var cod = Substring("\"I\":", block, ",");
                        var liga = Substring("\"L\":", block, ",");
                        var command1 = Substring("\"O1\":", block, ",");
                        var command2 = Substring("\"O2\":", block, ",");
                        var game_time = Substring("\"TS\":", block, "}");
                        var score_difference = 0;

                        if (block.Contains(",\"FS\":{")) {
                            var total_score = Substring(",\"FS\":{", block, "}");
                            var score1 = "0"; 
                            var score2 = "0";

                            if (total_score != "") {
                                var score = total_score.Split(',');

                                for (var j = 0; j < score.Length; j++) {
                                    var block_score = score[j];
                                    if (!block_score.Contains("S1")) { score1 = block_score.Split(':')[1]; }
                                    if (!block_score.Contains("S2")) { score2 = block_score.Split(':')[1]; }
                                }
                            }

                            score_difference = Math.Abs(Convert.ToInt32(score1) - Convert.ToInt32(score2));
                        }

                        OutputMatchLog(liga, score_difference, command1, command2, ligas_set_inc, ligas_set_ex);

                        var kfs = block.Split('{');
                        var tm = ""; var par = "";

                        foreach (var elem in kfs) {
                            if (elem.Contains("\"T\":10}") && elem.Contains("\"CE\":1,\"G\":17,")) {
                                par = SubstringRev(":", elem, ",\"T\":10}");
                                tm = SubstringRev("\"C\":", elem, ",\"CE\":1,\"G\":17,\"P\"");
                            }
                        }

                        var link = command1 + "-" + command2 + " ";

                        if ((score_difference >= (int)numericUpDown1.Value && score_difference <= (int)numericUpDown3.Value) && !linksold.Contains(cod)) {
                            var liga_bool = false;

                            if (ligas_set_inc[0] != "") {
                                foreach (var liga_elem in ligas_set_inc) {
                                    if (liga.Contains(liga_elem)) {
                                        liga_bool = true;
                                        break;
                                    }
                                }
                            }

                            if (ligas_set_ex[0] != "") {
                                foreach (var liga_elem in ligas_set_ex) {
                                    if (liga.Contains(liga_elem)) {
                                        liga_bool = false;
                                        break;
                                    }
                                }
                            }
                            Console.WriteLine($"Делаем ставку на этот матч? {liga_bool && par != "" && Convert.ToInt32(game_time) >= (int)numericUpDown4.Value}");
                            if (liga_bool && par != "" && Convert.ToInt32(game_time) >= (int)numericUpDown4.Value) {
                                var summ = numericUpDown2.Text;
                                var otchet_str = "ТМ" + par + " " + tm + " " + link + " " + summ;

                                Console.WriteLine("Попытка сделать ставку");
                                var stavka = MakeStavka(wb, tm, otchet_str, "10", cod, domain, textBox12.Text, usid1, summ, par);
                            }
                        }
                        Console.WriteLine(""); // Отделяет разные матчи
                    }

                    Console.WriteLine("-----"); // Отделяет разные выводы логов

                    if (!f) {
                        break;
                    }

                    for (int j = 5; j >= 1; j--)
                    {
                        this.Invoke((MethodInvoker)delegate () { textBox3.Text = "Пауза " + j + " сек"; });
                        if (!f) { 
                            break; 
                        }

                        Thread.Sleep(1000);

                        if (!f) { 
                            break; 
                        }
                    }

                    Thread.Sleep(500);
                }
                catch (Exception) {

                }
            }
        }


        private static string Substring(string T_, string ForS, string _T) {

            try {
                if (T_.Length == 0 || ForS.Length == 0 || _T.Length == 0) return "";
                if (!ForS.Contains(T_) || !ForS.Contains(_T)) return "";

                string s = ForS;
                string str1 = s.Split(new[] { T_ }, StringSplitOptions.None)[1];

                return str1.Split(new[] { _T }, StringSplitOptions.None)[0];
            }
            catch (Exception e) {
                return "";
            }
        }


        private Boolean MakeStavka(WebClient wb, string kf, string link, string type,
                            string game_id, string domen, string uhash, string usid, string summ, string par = "0") {

            try {
                if (Convert.ToInt32(summ) < 20) { summ = "20"; }

                var betGUID = "";
                var txtlink = link + "  TRY ";

                Invoke((MethodInvoker)delegate () { textBox3.Text = "Ставлю..."; });

                this.Invoke((MethodInvoker)delegate () {
                    textBox9.AppendText(txtlink);
                });

                for (var c = 0; c <= 3; c++) {

                    wb.Headers.Add("Content-Type", "application/json; charset=UTF-8");
                    wb.Headers.Add("Accept-Language", "ru-RU,ru;q=0.8,en-US;q=0.5,en;q=0.3");

                    var data = "{\"coupon\":{\"Live\":true,\"Events\":[{\"GameId\":\"" + game_id + "\"," +
                               "\"Type\":" + type + ",\"Coef\":" + kf + ",\"Param\":" + par + ",\"PV\":null,\"PlayerId\":0," +
                               "\"Kind\":1,\"InstrumentId\":0,\"Seconds\":0,\"Price\":0,\"Expired\":0}],\"Summ\"" +
                               ":\"" + summ + "\",\"Lng\":\"ru\",\"UserId\":" + usid + ",\"Vid\":0,\"hash\":\"" + uhash + "\"" +
                               ",\"CfView\":0,\"notWait\":true,\"CheckCf\":1,\"partner\":25" + betGUID + "}}";

                    if (!f) { 
                        break; 
                    }
                    OutputDataToFile("response", data);
                    var response = wb.UploadString($"https://{domen}/web-api/datalinelive/putbetscommon", "POST", data);

                    if (!f) { 
                        break; 
                    }

                    File.WriteAllText("txt.txt", data);

                    if (response == string.Empty) {
                        Invoke((MethodInvoker)delegate () { textBox9.AppendText("Ошибка авторизации!" + Environment.NewLine); });
                        Invoke(new MethodInvoker(Stop));

                        Console.WriteLine("Ставка не сделана - код ошибки 1");
                        return false;
                    }

                    if (response.Contains("betGUID")) { 
                        betGUID = ",\"betGUID\":\"" + Substring("\"betGUID\":\"", response, "\"") + "\""; 
                    }

                    if (response.Contains("\"waitTime\"")) {
                        var waitTime = Substring("\"waitTime\":", response, "}");
                        if (waitTime != "") { 
                            Thread.Sleep(Convert.ToInt32(waitTime) + 100); 
                        }
                    }

                    if (response.Contains("\"Success\":true") & response.Contains("\"waitTime\":0}"))//
                    {

                        this.Invoke((MethodInvoker)delegate () {
                            textBox9.AppendText(" OK" + Environment.NewLine);
                        });

                        linksold += game_id + " ";

                        return true;
                    } else if (!response.Contains("Error\":\"\"")) {
                        var err = Substring("Error\":\"", response, "\"");
                        if (!textBox9.Text.Contains(txtlink + " " + err)) {
                            Invoke((MethodInvoker)delegate () { textBox9.AppendText(" " + err + Environment.NewLine); });

                            Console.WriteLine("Ставка не сделана - код ошибки 2");
                            return false;

                        } else {
                            string[] result = textBox9.Lines.Where((x, y) => y != textBox9.Lines.Length - 1).ToArray();
                            Invoke((MethodInvoker)delegate () { textBox9.Text = string.Join(Environment.NewLine, result) + Environment.NewLine; });
                        }

                        Console.WriteLine("Ставка не сделана - код ошибки 3");
                        return false;
                    }
                }
            }
            catch (Exception er) {
                Invoke((MethodInvoker)delegate () { textBox9.AppendText(" Fail" + Environment.NewLine); });

                Console.WriteLine("Ставка не сделана - исключение\n");
                Console.WriteLine(er);
                return false;
            }

            Console.WriteLine("Ставка не сделана - код ошибки 5");
            return false;
        }


        private static string SubstringRev(string T_, string forS, string _T) {
            try {
                if (T_.Length == 0 || forS.Length == 0 || _T.Length == 0) return "";
                if (!forS.Contains(T_) || !forS.Contains(_T)) return "";

                string str1 = forS.Split(new[] { _T }, StringSplitOptions.None)[0];
                var s = str1.Split(new[] { T_ }, StringSplitOptions.None);
                return s[s.Length - 1];


            }
            catch (Exception) {
                return "";
            }
        }


        private string GetScoreSumm(string txtmatch) {
            var obsc = "0";
            if (txtmatch.Contains(",\"FS\":{")) {
                var obs = Substring(",\"FS\":{", txtmatch, "}"); //"""FS"":{", "}"
                var sc1 = "0"; var sc2 = "0";
                if (obs != "") {
                    var sc = obs.Split(',');

                    for (var j = 0; j < sc.Length; j++) {
                        var blocksc = sc[j];
                        if (blocksc.Contains("S1")) { sc1 = blocksc.Split(':')[1]; }
                        if (blocksc.Contains("S2")) { sc2 = blocksc.Split(':')[1]; }
                    }
                }
 
                obsc = sc1 + sc2;
            }

            return obsc;
        }


        private void Stop() {
            f = false;
            Invoke((MethodInvoker)delegate { textBox3.Text = @"Остановлен."; });
            File.WriteAllText(path, linksold);
            button1.Enabled = true;
        }


        private void button2_Click(object sender, EventArgs e) {
            Stop();
        }


        private void OutputMatchLog(string liga, int score_difference, string command1, string command2, string[] ligas_set_inc, string[] ligas_set_ex)  {

            string liga_bool = "Не подходит";
            string score_bool = "Не подходит";

            if (score_difference >= (int)numericUpDown1.Value && score_difference <= (int)numericUpDown3.Value) {
                score_bool = "Подходит";
            }

            if (ligas_set_inc[0] != "") {
                foreach (var liga_elem in ligas_set_inc) {
                    if (liga.Contains(liga_elem)) {
                        liga_bool = "Подходит";
                        break;
                    }
                }
            } else {
                liga_bool = "Подходит";
            }

            if (ligas_set_ex[0] != "") {
                foreach (var liga_elem in ligas_set_ex) {
                    if (liga.Contains(liga_elem)) {
                        liga_bool = "Не подходит";
                        break;
                    }
                }
            }

            Console.WriteLine($"{liga} - {liga_bool}");
            Console.WriteLine($"Разница в счете: {score_difference} - {score_bool}");
            Console.WriteLine($"Команды: {command1} - {command2}");
        }
        private void OutputBetLog() {

        }

        private void OutputDataToFile(string nameFile, string data) {
            try {
                StreamWriter sw = new StreamWriter($"{nameFile}.txt");
                sw.WriteLine(data);
                sw.Close();
            }
            catch (Exception e) {
                Console.WriteLine("Exception: " + e.Message);
            }
            finally {
                Console.WriteLine($"Данные занесены в файл {nameFile}");
            }
        }

    }
}
