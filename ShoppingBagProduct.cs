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
    
    public partial class ShoppingBagProduct
    {
        public int SBP_Id { get; set; }
        public int P_P_Id { get; set; }
        public short SBP_Quantity { get; set; }
        public int SB_SB_Id { get; set; }
    
        public virtual Product Product { get; set; }
        public virtual ShoppingBag ShoppingBag { get; set; }
    }
}
