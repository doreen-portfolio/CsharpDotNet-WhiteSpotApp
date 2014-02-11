using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Checklist.Models;
using System.Net.Mail;
using System.Text;

namespace Checklist.Controllers
{
    public class CheckController : Controller
    {

        /**
         * The database entity
         */
        private ChecklistEntities ctx = new ChecklistEntities();


        /**
         * Author: Clayton
         * Modified by: Clayton, Aaron
         * View to display Locations of the user logged in
         */
        public ActionResult LocationList()
        {
            ViewBag.Message = "List of Locations";//title of page

            IEnumerable<ws_locationView> locationQuery;
            if (User.Identity.Name == "admin")
            {
                locationQuery = from l in ctx.ws_locationView
                                select l;
            }
            else
            {
                locationQuery = from l in ctx.ws_locationView
                                where l.BusinessConsultant == User.Identity.Name
                                select l;
            }


            var dates = new List<KeyValuePair<DateTime, ws_locationView>>();
            var locations = new ws_locationView[locationQuery.Count()];
            var viewmodel = new ViewModel();
            int i = 0;
            foreach (var item in locationQuery)
            {
                locations[i] = item;
                var dateQuery = from v in ctx.SiteVisits
                                where v.LocationID == item.LocationId
                                orderby v.dateOfVisit descending
                                select v;
                DateTime date;
                if (dateQuery.FirstOrDefault() != null)
                {
                    date = dateQuery.FirstOrDefault().dateOfVisit;
                }
                else
                {
                    date = Convert.ToDateTime("1/1/0001");
                }
                var tempKey = new KeyValuePair<DateTime, ws_locationView>(date, item);

                dates.Add(tempKey);
                ++i;
            }

            dates = dates.OrderBy(x => x.Key).ToList();

            i = 0;
            foreach (var item in dates)
            {
                var tempInfo = new ViewInfo { Location = item.Value, LastVisit = item.Key };
                viewmodel.ViewList.Add(tempInfo);
                ++i;
            }

            return View(viewmodel);
        }

        /**
         * Author: Clayton
         * Modified by: Clayton, Aaron
         * View to display information of chosen location
         */
        public ActionResult LocationInfo(int locationId)
        {
            ViewBag.Message = "Location Information";//title of page

            //Query to grab the location information
            var locationQuery = from a in ctx.ws_locationView
                                where a.LocationId == locationId
                                select a;



            return View(locationQuery);
        }



        /**
         * Author: Clayton
         * Modified by: Clayton, Jung
         * View to display previous checklists of a location
         */
        public ActionResult PreviousChecklists(int locationId)
        {
            ViewBag.Message = " Previous Checklists";//title of page

            //**** fix this to be not a viewbag
            ViewBag.locationid = locationId;
            //*****

            var siteVistQuery = from s in ctx.SiteVisits
                                where s.LocationID == locationId
                                orderby s.dateOfVisit descending
                                select s;

            return View(siteVistQuery);
        }

        /**
         * Author: Clayton
         * Modified by: Clayton, Doreen, Aleeza
         * View to create a new checklists of a location
         */
        public ActionResult NewChecklist(int locationId)
        {
            ViewBag.Message = "New Checklist";//title of page

            var locationQuery = from l in ctx.ws_locationView
                                where l.LocationId == locationId
                                select l;//should be only 1 location

            ws_locationView locationResult = locationQuery.FirstOrDefault();

            ViewBag.Location = locationResult.LocationName; //Location name to be displayed

            var formQuery = from f in ctx.Forms
                            where f.Concept.Equals(locationResult.Concept)
                            select f;

            int formId = formQuery.FirstOrDefault().FormID;

            var sectionQuery = from s in ctx.Sections
                               where s.FormID == formId
                               orderby s.SectionOrder
                               select s;

            var answerForm = new AnswerForm
            {
                SiteVisitId = ctx.SiteVisits.Count() + 1,
                LocationId = locationId,
                FormId = formId,
                DateCreatedString = DateTime.Now.ToString("MM/dd/yyyy")
            };

            int a = 0;
            foreach (var sq in sectionQuery) //adds all section and question information to the model
            {
                var questionQuery = from q in ctx.Questions
                                    where q.SectionID == sq.SectionID
                                    && q.Active
                                    orderby q.QuestionOrder
                                    select q;

                foreach (var qq in questionQuery)
                {
                    answerForm.AnswerList.Add(new SiteAnswer());
                    answerForm.AnswerList[a].SectionName = sq.SectionName;
                    answerForm.AnswerList[a].Question = qq;
                    answerForm.AnswerList[a].QuestionId = qq.QuestionID;
                    answerForm.AnswerList[a].SiteAnswerId = a;
                    ++a;
                }
            }
            return View(answerForm);
        }

