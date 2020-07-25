using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Razor_Voting.Data
{
    public class DataAccess
    {
        private readonly string _connectionString;

        public DataAccess(string connection)
        {
            _connectionString = connection;
        }

        public async Task<List<POLL>> LoadPoll(string clientIP)
        {
            List<POLL> temp = null;
            int? choice_answered = null;

            // 1.  Get all the polls that are not expired.
            temp = await LoadOpenPollsAsync();

            //2.  Get specifics of this user (via their IP address) and how they have interacted with the poll.
            foreach (POLL item in temp)
            {
                //2.1  Choices to get back.
                item.CHOICES = await LoadPollChoicesAsync(item.POLL_ID);

                //2.2  If the IP address has answered then flag that.
                choice_answered = await LoadAnsweredStatusAsync(item.POLL_ID, clientIP);

                if (choice_answered == null)
                {
                    item.BEEN_ANSWERED = false;
                }
                else
                {
                    item.BEEN_ANSWERED = true;

                    // 2.3  Track down which choice was picked.
                    for (int i = 0; i < item.CHOICES.Count; i++)
                    {
                        if (item.CHOICES[i].CHOICE_ID == choice_answered)
                        {
                            item.CHOICES[i].USER_PICKED = true;
                            break;
                        }
                    }
                }
            }
            return temp;
        }

        internal async Task<int?> LoadAnsweredStatusAsync(Guid poll_id, string clientIP)
        {
            string sql = string.Empty;

            int? ret = null;

            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))
                {
                    con.Open();

                    sql = @" SELECT CHOICE_ID 
                            FROM POLL_VOTES WITH(NOLOCK)
                            WHERE POLL_ID = @POLL_ID AND IP_ADDRESS = @IP_ADDRESS  ";// '-- 2.0  SQL statement.
                    using (SqlCommand cmd = new SqlCommand(sql, con))
                    {
                        cmd.Parameters.Add("@POLL_ID", SqlDbType.UniqueIdentifier).Value = poll_id;
                        cmd.Parameters.Add("@IP_ADDRESS", SqlDbType.VarChar).Value = clientIP;

                        var foo = await cmd.ExecuteScalarAsync();

                        ret = (Int32?)foo;
                    }
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);

                throw ex;
            }

            return ret;
        }

        internal async Task SaveNewPollAsync(POLL myPoll)
        {
            string sql = string.Empty;
            Guid newID = Guid.NewGuid();

            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))// '-- AppSettings.Get("db")
                {

                    con.Open();

                    sql = @"INSERT INTO [dbo].[POLL]
                                   ([ID]
                                   ,[QUESTION]
                                   ,[EXPIRATION_DATE]
                                   )
                             VALUES
                                   (@ID
                                   , @QUESTION
                                   , @EXPIRATION_DATE
                                   )";// '-- 2.0  SQL statement.
                    using (SqlCommand cmd = new System.Data.SqlClient.SqlCommand(sql, con))
                    {
                        cmd.Parameters.Add("@ID", SqlDbType.UniqueIdentifier).Value = newID;
                        cmd.Parameters.Add("@QUESTION", SqlDbType.VarChar).Value = myPoll.POLL_QUESTION;
                        cmd.Parameters.Add("@EXPIRATION_DATE", SqlDbType.DateTime).Value = myPoll.EXPIRATION_DATE;

                        await cmd.ExecuteNonQueryAsync();
                    }

                    sql = @"INSERT INTO [dbo].[POLL_CHOICES]
                               (POLL_ID
                               ,CHOICE_ID
                               ,CHOICE
                               )
                         VALUES
                               (@POLL_ID
                               ,@CHOICE_ID
                               ,@CHOICE)";// '-- 2.0  SQL statement.
                    using (SqlCommand cmd = new System.Data.SqlClient.SqlCommand(sql, con))
                    {
                        cmd.Parameters.Add("@POLL_ID", SqlDbType.UniqueIdentifier).Value = newID;
                        cmd.Parameters.Add("@CHOICE_ID", SqlDbType.Int);
                        cmd.Parameters.Add("@CHOICE", SqlDbType.VarChar);

                        foreach (CHOICE item in myPoll.CHOICES)
                        {
                            cmd.Parameters["@CHOICE_ID"].Value = item.CHOICE_ID;
                            cmd.Parameters["@CHOICE"].Value = item.CHOICE_TEXT;
                            await cmd.ExecuteNonQueryAsync();
                        }


                    }

                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);

                throw ex;
            }

        }

        internal async Task<List<CHOICE>> LoadPollChoicesAsync(Guid poll_ID)
        {
            System.Data.DataSet ds;
            string sql = string.Empty;

            List<CHOICE> ret = null;

            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))// '-- AppSettings.Get("db")
                {
                    con.Open();

                    sql = @"  SELECT A.POLL_ID, A.CHOICE_ID, A.CHOICE, ISNULL(B.CHOICE_COUNT,0) AS CHOICE_COUNT
                             FROM [dbo].[POLL_CHOICES] A WITH(NOLOCK)
                             LEFT JOIN(
                                   SELECT POLL_ID, B.CHOICE_ID, COUNT(B.CHOICE_ID) AS CHOICE_COUNT
                                   FROM [dbo].[POLL_VOTES] B WITH(NOLOCK) 
                                   WHERE B.POLL_ID = @POLL_ID
                                   GROUP BY  POLL_ID, B.CHOICE_ID
                                   ) AS B ON A.POLL_ID = B.POLL_ID AND A.CHOICE_ID = B.CHOICE_ID
                            WHERE A.POLL_ID = @POLL_ID ";// '-- 2.0  SQL statement.
                    using (SqlDataAdapter adapt = new System.Data.SqlClient.SqlDataAdapter(sql, con))
                    {
                        adapt.SelectCommand.Parameters.Add("@POLL_ID", SqlDbType.UniqueIdentifier).Value = poll_ID;

                        ds = new DataSet();
                        await Task.Run(() =>
                        {
                            adapt.Fill(ds);
                        });
                    }
                }

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    ret = new List<CHOICE>();

                    foreach (DataRow item in ds.Tables[0].Rows)
                    {
                        ret.Add(new CHOICE()
                        {
                            CHOICE_ID = Int32.Parse(item["CHOICE_ID"].ToString()),
                            CHOICE_TEXT = item["CHOICE"].ToString(),
                            COUNT = Int32.Parse(item["CHOICE_COUNT"].ToString())
                        });
                    }

                    int total = (from x in ret
                                 select x.COUNT).Sum();

                    foreach (CHOICE item in ret)
                    {
                        item.PERCENTAGE = Math.Floor(((double)item.COUNT / total) * 100);
                    }

                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);

                throw ex;
            }

            return ret;
        }

        internal async Task<List<POLL>> LoadOpenPollsAsync()
        {
            System.Data.DataSet ds;
            string sql = string.Empty;

            List<POLL> ret = null;

            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))// '-- AppSettings.Get("db")
                {
                    con.Open();

                    sql = @"SELECT [ID]
                                ,[QUESTION]
                                ,[DATE_ENTERED]
                                ,EXPIRATION_DATE
                        FROM [dbo].[POLL] WITH(NOLOCK)
                        WHERE EXPIRATION_DATE > GETDATE() 
                        ORDER BY[DATE_ENTERED] DESC";// '-- 2.0  SQL statement.
                    using (SqlDataAdapter adapt = new System.Data.SqlClient.SqlDataAdapter(sql, con))
                    {
                        adapt.SelectCommand.Parameters.Add("", SqlDbType.VarChar).Value = "";

                        ds = new DataSet();
                        await Task.Run(() =>
                        {
                            adapt.Fill(ds);
                        });
                    }
                }

                if (ds != null && ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
                {
                    ret = new List<POLL>();
                    foreach (DataRow tempRow in ds.Tables[0].Rows)
                    {
                        ret.Add(new POLL()
                        {
                            POLL_ID = Guid.Parse(tempRow["ID"].ToString()),
                            POLL_QUESTION = tempRow["QUESTION"].ToString(),
                            EXPIRATION_DATE = DateTime.Parse(tempRow["EXPIRATION_DATE"].ToString()),
                            DATE_ENTERED = DateTime.Parse(tempRow["DATE_ENTERED"].ToString())
                        });

                    }
                }

            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);

                throw ex;
            }

            return ret;
        }

        internal async Task SavePollData(VotingData data)
        {

            //await Task.Run(() =>
            //{
            //    //save the data
            //});

            string sql = string.Empty;

            try
            {
                using (SqlConnection con = new SqlConnection(_connectionString))// '-- AppSettings.Get("db")
                {
                    con.Open();

                    sql = @"INSERT INTO [dbo].[POLL_VOTES]
                           ([POLL_ID]
                           ,[IP_ADDRESS]
                           ,[CHOICE_ID]
                           )
                     VALUES
                           (@POLL_ID
                           ,@IP_ADDRESS
                           ,@CHOICE_ID
		                   )";// '-- 2.0  SQL statement.
                    using (SqlCommand cmd = new System.Data.SqlClient.SqlCommand(sql, con))
                    {
                        cmd.Parameters.Add("@POLL_ID", SqlDbType.UniqueIdentifier).Value = data.POLL_ID;
                        cmd.Parameters.Add("@IP_ADDRESS", SqlDbType.VarChar).Value = data.IP_ADDRESS;
                        cmd.Parameters.Add("@CHOICE_ID", SqlDbType.Int).Value = data.CHOICE_ID;

                        await cmd.ExecuteNonQueryAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);

                throw ex;
            }


        }

    }
}
