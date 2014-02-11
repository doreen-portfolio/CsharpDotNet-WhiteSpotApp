using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Checklist.Models
{
    public class AnswerForm
    {
        public AnswerForm()
        {
            AnswerList = new List<SiteAnswer>();
            ActionItems = new List<SiteActionItem>();
            for (int i = 0; i < 5; ++i)
            {
                ActionItems.Add(new SiteActionItem());
            }
        }

        public string DateCreatedString { get; set; }
        public DateTime DateCreated { get; set; }
        public DateTime DateModified { get; set; }

        public int SiteVisitId { get; set; }
        public int LocationId { get; set; }
        public int FormId { get; set; }

        public string PublicComment { get; set; }
        public string PrivateComment { get; set; }
        public string ManagerOnDuty { get; set; }
        public string GeneralManager { get; set; }

        public List<SiteAnswer> AnswerList { get; set; }
        public List<SiteActionItem> ActionItems { get; set; }
    }

    public class SiteAnswer
    {
        public int QuestionId { get; set; }
        public int SiteAnswerId { get; set; }
        public string SectionName { get; set; }
        public Question Question { get; set; }

        [Required(ErrorMessage = "Required")]
        public int Value { get; set; }
        
        public string Comment { get; set; }
    }
}