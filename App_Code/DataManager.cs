﻿using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.OleDb;
using System.Linq;
using System.Web;
using BusinessLayer;


namespace DataLayer
{

    /// <summary>
    /// DataLayer Tier Management Object
    /// 
    /// Uses a generic pattern for all methods. The Entity Type must be specified for each method.
    /// </summary>
    public class DataManager
    {
        // Singleton: Enforce that there is only ever one _instance at any time. Reduces used resources.
        private static DataManager _instance;

        private readonly OleDbConnection _connection;

        private readonly string _buildSiteUserTable = "if OBJECT_ID(N'dbo.SiteUser', N'U') is NULL BEGIN " +
                                                     "create table dbo.SiteUser(id   int   IDENTITY(1, 1)   primary key, login   nvarchar(64)    not null, " +
                                                     "password    nvarchar(64)    not null, userType    char(1)     not null, emailAddress    nvarchar(100)   not null, " +
                                                     "homeNumber  nvarchar(11), workNumber  nvarchar(11), mobileNumber    nvarchar(14), firstName   nvarchar(32), " +
                                                     "lastName    nvarchar(32), streetAddress   nvarchar(64), suburb      nvarchar(24), city        nvarchar(16), " +
                                                     "isDisabled  bit     DEFAULT 0 ); END ";

        private readonly string _buildOrderTable = "if OBJECT_ID(N'dbo.CustomerOrder', N'U') is NULL BEGIN " +
                                                   "create table dbo.CustomerOrder(id int IDENTITY(1, 1)   primary key, userId  numeric(2, 0)    not null Foreign Key References dbo.SiteUser(id), " +
                                                   "status  nvarchar(7) not null DEFAULT 'waiting'); END ";

        private readonly string _buildSupplierTable = "if OBJECT_ID(N'dbo.Supplier', N'U') is NULL BEGIN " +
                                                      "create table dbo.Supplier(id int IDENTITY(1, 1)   primary key, name    nvarchar(32)    not null, " +
                                                      "contactNumber   nvarchar(11)    not null, emailAddress    nvarchar(64)    not null); END ";

        private readonly string _buildCategoryTable = "if OBJECT_ID(N'dbo.Category', N'U') is NULL BEGIN " +
                                                      "create table dbo.Category(id int IDENTITY(1, 1)   primary key, name    nvarchar(40)    not null); END ";

        private readonly string _buildColourTable = "if OBJECT_ID(N'dbo.Colour', N'U') is NULL BEGIN " +
                                                    "create table dbo.Colour(id int IDENTITY(1, 1)   primary key, name    nvarchar(24)    not null); END ";

        private readonly string _buildCapTable = "if OBJECT_ID(N'dbo.Cap', N'U') is NULL BEGIN " +
                                                 "create table dbo.Cap(id int IDENTITY(1, 1)   primary key, name    nvarchar(40)    not null, " +
                                                 "price   numeric(3, 2)    not null, description nvarchar(512)   not null, imageUrl nvarchar(96) not null, " +
                                                 "supplierId  int     not null    Foreign Key References dbo.Supplier(id), " +
                                                 "categoryId  int     not null    Foreign Key References dbo.Category(id)); END ";

        private readonly string _buildCapColourTable = "if OBJECT_ID(N'dbo.CapColour', N'U') is NULL BEGIN " +
                                                       "create table dbo.CapColour(colourId    int     not null    Foreign Key References dbo.Colour(id), " +
                                                       "capId       int         not null    Foreign Key References dbo.Cap(id), " +
                                                       "Constraint  capColour_pk    Primary Key(colourId, capId)); END ";

        private readonly string _buildOrderItemTable = "if OBJECT_ID(N'dbo.OrderItem', N'U') is NULL BEGIN " +
                                                       "create table dbo.OrderItem(orderId     int     not null    Foreign Key References dbo.CustomerOrder(id), " +
                                                       "capId       int     not null    Foreign Key References dbo.Cap(id), " +
                                                       "colourId    int     not null    Foreign Key References dbo.Colour(id), " +
                                                       "quantity    int     not null, " +
                                                       "Constraint  orderItem_pk    Primary Key(colourId, capId, orderId)); END ";

        private readonly string _insertDefaultUserAdmin = "if (select count(id) from dbo.SiteUser) = 0 BEGIN " +
                                                          "insert into SiteUser (login, password, userType, emailAddress) Values('AdminRolandAskew2016', " +
                                                          "'BB51AD0AAB66C70D3B26CEC4EFCC224273AF5E18', 'A', 'AskewR04@myunitec.ac.nz'); " +
                                                          "END ";

