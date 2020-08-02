using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Data.SqlClient;
using Newtonsoft.Json;
using System.Windows.Forms.DataVisualization.Charting;

namespace WindowsFormsApp1
{
    public partial class Form1 : Form
    {

        SqlConnection db = new SqlConnection();
        SqlCommand cmd;
        SqlDataReader dr;
        string RWpath = @"./RWData";
        

        public Form1()
        {
            InitializeComponent();
            this.datapath.Text = @"D:\MC_profit_sensor\SignalOrder";
            this.timer1.Interval = 1000;
            chart1.Series.Clear();
            label3.Hide();
            label3.Text = "";
            

        }


        private void Form1_Load(object sender, EventArgs e)
        {
            
            db.ConnectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;" +
                "AttachDbFilename=|DataDirectory|Database1.mdf;" +
                "Integrated Security=True";
            db.Open();
            if (db.State == ConnectionState.Open)
            {
                Console.WriteLine("PreState OK");
            }
        }

        private void checkedListBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void button2_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog path = new FolderBrowserDialog();
            path.ShowDialog();
            //Console.WriteLine(path.SelectedPath);

            if (path.SelectedPath != "")
            {
                this.datapath.Text = path.SelectedPath;
            }
        }

        private void panel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.timer1.Interval = int.Parse(textBox1.Text)*60*1000;
            textBox1.ReadOnly = true;
            this.listView1.Columns.Add("策略");
            this.listView1.Columns.Add("商品");
            this.listView1.Columns.Add("部位");
            this.listView1.Columns.Add("價格");
            this.listView1.Columns.Add("獲利");
            this.listView1.Columns.Add("入場日期");
            this.listView1.Columns.Add("出場日期");
            this.listView1.Columns.Add("出場時間");
            this.listView1.Columns.Add("狀況");
            this.listView1.Columns.Add("更新週期");
            this.listView1.Columns.Add("MaxDD更新");

            // Display grid lines.
            this.listView1.GridLines = true;
            // Set the view to show details.
            this.listView1.View = View.Details;
            string[] dirs = Printfile(this.datapath.Text);

            ListViewItem[] StrategyItem = new ListViewItem[dirs.Length];
            

            int i = 0;
            string fileNamed;
            string[] Sdata;
            foreach (string fileName in dirs)
            {
                string text = System.IO.File.ReadAllText(@fileName);
                Sdata = text.Split(',');
                
                StrategyItem[i] = new ListViewItem(Sdata[1]);
                StrategyItem[i].Checked = true;
                StrategyItem[i].SubItems.Add(Sdata[0]);
                for (int j = 2;j < 11; j++)
                {
                    StrategyItem[i].SubItems.Add(Sdata[j]);
                }
                StrategyItem[i].SubItems.Add("");
                i++;
                
            }
            
            //item2.SubItems.Add("6");
            this.listView1.Items.AddRange(StrategyItem);
            this.listView1.AutoResizeColumn(0,ColumnHeaderAutoResizeStyle.HeaderSize);
            Update_listView(dirs);
            this.button1.Hide();
            label3.Show();
            label3.Text = "更新時間" + DateTime.Now.ToString();