        /**
         * Author: Clayton
         * Modified By: Doreen
         * POST: AnswerForm
         */
        [HttpPost]
        public ActionResult SendConfirmation(AnswerForm answerForm)
        {
            if (!ModelState.IsValid)
            {
                return RedirectToAction("NewChecklist");
            }

            var visit = new SiteVisit { SiteVisitID = answerForm.SiteVisitId, dateModified = answerForm.DateModified };

            //a null date has the year value of 1
            if (answerForm.DateModified.Year < 1000)
            {
                visit.LocationID = answerForm.LocationId;
                visit.FormID = answerForm.FormId;
                visit.ManagerOnDuty = answerForm.ManagerOnDuty;
                visit.GeneralManager = answerForm.GeneralManager;
                visit.CommentPublic = answerForm.PublicComment;
                visit.CommentPrivate = answerForm.PrivateComment;

                visit.dateOfVisit = Convert.ToDateTime(answerForm.DateCreatedString);

                ctx.SiteVisits.Add(visit);
            }
            else
            {
                ctx.SiteVisits.Single(p => p.SiteVisitID == answerForm.SiteVisitId).ManagerOnDuty = answerForm.ManagerOnDuty;
                ctx.SiteVisits.Single(p => p.SiteVisitID == answerForm.SiteVisitId).GeneralManager = answerForm.GeneralManager;
                ctx.SiteVisits.Single(p => p.SiteVisitID == answerForm.SiteVisitId).CommentPublic = answerForm.PublicComment;
                ctx.SiteVisits.Single(p => p.SiteVisitID == answerForm.SiteVisitId).CommentPrivate = answerForm.PrivateComment;
                ctx.SiteVisits.Single(p => p.SiteVisitID == answerForm.SiteVisitId).dateModified = DateTime.Now;
            }
            ctx.SaveChanges();

            int i = 0;
            foreach (var item in answerForm.AnswerList)
            {
                var tempAns = new Answer();

                if (answerForm.DateModified.Year < 1000)
                {
                    tempAns.AnswerID = ctx.Answers.Count() + 1;
                    tempAns.SiteVisitID = answerForm.SiteVisitId;
                    tempAns.QuestionID = answerForm.AnswerList[i].QuestionId;
                    tempAns.Rating = answerForm.AnswerList[i].Value;
                    tempAns.Comment = answerForm.AnswerList[i].Comment;

                    ctx.Answers.Add(tempAns);
                }
                else
                {
                    var tempAnsQuery = (from ans in ctx.Answers
                                        where ans.SiteVisitID == answerForm.SiteVisitId
                                        select ans).ToList();

                    tempAns.AnswerID = tempAnsQuery[i].AnswerID;

                    ctx.Answers.Single(p => p.AnswerID == tempAns.AnswerID).Rating = answerForm.AnswerList[i].Value;
                    ctx.Answers.Single(p => p.AnswerID == tempAns.AnswerID).Comment = answerForm.AnswerList[i].Comment;
                }
                ctx.SaveChanges();
                ++i;
            }

            foreach (var action in answerForm.ActionItems)
            {
                if (action.Description == null)
                {
                    continue;
                }

                var temp = new SiteActionItem
                {
                    ActionID = ctx.SiteActionItems.Count() + 1,
                    Complete = false,
                    DateCreated = Convert.ToDateTime(answerForm.DateCreatedString),
                    SiteVisitID = answerForm.SiteVisitId,
                    LocationID = answerForm.LocationId,
                    Description = action.Description
                };

                ctx.SiteActionItems.Add(temp);
                ctx.SaveChanges();
            }

            //email code starts here
            ws_locationView location = (from l in ctx.ws_locationView
                                        where l.LocationId == answerForm.LocationId
                                        select l).FirstOrDefault();

            string email = location.Email;
            string businessConsultant = location.BusinessConsultant;
            string locationName = location.LocationName;

            SiteVisit sitevist = (from s in ctx.SiteVisits
                                  where s.SiteVisitID == visit.SiteVisitID
                                  select s).FirstOrDefault();

            String dateCreated = sitevist.dateOfVisit.ToString("MM-dd-yyy");

            string managerOnDuty = sitevist.ManagerOnDuty;
            string generalManager = sitevist.GeneralManager;
            string publicComment = sitevist.CommentPublic;


            int formId = (from f in ctx.Forms
                          where f.Concept.Equals(location.Concept)
                          select f).FirstOrDefault().FormID;

            var sectionQuery = from s in ctx.Sections
                               where s.FormID == formId
                               orderby s.SectionOrder
                               select s;

            List<Answer> ansQuery = (from aa in ctx.Answers
                                     where aa.SiteVisit.SiteVisitID == sitevist.SiteVisitID
                                     orderby aa.AnswerID
                                     select aa).ToList();

            using (var smtpClient = new SmtpClient())
            {
                const string mailFromAddress = "testing2013101@gmail.com"; //email we made to create smtp server, change in web.config if nessesary
                //String mailToAddress = email;
                const string mailToAddress = "testing2013101@gmail.com"; //the email address to send reports to; just for testing purposes


                StringBuilder body = new StringBuilder()
                .AppendLine("<html><body><h1 style='font-family:georgia;'>White Spot Site Visit Report</h1>")
                .AppendLine("<table>")
                .AppendLine("<tr><td width='215px'><b>Location: </b></td><td>" + locationName + "</td></tr>")
                .AppendLine("<tr><td><b>Date of Site Visit: </b></td><td>" + dateCreated + "</td></tr>")
                .AppendLine("<tr><td><b>Business Consultant: </b></td><td>" + businessConsultant + "</td></tr>")
                .AppendLine("<tr><td><b>Manager on Duty: </b></td><td>" + managerOnDuty + "</td></tr>")
                .AppendLine("<tr><td><b>General Manager: </b></td><td>" + generalManager + "</td></tr>")
                .AppendLine("</table>");
                String[] rating = { "", "Poor", "Good", "Excellent", "N/A" };

                int a = 0;
                foreach (var sq in sectionQuery)
                {
                    var questionQuery = from q in ctx.Questions
                                        where q.SectionID == sq.SectionID
                                        && q.Active
                                        orderby q.QuestionOrder
                                        select q;
                    body.AppendLine("<table>");
                    body.AppendLine("<tr><td width='215px'><h3>" + sq.SectionName + "</h3></td><td></td><td></td></tr>");

                    foreach (var qq in questionQuery)
                    {
                        body.AppendLine("<tr><td><b>" + qq.QuestionName + ": </b></td>");
                        body.AppendLine("<td width='100px'>" + rating[ansQuery[a].Rating] + "</td>");
                        body.AppendLine("<td>" + ansQuery[a].Comment + "</td></tr>");
                        ++a;
                    }
                    body.AppendLine("</table>");
                }
                body.AppendLine("<br /><h3>Follow-up items that require attention</h3>");

                var actionQuery = from aq in ctx.SiteActionItems
                                  where aq.Complete == false
                                  && aq.LocationID == answerForm.LocationId
                                  select aq;
                body.AppendLine("<table>");

                int j = 1;

                foreach (var act in actionQuery)
                {
                    body.AppendLine("<tr>" + j + ". " + act.Description + "</tr>");
                    j++;
                }
                body.AppendLine("</table>");
                body.AppendLine("<br /><h3>Overall Comments:</h3><br /> " + publicComment);
                body.AppendLine("</body></html>");

                var mailMessage = new MailMessage(mailFromAddress, mailToAddress)
                {
                    Subject = "White Spot Site Visit Report: " + locationName + " " + dateCreated,
                    Body = body.ToString(),
                    IsBodyHtml = true
                };
                smtpClient.Send(mailMessage);
            }

            return View(location);
        }


