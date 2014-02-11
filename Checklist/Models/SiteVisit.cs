//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Checklist.Models
{
    using System;
    using System.Collections.Generic;
    
    public partial class SiteVisit
    {
        public SiteVisit()
        {
            this.Answers = new HashSet<Answer>();
            this.SiteActionItems = new HashSet<SiteActionItem>();
        }
    
        public int SiteVisitID { get; set; }
        public int LocationID { get; set; }
        public int FormID { get; set; }
        public System.DateTime dateOfVisit { get; set; }
        public Nullable<System.DateTime> dateModified { get; set; }
        public string CommentPublic { get; set; }
        public string CommentPrivate { get; set; }
        public string ManagerOnDuty { get; set; }
        public string GeneralManager { get; set; }
    
        public virtual ICollection<Answer> Answers { get; set; }
        public virtual Form Form { get; set; }
        public virtual ICollection<SiteActionItem> SiteActionItems { get; set; }
    }
}