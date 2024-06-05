using EClaimsEntities.Models;
using EClaimsRepository.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace EClaimsWeb.Helpers
{
    public interface ISendMailServices
    {
        //Task<ResponseVM> SendEmail();
        Task SendEmail(string template, string screen, string subject, string senderName, string receiverName, string claimNo, string approvalType, int userID, string toEmail, string clickUrl);
        Task SendEmail(string template, string screen, string subject, string senderName, string receiverName, string claimNo, string approvalType, int userID, string toEmail, string clickUrl, string lastApprover, string nextApprover, string reason);
    }

    public class SendMailServices : ISendMailServices
    {
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly IConfiguration _configuration;
        private ILoggerManager _logger;
        private IRepositoryWrapper _repository;

        public SendMailServices(IWebHostEnvironment webHostEnvironment, IConfiguration configuration, ILoggerManager logger, IRepositoryWrapper repository)
        {
            _webHostEnvironment = webHostEnvironment;
            _configuration = configuration;
            _logger = logger;
            _repository = repository;
        }
        //public async Task<ResponseVM> SendEmail()
        public async Task SendEmail(string template,string screen, string subject,string senderName,string receiverName,string claimNo,string approvalType, int userID, string toEmail,string clickUrl)
        {
            try
            {
                //Email
                var message = EMailTemplate(template);
                message = message.Replace("ReceiverName", receiverName);
                message = message.Replace("SenderName", senderName);
                message = message.Replace("ClaimNo", claimNo);
                message = message.Replace("ApprovalType", approvalType);
                message = message.Replace("ClaimType", screen);
                message = message.Replace("ClickUrl", clickUrl);
                //message = message.Replace("Title", "Hangfire Test Mail");

                //message = message.Replace("message", "Welcome To The Code Hubs");
                //toEmail = "subash.kone@gmail.com";
                _logger.LogInfo($"Inside SendEmail before calling SendEmailAsync");
                await SendEmailAsync(toEmail, subject, message);
                //End Email
                //return new ResponseVM
                //{
                //    message = "message send successfully"
                //};
                //SendMailServices sr = new SendMailServices(null, null, null, null);
                //sr.SendPendingApprovalEmails("PendingApprovalMails.html");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside SendEmail : {ex.Message},{ex.StackTrace}");
                throw;
            }
        }

        public async Task SendEmail(string template, string screen, string subject, string senderName, string receiverName, string claimNo, string approvalType, int userID, string toEmail, string clickUrl, string lastApprover = "", string nextApprover = "", string reason = "")
        {
            try
            {
                //Email
                var message = EMailTemplate(template);
                message = message.Replace("ReceiverName", receiverName);
                message = message.Replace("SenderName", senderName);
                message = message.Replace("ClaimNo", claimNo);
                message = message.Replace("ApprovalType", approvalType);
                message = message.Replace("ClaimType", screen);
                message = message.Replace("ClickUrl", clickUrl);
                message = message.Replace("RejectReason", reason);
                message = message.Replace("LastApprover", lastApprover);
                message = message.Replace("NextApprover", nextApprover);
                //message = message.Replace("Title", "Hangfire Test Mail");

                //message = message.Replace("message", "Welcome To The Code Hubs");
                //toEmail = "subash.kone@gmail.com";
                _logger.LogInfo($"Inside SendEmail before calling SendEmailAsync");
                await SendEmailAsync(toEmail, subject, message);
                //End Email
                //return new ResponseVM
                //{
                //    message = "message send successfully"
                //};
               
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside SendEmail : {ex.Message},{ex.StackTrace}");
                throw;
            }
        }

        public async Task SendPendingApprovalEmails(string template)
        {
            try
            {
                
                var mstPendingApprovalEmails = await _repository.MstUser.GetAllPendingApprovalEmailsAsync();

                var GroupByQSVerifier = mstPendingApprovalEmails.GroupBy(s => new { s.UserID });

                //var GroupByQSVerifier = mstPendingApprovalEmails.Where(s => s.ApprovalStatus == 1).GroupBy(s => new { s.Verifier,s.ApprovalStatus });
                //var lstVerifiers = GroupByQSVerifier.Select(s => s.Key.Verifier);
                //var GroupByQSApprover = mstPendingApprovalEmails.Where(s => s.ApprovalStatus == 2).GroupBy(s => new { s.Approver,s.ApprovalStatus }).Select(s => s.Key.Approver);
                //var GroupByQSUserApprovers = mstPendingApprovalEmails.Where(s => s.ApprovalStatus == 6).GroupBy(s => new { s.UserApprovers, s.ApprovalStatus }).Select(s => s.Key.UserApprovers);
                //var GroupByQSHODApprover = mstPendingApprovalEmails.Where(s => s.ApprovalStatus == 7).GroupBy(s => new { s.HODApprover, s.ApprovalStatus }).Select(s => s.Key.HODApprover);

                List<CustomClaim> lstCustomClaims = new List<CustomClaim>();
                CustomClaim customClaim = new CustomClaim();
                foreach (var groupVer in GroupByQSVerifier)
                {
                    var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(groupVer.Key.UserID));
                    var toEmail = mstVerifierDetails.EmailAddress;
                    var receiverName = mstVerifierDetails.Name;
                    var message = EMailTemplate(template);
                    message = message.Replace("ReceiverName", receiverName);

                    //message = message.Replace("Title", "Hangfire Test Mail");
                    string clickUrl = string.Empty;
                    string domainUrl = _configuration.GetValue<string>("DomainUrl");
                    //string domainUrl = "https://localhost:5001"; //HttpContext.Request.Scheme + "://" + HttpContext.Request.Host;  
                    
                    foreach (var ver in groupVer)
                    {
                        if(ver.ApprovalStatus == 1 || ver.ApprovalStatus == 2)
                        {
                            if(ver.CNO.StartsWith("EC"))
                            {
                                string Myurl = domainUrl + "/" + "FinanceExpenseClaim/Details/" + ver.CID;
                                clickUrl = clickUrl + "<br /><br />" + "<a href=" + Myurl + ">" + Myurl + "</a>";
                            }
                            else if (ver.CNO.StartsWith("MC"))
                            {
                                string Myurl = domainUrl + "/" + "FinanceMileageClaim/Details/" + ver.CID;
                                clickUrl = clickUrl + "<br /><br />" + "<a href=" + Myurl + ">" + Myurl + "</a>";
                            }
                            else if (ver.CNO.StartsWith("TB"))
                            {
                                string Myurl = domainUrl + "/" + "FinanceTBClaim/Details/" + ver.CID;
                                clickUrl = clickUrl + "<br /><br />" + "<a href=" + Myurl + ">" + Myurl + "</a>";
                            }
                            else if (ver.CNO.StartsWith("PVCC"))
                            {
                                string Myurl = domainUrl + "/" + "FinancePVCClaim/Details/" + ver.CID;
                                clickUrl = clickUrl + "<br /><br />" + "<a href=" + Myurl + ">" + Myurl + "</a>";
                             }
                            else if (ver.CNO.StartsWith("PVGC"))
                            {
                                string Myurl = domainUrl + "/" + "FinancePVGClaim/Details/" + ver.CID;
                                clickUrl = clickUrl + "<br /><br />" + "<a href=" + Myurl + ">" + Myurl + "</a>";
                            }
                            else if (ver.CNO.StartsWith("HPVCC"))
                            {
                                string Myurl = domainUrl + "/" + "FinanceHRPVCClaim/Details/" + ver.CID;
                                clickUrl = clickUrl + "<br /><br />" + "<a href=" + Myurl + ">" + Myurl + "</a>";
                            }
                            else
                            {
                                string Myurl = domainUrl + "/" + "FinanceHRPVGClaim/Details/" + ver.CID;
                                clickUrl = clickUrl + "<br /><br />" + "<a href=" + Myurl + ">" + Myurl + "</a>";
                            }
                        }
                        else
                        {
                            if (ver.CNO.StartsWith("EC"))
                            {
                                string Myurl = domainUrl + "/" + "HodSummary/ECDetails/" + ver.CID;
                                clickUrl = clickUrl + "<br /><br />" + "<a href=" + Myurl + ">" + Myurl + "</a>";
                            }
                            else if (ver.CNO.StartsWith("MC"))
                            {
                                string Myurl = domainUrl + "/" + "HodSummary/Details/" + ver.CID;
                                clickUrl = clickUrl + "<br /><br />" + "<a href=" + Myurl + ">" + Myurl + "</a>";
                            }
                            else if (ver.CNO.StartsWith("TB"))
                            {
                                string Myurl = domainUrl + "/" + "HodSummary/TBCDetails/" + ver.CID;
                                clickUrl = clickUrl + "<br /><br />" + "<a href=" + Myurl + ">" + Myurl + "</a>";
                             }
                            else if (ver.CNO.StartsWith("PVCC"))
                            {
                                string Myurl = domainUrl + "/" + "HodSummary/PVCCDetails/" + ver.CID;
                                clickUrl = clickUrl + "<br /><br />" + "<a href=" + Myurl + ">" + Myurl + "</a>";
                            }
                            else if (ver.CNO.StartsWith("PVGC"))
                            {
                                string Myurl = domainUrl + "/" + "HodSummary/PVGCDetails/" + ver.CID;
                                clickUrl = clickUrl + "<br /><br />" + "<a href=" + Myurl + ">" + Myurl + "</a>";
                            }
                            else if (ver.CNO.StartsWith("HPVCC"))
                            {
                                string Myurl = domainUrl + "/" + "HRSummary/Details/" + ver.CID;
                                clickUrl = clickUrl + "<br /><br />" + "<a href=" + Myurl + ">" + Myurl + "</a>";
                             }
                            else
                            {
                                string Myurl = domainUrl + "/" + "HRSummary/HRPVGCDetails/" + ver.CID;
                                clickUrl = clickUrl + "<br /><br />" + "<a href=" + Myurl + ">" + Myurl + "</a>";
                            }
                        }
                        
                        

                        //var mstSenderDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(HttpContext.User.FindFirst("userid").Value));
                        //var senderName = mstSenderDetails.Name;
                        //var mstVerifierDetails = await _repository.MstUser.GetUserByIdAsync(Convert.ToInt32(ver.UserID));
                        //var toEmail = mstVerifierDetails.EmailAddress;
                        //var receiverName = mstVerifierDetails.Name;
                        //var claimNo = ver.CNO;
                        //var screen = "Expense Claim";
                        //var approvalType = "Verification Request";
                        //int userID = Convert.ToInt32(ver.UserID);
                        //var subject = "Expense Claim for Verification " + claimNo;

                       // BackgroundJob.Enqueue(() => _sendMailServices.SendEmail("EmailTemplate.html", screen, subject, senderName, receiverName, claimNo, approvalType, userID, toEmail, clickUrl));


                        //customClaim.CID = ver.CID;
                        //customClaim.CNO = ver.CNO;
                        //customClaim.Verifier = ver.Verifier;
                        //customClaim.UserID = Convert.ToInt32(ver.Verifier);
                        //customClaim.ApprovalStatus = ver.ApprovalStatus;
                        //lstCustomClaims.Add(customClaim);
                    }
                    message = message.Replace("ClickUrl", clickUrl);
                    //toEmail = "subash.kone@gmail.com";
                    var subject = "Claims which are pending for your verification/approvals as on Today " + DateTime.Now.ToString("dd/MM/yyyy");
                    _logger.LogInfo($"Inside SendEmail before calling SendEmailAsync");
                    await SendEmailAsync(toEmail, subject, message);
                }

                //foreach (var groupApp in GroupByQSApprover)
                //{
                //    foreach (var app in groupApp)
                //    {
                //        customClaim.CID = app.CID;
                //        customClaim.CNO = app.CNO;
                //        customClaim.Approver = app.Approver;
                //        customClaim.UserID = Convert.ToInt32(app.Approver);
                //        customClaim.ApprovalStatus = app.ApprovalStatus;
                //        lstCustomClaims.Add(customClaim);
                //    }
                //}

                //foreach (var groupUserApp in GroupByQSUserApprovers)
                //{
                //    foreach (var userApp in groupUserApp)
                //    {
                //        customClaim.CID = userApp.CID;
                //        customClaim.CNO = userApp.CNO;
                //        customClaim.UserApprovers = userApp.UserApprovers;
                //        customClaim.UserID = Convert.ToInt32(userApp.UserApprovers);
                //        customClaim.ApprovalStatus = userApp.ApprovalStatus;
                //        lstCustomClaims.Add(customClaim);
                //    }
                //}

                //foreach (var groupHODApprover in GroupByQSHODApprover)
                //{
                //    foreach (var userHODApp in groupHODApprover)
                //    {
                //        customClaim.CID = userHODApp.CID;
                //        customClaim.CNO = userHODApp.CNO;
                //        customClaim.HODApprover = userHODApp.HODApprover;
                //        customClaim.UserID = Convert.ToInt32(userHODApp.HODApprover);
                //        customClaim.ApprovalStatus = userHODApp.ApprovalStatus;
                //        lstCustomClaims.Add(customClaim);
                //    }
                //}

                //Email
                //var message = EMailTemplate(template);
                //message = message.Replace("ReceiverName", receiverName);
                //message = message.Replace("SenderName", senderName);
                //message = message.Replace("ClaimNo", claimNo);
                //message = message.Replace("ApprovalType", approvalType);
                //message = message.Replace("ClaimType", screen);
                //message = message.Replace("ClickUrl", clickUrl);
                //message = message.Rkceplace("RejectReason", reason);
                //message = message.Replace("LastApprover", lastApprover);
                //message = message.Replace("NextApprover", nextApprover);
                ////message = message.Replace("Title", "Hangfire Test Mail");

                //var message = "message Welcome To The Code Hubs";
                //var toEmail = "subash.kone@gmail.com";
                //var subject = "test";
               // _logger.LogInfo($"Inside SendEmail before calling SendEmailAsync");
               // await SendEmailAsync(toEmail, subject, message);
                //End Email
                //return new ResponseVM
                //{
                //    message = "message send successfully"
                //};
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside SendEmail : {ex.Message},{ex.StackTrace}");
                throw;
            }
        }

        public string EMailTemplate(string template)
        {
            try 
            {
                var path = string.Empty;
                if (template == "EmailTemplate.html")
                    path = Path.Combine(_webHostEnvironment.WebRootPath, "EmailTemplates", "EmailTemplate.html");
                else if(template == "ExportToBankTemplate.html")
                    path = Path.Combine(_webHostEnvironment.WebRootPath, "EmailTemplates", "ExportToBankTemplate.html");
                else if(template == "Rejected.html")
                    path = Path.Combine(_webHostEnvironment.WebRootPath, "EmailTemplates", "Rejected.html");
                else if (template == "QueryTemplate.html")
                    path = Path.Combine(_webHostEnvironment.WebRootPath, "EmailTemplates", "QueryTemplate.html");
                else if (template == "PendingApprovalMails.html")
                    path = Path.Combine(_webHostEnvironment.WebRootPath, "EmailTemplates", "PendingApprovalMails.html");
                else
                    path = Path.Combine(_webHostEnvironment.WebRootPath, "EmailTemplates", "ApprovedTemplate.html");
                string body = System.IO.File.ReadAllText(path);
                _logger.LogInfo($"Inside EMailTemplate");
                return body.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside EMailTemplate : {ex.Message},{ex.StackTrace}");
                throw;
            }
        }
        public async Task SendEmailAsync(string email, string subject, string message)
        {
            try
            {
                var _email = _configuration["EmailConfiguration:FromAddress"];
                var _dispName = _configuration["EmailConfiguration:DisplayName"];
                var _apiKey = _configuration["EmailConfiguration:SendGridApiKey"];

                var client = new SendGridClient(_apiKey);
                var messageBody = new SendGridMessage
                {
                    From = new EmailAddress(_email, _dispName),
                    Subject = subject,
                    HtmlContent = message
                };
                messageBody.AddTo(new EmailAddress(email));
                var response = await client.SendEmailAsync(messageBody);

                _logger.LogInfo($"Inside SendEmailAsync: Mail has been sent successfully");
                _logger.LogInfo($"{email},{message},{subject},{DateTime.Now}");
            }
            catch (Exception ex)
            {
                _logger.LogError($"Something went wrong inside SendEmailAsync : {ex.Message},{ex.StackTrace}");
                throw;
            }
        }
    }
}