        /**
         * Author: Clayton
         * Modified by: Clayton
         * View to display the selected previous checklist
         */
        [HttpPost]
        public ActionResult OldChecklist(int siteId)
        {
            ViewBag.Message = "Old Checklist";//title of page




            SiteVisit currentSite = (from sv in ctx.SiteVisits
                                     where sv.SiteVisitID == siteId
                                     select sv).FirstOrDefault();


            ws_locationView locationResult = (from l in ctx.ws_locationView
                                              where l.LocationId == currentSite.LocationID
                                              select l).FirstOrDefault();//should be only 1 location


            ViewBag.Location = locationResult.LocationName;  //Location name to be displayed

            int locationId = locationResult.LocationId;



            Form form = (from f in ctx.Forms
                         where f.Concept.Equals(locationResult.Concept)
                         select f).FirstOrDefault();


            int formId = form.FormID;


            var sectionQuery = from s in ctx.Sections
                                where s.FormID == formId
                                orderby s.SectionOrder
                                select s;


            var answerForm = new AnswerForm {LocationId = locationId, FormId = formId};

            DateTime tempDate = Convert.ToDateTime("1/1/0001");

            if (currentSite.dateModified != null)
            {
                tempDate = (DateTime)currentSite.dateModified;
            }


            if (tempDate.Year > 1000)
            {
                answerForm.DateModified = (DateTime)currentSite.dateModified;
            }
            else
            {
                answerForm.DateModified = Convert.ToDateTime("1/1/0001");
            }
            answerForm.SiteVisitId = siteId;

            answerForm.GeneralManager = currentSite.GeneralManager;
            answerForm.ManagerOnDuty = currentSite.ManagerOnDuty;
            answerForm.PublicComment = currentSite.CommentPublic;
            answerForm.PrivateComment = currentSite.CommentPrivate;
            answerForm.DateCreated = currentSite.dateOfVisit;

            List<Answer> ansQuery = (from aa in ctx.Answers
                                      where aa.SiteVisit.SiteVisitID == currentSite.SiteVisitID
                                      orderby aa.AnswerID
                                      select aa).ToList();



            int a = 0;
            foreach (var sq in sectionQuery)
            {
                var questionQuery = from q in ctx.Questions
                                     where q.SectionID == sq.SectionID
                                     orderby q.QuestionOrder
                                     select q;



                foreach (var qq in questionQuery)
                {
                    if (ansQuery.Count <= a)
                    {
                        break;
                    }
                    if (ansQuery[a].QuestionID != qq.QuestionID)
                    {
                        continue;
                    }

                    answerForm.AnswerList.Add(new SiteAnswer());
                    answerForm.AnswerList[a].SectionName = sq.SectionName;
                    answerForm.AnswerList[a].Question = qq;
                    answerForm.AnswerList[a].QuestionId = qq.QuestionID;
                    answerForm.AnswerList[a].Value = ansQuery[a].Rating;
                    answerForm.AnswerList[a].Comment = ansQuery[a].Comment;
                    ++a;
                }
            }

            var actionQuery = from ai in ctx.SiteActionItems
                               where ai.SiteVisitID == currentSite.SiteVisitID
                               select ai;

            int i = 0;
            foreach (var action in actionQuery)
            {
                answerForm.ActionItems[i] = action;
                i++;
            }


            return View(answerForm);
        }



