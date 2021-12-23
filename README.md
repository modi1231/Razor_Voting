# Razor_Voting
Razor_Voting is a basic illustration of how to create a fairly dyanmic voting and poll, show case the UI to the viewer, and track the results in a database.

Write up here: https://www.dreamincode.net/forums/topic/419774-razor-pages-core-31-basic-polls-and-voting/




=================
dreamincode.net tutorial backup ahead of decommissioning

 Posted 26 July 2020 - 12:44 PM 

I recently came across a need for a voting/poll mechanism in a project.  Nothing overly complex or crazy, but enough to get the job done, and it turns out to make an interesting tutorial.

[u][b]Software[/b][/u]
-- Visual Studios 2019

[u][b]Concepts[/b][/u]
-- C#
-- Core 3.1 / Razor pages

[u][b]Github link:[/b][/u] https://github.com/modi1231/Razor_Voting

With any good project a list of wants, and see what is required for a minimal viable product and what is gold plating.

[u]Minimally Viable Product[/u]
-- data pulled/stored in a database
-- multiple choices
-- expiration date
-- after a user votes do not allow them to vote again
-- if a user is popping back into a voted on poll, show the results, 
-- which one they voted for.
-- an easy admin page for folk to create a poll.
-- show multiple polls as active or voted on.

[u]Gold Plating[/u]
-- ajax to do voting
-- admin to delete polls
-- hide polls or 'soft delete'.

All reasonable to obtain.

Here is the look this is going for.  Two pages, basic UI cues, and repeatable functionality.  

Note that there is a fourth poll entry in the database, but it is expired so it doesn't show.  

[img]https://i.imgur.com/XeLcCKO.jpg[/img]


[b][u]Project Setup[/b][/u]
The project is pretty basic in itself, and follows my past projects.  

Start with an empty Razor project, hit 3.1 framework version, and start it up.

Create the 'Shared' folder inside the 'Pages' folder, create a folder at root called 'Data', that should setup the structure.

Inside 'Shared' folder add a '_layout.cshtml', and one level up add the 'viewimports' and 'viewstart'.  

In the startup.cs add the Razor pages to 'ConfigureServices' to add routing, pages, and mapping razor pages.

            [code]services.AddRazorPages();            [/code]

Clear up the 'configure' function 
            [code]
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();
            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
            });
        }            [/code]

From there that's a solid setup.  Create the index and admin razor pages in the 'pages' folder, and classes as you need them in the data folder.




[b][u]DATA [/b][/u]

Over all this whole project centers on one pivot - the data.  It's not necessarily about how pretty the UI looks, or nifty behind-the-scenes JS functionality, but about collecting data.  That'll be the crux so let's start with the data plan.

In the database start framing out what is needed.  Per the plan above, I'll need three tables.  One to hold the poll question and information, one for each poll's choices, and one last one to record a user's choice. 

The POLL table has a unique identifier ID key, a varchar for the question, an expiration date of the poll, and when it was entered.

The POLL_CHOICES table has a foreign key to the POLL id, a choice id key that is a numeric to also imply ordering, choice text, and when it was entered.  

Finally POLL_VOTES holds a foreign key to the POLL id, the IP address is a key (only one vote per question!), and the choice id (maps to the poll_choices choice id key).


Flipping to the classes I expand the information from the tables, and include implied information per the plan.

I opted to create a class called 'poll' to hold the pertinent information.

Polls needed a unique id, questions, a list of choices, flags if it was answered, and expiration date.

[code]
    public class POLL
    {
        public Guid POLL_ID { get; set; }
        
        [Display(Name = "Question")]
        public string POLL_QUESTION { get; set; }

        public List<CHOICE> CHOICES { get; set; }

        public bool BEEN_ANSWERED { get; set; }

        public DateTime DATE_ENTERED { get; set; }

        [Display(Name = "Expires")]
        public DateTime EXPIRATION_DATE { get; set; }

        public POLL()
        {
            CHOICES = new List<CHOICE>();
    }
    }[/code]

    Choices are are fairly straight forward as well.  It'll be a double duty vehicle!  An ID, text, and per the requirements house the count of how many folk picked that, percentage picked for the question, and flag if user picked it.

    Again, per the requirements the display will flip/flop if the user has answered a poll already or not.

    [code]
    public class CHOICE
    {
    public int CHOICE_ID { get; set; }

    public string CHOICE_TEXT { get; set; }

    public int COUNT { get; set; }

    public double PERCENTAGE { get; set; }

    public bool USER_PICKED { get; set; }
    }[/code]

    The final class will just be a vehicle to shuffle around the voter's information, the poll id, and choice picked.
    [code]
    public class VotingData
    {
    public Guid POLL_ID { get; set; }
    public string IP_ADDRESS { get; set; }
    public int CHOICE_ID { get; set; }
    public DateTime DATE_ENTERED { get; set; }
    }
    [/code]




[b][u]Data Access[/b][/u]
In alignment with my previous tutorials I have my data access functionality sequestered in the 'dataaccess.cs'.  

You can eyeball the previous tutorials for more depth, but the nut is this decouples a whole mess of bad possible code and keeps things tidy.

Per usual the constructor takes in the connection string for use.

The load grabs poll data, choices, and matches them up for a collection to be spit back out to the controller.

There's a 'save new poll' for the admin page, saving poll data, and some helper functions.  Nothing super exciting, but a reminder to keep what you have as async and definitely not to mix and match async vs not.


