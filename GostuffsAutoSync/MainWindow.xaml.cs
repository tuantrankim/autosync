using Microsoft.Win32;
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace GostuffsAutoSync
{
    /// <summary>
    /// cost: is standard_price
    /// public price: is list_price
    /// </summary>
    public partial class MainWindow : Window
    {
        int Timeout = 5;//5 seconds

        System.Windows.Threading.DispatcherTimer scheduler = new System.Windows.Threading.DispatcherTimer();
        public MainWindow()
        {
            InitializeComponent();
        }

        public DataTable ReadCsvFile(string filePath)
        {
            Regex CSVParser = new Regex(",(?=(?:[^\"]*\"[^\"]*\")*(?![^\"]*\"))");

            DataTable dtCsv = new DataTable();
             
            string Fulltext;
            if (File.Exists(filePath))
            {
                string FileSaveWithPath = filePath;
                using (StreamReader sr = new StreamReader(FileSaveWithPath))
                {
                    while (!sr.EndOfStream)
                    {
                        Fulltext = sr.ReadToEnd().ToString(); //read full file text  
                        
                        string[] rows = Fulltext.Split(new string[] { "\r\n" },StringSplitOptions.None); //split full file text into rows  
                        for (int i = 0; i < rows.Count() - 1; i++)
                        {
                            string[] rowValues = CSVParser.Split(rows[i]); //split each row with comma to get individual values  
                            {
                                if (i == 0)
                                {
                                    for (int j = 0; j < rowValues.Count(); j++)
                                    {
                                        string token = rowValues[j].Trim(' ', '"');
                                        dtCsv.Columns.Add(token); //add headers  
                                        //Columns: ProductVariant.DESCRIPTION,"lst_price","id","name",ProductVariant.STANDARD_PRICE
                                    }
                                    if (!dtCsv.Columns.Contains(Product.OLD_STANDARD_PRICE)) dtCsv.Columns.Add(Product.OLD_STANDARD_PRICE); //old cost (price)
                                    if (!dtCsv.Columns.Contains(Product.SYNC_STATE)) dtCsv.Columns.Add(Product.SYNC_STATE);
                                    if (!dtCsv.Columns.Contains(Product.SYNC_DATE)) dtCsv.Columns.Add(Product.SYNC_DATE);
                                    if (!dtCsv.Columns.Contains(Product.IS_PUBLISHED)) dtCsv.Columns.Add(Product.IS_PUBLISHED);
                                }
                                else
                                {
                                    
                                    DataRow dr = dtCsv.NewRow();
                                    for (int k = 0; k < rowValues.Count(); k++)
                                    {
                                        string token = rowValues[k].Trim(' ', '"');
                                        dr[k] = token;
                                    }

                                    //dr[ProductVariant.OLD_STANDARD_PRICE] = dr[ProductVariant.STANDARD_PRICE];
                                    //dr[ProductVariant.STANDARD_PRICE] = null;
                                    // skip product type "Service" which is not ok to sale
                                    // skip not consumable
                                    // skip "is_special_product"
                                    if (
                                        (dtCsv.Columns.Contains(Product.TYPE) && dr[Product.TYPE].ToString() != Product.CONSUMABLE)
                                        ||(dtCsv.Columns.Contains(Product.SALE_OK) && dr[Product.SALE_OK].ToString().ToUpper() != Product.TRUE)
                                        || (dtCsv.Columns.Contains(Product.IS_SPECIAL_PRODUCT) && dr[Product.IS_SPECIAL_PRODUCT].ToString().ToUpper() == Product.TRUE)
                                        )
                                    {
                                        continue;
                                    }
                                    
                                    dtCsv.Rows.Add(dr); //add other rows  
                                }
                            }
                        }
                    }
                }
            }
            return dtCsv;
        }


        private void btnOpenFile_Click(object sender, RoutedEventArgs e)
        {
            try
            { 
               
                OpenFileDialog openFileDialog = new OpenFileDialog();
                if (openFileDialog.ShowDialog() == true)
                {
                    
                    txtFilePath.Text = openFileDialog.FileName;
                    Properties.Settings.Default.ImportFile = txtFilePath.Text;
                    Properties.Settings.Default.Save();
                    DataTable dt = ReadCsvFile(txtFilePath.Text);
                    openDataTable(dt);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }
        private void openDataTable(DataTable dt)
        {
            
            //gridProduct.ItemsSource = dt.DefaultView;
            gridProduct.DataContext = dt.DefaultView;
            lbRowsCount.Text = dt.Rows.Count.ToString() + " rows";
            foreach (var gc in gridProduct.Columns)
            {
                gc.MaxWidth = 300;
                if (gc.Header.ToString() == Product.DESCRIPTION) gc.DisplayIndex = 0;
                else if (gc.Header.ToString() == Product.STANDARD_PRICE) gc.DisplayIndex = 1;
                else if (gc.Header.ToString() == Product.OLD_STANDARD_PRICE) gc.DisplayIndex = 2;
            }            
        }
        private void killWebDriver()
        {
            try
            {
                System.Diagnostics.ProcessStartInfo p;
                p = new System.Diagnostics.ProcessStartInfo("cmd.exe", "/C " + "taskkill /f /im chromedriver.exe");
                System.Diagnostics.Process proc = new System.Diagnostics.Process();
                proc.StartInfo = p;
                proc.Start();
                proc.WaitForExit();
                proc.Close();
            }
            catch (Exception ex) { }
        }
        private void btnAutomation_Click(object sender, RoutedEventArgs e)
        {
            btnAmazon_Click(null, null);
            btnWalmart_Click(null, null);
            btnEbay_Click(null, null);
            btnCostco_Click(null, null);
            btnNike_Click(null, null);
        }
        private void getAmazonPrice(IWebDriver wdr, string url, DataRow dr)
        {
            string xpathPrice = "//span[@id='priceblock_ourprice']|//span[@id='priceblock_saleprice']|//span[@id='priceblock_dealprice']";
            string xpathPriceList = "//span[@class='priceBlockStrikePriceString a-text-strike']";
            getPrice(wdr, url, dr, xpathPrice, xpathPriceList, Timeout);
        }
        private void getCostcoPrice(IWebDriver wdr, string url, DataRow dr)
        {
            string xpathPrice = "//div[@id='product-details']//div[@id='math-table']//div[@class='your-price row no-gutter']//span[@class='value']";
            string xpathPriceList = "//div[@id='product-details']//div[@id='math-table']//div[@class='online-price active']//span[@class='op-value']";
            getPrice(wdr, url, dr, xpathPrice, xpathPriceList, Timeout);
        }
        private void getWalmartPrice(IWebDriver wdr, string url, DataRow dr)
        {
            string xpathPrice = "//span[@id='price']//span[@itemprop='price']";
            string xpathPriceList = "//div[@class='display-inline-block product-secondary-price Price-enhanced']//span[@class='visuallyhidden']";
            getPrice(wdr, url, dr, xpathPrice, xpathPriceList, Timeout, "content");
        }
        private void getEbayPrice(IWebDriver wdr, string url, DataRow dr)
        {
            string xpathPrice = "//span[@id='prcIsum']";
            string xpathPriceList = "//span[@id='orgPrc']";

            getPrice(wdr, url, dr, xpathPrice, xpathPriceList, Timeout);
        }

        private void getMacysPrice(IWebDriver wdr, string url, DataRow dr)
        {
            string xpathPrice = "//div[@role='article']//div[@data-auto='main-price']";
            string xpathPriceList = "//div[@role='article']//div[@data-auto='gift-price']";
            getPrice(wdr, url, dr, xpathPrice, xpathPriceList, Timeout);
        }
        private void getNikePrice(IWebDriver wdr, string url, DataRow dr)
        {
            string xpathPrice = "//div[@data-test='product-price-reduced']";
            string xpathPriceList = "//div[@data-test='product-price']";
            getPrice(wdr, url, dr, xpathPrice, xpathPriceList, Timeout);
        }
        private void getPrice(IWebDriver wdr, string url, DataRow dr, string xpathPrice, string xpathPriceList, int seconds, string attributeName = "")
        {
            try
            {
                wdr.Url = url;
                var wait = new WebDriverWait(wdr, TimeSpan.FromSeconds(seconds));
                //Costco - waiting for btn appear
                if (url.Contains("costco"))
                {
                    var addToCard = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.VisibilityOfAllElementsLocatedBy(By.Id("add-to-cart-btn"))).FirstOrDefault();
                }
                //var addToCard = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.VisibilityOfAllElementsLocatedBy(By.Id("add-to-cart-btn"))).FirstOrDefault();
                var elementPrice = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.PresenceOfAllElementsLocatedBy(By.XPath(xpathPrice))).FirstOrDefault();

                string txtPrice = elementPrice.GetAttribute(attributeName);
                if (string.IsNullOrEmpty(txtPrice))
                {
                    txtPrice = elementPrice.Text;
                }
                Double price = toPrice(txtPrice);
                Double oldMSRP = toPrice(dr[Product.MSRP_PRICE].ToString());
                Double maxMSRP = Math.Max(oldMSRP, adjustedListPrice(price));
                dr[Product.STANDARD_PRICE] = price.ToString();
                dr[Product.MSRP_PRICE] = maxMSRP.ToString();

                // get list price

                var elementListPrice = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.PresenceOfAllElementsLocatedBy(By.XPath(xpathPriceList))).FirstOrDefault();

                string strMSRP = elementListPrice.GetAttribute(attributeName);
                if (string.IsNullOrEmpty(strMSRP))
                {
                    strMSRP = elementListPrice.Text;
                }
                Double MSRP_Price = toPrice(strMSRP);
                
                maxMSRP = Math.Max(maxMSRP, adjustedListPrice(MSRP_Price));
                if (maxMSRP > 0) {
                    dr[Product.MSRP_PRICE] = maxMSRP.ToString();
                }
            }
            catch (Exception ex)
            {
                //suppress exception
                ex.ToString();
            }
        }

        private double adjustedListPrice(double price) {
            return Math.Floor(price + price * Product.MSRP_MARGIN_PERCENTAGE);
        }
        private double toPrice(string strPrice)
        {
            Double price = 0;
            try
            {
                strPrice = strPrice.Replace(",", "");
                string txtPrice = Regex.Match(strPrice, @"\d+(\.\d+)?").Value;
                //string txtPrice = strPrice.Replace("US", "").Replace(" ", "").Replace("$", "");
                price = System.Convert.ToDouble(txtPrice);
            }
            catch (Exception ex)
            {
                //suppress exception
            }
            return price;
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Timeout = int.Parse(txtTimeout.Text);
            DataView dv = gridProduct.DataContext as DataView;
            if(!File.Exists(txtFilePath.Text) || dv == null)
            {
                MessageBox.Show("Sorry! No data to save");
                return;
            }
            using (StreamWriter writer = new StreamWriter(txtFilePath.Text))
            {
                Rfc4180Writer.WriteDataTable(dv.Table, writer, true);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                txtFilePath.Text = Properties.Settings.Default.ImportFile;
                DataTable dt = ReadCsvFile(txtFilePath.Text);
                openDataTable(dt);
            }
            catch (Exception ex)
            {
                //suppress
            }
        }

        private string automationExport()
        {
            DataView dv = gridProduct.DataContext as DataView;
            if (dv == null)
            {
                //MessageBox.Show("Sorry! Nothing to export");
                return null;
            }
            DataRow[] outputRows = dv.Table.Select("sync_state = 'changed'");
            if (outputRows.Length <= 0)
            {
                //MessageBox.Show("Sorry! Nothing to export");
                return null;
            }

            DataTable dtPriceChanged = dv.Table.Select("sync_state = 'changed'").CopyToDataTable();
            lbPriceChangedCount.Text = dtPriceChanged.Rows.Count + " rows";
            if (ckPublishValidPrice.IsChecked == true)
            {
                foreach (DataRow dr in dtPriceChanged.Rows)
                {
                    dr[Product.IS_PUBLISHED] = Product.TRUE;
                }
            }
            FileInfo info = new FileInfo(txtFilePath.Text);
            string outFile = info.Directory.FullName + "\\" + info.Name.Replace(".csv", "") + "_auto_" + DateTime.Now.ToString("yyyyMdd_hhmmss") + ".csv";
            using (StreamWriter writer = new StreamWriter(outFile))
            {
                Rfc4180Writer.WriteDataTable(dtPriceChanged, writer, true);
            }
            txtPriceChangedExport.Text = outFile;
            //MessageBox.Show("Export success.\n" + outFile);
            return outFile;
        }
        private void btnExportPriceChanged_Click(object sender, RoutedEventArgs e)
        {
            DataView dv = gridProduct.DataContext as DataView;
            if (dv == null)
            {
                MessageBox.Show("Sorry! Nothing to export");
                return;
            }
            DataRow[] outputRows = dv.Table.Select("sync_state = 'changed'");
            if (outputRows.Length <= 0)
            {
                MessageBox.Show("Sorry! Nothing to export");
                return;
            }

            DataTable dtPriceChanged = dv.Table.Select("sync_state = 'changed'").CopyToDataTable();
            lbPriceChangedCount.Text = dtPriceChanged.Rows.Count + " rows";
            if (ckPublishValidPrice.IsChecked == true)
            {
                foreach (DataRow dr in dtPriceChanged.Rows)
                {
                    dr[Product.IS_PUBLISHED] = Product.TRUE;
                }
            }
            FileInfo info = new FileInfo(txtFilePath.Text);
            string outFile = info.Directory.FullName + "\\" + info.Name.Replace(".csv", "") + "_price_changed_" + DateTime.Now.ToString("yyyyMdd_hhmmss") + ".csv";
            using (StreamWriter writer = new StreamWriter(outFile))
            {
                Rfc4180Writer.WriteDataTable(dtPriceChanged, writer, true);
            }
            txtPriceChangedExport.Text = outFile;
            MessageBox.Show("Export success.\n" + outFile);
        }
        private void btnExportInvalidPrice_Click(object sender, RoutedEventArgs e)
        {
            DataView dv = gridProduct.DataContext as DataView;
            if (dv == null)
            {
                MessageBox.Show("Sorry! Nothing to export");
                return;
            }
            DataRow[] outputRows = dv.Table.Select("sync_state = 'error'");
            if (outputRows.Length <= 0)
            {
                MessageBox.Show("Sorry! Nothing to export");
                return;
            }

            DataTable dtPriceError = dv.Table.Select("sync_state = 'error'").CopyToDataTable();
            if (ckUnpublishInvalidPrice.IsChecked == true)
            {
                foreach (DataRow dr in dtPriceError.Rows)
                {
                    dr[Product.IS_PUBLISHED] = "FALSE";
                }
            }
            lbInvalidPriceCount.Text = dtPriceError.Rows.Count + " rows";
            FileInfo info = new FileInfo(txtFilePath.Text);
            string outFile = info.Directory.FullName + "\\" + info.Name.Replace(".csv", "") + "_unpublished_" + DateTime.Now.ToString("yyyyMdd_hhmmss") + ".csv";
            using (StreamWriter writer = new StreamWriter(outFile))
            {
                Rfc4180Writer.WriteDataTable(dtPriceError, writer, true);
            }
            txtInvalidPrice.Text = outFile;
            MessageBox.Show("Export success.\n" + outFile);
        }

        private void btnAsyncAutomation_Click(object sender, RoutedEventArgs e)
        {
            btnAmazon_Click(null, null);
            btnWalmart_Click(null, null);
            btnEbay_Click(null, null);
            btnCostco_Click(null, null);
            btnMacys_Click(null, null);
            /*
            Timeout = int.Parse(txtTimeout.Text);
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            WorkerArgument arg = new WorkerArgument();
            arg.Timeout = int.Parse(txtTimeout.Text);
            DataView dv = gridProduct.DataContext as DataView;
            arg.Table = dv;// dv.ToTable();
            worker.RunWorkerAsync(arg);
            */

        }
        void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            WorkerArgument arg = e.Argument as WorkerArgument;
            //killWebDriver();

            var options = new ChromeOptions();
            // ("ignoreDefaultArgs");
            options.AddExcludedArgument("enable-automation");
            options.AddArgument("no-sandbox");
            options.Proxy = null;
            options.PageLoadStrategy = PageLoadStrategy.Eager;
            //options.AddExtension("extension_1_1_0_0.crx");
            IWebDriver wdr = new ChromeDriver(options);
            //int countChanges = 0;
            //int countInvalidChanges = 0;
            try
            {
                //timeout = arg.Timeout;
                DataView dv2 = arg.Table;// new DataView(arg.Table);
                DataView dv = new DataView(dv2.Table);
                dv.RowFilter = arg.RowFilter;
                if (arg.Vendor == "Costco") loginCostco(wdr);


                for (int i = 0; i < dv.Count; i++)
                {
                    if (arg.IsClearCookies)
                    {
                        wdr.Manage().Cookies.DeleteAllCookies();
                    }
                    DataRow dr = dv[i].Row;

                    //support re-run
                    if (
                        !(toPrice(dr[Product.STANDARD_PRICE].ToString()) <= 0
                        || dr[Product.SYNC_STATE].ToString() == "error"
                        || dr[Product.IS_SPECIAL_PRODUCT].ToString() == Product.TRUE
                        )
                    ) continue;
                    
                    string url = dr[Product.DESCRIPTION].ToString();
                    if (url.Contains("amazon"))
                    {
                        getAmazonPrice(wdr, url, dr);
                    }
                    else if (url.Contains("costco"))
                    {
                        getCostcoPrice(wdr, url, dr);
                    }
                    else if (url.Contains("walmart"))
                    {
                        getWalmartPrice(wdr, url, dr);
                    }
                    else if (url.Contains("ebay.com"))
                    {
                        getEbayPrice(wdr, url, dr);
                    }
                    else if (url.Contains("macys.com"))
                    {
                        getMacysPrice(wdr, url, dr);
                    }
                    else if (url.Contains("nike.com"))
                    {
                        getNikePrice(wdr, url, dr);
                    }
                    // color rows
                    // var dgr = gridProduct.ItemContainerGenerator.ContainerFromItem(dr.Row) as DataGridRow;
                    if (toPrice(dr[Product.STANDARD_PRICE].ToString()) <= 0)
                    {
                        dr[Product.SYNC_STATE] = "error";
                        arg.InvalidPriceCount++;
                        // dgr.Background = Brushes.LightPink;
                    }
                    else if (dr[Product.STANDARD_PRICE].ToString() != dr[Product.OLD_STANDARD_PRICE].ToString())
                    {
                        dr[Product.SYNC_STATE] = "changed";
                        arg.PriceChangedCount++;
                        //dgr.Background = Brushes.LightSeaGreen;
                    }
                    else
                    {
                        dr[Product.SYNC_STATE] = "same";
                    }
                    dr[Product.SYNC_DATE] = DateTime.Now.ToString("g");
                    stopBrowsing(wdr);
                }

                //lbPriceChangedCount.Text = countChanges + " rows";
                //lbInvalidPriceCount.Text = countInvalidChanges + " rows";
            }
            catch (Exception ex)
            {
                //MessageBox.Show(ex.ToString());
            }
            finally
            {
                try
                {
                    e.Result = arg;
                    wdr.Close();
                    wdr.Quit();
                }
                catch
                {
                    //suppress
                }
            }
        }
        void loginCostco(IWebDriver wdr)
        {
            try
            {
                wdr.Url = "https://www.costco.com/LogonForm";
                var wait = new WebDriverWait(wdr, TimeSpan.FromSeconds(30));
                //Costco - waiting for btn appear
                var logonId = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.VisibilityOfAllElementsLocatedBy(By.Id("logonId"))).FirstOrDefault();
                var logonPassword = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.VisibilityOfAllElementsLocatedBy(By.Id("logonPassword"))).FirstOrDefault();
                logonId.SendKeys("kinhquocnguyen@gmail.com");
                logonPassword.SendKeys("Tu2nTr2n");
                logonPassword.Submit();
                Thread.Sleep(10000);
            }
            catch (Exception ex)
            {
                //suppress exception
                ex.ToString();
            }
        }
        void stopBrowsing(IWebDriver driver)
        {
            try
            {
                var element = driver.FindElement(By.TagName("body"));
                element.SendKeys(Keys.Escape);
            } catch { }
        }
        void worker_DoWork2(object sender, DoWorkEventArgs e)
        {
            int max = (int)e.Argument;
            int result = 0;
            for (int i = 0; i < max; i++)
            {
                int progressPercentage = Convert.ToInt32(((double)i / max) * 100);
                if (i % 42 == 0)
                {
                    result++;
                    (sender as BackgroundWorker).ReportProgress(progressPercentage, i);
                }
                else
                    (sender as BackgroundWorker).ReportProgress(progressPercentage);
                System.Threading.Thread.Sleep(1);

            }
            e.Result = result;
        }

        void worker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            //pbCalculationProgress.Value = e.ProgressPercentage;
            //if (e.UserState != null)
            //    lbResults.Items.Add(e.UserState);
        }

        void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            //DataTable dt = e.Result as DataTable;
            //openDataTable(dt);
            WorkerArgument arg = e.Result as WorkerArgument;
            if (arg.Vendor == "All" && ckScheduleMinute.IsChecked == true)
            {
                string outFile = automationExport();
                if (outFile != null)
                {
                    importToGoStuff(outFile,"http://gostuffs.com/web#model=product.template&action=import&menu_id=138",Timeout);
                }
            }
            else
            {
                lbPriceChangedCount.Text = arg.PriceChangedCount + " rows";
                lbInvalidPriceCount.Text = arg.InvalidPriceCount + " rows";
                MessageBox.Show("Done " + arg.Vendor);
            }
        }
        
        private void importToGoStuff(string fileName, string url, int seconds)
        {
            var options = new ChromeOptions();
            //options.AddArgument("no-sandbox");
            //options.Proxy = null;
            //options.PageLoadStrategy = PageLoadStrategy.Eager;
            IWebDriver wdr = new ChromeDriver(options);
            try
            {
                string loginXpath = "//input[@name='login']";
                string passwordXpath = "//input[@name='password']";
                //string loadBtnXpath = "//button[@class='btn btn-primary oe_import_file']";
                string fileUploadXpath = "//input[@type='file']";
                string btnImportXpath = "//button[@class='btn btn-primary o_import_import']";
                wdr.Url = url;
                var wait = new WebDriverWait(wdr, TimeSpan.FromSeconds(seconds));
                var login = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.VisibilityOfAllElementsLocatedBy(By.XPath(loginXpath))).FirstOrDefault();
                login.SendKeys(txtUser.Text);
                var password = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.VisibilityOfAllElementsLocatedBy(By.XPath(passwordXpath))).FirstOrDefault();
                password.SendKeys(txtPwd.Password);
                password.Submit();
                //var addToCard = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.VisibilityOfAllElementsLocatedBy(By.Id("add-to-cart-btn"))).FirstOrDefault();
                //var loadBtn = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.VisibilityOfAllElementsLocatedBy(By.XPath(loadBtnXpath))).FirstOrDefault();
                //loadBtn.Click();
                var fileUpload = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.ElementExists(By.XPath(fileUploadXpath)));
                fileUpload.SendKeys(fileName);
                var btnImport = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.VisibilityOfAllElementsLocatedBy(By.XPath(btnImportXpath))).FirstOrDefault();
                if (btnImport.Text.ToUpper() == "IMPORT")
                {
                    btnImport.Click();
                    Thread.Sleep(TimeSpan.FromSeconds(seconds));
                }

            }
            catch (Exception ex)
            {
                //suppress exception
                ex.ToString();
            }
            finally
            {
                try
                {
                    wdr.Close();
                    wdr.Quit();
                }
                catch
                {
                    //suppress
                }
            }
        }
        private void btnRunAll_Click(object sender, RoutedEventArgs e)
        {
            Timeout = int.Parse(txtTimeout.Text);
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            WorkerArgument arg = new WorkerArgument();
            arg.Timeout = int.Parse(txtTimeout.Text);
            arg.Vendor = "All";
            DataView dv = gridProduct.DataContext as DataView;
            arg.RowFilter = "";
            arg.Table = dv;// dv.ToTable();
            worker.RunWorkerAsync(arg);
        }
        private void btnAmazon_Click(object sender, RoutedEventArgs e)
        {
            Timeout = int.Parse(txtTimeout.Text);
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            WorkerArgument arg = new WorkerArgument();
            arg.Timeout = int.Parse(txtTimeout.Text);
            arg.Vendor = "Amazon";
            DataView dv = gridProduct.DataContext as DataView;
            arg.RowFilter = "description like '%amazon%'";
            arg.Table = dv;// dv.ToTable();
            worker.RunWorkerAsync(arg);
        }

        private void btnWalmart_Click(object sender, RoutedEventArgs e)
        {
            Timeout = int.Parse(txtTimeout.Text);
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            WorkerArgument arg = new WorkerArgument();
            arg.Timeout = int.Parse(txtTimeout.Text);
            arg.Vendor = "Walmart";
            DataView dv = gridProduct.DataContext as DataView;
            arg.RowFilter = "description like '%walmart%'";
            arg.Table = dv;// dv.ToTable();
            worker.RunWorkerAsync(arg);
        }

        private void btnCostco_Click(object sender, RoutedEventArgs e)
        {
            Timeout = int.Parse(txtTimeout.Text);
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            WorkerArgument arg = new WorkerArgument();
            arg.Timeout = int.Parse(txtTimeout.Text);
            arg.Vendor = "Costco";
            DataView dv = gridProduct.DataContext as DataView;
            arg.RowFilter = "description like '%costco%'";
            arg.Table = dv;// dv.ToTable();
            worker.RunWorkerAsync(arg);
        }

        private void btnEbay_Click(object sender, RoutedEventArgs e)
        {
            Timeout = int.Parse(txtTimeout.Text);
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            WorkerArgument arg = new WorkerArgument();
            arg.Timeout = int.Parse(txtTimeout.Text);
            arg.Vendor = "Ebay";
            DataView dv = gridProduct.DataContext as DataView;
            arg.RowFilter = "description like '%ebay%'";
            arg.Table = dv;// dv.ToTable();
            worker.RunWorkerAsync(arg);
        }

        private void btnMacys_Click(object sender, RoutedEventArgs e)
        {
            Timeout = int.Parse(txtTimeout.Text);
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            WorkerArgument arg = new WorkerArgument();
            arg.Timeout = int.Parse(txtTimeout.Text);
            arg.Vendor = "Maycys";
            DataView dv = gridProduct.DataContext as DataView;
            arg.RowFilter = "description like '%macys%'";
            arg.Table = dv;// dv.ToTable();
            worker.RunWorkerAsync(arg);
        }

        private void btnNike_Click(object sender, RoutedEventArgs e)
        {
            Timeout = int.Parse(txtTimeout.Text);
            BackgroundWorker worker = new BackgroundWorker();
            worker.WorkerReportsProgress = true;
            worker.DoWork += worker_DoWork;
            worker.ProgressChanged += worker_ProgressChanged;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            WorkerArgument arg = new WorkerArgument();
            arg.Timeout = int.Parse(txtTimeout.Text);
            arg.Vendor = "Nike";
            DataView dv = gridProduct.DataContext as DataView;
            arg.RowFilter = "description like '%nike.com%'";
            arg.Table = dv;// dv.ToTable();
            worker.RunWorkerAsync(arg);
        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            Timeout = int.Parse(txtTimeout.Text);
            DataView dv = gridProduct.DataContext as DataView;
            foreach (DataRow dr in dv.Table.Rows)
            {
                double price = toPrice(dr[Product.STANDARD_PRICE].ToString());
                if (price > 0)
                {
                    dr[Product.OLD_STANDARD_PRICE] = price.ToString();
                    dr[Product.STANDARD_PRICE] = null;
                    dr[Product.SYNC_STATE] = "";
                    dr[Product.SYNC_DATE] = "";
                }
            }
        }

        private void btnCheck_Click(object sender, RoutedEventArgs e)
        {
            Timeout = int.Parse(txtTimeout.Text);
            DataView dv = gridProduct.DataContext as DataView;
            DataTable dt = dv.Table;
            for (int i = 0; i < dt.Rows.Count -1; i++)
            {
                for (int j = i + 1; j < dt.Rows.Count; j++)
                {
                    if (dt.Rows[i][Product.DESCRIPTION].ToString().ToLower() == dt.Rows[j][Product.DESCRIPTION].ToString().ToLower())
                    {
                        dt.Rows[i][Product.SYNC_STATE] = dt.Rows[j][Product.SYNC_STATE] = "duplicated";
                    }
                }
            }
        }
        private void ckScheduleMinute_Checked(object sender, RoutedEventArgs e)
        {
            string msg = "Are you sure to auto sync price every " + txtScheduleMinutes.Text + " minutes.\n";
            msg += "- The steps bellow will be executed automatically.\n";
            msg += "- Update price on GoStuffs system.\n";
            msg += "- Unpublish invalid (price) products.\n";
            msg += "- Publish valid (price) products.";
            MessageBoxResult result = MessageBox.Show(msg, "Real time price updating", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                if (string.IsNullOrEmpty(txtUser.Text) || string.IsNullOrEmpty(txtPwd.Password))
                {
                    MessageBox.Show("Please enter username and password", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    ckScheduleMinute.IsChecked = false;
                    return;
                }
                //btnOpenFile_Click(null, null);
                int schedulerMinutes = Convert.ToInt32(txtScheduleMinutes.Text.Trim());
                scheduler.Tick += dispatcherTimer_Tick;
                scheduler.Interval = TimeSpan.FromMinutes(schedulerMinutes);
                scheduler.Start();

            }
            else ckScheduleMinute.IsChecked = false;
        }
        private void ckScheduleMinute_Unchecked(object sender, RoutedEventArgs e)
        {
            scheduler.Tick -= dispatcherTimer_Tick;
            scheduler.Stop();
        }
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            // code goes here
            btnReset_Click(null, null);
            btnRunAll_Click(null, null);
        }

    }
}