        private readonly string _insertDefaultColours = "if (select count(id) from dbo.Colour) = 0 BEGIN " +
                                                        "insert into colour (name)values('Black'), ('White'), ('Blue'), ('Green'), ('Red'), ('Pink'), ('Yellow'), ('Orange'), ('Grey'); " +
                                                        "END ";

        private readonly string _insertDefaultCategories = "if (select count(id) from dbo.Category) = 0 BEGIN " +
                                                           "insert into category (name)values('Business Caps'), ('Women''s Caps'), ('Men''s Caps'), ('Children''s Caps'); " +
                                                           "END ";

        private readonly string _selectAllCustomers = "Select * from SiteUser where userType='C';";

        private readonly string _selectAllAdmins = "Select * from SiteUser where userType='A';";

        private readonly string _selectSingleCustomerById = "Select * from SiteUser where userType='C' and id=?;";

        private readonly string _selectSingleCustomerByLogin = "Select * from SiteUser where userType='C' and login=?;";

        private readonly string _selectSingleCustomerByEmail = "Select * from SiteUser where userType='C' and emailAddress=?;";

        private readonly string _selectSingleAdminById = "Select * from SiteUser where userType='A' and id=?;";

        private readonly string _selectSingleAdminByLogin = "Select * from SiteUser where userType='A' and login=?;";

        private readonly string _insertAdministrator =
            "Insert into SiteUser (login, password, userType, emailAddress) values (?, ?, 'A', ?);";

        private readonly string _insertCustomer =
            "Insert into SiteUser (login, password, userType, emailAddress, firstName, lastName, homeNumber, workNumber, mobileNumber, streetAddress, suburb, city, isDisabled) values (?, ?, 'C', ?, ?, ?, ?, ?, ?, ?, ?, ?, ?);";

        private readonly string _updateCustomerNoPassword =
            "Update SiteUser set login=?, userType='C', emailAddress=?, firstName=?, lastName=?, homeNumber=?, workNumber=?, mobileNumber=?, streetAddress=?, suburb=?, city=? where id=?;";

        private readonly string _updateCustomersPassword =
            "Update SiteUser set password=? where id=? and userType='C';";

        private readonly string _updateAdministratorNoPassword =
            "Update SiteUser set login=?, userType='A', emailAddress=? where id=?;";

        private readonly string _updateAdministratorsPassword =
            "Update SiteUser set password=? where id=? and userType='A';";

        private readonly string _updateCustomerIsDisabled = "Update SiteUser set IsDisabled=1 where id=?";

        private readonly string _selectAllCategories = "select * from Category;";

        private readonly string _selectSingleCategoryById = "Select * from Category where id=?;";

        private readonly string _insertCategory = "insert into Category (name) values (?);";

        private readonly string _updateCategory = "update Category set name=? where id=?;";

        private readonly string _selectAllColours = "select * from Colour;";

        private readonly string _selectSingleColourById = "Select * from Colour where id=?;";

        private readonly string _insertColour = "insert into Colour (name) values (?);";

        private readonly string _updateColour = "update Colour set name=? where id=?;";

        private readonly string _insertSupplier = "insert into Supplier (name, contactNumber, emailAddress) values (?, ?, ?);";

        private readonly string _updateSupplier = "update Supplier set name=? contactNumber=? emailAddress=? where id=?;";

        private readonly string _selectAllSuppliers = "select * from Supplier;";

        private readonly string _selectSingleSupplierById = "Select * from Supplier where id=?;";


        private DataManager()
        {
            _connection = new OleDbConnection(ConfigurationManager.ConnectionStrings["DeveloperExpressConnection"]
                .ConnectionString);

            BuildDatabase();
        }

