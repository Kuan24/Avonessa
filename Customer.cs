//------------------------------------------------------------------------------
// <auto-generated>
//     Этот код создан по шаблону.
//
//     Изменения, вносимые в этот файл вручную, могут привести к непредвиденной работе приложения.
//     Изменения, вносимые в этот файл вручную, будут перезаписаны при повторном создании кода.
// </auto-generated>
//------------------------------------------------------------------------------

namespace AvonessaMVC5
{
    using System;
    using System.Collections.Generic;
    
    public partial class Customer
    {
        public Customer()
        {
            this.CustomerOrders = new HashSet<CustomerOrder>();
        }
    
        public int CR_Id { get; set; }
        public string CR_ContactName { get; set; }
        public string CR_Email { get; set; }
        public string CR_AddressCountry { get; set; }
        public System.DateTime CR_Date { get; set; }
        public string CR_PostalCode { get; set; }
        public string CR_AddressCountryCode { get; set; }
        public string CR_AddressState { get; set; }
        public string CR_AddressCity { get; set; }
        public string CR_AddressStreet { get; set; }
        public string CR_PayerId { get; set; }
        public Nullable<bool> CR_IsRussian { get; set; }
    
        public virtual ICollection<CustomerOrder> CustomerOrders { get; set; }
    }
}
