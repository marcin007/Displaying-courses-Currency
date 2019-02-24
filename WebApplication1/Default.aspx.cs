using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Data.SQLite;
using System.Web.Helpers;
using System.IO;
using System.Collections.Generic;

namespace WebApplication1
{
    public partial class _Default : Page
    {
        const string DB_FILE = "main.sqlite";
        const string BASE_CURRENCY = "PLN";

        /**
         * Ustawia filtrowanie dat po naciśnięciu przycisku Filtruj
         */
        protected void applyDateFilter(object sender, EventArgs e)
        {
            Session["filter_from"] = filter_from.Text;
            Session["filter_to"] = filter_to.Text;
            // Response.Redirect(Request.RawUrl);
        }

        /**
         * Wykreśla przebieg kursu danej waluty na osobnej podstronie.
         */
        private void plotCurrency(string currencyId, string currencyName)
        {
            Chart ch = new Chart(1024, 768);
            List<string> dates = new List<string>();
            List<double> rates = new List<double>();

            SQLiteConnection conn = openDB();
            conn.Open();
            SQLiteCommand cmd = new SQLiteCommand(
                @"SELECT er.id, er.time, er.rate, c.name
                  FROM exchange_rate as er, currency as c
                  WHERE c.id = ? AND er.currency = c.id
                  ORDER BY er.time", conn);
            cmd.Parameters.AddWithValue(null, currencyId);
            SQLiteDataReader reader = cmd.ExecuteReader();
            
            while (reader.Read())
            {
                dates.Add(reader["time"].ToString());
                rates.Add(Double.Parse(reader["rate"].ToString()));
            }
            conn.Close();

            ch.AddTitle("Kurs wymiany waluty "+currencyName);
            ch.AddSeries(
                name: "",
                chartType: "Line",
                xValue: dates,
                yValues: rates,
                axisLabel: "Data",
                yFields: "Kurs wymiany"
                );
            ch.Write();
        }

        /**
         * Procedura dodawania nowego komentarza
         */
        private void addComment()
        {
            string id = null;
            try
            {
                if (Session["comment_id"] != null) {
                    id = Session["comment_id"].ToString();
                }
            }
            catch(IndexOutOfRangeException e)
            {
                return;
            }

            SQLiteConnection conn = openDB();
            conn.Open();
            SQLiteCommand cmd = new SQLiteCommand("INSERT INTO comment(exchange_rate, content) VALUES(?,?)", conn);
            cmd.Parameters.AddWithValue(null, id);
            cmd.Parameters.AddWithValue(null, comment_post_text.Text);
            cmd.ExecuteNonQuery();
            conn.Close();
            showComments(id);
        }

        /**
         * Procedura wyświetlania komentarzy
         */
        private void showComments(string exchange_rate_id)
        {
            SQLiteConnection conn = openDB();
            conn.Open();
            comments_loaded.Controls.Add(new LiteralControl("<h2>Wszystkie komentarze do kursu wymiany o id " + exchange_rate_id + "</h2><br/>"));

            SQLiteCommand cmd = new SQLiteCommand(@"
            SELECT content
            FROM comment
            WHERE exchange_rate = ?
            ", conn);
            cmd.Parameters.AddWithValue(null, exchange_rate_id);
            SQLiteDataReader reader = cmd.ExecuteReader();

            comments_loaded.Controls.Add(new LiteralControl("<ul>"));
            while (reader.Read())
            {
                comments_loaded.Controls.Add(new LiteralControl("<li>"+reader["content"].ToString() + "</li>"));
            }
            comments_loaded.Controls.Add(new LiteralControl("</ul>"));

            conn.Close();
        }

        /**
         * Wyświetla tabelę dla pojedynczej waluty.
         */
        private void AddCurrencyTable(SQLiteDataReader reader, string currencyId, string currencyName)
        {
            Table t = new Table();
            TableRow r = null;
            TableCell c = null;



            r = new TableRow();
            c = new TableCell();
            c.Controls.Add(new LiteralControl("id"));
            r.Cells.Add(c);

            c = new TableCell();
            c.Controls.Add(new LiteralControl(
                String.Format("1 {0} = X {1}", BASE_CURRENCY, currencyName)
            ));
            r.Cells.Add(c);

            c = new TableCell();
            Button b = new Button();
            b.Text = "Wykres";
            b.Click += new EventHandler((o, e) =>
            {
                plotCurrency(currencyId, currencyName);
            });

            c.Controls.Add(b);
            r.Cells.Add(c);

            c = new TableCell();
            c.Controls.Add(new LiteralControl("Komentarze"));
            r.Cells.Add(c);

            t.Rows.Add(r);

            t.CssClass = "table";

            tables.Controls.Add(t);

            while (reader.Read())
            {
                string exchange_rate_id = reader["id"].ToString();
                r = new TableRow();

                c = new TableCell();
                c.Controls.Add(new LiteralControl(exchange_rate_id));
                r.Cells.Add(c);

                c = new TableCell();
                c.Controls.Add(new LiteralControl(reader["time"].ToString()));
                r.Cells.Add(c);

                c = new TableCell();
                c.Controls.Add(new LiteralControl(reader["rate"].ToString()));
                r.Cells.Add(c);

                c = new TableCell();
                b = new Button();
                b.Text = reader["comment_count"].ToString();
                b.Click += new EventHandler((object sender, EventArgs e) => {
                    Session["comment_id"] = exchange_rate_id;
                    showComments(exchange_rate_id);
                });

                c.Controls.Add(b);
                r.Cells.Add(c);

                t.Rows.Add(r);
            }
        }

        /**
         * Generuje napis używany w zapytaniu do bazy danych odpowiedzialny za
         * filtrowanie wpisów.
         * 
         * Bazowo zwraca "1", ponieważ w wywołaniu zapytania wstawiane jest w miejscu:
         * 
         * WHERE er.currency = ? AND <TUTAJ>
         * A "1" jest niezmiennikiem iloczynu logicznego
         * 
         * Jeśli aktualnie ustawiony jest jakiś filtr (filter_from) albo (filter_to)
         * 
         * to zwracany napis przybieże postać
         * 
         * AND er.time >= WARTOSC_FILTER_FROM AND er.time <= WARTOSC_FILTER_TO
         */
        private string generateFilterCondition(string field_name)
        {
            if (Session["filter_from"] == null && Session["filter_to"] == null)
            {
                return "1";
            }
            string result = "1 ";
            if (Session["filter_from"] != "")
            {
                string from = Session["filter_from"].ToString();
                result += String.Format("AND {0} >= date('{1}') ", field_name, from);
            }

            if (Session["filter_to"] != "")
            {
                string to = Session["filter_to"].ToString();
                result += String.Format("AND {0} <= date('{1}') ", field_name, to);
            }
            return result;
        }

        /**
         * Wyświetla wszystkie tabele wszystkich walut.
         */
        private void AddCurrencyTables(SQLiteDataReader reader, SQLiteConnection conn)
        {
            TableRow r;
            TableCell c;
            while (reader.Read())
            {
                string currencyName = reader["name"].ToString();
                string currencyId = reader["id"].ToString();
                SQLiteCommand history_cmd = new SQLiteCommand(String.Format(@"
                SELECT er.id, er.rate, er.time, (SELECT COUNT(*)
                    FROM comment WHERE exchange_rate = er.id) as comment_count

                FROM exchange_rate as er
                WHERE er.currency = ? AND {0}
                ORDER BY er.time", generateFilterCondition("er.time")), conn);

                history_cmd.Parameters.AddWithValue(null, currencyId);
                SQLiteDataReader history_reader = history_cmd.ExecuteReader();

                AddCurrencyTable(history_reader, currencyId, currencyName);
            }
        }

        /**
         * Nawiązuje połączenie z bazą danych
         */
        private SQLiteConnection openDB()
        {
            string db_file = String.Format("{0}\\..\\..\\{1}", System.AppContext.BaseDirectory, DB_FILE);
            SQLiteConnection conn = new SQLiteConnection("Data Source=" + db_file + ";Version=3;");

            return conn;
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            SQLiteConnection conn = openDB();
            conn.Open();

            SQLiteCommand cmd;
            SQLiteDataReader reader;

            cmd = new SQLiteCommand(
                @"SELECT id, name
                  FROM currency
                  ORDER BY id", conn);
            reader = cmd.ExecuteReader();

            Session["filter_from"] = filter_from.Text;
            Session["filter_to"] = filter_to.Text;
            AddCurrencyTables(reader, conn);
            conn.Close();

            comment_post_button.ServerClick += new EventHandler((s, ea) => addComment());

            if (Session["filter_from"] != null)
            {
                filter_from.Text = Session["filter_from"].ToString();
            }
            if (Session["filter_to"] != null)
            {
                filter_to.Text = Session["filter_to"].ToString();
            }
         
        }
    }
}