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
    
    public partial class OrderProduct
    {
        public int OP_Id { get; set; }
        public int P_P_Id { get; set; }
        public int O_O_Id { get; set; }
        public short P_Quantity { get; set; }
    
        public virtual Order Order { get; set; }
        public virtual Product Product { get; set; }
    }
}