        /**
         * Author:Aaron
         * Modified by: Aaron, Aleeza
         * Partial view to display action items.
         */
        public PartialViewResult _ActionItemsPartial(int loc)
        {
            var query = from l in ctx.SiteActionItems
                        where l.LocationID == loc
                        && l.Complete == false
                        select l;
            Session["loc"] = loc;
            return PartialView("_ActionItems", query);
        }

        /**
         * Author:Aleeza
         * Partial view for the displaying and hiding of
         * completed action items.
         */
        public PartialViewResult _ActionItemsComplete(int loc)
        {
            var query = from l in ctx.SiteActionItems
                        where l.LocationID == loc
                        && l.Complete
                        select l;
            Session["loc"] = loc;
            if (query.Any())
            {
                Session["count"] = 5;
            }
            else
            {
                Session["count"] = 0;
            }
            return PartialView("_ActionItemsComplete");
        }

        /**
        * Author: Aaron
        * Modified by: Aaron
        * Creates an Action Item for the partial view on location info page
        */
        public PartialViewResult CreateAction(int loc)
        {

            var temp = new SiteActionItem { LocationID = loc };

            return PartialView("_AddActionItems", temp);
        }

        /**
         * Author: Aaron
         * Modified by: Aaron, Aleeza
         * Submits action item into database from location info page
         */
        [ValidateAntiForgeryToken]
        public PartialViewResult ActionItemSubmit(SiteActionItem actionItem)
        {
            actionItem.ActionID = ctx.SiteActionItems.Count() + 1;
            actionItem.DateCreated = DateTime.Now;
            actionItem.SiteVisitID = 0;
            try
            {
                ctx.SiteActionItems.Add(actionItem);
                ctx.SaveChanges();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.StackTrace);
                IEnumerable<SiteActionItem> itemsCaught = ctx.SiteActionItems.Where(i => (i.LocationID == actionItem.LocationID && i.Complete == false)).ToList();
                return PartialView("_ActionItems", itemsCaught);
            }
            IEnumerable<SiteActionItem> items = ctx.SiteActionItems.Where(i => (i.LocationID == actionItem.LocationID && i.Complete == false)).ToList();

