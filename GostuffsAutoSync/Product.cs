using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GostuffsAutoSync
{
    class Product
    {
        public const double MSRP_MARGIN_PERCENTAGE = 0.11; //11% = 5% Gostuffs + 6% payment with tax adjust
        public const string DESCRIPTION = "description";
        public const string External_ID = "id";
        public const string NAME = "Name";
        public const string STANDARD_PRICE = "standard_price";//cost => base_price
        public const string MSRP_PRICE = "list_price"; //"lst_price" if using variant;
        public const string INTERNAL_REFERENCE = "default_code";
        public const string TYPE = "type";
        public const string IS_SPECIAL_PRODUCT = "is_special_product";
        public const string WEIGHT = "weight";
        public const string VOLUMN = "volume";
        public const string IS_PUBLISHED = "is_published";
        public const string SALE_OK = "sale_ok";
        public const string ACTIVE = "active";
        public const string OLD_STANDARD_PRICE = "old_standard_price";
        public const string SYNC_STATE = "sync_state";
        public const string SYNC_DATE = "sync_date";
        public const string TRUE = "TRUE";
        public const string FALSE = "FALSE";
        public const string CONSUMABLE = "Consumable";
        public const string CHANGED = "changed";

    }
}