        /// <summary>
        ///     Queries to build tables and initial state.
        /// </summary>
        private void BuildDatabase()
        {
            var dbCommand = new OleDbCommand(_buildSiteUserTable, _connection);
            RunDbCommandNoResults(dbCommand);

            dbCommand = new OleDbCommand(_buildOrderTable, _connection);
            RunDbCommandNoResults(dbCommand);

            dbCommand = new OleDbCommand(_buildCategoryTable, _connection);
            RunDbCommandNoResults(dbCommand);

            dbCommand = new OleDbCommand(_buildSupplierTable, _connection);
            RunDbCommandNoResults(dbCommand);

            dbCommand = new OleDbCommand(_buildColourTable, _connection);
            RunDbCommandNoResults(dbCommand);

            dbCommand = new OleDbCommand(_buildCapTable, _connection);
            RunDbCommandNoResults(dbCommand);

            dbCommand = new OleDbCommand(_buildCapColourTable, _connection);
            RunDbCommandNoResults(dbCommand);

            dbCommand = new OleDbCommand(_buildOrderItemTable, _connection);
            RunDbCommandNoResults(dbCommand);

            dbCommand = new OleDbCommand(_insertDefaultCategories, _connection);
            RunDbCommandNoResults(dbCommand);

            dbCommand = new OleDbCommand(_insertDefaultColours , _connection);
            RunDbCommandNoResults(dbCommand);

            dbCommand = new OleDbCommand(_insertDefaultUserAdmin, _connection);
            RunDbCommandNoResults(dbCommand);

        }

        /// <summary>
        ///     Run an SQL query without returning a result set.
        /// </summary>
        /// <param name="dbCommand"></param>
        private void RunDbCommandNoResults(OleDbCommand dbCommand)
        {
            try
            {
                _connection.Open();
                dbCommand.ExecuteNonQuery();
            }
            finally
            {
                _connection.Close();
            }
            
        }

        /// <summary>
        ///     Getter for DataManager Singleton Instance
        /// </summary>
        public static DataManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DataManager();
                }

