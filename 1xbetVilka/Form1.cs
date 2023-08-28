using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Policy;
using System.Text;
using System.Threading;
using System.Web;
using System.Windows.Forms;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace _1xbetVilka {
    
    public struct Match {
        public string GameId;
        public string Liga;
        public string Command1;
        public string Command2;
        public int Score1;
        public int Score2;
        public int GameTime;
        public string Coefficient;
        public string Parameter;
    }

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
            WebClient wc = new WebClient();
            wc.Encoding = Encoding.UTF8;
            Console.OutputEncoding = Encoding.UTF8;

            wc.Headers.Add("Cookie", "lng=ru; SESSION=" + textBox8.Text);
            wc.Headers.Add("X-Requested-With", "XMLHttpRequest");

            var usid1 = textBox1.Text.Split(':')[0];

            if (!File.Exists(path)) {
                File.Create(path).Close();
            }

            linksold = File.ReadAllText(path);

            File.WriteAllText(usersets, usid1 + ";" + textBox12.Text + ";" + textBox5.Text);

            while (f) {
                try {
                    Invoke((MethodInvoker)delegate () { textBox3.Text = "Работаю..."; });

                    var domain = textBox5.Text;
                    string url = $"https://{domain}/service-api/LiveFeed/Get1x2_VZip?sports=3&count=50&mode=4&country=2";

                    List<Dictionary<string, string>> matches = GetMatches(wc, url);

                    foreach (var match in matches) {

                        bool matchBool = CheckMatch(match);
                        Console.WriteLine($"Делаем ставку на этот матч? {matchBool}");

                        if (matchBool) {
                            string summ = numericUpDown2.Text;
                            string otchet_str = 
                                $"ТМ {match["Parameter"]}, К: {match["Coefficient"]} " +
                                $"{match["Command1"]} - {match["Command2"]} {summ}";

                            Console.WriteLine("Попытка сделать ставку");
                            
                            var stavka = MakeStavka(
                                wc, 
                                match["Coefficient"], 
                                otchet_str, 
                                match["GameId"], 
                                domain, 
                                textBox12.Text, 
                                usid1, 
                                summ, 
                                match["Parameter"]);
                            
                        }

                        Console.WriteLine(""); // Отделяет разные матчи
                    }

                    Console.WriteLine("-----"); // Отделяет разные выводы логов

                    if (!f) {
                        break;
                    }

                    for (int j = 5; j >= 1; j--) {
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
                catch (Exception e) {
                    Console.WriteLine(e);
                }
            }
        }


        private Boolean MakeStavka(WebClient wb, string kf, string link,
                            string game_id, string domain, string uhash, string usid, string summ, string par = "0") {

            try {
                if (Convert.ToInt32(summ) < 10) {
                    Console.WriteLine("Ставка не сделана - маленькая ставка");
                    Invoke((MethodInvoker)delegate () { 
                        textBox9.AppendText(" Fail Минимальная ставка 10" + Environment.NewLine); 
                    });
                    return false;
                }

                string betGUID = "";
                var txtlink = link + "  TRY ";

                Invoke((MethodInvoker)delegate () { 
                    textBox3.Text = "Ставлю..."; 
                });

                this.Invoke((MethodInvoker)delegate () {
                    textBox9.AppendText(txtlink);
                });

                for (var c = 0; c <= 3; c++) {

                    wb.Headers.Add("Content-Type", "application/json; charset=UTF-8");
                    wb.Headers.Add("Accept-Language", "ru-RU,ru;q=0.8,en-US;q=0.5,en;q=0.3");

                    var data = "{\"coupon\":{\"Live\":true,\"Events\":[{\"GameId\":\"" + game_id + "\"," +
                               "\"Type\":10,\"Coef\":" + kf + ",\"Param\":" + par + ",\"PV\":null,\"PlayerId\":0," +
                               "\"Kind\":1,\"InstrumentId\":0,\"Seconds\":0,\"Price\":0,\"Expired\":0}],\"Summ\"" +
                               ":\"" + summ + "\",\"Lng\":\"ru\",\"UserId\":" + usid + ",\"Vid\":0,\"hash\":\"" + uhash + "\"" +
                               ",\"CfView\":0,\"notWait\":true,\"CheckCf\":1,\"partner\":25,\"betGUID\":" + betGUID + "}}";
                    Console.WriteLine($"Попытка: {c+1}");
                    Console.WriteLine(data);
                    Console.WriteLine();
                    if (!f) {
                        break;
                    }

                    string url = $"https://{domain}/web-api/datalinelive/putbetscommon";
                    JObject response = JObject.Parse(wb.UploadString(url, "POST", data));

                    if (!f) {
                        break;
                    }

                    if (response.Count == 0) {
                        Invoke((MethodInvoker)delegate () { textBox9.AppendText("Ошибка авторизации!" + Environment.NewLine); });
                        Invoke(new MethodInvoker(Stop));

                        Console.WriteLine("Ставка не сделана - код ошибки 1");
                        return false;
                    }

                    if ((string)response["Value"]["betGUID"] != null) {
                        betGUID = (string)response["Value"]["betGUID"];
                    }

                    if ((string)response["Value"]["waitTime"] != null) {
                        Thread.Sleep((int)response["Value"]["waitTime"] + 100);
                    }

                    if ((bool)response["Success"] == true & (string)response["Value"]["waitTime"] == "0") {

                        this.Invoke((MethodInvoker)delegate () {
                            textBox9.AppendText(" OK" + Environment.NewLine);
                        });

                        linksold += game_id + " ";

                        return true;
                    } else if ((string)response["Error"] != "") {
                        string error = (string)response["Error"];
                        if (!textBox9.Text.Contains(txtlink + " " + error)) {
                            Invoke((MethodInvoker)delegate () { textBox9.AppendText(" " + error + Environment.NewLine); });

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

            Console.WriteLine("Ставка не сделана - закончились попытки");
            return false;
        }


        private void OutputMatchLog(string liga, int score_difference, string command1, string command2, string[] ligas_set_inc, string[] ligas_set_ex) {

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

        private bool CheckMatch(Dictionary<string, string> match) {
            var ligas_set_inc = textBox4.Text.Split(',');
            var ligas_set_ex = textBox2.Text.Split(',');
            int scoreDifference = Math.Abs(Int32.Parse(match["Score1"]) - Int32.Parse(match["Score2"]));

            OutputMatchLog(match["Liga"], scoreDifference, match["Command1"], match["Command2"], ligas_set_inc, ligas_set_ex);

            if ((scoreDifference >= (int)numericUpDown1.Value && scoreDifference <= (int)numericUpDown3.Value) && !linksold.Contains(match["GameId"])) {
                var liga_bool = false;

                if (ligas_set_inc[0] != "") {
                    foreach (var liga_elem in ligas_set_inc) {
                        if (match["Liga"].Contains(liga_elem)) {
                            liga_bool = true;
                            break;
                        }
                    }
                } else {
                    liga_bool = true;
                }

                if (ligas_set_ex[0] != "") {
                    foreach (var liga_elem in ligas_set_ex) {
                        if (match["Liga"].Contains(liga_elem)) {
                            liga_bool = false;
                            break;
                        }
                    }
                }

                bool matchBool = liga_bool && match["Parameter"] != "" && Int32.Parse(match["GameTime"]) >= (int)numericUpDown4.Value;
                return matchBool;

            }
            return false;
        }


        static List<Dictionary<string, string>> GetMatches(WebClient wc, string url) {

            Console.WriteLine("Идет получение матчей");

            JObject json = JObject.Parse(wc.DownloadString(url));
            List<Dictionary<string, string>> allMatches = new List<Dictionary<string, string>> { };

            foreach (var item in json["Value"]) {
                Dictionary<string, string> dict = TransformMatch(item);
                if (dict.Count != 0) {
                    allMatches.Add(dict);
                }
            }
            Console.WriteLine("\n   Получение матчей завершено");
            return allMatches;
        }

        static private Dictionary<string, string> TransformMatch(JToken item) {

            string Liga = (string)item["L"];
            string GameId = (string)item["I"];
            string Command1 = (string)item["O1"];
            string Command2 = (string)item["O2"];
            string GameTime = (string)item["SC"]["TS"];
            string Score2 = (string)item["SC"]["FS"]["S2"];
            string Score1 = (string)item["SC"]["FS"]["S1"];

            List<string> list = SearchParameters(item);
            string Parameter = list[0];
            string Coefficient = list[1];

            if (Liga == null) {
                Console.WriteLine("     Неизвестный матч пропущен " +
                    "- отсутствует лига");
                return new Dictionary<string, string> { };
            } else if (Command1 == null || Command2 == null) {
                Console.WriteLine("     Неизвестный матч пропущен " +
                    "- отсутствует команда(ы)");
                return new Dictionary<string, string> { };
            } else if (GameId == null) {
                Console.WriteLine($"    Матч \"{Command1}-{Command2}\" пропущен " +
                    $"- отсутствует игровой id");
                return new Dictionary<string, string> { };
            } else if (GameTime == null) {
                Console.WriteLine($"    Матч \"{Command1}-{Command2}\" пропущен " +
                    $"- отсутствует время игры");
                return new Dictionary<string, string> { };
            } else if (Score1 == null || Score2 == null) {
                Console.WriteLine($"    Матч \"{Command1}-{Command2}\" пропущен " +
                    $"- отсутствует счет игры");
                return new Dictionary<string, string> { };
            } else if (Coefficient == null || Parameter == null) {
                Console.WriteLine($"    Матч \"{Command1}-{Command2}\" пропущен " +
                    $"- отсутствует параметры ставки");
                return new Dictionary<string, string> { };
            } else {
                //Console.WriteLine($"   Матч \"{Command1}-{Command2}\" добавлен");
                return new Dictionary<string, string> {
                {"Liga", Liga },
                {"GameId", GameId },
                {"Command1", Command1 },
                {"Command2", Command2 },
                {"GameTime", GameTime },
                {"Score1", Score1 },
                {"Score2", Score2 },
                {"Parameter", Parameter },
                {"Coefficient", Coefficient },
            };
            };
        }

        static private List<string> SearchParameters(JToken item) {

            string Parameter = null;
            string Coefficient = null;
            float t, ce, g; // поля json

            foreach (var inAE in item["AE"]) {
                foreach (var inME in inAE["ME"]) {
                    try {
                        t = (float)inME["T"];
                        ce = (float)inME["CE"];
                        g = (float)inME["G"];
                    }
                    catch (Exception e) {
                        t = 0f;
                        ce = 0f;
                        g = 0f;
                    }
                    if (t == 10 && ce == 1 && g == 17) {
                        Parameter = (string)inME["P"];
                        Coefficient = (string)inME["P"];

                        return new List<string> {
                        Parameter,
                        Coefficient
                    };
                    }
                }
            }
            return new List<string> { Parameter, Coefficient };
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
    }
}