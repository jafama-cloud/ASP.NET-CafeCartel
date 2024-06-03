using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using System.Data.SqlClient;
using System.IO;
using System.Drawing;
using System.Drawing.Imaging;
using System.Web.Services;

namespace ManusFinalProject.Controllers
{
    public class HomeController : Controller
    {
        string connStr = @"Data Source=(LocalDB)\MSSQLLocalDB;AttachDbFilename=D:\JVFILES\ASP\ManusFinalProject\ManusFinalProject\App_Data\Database1.mdf;Integrated Security=True";
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            ViewBag.Message = "Your application description page.";

            return View();
        }

        public ActionResult Contact()
        {
            ViewBag.Message = "Your contact page.";

            return View();
        }

        public ActionResult CreateAccount()
        {
            return View();
        }

        public ActionResult Register()
        {
            var data = new List<object>();
            string username = Request["username"];
            string password = Request["password"];

            using (var db = new SqlConnection(connStr))
            {
                db.Open();

                // Check if the username already exists
                using (var checkCmd = db.CreateCommand())
                {
                    checkCmd.CommandType = CommandType.Text;
                    checkCmd.CommandText = "SELECT COUNT(*) FROM ACCOUNT WHERE username = @username";
                    checkCmd.Parameters.AddWithValue("@username", username);

                    int userCount = (int)checkCmd.ExecuteScalar();
                    if (userCount > 0)
                    {
                        // Username already exists
                        data.Add(new
                        {
                            mess = -1,
                            message = "Username already exists. Please choose a different username."
                        });

                        return Json(data, JsonRequestBehavior.AllowGet);
                    }
                }

                // If username does not exist, insert new user and get the account_id
                using (var cmd = db.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "INSERT INTO ACCOUNT (username, password) OUTPUT INSERTED.account_id VALUES (@username, @password)";
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", password);

                    var accountId = cmd.ExecuteScalar();
                    if (accountId != null)
                    {
                        Session["AccountId"] = accountId.ToString();

                        data.Add(new
                        {
                            mess = 1,
                            message = "Set up your profile."
                        });
                    }
                    else
                    {
                        data.Add(new
                        {
                            mess = 0,
                            message = "Failed to create account. Please try again."
                        });
                    }
                }

                db.Close();
            }

            return Json(data, JsonRequestBehavior.AllowGet);
        }


        public ActionResult AdminLogin()
        {
            return View();
        }

