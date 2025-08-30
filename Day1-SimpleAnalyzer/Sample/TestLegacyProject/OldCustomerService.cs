using System;
using System.Collections;
using System.Data.SqlClient;

namespace LegacyApp.Services
{
    public class OldCustomerService
    {
        private string _connectionString = "Server=.;Database=LegacyDB;Trusted_Connection=true;";
        
        public ArrayList GetAllCustomers()
        {
            ArrayList customers = new ArrayList();
            SqlConnection conn = null;
            
            try
            {
                conn = new SqlConnection(_connectionString);
                conn.Open();
                
                SqlCommand cmd = new SqlCommand("SELECT * FROM Customers", conn);
                SqlDataReader reader = cmd.ExecuteReader();
                
                while (reader.Read())
                {
                    Hashtable customer = new Hashtable();
                    customer.Add("Id", reader["Id"]);
                    customer.Add("Name", reader["Name"]);
                    customer.Add("Email", reader["Email"]);
                    customers.Add(customer);
                }
            }
            catch (Exception ex)
            {
                // Poor error handling
                throw ex;
            }
            finally
            {
                if (conn != null)
                    conn.Close();
            }
            
            return customers;
        }
        
        public bool SaveCustomer(Hashtable customerData)
        {
            SqlConnection conn = new SqlConnection(_connectionString);
            
            try
            {
                conn.Open();
                string sql = "INSERT INTO Customers (Name, Email) VALUES ('" + 
                           customerData["Name"] + "', '" + customerData["Email"] + "')";
                SqlCommand cmd = new SqlCommand(sql, conn);
                cmd.ExecuteNonQuery();
                return true;
            }
            catch
            {
                return false;
            }
            finally
            {
                conn.Close();
            }
        }
    }
}