                return _instance;
            }
        }

        /// <summary>
        ///     OleDb method to get list of all customers.
        /// </summary>
        /// <returns></returns>
        public List<Customer> GetAllCustomers()
        {
            List<Customer> records = new List<Customer>();
            OleDbDataReader reader = null;

            try
            {
                _connection.Open();
                reader = (new OleDbCommand(_selectAllCustomers, _connection)).ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        Customer customer = new Customer();
                        customer.ID = reader.GetInt32(0);
                        customer.FirstName = reader["firstName"].ToString();
                        customer.LastName = reader["lastName"].ToString();
                        customer.Login = reader["login"].ToString();
                        customer.Email = reader["emailAddress"].ToString();
                        customer.HomeNumber = reader["homeNumber"].ToString();
                        customer.WorkNumber = reader["workNumber"].ToString();
                        customer.MobileNumber = reader["mobileNumber"].ToString();
                        customer.StreetAddress = reader["streetAddress"].ToString();
                        customer.Suburb = reader["suburb"].ToString();
                        customer.City = reader["city"].ToString();
                        records.Add(customer);
                    }
                }
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
                _connection.Close();
            }

            return records;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="id"></param>
        public void DisableExistingCustomer(int id)
        {
            OleDbCommand command = new OleDbCommand(_updateCustomerIsDisabled, _connection);
            command.Parameters.Add(new OleDbParameter("@IDENTIFIER", OleDbType.Integer));
            command.Parameters["@IDENTIFIER"].Value = id;
            RunDbCommandNoResults(command);
        }

        /// <summary>
        ///     Return a single customer referenced by id. If no customer fetched, return null.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Customer GetSingleCustomerById(int id)
        {
            _connection.Open();
            OleDbDataReader reader = null;
            Customer customer = null;

            try
            {
                OleDbCommand command = new OleDbCommand(_selectSingleCustomerById, _connection);
                command.Parameters.Add(new OleDbParameter("@IDENTIFIER", OleDbType.Integer));
                command.Parameters["@IDENTIFIER"].Value = id;

                reader = (command.ExecuteReader());


                if (reader.HasRows && reader.Read())
                {
                    customer = new Customer();
                    customer.ID = Convert.ToInt32(reader["id"]);
                    customer.Login = reader["login"].ToString();
                    customer.Email = reader["emailAddress"].ToString();
                    customer.Password = reader["password"].ToString();
                    customer.FirstName = reader["firstName"].ToString();
                    customer.LastName = reader["lastName"].ToString();
                    customer.HomeNumber = reader["homeNumber"].ToString();
                    customer.WorkNumber = reader["workNumber"].ToString();
                    customer.MobileNumber = reader["mobileNumber"].ToString();
                    customer.StreetAddress = reader["streetAddress"].ToString();
                    customer.Suburb = reader["suburb"].ToString();
                    customer.City = reader["city"].ToString();
                    customer.IsDisabled = Convert.ToBoolean(reader["isDisabled"]);

                }

                reader.Close();
            }
            catch (Exception ex)
            {
                throw ex;
            }
            finally
            {
                reader.Close();
                _connection.Close();
            }


            return customer;
        }

        /// <summary>
        ///     Return a single customer referenced by login. If no customer fetched, return null.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Customer GetSingleCustomerByLogin(string login)
        {
            _connection.Open();
            Customer customer = null;
            OleDbDataReader reader = null;

            try
            {
                OleDbCommand command = new OleDbCommand(_selectSingleCustomerByLogin, _connection);
                command.Parameters.Add(new OleDbParameter("@LOGIN", OleDbType.VarChar));
                command.Parameters["@LOGIN"].Value = login;
                reader = (command.ExecuteReader());
                
                if (reader.HasRows && reader.Read())
                {
                    customer = new Customer();
                    customer.ID = Convert.ToInt32(reader["id"]);
                    customer.Login = reader["login"].ToString();
                    customer.Email = reader["emailAddress"].ToString();
                    customer.Password = reader["password"].ToString();
                    customer.FirstName = reader["firstName"].ToString();
                    customer.LastName = reader["lastName"].ToString();
                    customer.HomeNumber = reader["homeNumber"].ToString();
                    customer.WorkNumber = reader["workNumber"].ToString();
                    customer.MobileNumber = reader["mobileNumber"].ToString();
                    customer.StreetAddress = reader["streetAddress"].ToString();
                    customer.Suburb = reader["suburb"].ToString();
                    customer.City = reader["city"].ToString();
                    customer.IsDisabled = Convert.ToBoolean(reader["isDisabled"]);

                }
                
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
                _connection.Close();

            }

            return customer;
        }

        /// <summary>
        ///     Return a single customer referenced by email. If no customer fetched, return null.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Customer GetSingleCustomerByEmail(string email)
        {
            _connection.Open();
            OleDbDataReader reader = null;
            Customer customer = null;

            try
            {
                OleDbCommand command = new OleDbCommand(_selectSingleCustomerByEmail, _connection);
                command.Parameters.Add(new OleDbParameter("@EMAIL", OleDbType.VarChar));
                command.Parameters["@EMAIL"].Value = email;
                reader = (command.ExecuteReader());

                if (reader.HasRows && reader.Read())
                {
                    customer = new Customer();
                    customer.ID = Convert.ToInt32(reader["id"]);
                    customer.Login = reader["login"].ToString();
                    customer.Email = reader["emailAddress"].ToString();
                    customer.Password = reader["password"].ToString();
                    customer.FirstName = reader["firstName"].ToString();
                    customer.LastName = reader["lastName"].ToString();
                    customer.HomeNumber = reader["homeNumber"].ToString();
                    customer.WorkNumber = reader["workNumber"].ToString();
                    customer.MobileNumber = reader["mobileNumber"].ToString();
                    customer.StreetAddress = reader["streetAddress"].ToString();
                    customer.Suburb = reader["suburb"].ToString();
                    customer.City = reader["city"].ToString();
                    customer.IsDisabled = Convert.ToBoolean(reader["isDisabled"]);

                }
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
                _connection.Close();

            }

            return customer;
        }

        /// <summary>
        ///     Add a new customer with this login, email and data
        ///     Use randomised password for security.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="login"></param>
        /// <param name="passwordHash"></param>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <param name="homeNumber"></param>
        /// <param name="workNumber"></param>
        /// <param name="mobileNumber"></param>
        /// <param name="streetAddress"></param>
        /// <param name="suburb"></param>
        /// <param name="city"></param>
        public void AddNewCustomer(string email, string login, string passwordHash,
            string firstName, string lastName, string homeNumber,
                    string workNumber, string mobileNumber, string streetAddress, string suburb, string city)
        {
            OleDbCommand command = new OleDbCommand(_insertCustomer, _connection);
            command.Parameters.Add(new OleDbParameter("@LOGIN", OleDbType.VarChar));
            command.Parameters.Add(new OleDbParameter("@PASSWORD", OleDbType.VarChar));
            command.Parameters.Add(new OleDbParameter("@EMAIL", OleDbType.VarChar));
            command.Parameters.Add(new OleDbParameter("@FIRSTNAME", OleDbType.VarChar));
            command.Parameters.Add(new OleDbParameter("@LASTNAME", OleDbType.VarChar));
            command.Parameters.Add(new OleDbParameter("@HOMENUMBER", OleDbType.VarChar));
            command.Parameters.Add(new OleDbParameter("@WORKNUMBER", OleDbType.VarChar));
            command.Parameters.Add(new OleDbParameter("@MOBILENUMBER", OleDbType.VarChar));
            command.Parameters.Add(new OleDbParameter("@STREETADDRESS", OleDbType.VarChar));
            command.Parameters.Add(new OleDbParameter("@SUBURB", OleDbType.VarChar));
            command.Parameters.Add(new OleDbParameter("@CITY", OleDbType.VarChar));
            command.Parameters.Add(new OleDbParameter("@DISABLED", OleDbType.Boolean));
            command.Parameters["@EMAIL"].Value = email;
            command.Parameters["@LOGIN"].Value = login;
            command.Parameters["@PASSWORD"].Value = passwordHash;
            command.Parameters["@FIRSTNAME"].Value = firstName;
            command.Parameters["@LASTNAME"].Value = lastName;
            command.Parameters["@HOMENUMBER"].Value = homeNumber;
            command.Parameters["@WORKNUMBER"].Value = workNumber;
            command.Parameters["@MOBILENUMBER"].Value = mobileNumber;
            command.Parameters["@STREETADDRESS"].Value = streetAddress;
            command.Parameters["@SUBURB"].Value = suburb;
            command.Parameters["@CITY"].Value = city;
            command.Parameters["@DISABLED"].Value = false;

            RunDbCommandNoResults(command);
        }

        /// <summary>
        ///     Update an existing customer by id.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="email"></param>
        /// <param name="login"></param>
        /// <param name="firstName"></param>
        /// <param name="lastName"></param>
        /// <param name="homeNumber"></param>
        /// <param name="workNumber"></param>
        /// <param name="mobileNumber"></param>
        /// <param name="streetAddress"></param>
        /// <param name="suburb"></param>
        /// <param name="city"></param>
        public void UpdateExistingCustomer(int id, string email, string login,
            string firstName, string lastName, string homeNumber,
                    string workNumber, string mobileNumber, string streetAddress, string suburb, string city)
        {

            OleDbCommand command =
                new OleDbCommand(_updateCustomerNoPassword, _connection);
            command.Parameters.Add(new OleDbParameter("@LOGIN", OleDbType.VarChar));
            command.Parameters.Add(new OleDbParameter("@EMAIL", OleDbType.VarChar));
            command.Parameters.Add(new OleDbParameter("@FIRSTNAME", OleDbType.VarChar));
            command.Parameters.Add(new OleDbParameter("@LASTNAME", OleDbType.VarChar));
            command.Parameters.Add(new OleDbParameter("@HOMENUMBER", OleDbType.VarChar));
            command.Parameters.Add(new OleDbParameter("@WORKNUMBER", OleDbType.VarChar));
            command.Parameters.Add(new OleDbParameter("@MOBILENUMBER", OleDbType.VarChar));
            command.Parameters.Add(new OleDbParameter("@STREETADDRESS", OleDbType.VarChar));
            command.Parameters.Add(new OleDbParameter("@SUBURB", OleDbType.VarChar));
            command.Parameters.Add(new OleDbParameter("@CITY", OleDbType.VarChar));
            command.Parameters.Add(new OleDbParameter("@IDENTIFIER", OleDbType.Integer));
            command.Parameters["@EMAIL"].Value = email;
            command.Parameters["@LOGIN"].Value = login;
            command.Parameters["@IDENTIFIER"].Value = id;
            command.Parameters["@FIRSTNAME"].Value = firstName;
            command.Parameters["@LASTNAME"].Value = lastName;
            command.Parameters["@HOMENUMBER"].Value = homeNumber;
            command.Parameters["@WORKNUMBER"].Value = workNumber;
            command.Parameters["@MOBILENUMBER"].Value = mobileNumber;
            command.Parameters["@STREETADDRESS"].Value = streetAddress;
            command.Parameters["@SUBURB"].Value = suburb;
            command.Parameters["@CITY"].Value = city;
            RunDbCommandNoResults(command);

            
        }

        /// <summary>
        ///     
        /// </summary>
        /// <param name="id"></param>
        /// <param name="passwordHash"></param>
        public void UpdateExistingCustomerPassword(int id, string passwordHash)
        {
            OleDbCommand command =
                new OleDbCommand(_updateCustomersPassword,
                    _connection);
            command.Parameters.Add(new OleDbParameter("@PASSWORD", OleDbType.VarChar));
            command.Parameters.Add(new OleDbParameter("@IDENTIFIER", OleDbType.Integer));
            command.Parameters["@IDENTIFIER"].Value = id;
            command.Parameters["@PASSWORD"].Value = passwordHash;
            RunDbCommandNoResults(command);
        }

        /// <summary>
        ///     OleDb method to get list of all admins.
        /// </summary>
        /// <returns></returns>
        public List<Administrator> GetAllAdministrators()
        {
            List<Administrator> records = new List<Administrator>();
            OleDbDataReader reader = null;

            try
            {
                _connection.Open();
                reader = (new OleDbCommand(_selectAllAdmins, _connection)).ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        Administrator admin = new Administrator();
                        admin.ID = Convert.ToInt32(reader["id"]);
                        admin.Login = reader["login"].ToString();
                        admin.Email = reader["emailAddress"].ToString();
                        admin.Password = reader["password"].ToString();
                        records.Add(admin);

                    }
                }
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
                _connection.Close();
            }

            

            return records;
        }

        /// <summary>
        ///     Return a single customer referenced by id. If no customer fetched, return null.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Administrator GetSingleAdministratorById(int id)
        {
            _connection.Open();
            OleDbDataReader reader = null;
            Administrator record = null;

            try
            {
                OleDbCommand command = new OleDbCommand(_selectSingleAdminById, _connection);
                command.Parameters.Add(new OleDbParameter("@IDENTIFIER", OleDbType.Integer));
                command.Parameters["@IDENTIFIER"].Value = id;
                reader = (command.ExecuteReader());


                if (reader.HasRows && reader.Read())
                {
                    record = new Administrator();
                    record.ID = Convert.ToInt32(reader["id"]);
                    record.Login = reader["login"].ToString();
                    record.Email = reader["emailAddress"].ToString();
                    record.Password = reader["password"].ToString();

                }
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
                _connection.Close();
            }

            return record;
        }

        /// <summary>
        ///     Return a single admin referenced by login. If no admin fetched, return null.
        /// </summary>
        /// <param name="login"></param>
        /// <returns></returns>
        public Administrator GetSingleAdministratorByLogin(string login)
        {
            _connection.Open();
            OleDbDataReader reader = null;
            Administrator record = null;

            try
            {
                OleDbCommand command = new OleDbCommand(_selectSingleAdminByLogin, _connection);
                command.Parameters.Add(new OleDbParameter("@LOGIN", OleDbType.VarChar));
                command.Parameters["@LOGIN"].Value = login;
                reader = (command.ExecuteReader());

                if (reader.HasRows && reader.Read())
                {
                    record = new Administrator();
                    record.ID = Convert.ToInt32(reader["id"]);
                    record.Login = reader["login"].ToString();
                    record.Email = reader["emailAddress"].ToString();
                    record.Password = reader["password"].ToString();

                }
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
                _connection.Close();
            }
            
            return record;
        }


        /// <summary>
        ///     Add a new admin with this login  and email
        ///     Use randomised password for security.
        /// </summary>
        /// <param name="email"></param>
        /// <param name="login"></param>
        /// <param name="passwordHash"></param>
        public void AddNewAdmin(string email, string login, string passwordHash)
        {
            OleDbCommand command = new OleDbCommand(_insertAdministrator, _connection);
            command.Parameters.Add(new OleDbParameter("@LOGIN", OleDbType.VarChar));
            command.Parameters.Add(new OleDbParameter("@PASSWORD", OleDbType.VarChar));
            command.Parameters.Add(new OleDbParameter("@EMAIL", OleDbType.VarChar));
            command.Parameters["@LOGIN"].Value = login;
            command.Parameters["@EMAIL"].Value = email;
            command.Parameters["@PASSWORD"].Value = passwordHash;
            RunDbCommandNoResults(command);
        }

        /// <summary>
        ///     Update an existing admin by id.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="email"></param>
        /// <param name="login"></param>
        public void UpdateExistingAdmin(int id, string email, string login)
        {
            OleDbCommand command =
                new OleDbCommand(_updateAdministratorNoPassword,
                    _connection);
            command.Parameters.Add(new OleDbParameter("@LOGIN", OleDbType.VarChar));
            command.Parameters.Add(new OleDbParameter("@EMAIL", OleDbType.VarChar));
            command.Parameters.Add(new OleDbParameter("@IDENTIFIER", OleDbType.Integer));
            command.Parameters["@IDENTIFIER"].Value = id;
            command.Parameters["@EMAIL"].Value = email;
            command.Parameters["@LOGIN"].Value = login;

            RunDbCommandNoResults(command);
        }

        /// <summary>
        ///     
        /// </summary>
        /// <param name="id"></param>
        /// <param name="passwordHash"></param>
        public void UpdateExistingAdminPassword(int id, string passwordHash)
        {
            OleDbCommand command =
                new OleDbCommand(_updateAdministratorsPassword,
                    _connection);
            command.Parameters.Add(new OleDbParameter("@PASSWORD", OleDbType.VarChar));
            command.Parameters.Add(new OleDbParameter("@IDENTIFIER", OleDbType.Integer));
            command.Parameters["@PASSWORD"].Value = passwordHash;
            command.Parameters["@IDENTIFIER"].Value = id;

            RunDbCommandNoResults(command);
        }


        /// <summary>
        ///     get list of all Category.
        /// </summary>
        /// <returns></returns>
        public List<Category> GetAllCategories()
        {
            List<Category> records = new List<Category>();
            OleDbDataReader reader = null;

            try
            {
                _connection.Open();
                reader = (new OleDbCommand(_selectAllCategories, _connection)).ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        Category category = new Category();
                        category.ID = Convert.ToInt32(reader["id"]);
                        category.Name = reader["name"].ToString();
                        records.Add(category);
                    }
                }
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
                _connection.Close();
            }

            return records;
        }

        /// <summary>
        ///     Return a single Category referenced by id. If no Category fetched, return null.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Category GetSingleCategoryById(int id)
        {
            _connection.Open();
            OleDbDataReader reader = null;
            Category category = null;

            try
            {
                OleDbCommand command = new OleDbCommand(_selectSingleCategoryById, _connection);
                command.Parameters.Add(new OleDbParameter("@IDENTIFIER", OleDbType.Integer));
                command.Parameters["@IDENTIFIER"].Value = id;
                reader = (command.ExecuteReader());


                if (reader.HasRows && reader.Read())
                {
                    category = new Category();
                    category.ID = Convert.ToInt32(reader["id"]);
                    category.Name = reader["name"].ToString();
                }
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
                _connection.Close();
            }


            return category;
        }

        /// <summary>
        ///     Add a new Category with this name
        /// </summary>
        /// <param name="name"></param>
        public void AddNewCategory(string name)
        {
            OleDbCommand command = new OleDbCommand(_insertCategory, _connection);
            command.Parameters.Add(new OleDbParameter("@NAME", OleDbType.VarChar));
            command.Parameters["@NAME"].Value = name;
            RunDbCommandNoResults(command);
        }

        /// <summary>
        ///     Update an existing Category by id.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        public void UpdateExistingCategory(int id, string name)
        {
            OleDbCommand command =
                new OleDbCommand(_updateCategory,
                    _connection);
            command.Parameters.Add(new OleDbParameter("@NAME", OleDbType.VarChar));
            command.Parameters.Add(new OleDbParameter("@IDENTIFIER", OleDbType.Integer));
            command.Parameters["@IDENTIFIER"].Value = id;
            command.Parameters["@NAME"].Value = name;

            RunDbCommandNoResults(command);
        }



        /// <summary>
        ///     get list of all Colour.
        /// </summary>
        /// <returns></returns>
        public List<Colour> GetAllColours()
        {
            List<Colour> records = new List<Colour>();
            OleDbDataReader reader = null;

            try
            {
                _connection.Open();
                reader = (new OleDbCommand(_selectAllColours, _connection)).ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        Colour colour = new Colour();
                        colour.ID = Convert.ToInt32(reader["id"]);
                        colour.Name = reader["name"].ToString();
                        records.Add(colour);
                    }
                }
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
                _connection.Close();
            }

            return records;
        }

        /// <summary>
        ///     Return a single Colour referenced by id. If no Colour fetched, return null.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Colour GetSingleColourById(int id)
        {
            _connection.Open();
            OleDbDataReader reader = null;
            Colour colour = null;

            try
            {
                OleDbCommand command = new OleDbCommand(_selectSingleColourById, _connection);
                command.Parameters.Add(new OleDbParameter("@IDENTIFIER", OleDbType.Integer));
                command.Parameters["@IDENTIFIER"].Value = id;
                reader = (command.ExecuteReader());


                if (reader.HasRows && reader.Read())
                {
                    colour = new Colour();
                    colour.ID = Convert.ToInt32(reader["id"]);
                    colour.Name = reader["name"].ToString();
                }
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
                _connection.Close();
            }


            return colour;
        }

        /// <summary>
        ///     Add a new Colour with this name
        /// </summary>
        /// <param name="name"></param>
        public void AddNewColour(string name)
        {
            OleDbCommand command = new OleDbCommand(_insertColour, _connection);
            command.Parameters.Add(new OleDbParameter("@NAME", OleDbType.VarChar));
            command.Parameters["@NAME"].Value = name;
            RunDbCommandNoResults(command);
        }

        /// <summary>
        ///     Update an existing Colour by id.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        public void UpdateExistingColour(int id, string name)
        {
            OleDbCommand command =
                new OleDbCommand(_updateColour,
                    _connection);
            command.Parameters.Add(new OleDbParameter("@NAME", OleDbType.VarChar));
            command.Parameters.Add(new OleDbParameter("@IDENTIFIER", OleDbType.Integer));
            command.Parameters["@IDENTIFIER"].Value = id;
            command.Parameters["@NAME"].Value = name;

            RunDbCommandNoResults(command);
        }


        /// <summary>
        ///     get list of all suppliers.
        /// </summary>
        /// <returns></returns>
        public List<Supplier> GetAllSuppliers()
        {
            List<Supplier> records = new List<Supplier>();
            OleDbDataReader reader = null;

            try
            {
                _connection.Open();
                reader = (new OleDbCommand(_selectAllSuppliers, _connection)).ExecuteReader();

                if (reader.HasRows)
                {
                    while (reader.Read())
                    {
                        Supplier item = new Supplier();
                        item.ID = Convert.ToInt32(reader["id"]);
                        item.Name = reader["name"].ToString();
                        item.Email = reader["emailAddress"].ToString();
                        item.ContactNumber = reader["contactNumber"].ToString();
                        records.Add(item);
                    }
                }
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
                _connection.Close();
            }

            return records;
        }

        /// <summary>
        ///     Return a single supplier referenced by id. If no supplier fetched, return null.
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Supplier GetSingleSupplierById(int id)
        {
            OleDbDataReader reader = null;
            Supplier item = null;

            try
            {
                _connection.Open();
                OleDbCommand command = new OleDbCommand(_selectSingleSupplierById, _connection);
                command.Parameters.Add(new OleDbParameter("@IDENTIFIER", OleDbType.Integer));
                command.Parameters["@IDENTIFIER"].Value = id;
                reader = (command.ExecuteReader());


                if (reader.HasRows && reader.Read())
                {
                    item = new Supplier();
                    item.ID = Convert.ToInt32(reader["id"]);
                    item.Name = reader["name"].ToString();
                    item.Email = reader["emailAddress"].ToString();
                    item.ContactNumber = reader["contactNumber"].ToString();
                }
            }
            finally
            {
                if (reader != null)
                {
                    reader.Close();
                }
                _connection.Close();
            }


            return item;
        }

        /// <summary>
        ///     Add a new supplier with this name, contact number  and email
        /// </summary>
        /// <param name="name"></param>
        /// <param name="contactNumber"></param>
        /// <param name="email"></param>
        public void AddNewSupplier(string name, string contactNumber, string email)
        {
            OleDbCommand command = new OleDbCommand(_insertSupplier, _connection);
            command.Parameters.Add(new OleDbParameter("@NAME", OleDbType.VarChar));
            command.Parameters.Add(new OleDbParameter("@CONTACTNUMBER", OleDbType.VarChar));
            command.Parameters.Add(new OleDbParameter("@EMAIL", OleDbType.VarChar));
            command.Parameters["@NAME"].Value = name;
            command.Parameters["@CONTACTNUMBER"].Value = contactNumber;
            command.Parameters["@EMAIL"].Value = email;
            RunDbCommandNoResults(command);
        }

        /// <summary>
        ///     Update an existing supplier by id.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="name"></param>
        /// <param name="contactNumber"></param>
        /// <param name="email"></param>
        public void UpdateExistingSupplier(int id, string name, string contactNumber, string email)
        {
            OleDbCommand command =
                new OleDbCommand(_updateSupplier,
                    _connection);
            command.Parameters.Add(new OleDbParameter("@IDENTIFIER", OleDbType.Integer));
            command.Parameters.Add(new OleDbParameter("@NAME", OleDbType.VarChar));
            command.Parameters.Add(new OleDbParameter("@CONTACTNUMBER", OleDbType.VarChar));
            command.Parameters.Add(new OleDbParameter("@EMAIL", OleDbType.VarChar));
            command.Parameters["@IDENTIFIER"].Value = id;
            command.Parameters["@NAME"].Value = name;
            command.Parameters["@CONTACTNUMBER"].Value = contactNumber;
            command.Parameters["@EMAIL"].Value = email;

            RunDbCommandNoResults(command);
        }


    }

}