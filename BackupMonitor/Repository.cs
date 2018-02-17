using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BackupMonitor
{
    class Repository
    {
        private readonly string _connString = ConfigurationManager.ConnectionStrings["NASDB"].ToString();

        public BindingList<BackupItem> ReadBackupItems()
        {
            BindingList<BackupItem> items = new BindingList<BackupItem>();

            using (SqlConnection conn = new SqlConnection(_connString))
            {
                conn.Open();
                string query = "SELECT * FROM BackupItems (NOLOCK) WHERE IsDeleted = 0 ORDER BY SourcePath";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    SqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        BackupItem item = new BackupItem();
                        item.BackupItemId = int.Parse(reader["BackupItemID"].ToString());
                        item.Name = reader["Name"].ToString();
                        item.SourcePath = reader["SourcePath"].ToString();
                        item.TargetPath = reader["TargetPath"].ToString();
                        item.Status = "LOADING...";
                        items.Add(item);
                    }
                    reader.Close();
                }
            }

            return items;
        }

        public void CreateBackupItem(BackupItem newItem)
        {
            using (SqlConnection conn = new SqlConnection(_connString))
            {
                conn.Open();
                string query = "INSERT BackupItems (Name, SourcePath, TargetPath) ";
                query += " VALUES ('";
                query += newItem.Name;
                query += "', '";
                query += newItem.SourcePath;
                query += "', '";
                query += newItem.TargetPath;
                query += "')";
                using (SqlCommand cmd = new SqlCommand(query, conn))
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.ExecuteNonQuery();
                }
            }
        }


    }
}