[code]
  public class DataAccess
    {
        private string _connectionString;

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
            System.Data.DataSet ds;
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
[/code]




[b][u]Index[/b][/u]

To keep the tutorial tight the UI is broken into two pages - the voting/polling page, and an admin page.

The index page main loads in the connection string, stashes the IP, and on the general 'get' makes a call out to load the polling data for a given IP.
[code]
         public indexModel(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {

            _connection = configuration["ConnectionStrings:DefaultConnection"];
            _accessor = httpContextAccessor;

            clientIP = _accessor.HttpContext.Connection.RemoteIpAddress.MapToIPv4().ToString();
        }

        public async Task OnGetAsync()
        {
            DataAccess da = new DataAccess(_connection);
            choiceSelected = "";

            myPoll = await da.LoadPoll(clientIP);

        }
[/code]

The post has the poll ID parameter, and takes the relevant data selected and shovels it off to the database.
[code]
        public async Task<IActionResult>
    OnPostAsync(string pollID)
    {
    Message = $"Selected: {choiceSelected}";

    DataAccess da = new DataAccess(_connection);
    int choice = Int32.Parse(choiceSelected);

    VotingData data = new VotingData()
    {
    IP_ADDRESS = clientIP,
    CHOICE_ID = choice,
    POLL_ID = Guid.Parse(pollID)
    };

    await da.SavePollData(data);

    return RedirectToPage("index");
    }
[/code]

    The view of the page is only a slight bit tricky, but can be broken down into two buckets - has the IP answered a poll or not.

    Utilizing the handy dandy Razor coding, I iterate over the collection of polls.  If the flag for 'been answered' is checked I show the stats, which option was picked, and read 0only information.

[code]
    @foreach (Data.POLL item in Model.myPoll)
    {
        @*<div style="border:2px solid black;margin:10px;padding:5px;">*@
        <div class="question_box">
            @if (item.BEEN_ANSWERED)
            {
                <div>
                    <div>
                        <div class="question">
                            @item.POLL_QUESTION
                        </div>
                        <div class="expiration"><i> Expiration Date: @item.EXPIRATION_DATE</i></div>
                        <br />
                        @foreach (Razor_Voting.Data.CHOICE item_choice in item.CHOICES)
                        {
                            <span>
                                @item_choice.CHOICE_TEXT : @item_choice.PERCENTAGE % (@item_choice.COUNT)
                                @if (item_choice.USER_PICKED)
                                {<span style="font-weight:bold;">*</span>}
                                <br />
                            </span>
                        }
                    </div>
                    <p class="responded">You have responded on <i>@item.DATE_ENTERED</i>.</p>
                </div>
            }
[/code]
            If not then create input radio boxes for user selection and a button to submit the data.
[code]
            else
            {
            <div>
                <form method="post">
                    <div class="question">@item.POLL_QUESTION</div>
                    <div class="expiration"><i> Expiration Date: @item.EXPIRATION_DATE</i></div>
                    <p><u>Choices:</u></p>
                    @foreach (Razor_Voting.Data.CHOICE item_choice in item.CHOICES)
                    {
                        <span>
                            <input type="radio" asp-for="choiceSelected" value="@item_choice.CHOICE_ID" />
                            <label>@item_choice.CHOICE_TEXT</label>
                            <br />
                        </span>
                    }
                    <div>
                        <input type="submit" asp-route-pollID="@item.POLL_ID" value="Submit Vote" style="margin-top:5px;" />
                    </div>
                </form>
            </div>
            <br />
            }

        </div>
    }
[/code]
    Reminder on that submit - the asp-route parameter name must be cased correctly to the paramete name in the 'post' or you are going to have a bad time.




[b][u]Admin[/b][/u]

    The admin page is even more quick and dirty to get the example across.  You should lock this down and beef up security in what ever fashion works best for your site.

    The admin page is simple for the data requirements.  A box to get the question text, expiration date date/time, and a choices box.

    I cribbed a little with the choices box that each line in the box is broken into a choice around the new line areas.

    Not the most elegant, but effective.

    Basic strings with the 'bind property' decorator so the values survive the trip back.
[code]
    [TempData]
    public string Message { get; set; }// no private set b/c we need data back

    private readonly string _connection;

    private IHttpContextAccessor _accessor;

    [BindProperty]
    [Display(Name = "Question")]
    public string Question { get; set; }

    [BindProperty]
    [Display(Name = "Expiration Date")]
    public DateTime ExpirationDate { get; set; }

    [BindProperty]
    [Display(Name = "Choices")]
    public string Choices { get; set; }

    public adminModel(IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
    {

    _connection = configuration["ConnectionStrings:DefaultConnection"];
    _accessor = httpContextAccessor;
    Message += $" {_accessor.HttpContext.Connection.RemoteIpAddress}";

    ExpirationDate = DateTime.Now.AddDays(3);
    }
[/code]

    The post splits the choices textbox by the new line, crams it all into a 'POLL' object, and tosses it to the save.
[code]
    public async Task<IActionResult>
        OnPostAsync()
        {
        POLL myPoll = new POLL();
        myPoll.POLL_QUESTION = Question;
        myPoll.EXPIRATION_DATE = ExpirationDate;

        string[] tempChoices = Choices.Split(Environment.NewLine);
        for (int i = 0; i < tempChoices.Length; i++)
        {
        myPoll.CHOICES.Add(new CHOICE()
        {
        CHOICE_ID = i+1,
        CHOICE_TEXT = tempChoices[i]
        });
        }

        DataAccess da = new DataAccess(_connection);
        await da.SaveNewPollAsync(myPoll);

        Message = "Saved";

        return RedirectToPage("admin");
        }
[/code]




[b][u]Wrap up[/u][/b]
That wraps up the brief foray into planning, polls, UI ticks, and tracking.  Obviously you would need to carry this on to best suit your needs, security requirements, and tracking requirements.  The table definitions are in the github repo.