            this.timer1.Tick += new EventHandler(Timer1_Tick);
            this.timer1.Enabled = true;
        }

        public void Timer1_Tick(object Sender, EventArgs e)
        {
            string[] dirs = Printfile(this.datapath.Text);
            //PreStateToSQL(dirs);
            int i = 0;
            string fileNamed;
            string[] Sdata;
            string[] PreSdata;
            foreach (string fileName in dirs)
            {
                string text = System.IO.File.ReadAllText(@fileName);
                Sdata = text.Split(',');
                PreSdata = new string[Sdata.Length];

                ListViewItem item = listView1.Items[i];
                for (int j = 0; j < 11; j++)
                {
                    PreSdata[j] = item.SubItems[j].Text;
                }

                for (int j = 2; j < 11; j++)
                {
                    item.SubItems[j].Text = Sdata[j];
                }
                IsRWData(Sdata, PreSdata);
                //Console.WriteLine(item.SubItems[0].Text);
                i++;
            }
            Update_listView(dirs);
            label3.Text = "更新時間" + DateTime.Now.ToString();
        }

        public void Update_listView(string[] dirs)
        {
            listView3.Clear();
            listView3.Columns.Add("策略");
            listView3.Columns.Add("更新日期");
            listView3.Columns.Add("DD");
            listView3.GridLines = true;
            // Set the view to show details.
            listView3.View = View.Details;
            ListViewItem[] StrategyItem = new ListViewItem[dirs.Length];

            int i = 0;
            string fileNamed;
            string[] Sdata;
            foreach (string fileName in dirs)
            {
                string text = System.IO.File.ReadAllText(@fileName);
                Sdata = text.Split(',');

                StrategyItem[i] = new ListViewItem(Sdata[1]);
                StrategyItem[i].Checked = true;
                cmd = new SqlCommand("select * from PreState where strategy = '" + Sdata[1] + "'", db);
                dr = cmd.ExecuteReader();
                dr.Read();
                
                if (dr.HasRows)
                {
                    //Console.WriteLine(dr["update"]);
                    StrategyItem[i].SubItems.Add(dr["update"].ToString());
                    string[] tempDate = dr["update"].ToString().Split('/');
                    //string[] tempDate = { "2020","7","20"};
                    int dataPorfit = 1000000 + int.Parse(tempDate[0]) % 100 * 10000 + int.Parse(tempDate[1]) * 100 + int.Parse(tempDate[2]);
                    //Console.WriteLine(dataPorfit);

                    int fixDD = readfixDD(Sdata[1], dataPorfit);
                    StrategyItem[i].SubItems.Add(fixDD.ToString());

                    dr.Close();
                    cmd = new SqlCommand("UPDATE PreState set " +
                        "[fixDD] = '" + fixDD.ToString() + "'" +
                        "WHERE strategy = '" + Sdata[1].ToString() + "'"
                        , db);
                    cmd.ExecuteNonQuery();

                    DateTime updatedate = new DateTime(int.Parse(tempDate[0]), int.Parse(tempDate[1]), int.Parse(tempDate[2]));
                    DateTime todaydate = DateTime.Now;
                    TimeSpan ts2 = DateTime.Now - updatedate;

                    if (ts2.Days > int.Parse(listView1.Items[i].SubItems[9].Text) || fixDD < int.Parse(listView1.Items[i].SubItems[10].Text))
                    {
                        StrategyItem[i].BackColor = Color.DarkRed;
                        StrategyItem[i].ForeColor = Color.White;
                    }

                    
                }
                dr.Close();
                i++;

            }
            //item2.SubItems.Add("6");
            this.listView3.Items.AddRange(StrategyItem);
            this.listView3.AutoResizeColumn(0, ColumnHeaderAutoResizeStyle.HeaderSize);
        }

        public void PreStateToSQL(int index)
        {
            //Console.WriteLine(listView3.Items[index]);
            ListViewItem item = listView3.Items[index];
            cmd = new SqlCommand("select * from PreState where strategy = '"+ listView3.Items[index].SubItems[0].Text + "'", db);
            dr = cmd.ExecuteReader();
            dr.Read();
            if (dr.HasRows)
            {
                //Console.WriteLine("havedata");
                dr.Close();
                cmd = new SqlCommand("UPDATE PreState set " +
                    "[update] = '" + DateTime.Now.ToShortDateString() + "'," +
                    "[fixDD] = '" + "0" + "'" +
                    "WHERE strategy = '" + listView3.Items[index].SubItems[0].Text + "'"
                    , db);
                cmd.ExecuteNonQuery();
            }
            else
            {
                //Console.WriteLine("nondata");
                dr.Close();
                cmd = new SqlCommand("INSERT INTO PreState ([strategy],[update] ,[fixDD]) VALUES (" +
                    "'" + listView3.Items[index].SubItems[0].Text + "'," +
                    "'" + DateTime.Now.ToShortDateString() + "'," +
                    "'" +"0" + "')", db);
                cmd.ExecuteNonQuery();
            }
            dr.Close();
            

        }

        public int readfixDD(string fileName,int update)
        {
            
            string path = RWpath + @"\" + fileName + ".txt";

            if (File.Exists(path))
            {
                string s;

                using (StreamReader sr = File.OpenText(path))
                {
                    s = sr.ReadLine();

                }
                DataTable dt = JsonConvert.DeserializeObject<DataTable>(s);
                int dataProfit = 0;
                for (int index = 0; index < dt.Rows.Count; index++)
                {
                    if (int.Parse(dt.Rows[index]["date"].ToString()) > update)
                    {
                        dataProfit = dataProfit + int.Parse(dt.Rows[index]["Profit"].ToString());
                    }
                    
                    
                }
                return dataProfit;
            }

            return 0;
        }

        private void button3_Click(object sender, EventArgs e)
        {
            /*
            SqlCommand cmd1;
            cmd1 = new SqlCommand("select * from PreState ", db);
            SqlDataReader dr1;
            dr1 = cmd1.ExecuteReader();
            while (dr1.Read())
            {

                Console.WriteLine(dr1["strategy"].ToString() + " " + 
                    dr1["commodity"].ToString() + " " + 
                    dr1["MP"].ToString() + " " + 
                    dr1["Price"].ToString() + " " + 
                    dr1["Profit"].ToString() + " " + 
                    dr1["InDate"].ToString() + " " + 
                    dr1["OutDate"].ToString() + " " + 
                    dr1["OutTime"].ToString());


            }
            dr1.Close();
            */
            RWDataToTxt();
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count > 0)
            {
                int Index = listView1.SelectedItems[0].Index;//取當前選中項的index,SelectedItems[0]這必須爲0
                //String aa = listView1.Items[Index].SubItems[0].Text;//用我們剛取到的index取被選中的某一列的值從0開始
                //MessageBox.Show(aa);
                readDataForTxt(listView1.Items[Index].SubItems[0].Text);
            }
            
        }

        public string[] Printfile(string sourceDirectory)
        {
            string[] dirs = { };

            try
            {
                dirs = Directory.GetFiles(sourceDirectory);

            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
            return dirs;
        }

        public void IsRWData(string[] Sdata, string[] PreSdata)
        {
            int MP = (Sdata[8] == "in") ? 1 : 0;
            int PreMP = (PreSdata[8] == "in") ? 1 : 0;

            if (int.Parse(Sdata[2])*MP != int.Parse(PreSdata[2])*PreMP)
            {

                if (!System.IO.Directory.Exists(RWpath))
                {
                    Directory.CreateDirectory(RWpath);
                    dataToTxt( Sdata, PreSdata);
                }
                else
                {
                    //Console.WriteLine("Directory already exists.");
                    dataToTxt( Sdata, PreSdata);
                }
            }
        }

        public void RWDataToTxt()
        {
           

            if (!System.IO.Directory.Exists(RWpath))
            {
                Directory.CreateDirectory(RWpath);
            }
            else
            {
                Console.WriteLine("Directory already exists.");
            }
        }

        public void dataToTxt(string[] Sdata, string[] PreSdata)
        {
            string fileName = Sdata[1];
            string path = RWpath + @"\" + fileName + ".txt";

            int MP = (Sdata[8] == "in") ? 1 : 0;
            int PreMP = (PreSdata[8] == "in") ? 1 : 0;

            if (!File.Exists(path))
            {
                using(StreamWriter sw = File.CreateText(path))
                {
                    
                    DataTable dt = new DataTable();
                    dt.Columns.Add(new DataColumn("Date"));
                    dt.Columns.Add(new DataColumn("Profit"));
                    DataRow row = dt.NewRow();
                    
                    if(MP != PreMP)
                    {
                        row["date"] = Sdata[6];
                        row["Profit"] = Sdata[4];
                    }
                    else
                    {
                        row["date"] = Sdata[6];
                        row["Profit"] = Sdata[4];
                    }
                    
                    dt.Rows.Add(row);

                    string jsonString = JsonConvert.SerializeObject(dt);
                    sw.WriteLine(jsonString);
                }
            }
            else
            {
                string s;
                
                using (StreamReader sr = File.OpenText(path))
                {
                    s = sr.ReadLine();
                    
                }
                DataTable dt = JsonConvert.DeserializeObject<DataTable>(s);
                StreamWriter sw = new StreamWriter(path);
                DataRow row = dt.NewRow();

                if (MP != PreMP)
                {
                    row["date"] = Sdata[6];
                    row["Profit"] = Sdata[4];
                }
                else
                {
                    row["date"] = Sdata[6];
                    row["Profit"] = Sdata[4];
                }

                dt.Rows.Add(row);

                string jsonString = JsonConvert.SerializeObject(dt);
                sw.WriteLine(jsonString);
                //sw.Write(s + Sdata[1]);
                sw.Close();
            }
        }

        public void readDataForTxt(string fileName)
        {
            listView2.Clear();
            chart1.Series.Clear();
            string path = RWpath + @"\" + fileName + ".txt";

            if (File.Exists(path))
            {
                string s;

                using (StreamReader sr = File.OpenText(path))
                {
                    s = sr.ReadLine();

                }
                DataTable dt = JsonConvert.DeserializeObject<DataTable>(s);

                int dataRow = (dt.Rows.Count > 10) ? 10 : dt.Rows.Count;
                this.listView2.Columns.Add("次數");
                this.listView2.Columns.Add("出場時間");
                this.listView2.Columns.Add("獲利");
                this.listView2.GridLines = true;
                // Set the view to show details.
                this.listView2.View = View.Details;

                ListViewItem[] dataItem = new ListViewItem[dataRow];
                int j = 0;
                for (int i = dt.Rows.Count - 1;i > dt.Rows.Count - dataRow-1; i--)
                {
                    
                    dataItem[j] = new ListViewItem(i.ToString());
                    dataItem[j].Checked = true;
                    dataItem[j].SubItems.Add(dt.Rows[i]["date"].ToString());
                    dataItem[j].SubItems.Add(dt.Rows[i]["Profit"].ToString());

                    Console.WriteLine(dt.Rows[i]["date"].ToString());
                    Console.WriteLine(int.Parse(dt.Rows[i]["Profit"].ToString()));
                    j++;
                }
                this.listView2.Items.AddRange(dataItem);
                this.listView2.AutoResizeColumn(0, ColumnHeaderAutoResizeStyle.HeaderSize);

                poltChart(dt, fileName);
            }
        }

        private void poltChart(DataTable dt, string fileName)
        {
            //chart1.Series.Clear();
            //標題 最大數值
            Series series1 = new Series(fileName, dt.Rows.Count);  

            //設定線條顏色
            series1.Color = Color.Blue;

            //設定字型
            series1.Font = new System.Drawing.Font("新細明體", 14);

            //折線圖
            series1.ChartType = SeriesChartType.Line;

            //將數值顯示在線上
            series1.IsValueShownAsLabel = true;

            int dataProfit = 0;
            //將數值新增至序列
            for (int index = 0; index < dt.Rows.Count; index++)
            {
                dataProfit = dataProfit + int.Parse(dt.Rows[index]["Profit"].ToString());
                series1.Points.AddXY(dt.Rows[index]["date"].ToString(), dataProfit);
            }

            //將序列新增到圖上
            this.chart1.Series.Add(series1);

        } 

        private void listView2_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void chart1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void listView3_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listView3.SelectedItems.Count > 0)
            {
                int Index = listView3.SelectedItems[0].Index;//取當前選中項的index,SelectedItems[0]這必須爲0
                //String aa = listView1.Items[Index].SubItems[0].Text;//用我們剛取到的index取被選中的某一列的值從0開始
                //MessageBox.Show(aa);

                DialogResult result = MessageBox.Show("本日策略更新","更新視窗", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    PreStateToSQL(Index);
                }
                
            }
        }
    }
}