        public ActionResult admin()
        {
            var data = new List<object>();
            string username = Request["username"];
            string password = Request["password"];
            using (var db = new SqlConnection(connStr))
            {
                db.Open();
                using (var cmd = db.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "SELECT COUNT(*) FROM administrator WHERE username = @username AND password = @password";
                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", password);

                    int count = (int)cmd.ExecuteScalar(); // Returns the number of matching rows

                    if (count > 0)
                    {
                        data.Add(new
                        {
                            mess = 1
                        });
                    }
                }
                db.Close();
            }
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult CustomerLogin()
        {
            ViewBag.SuccessMessage = TempData["SuccessMessage"];
            return View();
        }

        public ActionResult customer()
        {
            var data = new List<object>();
            string username = Request["username"];
            string password = Request["password"];
            using (var db = new SqlConnection(connStr))
            {
                db.Open();
                using (var cmd = db.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "SELECT account.account_id, profile.Account_Id, profile.FirstName FROM account JOIN profile ON account.account_id = profile.Account_Id WHERE username = @username AND password = @password";
                    //cmd.CommandText = "SELECT * FROM ACCOUNT WHERE username = @username AND password = @password";

                    cmd.Parameters.AddWithValue("@username", username);
                    cmd.Parameters.AddWithValue("@password", password);

                    var reader = cmd.ExecuteReader();
                    if (reader.Read())
                    {
                        var accountId = reader["account_id"].ToString();
                        var firstName = reader["FirstName"].ToString();

                        // Store the account ID and first name in session
                        Session["AccountId"] = accountId;
                        Session["FirstName"] = firstName;

                        data.Add(new
                        {
                            mess = 1
                        });
                    }
                    reader.Close();
                }
                db.Close();
            }
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Logout()
        {
            // Clear the session
            Session.Clear();
            Session.Abandon();

            // Optionally, you can add logic to redirect to the login page
            //return RedirectToAction("CustomerLogin", "Home");

            // Return a JSON response indicating the user is logged out
            return Json(new { success = true, message = "Logged out successfully." }, JsonRequestBehavior.AllowGet);
        }


        public ActionResult ProductEntryForm()
        {
            return View();
        }

        [HttpPost]
        public ActionResult ProductEntryForm(FormCollection collection, HttpPostedFileBase image)
        {
            string img = Path.GetFileName(image.FileName);
            var extension = Path.GetExtension(img).ToLower();
            int filesize = image.ContentLength;

            string logpath = "D:\\JVFILES\\ASP\\ManusFinalProject\\ManusFinalProject\\Content\\Images\\";
            string filepath = Path.Combine(logpath, img);
            image.SaveAs(filepath);
            var ProductID = GenerateID(); // Call a function to generate a unique product ID
            var productName = collection["productName"];
            var brandName = collection["brandName"];
            var blendType = collection["blendType"];
            var flavorProfile = collection["flavorProfile"];
            var roastLevel = collection["roastLevel"];
            var origin = collection["origin"];
            var packagingSize = collection["packagingSize"];
            var brewingMethod = collection["brewingMethod"];
            var expiryDate = collection["expiryDate"];
            var price = collection["price"];
            var availability = collection["availability"];
            var quantity = collection["quantity"];

            using (var db = new SqlConnection(connStr))
            {
                db.Open();
                using (var cmd = db.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "INSERT INTO PRODUCT (ProductID, ProductName, BrandName, SpecificCoffeeBlend, FlavorProfile, RoastLevel, Origin, PackagingSize, Price, Availability, RecommendedBrewingMethod, ExpiryDate, Quantity, Image) " +
                                      "VALUES (@productID, @productName, @brandName, @blendType, @flavorProfile, @roastLevel, @origin, @packagingSize, @price, @availability, @brewingMethod, @expiryDate, @quantity, @file)";
                    cmd.Parameters.AddWithValue("@productID", ProductID);
                    cmd.Parameters.AddWithValue("@productName", productName);
                    cmd.Parameters.AddWithValue("@brandName", brandName);
                    cmd.Parameters.AddWithValue("@blendType", blendType);
                    cmd.Parameters.AddWithValue("@flavorProfile", flavorProfile);
                    cmd.Parameters.AddWithValue("@roastLevel", roastLevel);
                    cmd.Parameters.AddWithValue("@origin", origin);
                    cmd.Parameters.AddWithValue("@packagingSize", packagingSize);
                    cmd.Parameters.AddWithValue("@price", price);
                    cmd.Parameters.AddWithValue("@availability", availability);
                    cmd.Parameters.AddWithValue("@brewingMethod", brewingMethod);
                    cmd.Parameters.AddWithValue("@expiryDate", expiryDate);
                    cmd.Parameters.AddWithValue("@quantity", quantity);
                    cmd.Parameters.AddWithValue("@file", img);

                    var ctr = cmd.ExecuteNonQuery();
                    if (ctr > 0)
                    {
                        Response.Write("<script>alert('Product added successfully.')</script>");
                    }
                }
            }

            return View();
        }

        private string GenerateID()
        {
            string dateTime = DateTime.Now.ToString("yyyyMMddHHmmss");
            string productID = dateTime;
            return productID;
        }
        public ActionResult DisplayProducts()
        {
            return View();
        }

        public ActionResult DeleteProduct()
        {
            var data = new List<object>();
            var productId = Request["id"];
            using (var db = new SqlConnection(connStr))
            {
                db.Open();
                using (var cmd = db.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "DELETE FROM PRODUCT WHERE ProductID = @ProductId";
                    cmd.Parameters.AddWithValue("@ProductId", productId);
                    var ctr = cmd.ExecuteNonQuery();
                    if (ctr > 0)
                    {
                        data.Add(new { mess = 1 });
                    }
                    else
                    {
                        data.Add(new { mess = 0 });
                    }
                }
            }
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult EditProduct()
        {
            var data = new List<object>();
            var productId = Request["productID"];
            var productName = Request["productName"];
            var brandName = Request["brandName"];
            var blendType = Request["blendType"];
            var flavorProfile = Request["flavorProfile"];
            var roastLevel = Request["roastLevel"];
            var origin = Request["origin"];
            var packagingSize = Request["packagingSize"];
            var brewingMethod = Request["brewingMethod"];
            var expiryDate = Request["expiryDate"];
            var price = Request["price"];
            var availability = Request["availability"];
            var quantity = Request["quantity"];
            var imageName = "";

            if (Request.Files.Count > 0)
            {
                var image = Request.Files["image"];
                if (image != null && image.ContentLength > 0)
                {
                    imageName = Path.GetFileName(image.FileName);
                    var uploadDir = Server.MapPath("~/Content/Images/");
                    if (!Directory.Exists(uploadDir))
                    {
                        Directory.CreateDirectory(uploadDir);
                    }
                    var imagePath = Path.Combine(uploadDir, imageName);
                    image.SaveAs(imagePath);
                }
            }

            using (var db = new SqlConnection(connStr))
            {
                db.Open();
                using (var cmd = db.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "UPDATE PRODUCT SET ProductName=@ProductName, BrandName=@BrandName, SpecificCoffeeBlend=@BlendType, FlavorProfile=@FlavorProfile, RoastLevel=@RoastLevel, Origin=@Origin, PackagingSize=@PackagingSize, RecommendedBrewingMethod=@BrewingMethod, ExpiryDate=@ExpiryDate, Price=@Price, Availability=@Availability, Quantity=@Quantity" +
                                      (string.IsNullOrEmpty(imageName) ? "" : ", Image=@Image") +
                                      " WHERE ProductID=@ProductID";
                    cmd.Parameters.AddWithValue("@ProductID", productId);
                    cmd.Parameters.AddWithValue("@ProductName", productName);
                    cmd.Parameters.AddWithValue("@BrandName", brandName);
                    cmd.Parameters.AddWithValue("@BlendType", blendType);
                    cmd.Parameters.AddWithValue("@FlavorProfile", flavorProfile);
                    cmd.Parameters.AddWithValue("@RoastLevel", roastLevel);
                    cmd.Parameters.AddWithValue("@Origin", origin);
                    cmd.Parameters.AddWithValue("@PackagingSize", packagingSize);
                    cmd.Parameters.AddWithValue("@BrewingMethod", brewingMethod);
                    cmd.Parameters.AddWithValue("@ExpiryDate", expiryDate);
                    cmd.Parameters.AddWithValue("@Price", price);
                    cmd.Parameters.AddWithValue("@Availability", availability);
                    cmd.Parameters.AddWithValue("@Quantity", quantity);
                    if (!string.IsNullOrEmpty(imageName))
                    {
                        cmd.Parameters.AddWithValue("@Image", imageName);
                    }

                    var ctr = cmd.ExecuteNonQuery();
                    if (ctr > 0)
                    {
                        data.Add(new { mess = 1 });
                    }
                    else
                    {
                        data.Add(new { mess = 0 });
                    }
                }
            }
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult CustomerEntryForm()
        {
            var firstName = Session["FirstName"] != null ? Session["FirstName"].ToString() : "User";

            ViewBag.FirstName = firstName;

            return View();
        }

        [HttpPost]
        public ActionResult CustomerEntryForm(FormCollection collection)
        {
            var accountId = Session["AccountId"] != null ? Session["AccountId"].ToString() : null;

            if (accountId == null)
            {
                // Redirect to login page or handle unauthorized access
                return RedirectToAction("CustomerLogin");
            }

            var firstName = collection["firstName"];
            var lastName = collection["lastName"];
            var dateOfBirth = collection["dateOfBirth"];
            var gender = collection["gender"];
            var contactNumber = collection["contactNumber"];
            var email = collection["email"];
            var streetAddress = collection["streetAddress"];
            var city = collection["city"];
            var barangay = collection["barangay"];
            var postalCode = collection["postalCode"];
            var country = collection["country"];
            var occupation = collection["occupation"];
            var educationLevel = collection["educationLevel"];
            var shippingAddress = collection["shippingAddress"];

            using (var db = new SqlConnection(connStr))
            {
                db.Open();
                using (var cmd = db.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    // Check if the profile already exists
                    cmd.CommandText = "SELECT COUNT(*) FROM Profile WHERE Account_Id = @accountId";
                    cmd.Parameters.AddWithValue("@accountId", accountId);

                    int count = (int)cmd.ExecuteScalar();
                    if (count > 0)
                    {
                        Response.Write("<script>alert('Profile already exists. You cannot create a new profile.')</script>");
                        return View();
                    }
                }

                using (var cmd = db.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "INSERT INTO Profile (Account_Id, FirstName, LastName, DateOfBirth, Gender, ContactNumber, Email, StreetAddress, City, Barangay, PostalCode, Country, Occupation, EducationLevel, ShippingAddress) " +
                                      "VALUES (@accountId, @firstName, @lastName, @dateOfBirth, @gender, @contactNumber, @email, @streetAddress, @city, @barangay, @postalCode, @country, @occupation, @educationLevel, @shippingAddress)";
                    cmd.Parameters.AddWithValue("@accountId", accountId);
                    cmd.Parameters.AddWithValue("@firstName", firstName);
                    cmd.Parameters.AddWithValue("@lastName", lastName);
                    cmd.Parameters.AddWithValue("@dateOfBirth", dateOfBirth);
                    cmd.Parameters.AddWithValue("@gender", gender);
                    cmd.Parameters.AddWithValue("@contactNumber", contactNumber);
                    cmd.Parameters.AddWithValue("@email", email);
                    cmd.Parameters.AddWithValue("@streetAddress", streetAddress);
                    cmd.Parameters.AddWithValue("@city", city);
                    cmd.Parameters.AddWithValue("@barangay", barangay);
                    cmd.Parameters.AddWithValue("@postalCode", postalCode);
                    cmd.Parameters.AddWithValue("@country", country);
                    cmd.Parameters.AddWithValue("@occupation", occupation);
                    cmd.Parameters.AddWithValue("@educationLevel", educationLevel);
                    cmd.Parameters.AddWithValue("@shippingAddress", shippingAddress);

                    var rowsAffected = cmd.ExecuteNonQuery();
                    if (rowsAffected > 0)
                    {
                        // After successful database operation
                        TempData["SuccessMessage"] = "Account profile created successfully. Please login to your account.";
                        return RedirectToAction("CustomerLogin", "Home");

                    }
                    else
                    {
                        Response.Write("<script>alert('Failed to save profile data.')</script>");
                    }
                }

            }
            
            return View();
        }


        public ActionResult CustomerProfile()
        {
            var firstName = Session["FirstName"] != null ? Session["FirstName"].ToString() : "User";

            ViewBag.FirstName = firstName;

            return View();
        }

        [HttpPost]
        public ActionResult EditProfile()
        {
            var data = new List<object>();

            // Retrieve form data
            var accountId = Request["accountId"];
            var firstName = Request["editFirstName"];
            var lastName = Request["editLastName"];
            var dateOfBirth = Request["editDateOfBirth"];
            var gender = Request["gender"];
            var contactNumber = Request["editContactNumber"];
            var email = Request["editEmail"];
            var streetAddress = Request["editStreetAddress"];
            var city = Request["editCity"];
            var barangay = Request["editBarangay"];
            var postalCode = Request["editPostalCode"];
            var country = Request["editCountry"];
            var occupation = Request["editOccupation"];
            var educationLevel = Request["editEducationLevel"];
            var shippingAddress = Request["editShippingAddress"];

            // Update the profile in the database
            // Make sure to use parameterized queries to prevent SQL injection
            using (var db = new SqlConnection(connStr))
            {
                db.Open();
                using (var cmd = db.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "UPDATE PROFILE SET " +
                                      "FirstName = @FirstName, " +
                                      "LastName = @LastName, " +
                                      "DateOfBirth = @DateOfBirth, " +
                                      "Gender = @Gender, " +
                                      "ContactNumber = @ContactNumber, " +
                                      "Email = @Email, " +
                                      "StreetAddress = @StreetAddress, " +
                                      "City = @City, " +
                                      "Barangay = @Barangay, " +
                                      "PostalCode = @PostalCode, " +
                                      "Country = @Country, " +
                                      "Occupation = @Occupation, " +
                                      "EducationLevel = @EducationLevel, " +
                                      "ShippingAddress = @ShippingAddress " +
                                      "WHERE Account_Id = @AccountId";

                    // Add parameters
                    cmd.Parameters.AddWithValue("@FirstName", firstName);
                    cmd.Parameters.AddWithValue("@LastName", lastName);
                    cmd.Parameters.AddWithValue("@DateOfBirth", dateOfBirth);
                    cmd.Parameters.AddWithValue("@Gender", gender);
                    cmd.Parameters.AddWithValue("@ContactNumber", contactNumber);
                    cmd.Parameters.AddWithValue("@Email", email);
                    cmd.Parameters.AddWithValue("@StreetAddress", streetAddress);
                    cmd.Parameters.AddWithValue("@City", city);
                    cmd.Parameters.AddWithValue("@Barangay", barangay);
                    cmd.Parameters.AddWithValue("@PostalCode", postalCode);
                    cmd.Parameters.AddWithValue("@Country", country);
                    cmd.Parameters.AddWithValue("@Occupation", occupation);
                    cmd.Parameters.AddWithValue("@EducationLevel", educationLevel);
                    cmd.Parameters.AddWithValue("@ShippingAddress", shippingAddress);
                    cmd.Parameters.AddWithValue("@AccountId", accountId);

                    // Execute the update query
                    var rowsAffected = cmd.ExecuteNonQuery();

                    // Check if the profile was successfully updated
                    if (rowsAffected > 0)
                    {
                        data.Add(new { mess = 1 });
                        // Update ViewBag.FirstName with the new first name
                        var updatedFirstName = firstName; // or retrieve the updated first name from the database
                        Session["FirstName"] = updatedFirstName;
                    }
                    else
                    {
                        data.Add(new { mess = 0 });
                    }
                }
            }

            // Return the JSON response
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult DeleteProfile()
        {
            var data = new List<object>();
            var accountId = Request["accountId"];
            using (var db = new SqlConnection(connStr))
            {
                db.Open();
                using (var cmd = db.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "DELETE FROM PROFILE WHERE Account_Id = @accountId";
                    cmd.Parameters.AddWithValue("@AccountId", accountId);
                    var ctr = cmd.ExecuteNonQuery();
                    if (ctr > 0)
                    {
                        data.Add(new { mess = 1 });
                    }
                    else
                    {
                        data.Add(new { mess = 0 });
                    }
                }
            }
            return Json(data, JsonRequestBehavior.AllowGet);
        }

        public ActionResult DeleteAccount()
        {
            var data = new List<object>();
            var accountId = Request["accountId"];

            // Delete related records from the profile table first
            using (var db = new SqlConnection(connStr))
            {
                db.Open();
                using (var deleteCmd = db.CreateCommand())
                {
                    deleteCmd.CommandType = CommandType.Text;
                    deleteCmd.CommandText = "DELETE FROM profile WHERE Account_Id = @accountId";
                    deleteCmd.Parameters.AddWithValue("@accountId", accountId);
                    deleteCmd.ExecuteNonQuery();
                }
            }

            // Delete the account from the ACCOUNT table
            using (var db = new SqlConnection(connStr))
            {
                db.Open();
                using (var cmd = db.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "DELETE FROM ACCOUNT WHERE account_id = @accountId";
                    cmd.Parameters.AddWithValue("@AccountId", accountId);
                    var ctr = cmd.ExecuteNonQuery();
                    if (ctr > 0)
                    {
                        data.Add(new { mess = 1 });
                    }
                    else
                    {
                        data.Add(new { mess = 0 });
                    }
                }
            }

            return Json(data, JsonRequestBehavior.AllowGet);
        }


        public ActionResult CustomerProducts()
        {
            var firstName = Session["FirstName"] != null ? Session["FirstName"].ToString() : "User";

            ViewBag.FirstName = firstName;

            return View();
        }

        [HttpPost]
        public ActionResult AddToCart()
        {
            var data = new List<object>();
            var accountId = Request["accountId"];
            var productId = Request["productId"];
            var productName = Request["productName"];
            var quantity = Convert.ToInt32(Request["quantity"]);
            var price = Convert.ToDecimal(Request["price"]);

            using (var db = new SqlConnection(connStr))
            {
                db.Open();

                // Check if the product already exists in the cart for this account
                using (var checkCmd = db.CreateCommand())
                {
                    checkCmd.CommandType = CommandType.Text;
                    checkCmd.CommandText = "SELECT COUNT(*) FROM CART WHERE AccountID = @AccountID AND ProductID = @ProductID";
                    checkCmd.Parameters.AddWithValue("@AccountID", accountId);
                    checkCmd.Parameters.AddWithValue("@ProductID", productId);

                    var productExists = (int)checkCmd.ExecuteScalar() > 0;

                    if (productExists)
                    {
                        // Product exists, update the quantity and total amount
                        decimal totalAmount = quantity * price;

                        using (var updateCmd = db.CreateCommand())
                        {
                            updateCmd.CommandType = CommandType.Text;
                            updateCmd.CommandText = "UPDATE CART SET Quantity = @Quantity, TotalAmount = @TotalAmount " +
                                                    "WHERE AccountID = @AccountID AND ProductID = @ProductID";
                            updateCmd.Parameters.AddWithValue("@Quantity", quantity);
                            updateCmd.Parameters.AddWithValue("@TotalAmount", totalAmount);
                            updateCmd.Parameters.AddWithValue("@AccountID", accountId);
                            updateCmd.Parameters.AddWithValue("@ProductID", productId);

                            var ctr = updateCmd.ExecuteNonQuery();
                            if (ctr > 0)
                            {
                                data.Add(new { mess = 1 });
                            }
                            else
                            {
                                data.Add(new { mess = 0 });
                            }
                        }
                    }
                    else
                    {
                        // Product does not exist, insert a new row
                        decimal totalAmount = quantity * price;

                        using (var insertCmd = db.CreateCommand())
                        {
                            insertCmd.CommandType = CommandType.Text;
                            insertCmd.CommandText = "INSERT INTO CART (AccountID, ProductID, ProductName, Quantity, Price, TotalAmount) " +
                                                    "VALUES (@AccountID, @ProductID, @ProductName, @Quantity, @Price, @TotalAmount)";
                            insertCmd.Parameters.AddWithValue("@AccountID", accountId);
                            insertCmd.Parameters.AddWithValue("@ProductID", productId);
                            insertCmd.Parameters.AddWithValue("@ProductName", productName);
                            insertCmd.Parameters.AddWithValue("@Quantity", quantity);
                            insertCmd.Parameters.AddWithValue("@Price", price);
                            insertCmd.Parameters.AddWithValue("@TotalAmount", totalAmount);

                            var ctr = insertCmd.ExecuteNonQuery();
                            if (ctr > 0)
                            {
                                data.Add(new { mess = 1 });
                            }
                            else
                            {
                                data.Add(new { mess = 0 });
                            }
                        }
                    }
                }
            }

            return Json(data, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult BuyNow()
        {
            var data = new List<object>();
            var accountId = Session["AccountId"];
            var productId = Request["productId"];
            var productName = Request["productName"];
            var quantity = int.Parse(Request["quantity"]);
            var price = decimal.Parse(Request["price"]);
            var totalAmount = price * quantity;
            var success = false;
            var message = "An error occurred during the buy now process.";

            using (var db = new SqlConnection(connStr))
            {
                db.Open();

                using (var transaction = db.BeginTransaction())
                {
                    try
                    {
                        // Add the product to the order table for immediate purchase
                        using (var orderCmd = db.CreateCommand())
                        {
                            orderCmd.Transaction = transaction;
                            orderCmd.CommandType = CommandType.Text;
                            orderCmd.CommandText = "INSERT INTO [order] (AccountID, ProductID, ProductName, Quantity, Price, TotalAmount, Status) " +
                                                    "VALUES (@AccountID, @ProductID, @ProductName, @Quantity, @Price, @TotalAmount, 'Pending')";
                            orderCmd.Parameters.AddWithValue("@AccountID", accountId);
                            orderCmd.Parameters.AddWithValue("@ProductID", productId);
                            orderCmd.Parameters.AddWithValue("@ProductName", productName);
                            orderCmd.Parameters.AddWithValue("@Quantity", quantity);
                            orderCmd.Parameters.AddWithValue("@Price", price);
                            orderCmd.Parameters.AddWithValue("@TotalAmount", totalAmount);

                            orderCmd.ExecuteNonQuery();
                        }

                        // Retrieve the current quantity from the product table
                        int currentQuantity = 0;
                        using (var quantityCmd = db.CreateCommand())
                        {
                            quantityCmd.Transaction = transaction;
                            quantityCmd.CommandType = CommandType.Text;
                            quantityCmd.CommandText = "SELECT Quantity FROM PRODUCT WHERE ProductID = @ProductID";
                            quantityCmd.Parameters.AddWithValue("@ProductID", productId);

                            var result = quantityCmd.ExecuteScalar();
                            if (result != null)
                            {
                                currentQuantity = Convert.ToInt32(result);
                            }
                            else
                            {
                                throw new Exception("Product not found.");
                            }
                        }

                        // Calculate the new quantity
                        int newQuantity = currentQuantity - quantity;
                        if (newQuantity < 0)
                        {
                            throw new Exception("Insufficient stock available.");
                        }

                        // Update the product table with the new quantity
                        using (var updateCmd = db.CreateCommand())
                        {
                            updateCmd.Transaction = transaction;
                            updateCmd.CommandType = CommandType.Text;
                            updateCmd.CommandText = "UPDATE PRODUCT SET Quantity = @NewQuantity WHERE ProductID = @ProductID";
                            updateCmd.Parameters.AddWithValue("@NewQuantity", newQuantity);
                            updateCmd.Parameters.AddWithValue("@ProductID", productId);

                            updateCmd.ExecuteNonQuery();
                        }

                        // Commit the transaction
                        transaction.Commit();
                        success = true;
                        message = "Product has been successfully purchased.";
                    }
                    catch (Exception ex)
                    {
                        // Rollback the transaction in case of error
                        transaction.Rollback();
                        message = ex.Message;
                    }
                }
            }

            return Json(new { success, message });
        }


        public ActionResult ViewCart()
        {
            var firstName = Session["FirstName"] != null ? Session["FirstName"].ToString() : "User";

            ViewBag.FirstName = firstName;

            return View();
        }

        [HttpGet]
        public ActionResult GetCartItems()
        {
            var cartItems = new List<object>();
            var accountId = Session["AccountId"];

            using (var db = new SqlConnection(connStr))
            {
                db.Open();
                using (var cmd = db.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "SELECT c.ProductID, p.ProductName, c.Quantity, c.Price " +
                                      "FROM CART c INNER JOIN PRODUCT p ON c.ProductID = p.ProductID " +
                                      "WHERE c.AccountID = @AccountID";
                    cmd.Parameters.AddWithValue("@AccountID", accountId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        // Add items to a temporary list in reverse order
                        var tempList = new List<object>();
                        while (reader.Read())
                        {
                            tempList.Insert(0, new
                            {
                                ProductID = reader["ProductID"].ToString(),
                                ProductName = reader["ProductName"].ToString(),
                                Quantity = Convert.ToInt32(reader["Quantity"]),
                                Price = Convert.ToDecimal(reader["Price"])
                            });
                        }

                        // Append the temporary list to the beginning of the cartItems list
                        cartItems.InsertRange(0, tempList);
                    }
                }
            }

            return Json(cartItems, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult UpdateCartItem()
        {
            var accountId = Session["AccountId"];
            var success = false;
            var productId = Request["productId"];
            var quantity = Convert.ToInt32(Request["quantity"]);

            using (var db = new SqlConnection(connStr))
            {
                db.Open();

                // Retrieve the price of the product
                decimal price = 0;
                using (var priceCmd = db.CreateCommand())
                {
                    priceCmd.CommandType = CommandType.Text;
                    priceCmd.CommandText = "SELECT Price FROM PRODUCT WHERE ProductID = @ProductID";
                    priceCmd.Parameters.AddWithValue("@ProductID", productId);

                    price = (decimal)priceCmd.ExecuteScalar();
                }

                decimal totalAmount = quantity * price;

                using (var cmd = db.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "UPDATE CART SET Quantity = @Quantity, TotalAmount = @TotalAmount " +
                                      "WHERE AccountID = @AccountID AND ProductID = @ProductID";
                    cmd.Parameters.AddWithValue("@Quantity", quantity);
                    cmd.Parameters.AddWithValue("@TotalAmount", totalAmount);
                    cmd.Parameters.AddWithValue("@AccountID", accountId);
                    cmd.Parameters.AddWithValue("@ProductID", productId);

                    success = cmd.ExecuteNonQuery() > 0;
                }
            }

            return Json(new { success });
        }


        [HttpPost]
        public ActionResult RemoveCartItem()
        {
            var accountId = Session["AccountId"];
            var success = false;
            var productId = Request["productId"];

            using (var db = new SqlConnection(connStr))
            {
                db.Open();
                using (var cmd = db.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "DELETE FROM CART WHERE AccountID = @AccountID AND ProductID = @ProductID";
                    cmd.Parameters.AddWithValue("@AccountID", accountId);
                    cmd.Parameters.AddWithValue("@ProductID", productId);

                    success = cmd.ExecuteNonQuery() > 0;
                }
            }

            return Json(new { success });
        }

        public ActionResult CustomerList()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Checkout()
        {
            var data = new List<object>();
            var accountId = Session["AccountId"];
            var productId = Request["productId"];
            var productName = Request["productName"];
            var quantity = int.Parse(Request["quantity"]);
            var price = decimal.Parse(Request["price"]);
            var totalAmount = price * quantity;
            var success = false;
            var message = "An error occurred during checkout.";

            using (var db = new SqlConnection(connStr))
            {
                db.Open();

                using (var transaction = db.BeginTransaction())
                {
                    try
                    {
                        // Check if the item is already checked out
                        var existingOrder = false;
                        var orderId = 0;
                        using (var checkCmd = db.CreateCommand())
                        {
                            checkCmd.Transaction = transaction;
                            checkCmd.CommandType = CommandType.Text;
                            checkCmd.CommandText = "SELECT OrderID FROM [order] WHERE AccountID = @AccountID AND ProductID = @ProductID AND Status = 'Pending'";
                            checkCmd.Parameters.AddWithValue("@AccountID", accountId);
                            checkCmd.Parameters.AddWithValue("@ProductID", productId);

                            var result = checkCmd.ExecuteScalar();
                            existingOrder = result != null;
                            if (existingOrder)
                            {
                                orderId = (int)result;
                            }
                        }

                        if (existingOrder)
                        {
                            // Delete the existing order
                            using (var updateCmd = db.CreateCommand())
                            {
                                updateCmd.Transaction = transaction;
                                updateCmd.CommandType = CommandType.Text;
                                updateCmd.CommandText = "DELETE FROM [order] WHERE OrderID = @OrderID";
                                updateCmd.Parameters.AddWithValue("@OrderID", orderId);

                                updateCmd.ExecuteNonQuery();
                            }
                        }

                        // Insert the new order
                        using (var orderCmd = db.CreateCommand())
                        {
                            orderCmd.Transaction = transaction;
                            orderCmd.CommandType = CommandType.Text;
                            orderCmd.CommandText = "INSERT INTO [order] (AccountID, ProductID, ProductName, Quantity, Price, TotalAmount, Status) " +
                                                    "VALUES (@AccountID, @ProductID, @ProductName, @Quantity, @Price, @TotalAmount, 'Pending')";
                            orderCmd.Parameters.AddWithValue("@AccountID", accountId);
                            orderCmd.Parameters.AddWithValue("@ProductID", productId);
                            orderCmd.Parameters.AddWithValue("@ProductName", productName);
                            orderCmd.Parameters.AddWithValue("@Quantity", quantity);
                            orderCmd.Parameters.AddWithValue("@Price", price);
                            orderCmd.Parameters.AddWithValue("@TotalAmount", totalAmount);

                            orderCmd.ExecuteNonQuery();
                        }

                        // Clear the cart after checkout
                        using (var clearCartCmd = db.CreateCommand())
                        {
                            clearCartCmd.Transaction = transaction;
                            clearCartCmd.CommandType = CommandType.Text;
                            clearCartCmd.CommandText = "DELETE FROM CART WHERE AccountID = @AccountID AND ProductID = @ProductID";
                            clearCartCmd.Parameters.AddWithValue("@AccountID", accountId);
                            clearCartCmd.Parameters.AddWithValue("@ProductID", productId);

                            clearCartCmd.ExecuteNonQuery();
                        }

                        // Retrieve the current quantity from the product table
                        int currentQuantity = 0;
                        using (var quantityCmd = db.CreateCommand())
                        {
                            quantityCmd.Transaction = transaction;
                            quantityCmd.CommandType = CommandType.Text;
                            quantityCmd.CommandText = "SELECT Quantity FROM PRODUCT WHERE ProductID = @ProductID";
                            quantityCmd.Parameters.AddWithValue("@ProductID", productId);

                            var result = quantityCmd.ExecuteScalar();
                            if (result != null)
                            {
                                currentQuantity = Convert.ToInt32(result);
                            }
                            else
                            {
                                throw new Exception("Product not found.");
                            }
                        }

                        // Calculate the new quantity
                        int newQuantity = currentQuantity - quantity;
                        if (newQuantity < 0)
                        {
                            throw new Exception("Insufficient stock available.");
                        }

                        // Update the product table with the new quantity
                        using (var updateCmd = db.CreateCommand())
                        {
                            updateCmd.Transaction = transaction;
                            updateCmd.CommandType = CommandType.Text;
                            updateCmd.CommandText = "UPDATE PRODUCT SET Quantity = @NewQuantity WHERE ProductID = @ProductID";
                            updateCmd.Parameters.AddWithValue("@NewQuantity", newQuantity);
                            updateCmd.Parameters.AddWithValue("@ProductID", productId);

                            updateCmd.ExecuteNonQuery();
                        }

                        // Commit the transaction
                        transaction.Commit();
                        success = true;
                        message = existingOrder ? "Order has been updated successfully." : "Order has been successfully placed.";
                    }
                    catch (Exception ex)
                    {
                        // Rollback the transaction in case of error
                        transaction.Rollback();
                        message = ex.Message;
                    }
                }
            }

            return Json(new { success, message });
        }

        public ActionResult MyOrders()
        {
            var firstName = Session["FirstName"] != null ? Session["FirstName"].ToString() : "User";

            ViewBag.FirstName = firstName;

            return View();
        }

        [HttpGet]
        public ActionResult GetOrders()
        {
            var orderItems = new List<object>();
            var accountId = Session["AccountId"];

            using (var db = new SqlConnection(connStr))
            {
                db.Open();
                using (var cmd = db.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "SELECT * FROM [ORDER] WHERE AccountID = @AccountID ORDER BY OrderID DESC";
                    cmd.Parameters.AddWithValue("@AccountID", accountId);

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            orderItems.Add(new
                            {
                                OrderId = reader["OrderID"],
                                ProductID = reader["ProductID"].ToString(),
                                ProductName = reader["ProductName"].ToString(),
                                Quantity = Convert.ToInt32(reader["Quantity"]),
                                Price = Convert.ToDecimal(reader["Price"]),
                                TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                                Status = reader["Status"].ToString()
                            });
                        }
                    }
                }
            }

            return Json(orderItems, JsonRequestBehavior.AllowGet);
        }

        [HttpPost]
        public ActionResult CancelOrder()
        {
            var orderIdString = Request["orderId"];
            if (!string.IsNullOrEmpty(orderIdString))
            {
                try
                {
                    var orderId = Convert.ToInt32(orderIdString);

                    // Update the status of the order with the provided orderId to "Cancelled" in the database
                    using (var db = new SqlConnection(connStr))
                    {
                        db.Open();

                        // Check the current status of the order
                        using (var checkCmd = db.CreateCommand())
                        {
                            checkCmd.CommandType = CommandType.Text;
                            checkCmd.CommandText = "SELECT Status FROM [ORDER] WHERE OrderID = @OrderID";
                            checkCmd.Parameters.AddWithValue("@OrderID", orderId);
                            var currentStatus = (string)checkCmd.ExecuteScalar();

                            if (currentStatus == null)
                            {
                                return Json(new { success = false, message = "Order not found." });
                            }
                            else if (currentStatus == "Cancelled")
                            {
                                return Json(new { success = false, message = "Order is already cancelled." });
                            }
                        }

                        // Proceed to cancel the order
                        using (var cmd = db.CreateCommand())
                        {
                            cmd.CommandType = CommandType.Text;
                            cmd.CommandText = "UPDATE [ORDER] SET Status = 'Cancelled' WHERE OrderID = @OrderID";
                            cmd.Parameters.AddWithValue("@OrderID", orderId);
                            int rowsAffected = cmd.ExecuteNonQuery();

                            if (rowsAffected > 0)
                            {
                                return Json(new { success = true, message = "Order cancelled successfully." });
                            }
                            else
                            {
                                return Json(new { success = false, message = "Failed to cancel order. Order not found." });
                            }
                        }
                    }
                }
                catch (FormatException)
                {
                    // Handle the case where orderIdString is not in the correct format for conversion
                    return Json(new { success = false, message = "Invalid orderId format." });
                }
            }
            else
            {
                // Handle the case where orderIdString is null or empty
                return Json(new { success = false, message = "orderId is missing." });
            }
        }

        public ActionResult CustomerOrders()
        {
            return View();
        }

        [HttpGet]
        public ActionResult GetAllOrders()
        {
            var orderItems = new List<object>();

            using (var db = new SqlConnection(connStr))
            {
                db.Open();
                using (var cmd = db.CreateCommand())
                {
                    cmd.CommandType = CommandType.Text;
                    cmd.CommandText = "SELECT * FROM [ORDER] ORDER BY OrderID DESC";

                    using (var reader = cmd.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            orderItems.Add(new
                            {
                                OrderID = reader["OrderID"],
                                AccountID = reader["AccountID"],
                                ProductID = reader["ProductID"].ToString(),
                                ProductName = reader["ProductName"].ToString(),
                                Quantity = Convert.ToInt32(reader["Quantity"]),
                                Price = Convert.ToDecimal(reader["Price"]),
                                TotalAmount = Convert.ToDecimal(reader["TotalAmount"]),
                                Status = reader["Status"].ToString()
                            });
                        }
                    }
                }
            }

            return Json(orderItems, JsonRequestBehavior.AllowGet);
        }


    }
}