            return PartialView("_ActionItems", items);
        }

        public PartialViewResult ListAction(SiteActionItem actionItem)
        {
            return PartialView("_DisplayActionItems", actionItem);
        }

        /**
         * Author: Aaron
         * Modified By: Aaron
         * Ajax operation to complete an action item
         * returns partial view of the action items as well as the
         * updated list of incomplete action items
         */
        [ValidateAntiForgeryToken]
        public PartialViewResult ActionItemComplete(SiteActionItem actionItem)
        {
            ctx.SiteActionItems.Single(i => i.ActionID == actionItem.ActionID).Complete = true;
            ctx.SiteActionItems.Single(i => i.ActionID == actionItem.ActionID).DateComplete = DateTime.Now;
            ctx.SaveChanges();
            IEnumerable<SiteActionItem> items = ctx.SiteActionItems.Where(i => (i.LocationID == actionItem.LocationID && i.Complete == false)).ToList();
            return PartialView("_ActionItems", items);
        }

        public PartialViewResult CompleteAction(int loc)
        {
            List<SiteActionItem> items = ctx.SiteActionItems.Where(i => (i.LocationID == loc && i.Complete)).ToList();

            items = items.OrderByDescending(x => x.DateComplete).ToList();

            return PartialView("_DisplayCompleteItems", items);
        }

        public PartialViewResult HideAction()
        {
            return PartialView("_ActionItemsComplete");
        }


        [ChildActionOnly]
        public PartialViewResult CompleteActionNew(int locationId)
        {
            var actionQuery = from sa in ctx.SiteActionItems
                              where sa.LocationID == locationId
                              && sa.Complete == false
                              select sa;



            return PartialView("_CompleteActionNew", actionQuery);
        }

        public PartialViewResult ActionRow(SiteActionItem actionItem)
        {
            return PartialView("_actionRow", actionItem);
        }

        public PartialViewResult AjaxFinish(int actId, int locId)
        {
            ctx.SiteActionItems.Single(i => i.ActionID == actId).Complete = true;
            ctx.SiteActionItems.Single(i => i.ActionID == actId).DateComplete = DateTime.Now;
            ctx.SaveChanges();

            var actionQuery = from sa in ctx.SiteActionItems
                              where sa.LocationID == locId
                              && sa.Complete == false
                              select sa;


            return PartialView("_CompleteActionNew", actionQuery);
        }
        /**
         * Author: Aaron
         * Modified By:
         * Increments counter for number of action items to
         * display and send the list back to the view for
         * page redisplay
         */
        public PartialViewResult ShowMore(int loc)
        {
            List<SiteActionItem> items = ctx.SiteActionItems.Where(i => (i.LocationID == loc && i.Complete)).ToList();

            items = items.OrderByDescending(x => x.DateComplete).ToList();

            if (items.Any())
            {
                Session["count"] = 5 + (int)Session["count"];
            }
            return PartialView("_DisplayCompleteItems", items);
        }
    }
}
