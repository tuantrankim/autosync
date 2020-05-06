using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostuffsAutoSync
{
    class Mining
    {
        public static double getAmazonPrice(IWebDriver wdr, string url)
        {
            return getPrice(wdr, url, "//span[@id='priceblock_ourprice']", 3);
        }
        public static double getCostcoPrice(IWebDriver wdr, string url)
        {
            //return getPrice(wdr, url, "//div[@id='product-details']//div[@class='online-price active']//span[@class='op-value']", 5);
            return getPrice(wdr, url, "//div[@id='product-details']//div[@id='math-table']//div[@class='your-price row no-gutter']//span[@class='value']", 3);
        }
        public static double getWalmartPrice(IWebDriver wdr, string url)
        {
            return getPrice(wdr, url, "//span[@id='price']//span[@itemprop='price']", 3, "content");
        }
        public static double getPrice(IWebDriver wdr, string url, string xpath, int seconds, string attributeName = "")
        {
            try
            {
                wdr.Url = url;
                var wait = new WebDriverWait(wdr, TimeSpan.FromSeconds(seconds));
                var elements = wait.Until(SeleniumExtras.WaitHelpers.ExpectedConditions.PresenceOfAllElementsLocatedBy(By.XPath(xpath)));
                //var collection = wdr.FindElements(By.XPath(xpath));
                //IWebElement searchText = wdr.FindElement(By.XPath(xpath));
                foreach (IWebElement element in elements)
                {
                    string txtPrice = element.GetAttribute(attributeName);
                    if (string.IsNullOrEmpty(txtPrice))
                    {
                        txtPrice = element.Text;
                    }

                    txtPrice = txtPrice.Replace(" ", "").Replace("$", "");
                    Double price = System.Convert.ToDouble(txtPrice.Trim(' ').Trim('$').Trim());
                    return price;
                }
            }
            catch (Exception ex)
            {
                //suppress exception
                ex.ToString();
            }
            return 0;
        }
    }
}
