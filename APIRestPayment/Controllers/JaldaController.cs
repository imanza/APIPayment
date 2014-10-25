using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;

namespace APIRestPayment.Controllers
{
    public class JaldaController : BaseApiController
    {
        #region Handlers
        CASPaymentDAO.DataHandler.JaldaContractDataHandler jaldaContractHandler = new CASPaymentDAO.DataHandler.JaldaContractDataHandler(WebApiApplication.SessionFactory);
        CASPaymentDAO.DataHandler.JaldaTicksDataHandler jaldaThicksHandler = new CASPaymentDAO.DataHandler.JaldaTicksDataHandler(WebApiApplication.SessionFactory);
        CASPaymentDAO.DataHandler.AccountDataHandler accountHandler = new CASPaymentDAO.DataHandler.AccountDataHandler(WebApiApplication.SessionFactory);
        #endregion

        //public HttpResponseMessage Post([FromBody] APIRestPayment.Models.POSTModels.JaldaContractPOSTModel jaldaPOSTModel)
        //{
        //    try
        //    {
        //        string ParseErrorMessage;
        //        CASPaymentDTO.Domain.JaldaContract jaldaContract = TheModelFactory.Parse(jaldaPOSTModel, out ParseErrorMessage);

        //        if (jaldaContract == null)
        //        {
        //            return Request.CreateResponse(HttpStatusCode.BadRequest, new Models.QueryResponseModel
        //            {
        //                meta = new Models.MetaModel
        //                {
        //                    code = (int)HttpStatusCode.BadRequest,
        //                    errorMessage = ParseErrorMessage,
        //                }
        //            });
        //        }
        //        //////////////////
        //        //check the validity of this post. check jalda request parameters.                
        //        if (CheckValidityofPost(jaldaContract))
        //        {
        //            //Complete unassigned data in account

        //            account.IsActive = true;
        //            account.Paymentcode = this.GeneratePaymentCode();
        //            account.Accountnumber = this.GenerateAccountNumber(account);
        //            account.Balance = 0;
        //            account.Dateofopening = DateTime.Now;

        //            /////////////////
        //            ///Here we start a transaction to save the account
        //            try
        //            {
        //                accountHandler.Save(account);
        //                //Return account with details including its Id
        //                account = accountHandler.Search(account).Cast<CASPaymentDTO.Domain.Account>().First();
        //                if (account != null)
        //                {
        //                    var result = Request.CreateResponse(HttpStatusCode.Created, new Models.QueryResponseModel
        //                    {
        //                        meta = new Models.MetaModel
        //                        {
        //                            code = (int)HttpStatusCode.Created,
        //                        },
        //                        data = this.TheModelFactory.Create(account, DataAccessTypes.Owner),
        //                    });
        //                    return result;
        //                }


        //            }
        //            catch (TransactionAbortedException ex)
        //            {
        //                return Request.CreateResponse(HttpStatusCode.BadRequest, new Models.QueryResponseModel
        //                {
        //                    meta = new Models.MetaModel
        //                    {
        //                        code = (int)HttpStatusCode.BadRequest,
        //                        errorMessage = "Could not save the account.\n" + ex
        //                    },
        //                });
        //            }
        //            catch (ApplicationException ex)
        //            {
        //                return Request.CreateResponse(HttpStatusCode.BadRequest, new Models.QueryResponseModel
        //                {
        //                    meta = new Models.MetaModel
        //                    {
        //                        code = (int)HttpStatusCode.BadRequest,
        //                        errorMessage = "Some problems happend while saving the account. Account not saved!\n" + ex
        //                    },
        //                });
        //            }
        //        }
        //        else
        //        {
        //            return Request.CreateResponse(HttpStatusCode.Unauthorized, new Models.QueryResponseModel
        //            {
        //                meta = new Models.MetaModel
        //                {
        //                    code = (int)HttpStatusCode.Unauthorized,
        //                    errorMessage = "You are not allowed to create accounts for other users!"
        //                },
        //            });
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        return Request.CreateResponse(HttpStatusCode.BadRequest, new Models.QueryResponseModel
        //        {
        //            meta = new Models.MetaModel
        //            {
        //                code = (int)HttpStatusCode.BadRequest,
        //                errorMessage = "The account cannot be saved:\n" + ex
        //            },
        //        });
        //    }
        //    return Request.CreateResponse(HttpStatusCode.BadRequest, new Models.QueryResponseModel
        //    {
        //        meta = new Models.MetaModel
        //        {
        //            code = (int)HttpStatusCode.BadRequest,
        //            errorMessage = "Account not saved due to problems\n"
        //        },
        //    });
        //}

        public HttpResponseMessage Post([FromBody] APIRestPayment.Models.POSTModels.JaldaThickPOSTModel jaldaThickPOSTModel)
        {
            CASPaymentDTO.Domain.JaldaContract jaldaContract;
            string ErrorMessage = "";
            string BodyMessage = "";
            string BodyTrailer = (jaldaThickPOSTModel.SerialNumber!=null)? ("Response to serial:"+jaldaThickPOSTModel.SerialNumber +" \r\n OrderNumber:"+jaldaThickPOSTModel.OrderNumber) : "-";
            bool? terminateContract;
            if (CheckValidityofPost(jaldaThickPOSTModel, out jaldaContract, out ErrorMessage, out terminateContract))
            {
                if (jaldaThickPOSTModel.JaldaThickType == Constants.JaldaThickTypes.Pay)
                {
                    //1) set initial Serial Number and isStarted
                    jaldaContract.Startserialnumber =(int) jaldaThickPOSTModel.SerialNumber;
                    jaldaContract.IsStarted = true;

                    //2) set responce to STARTED
                    BodyMessage = "STARTED";

                    //3) subtract deposit from Account. Deposit amount = maxAmount in contract. Before performing deposit we must ensure payer has enough credit in his/her account.
                    PaymentsController paymentController = new PaymentsController();
                    bool hasEnoughMoney = paymentController.CheckBalance(jaldaContract.SourceAccountItem, jaldaContract.Maxamount).Result;
                }
                else if (jaldaThickPOSTModel.JaldaThickType == Constants.JaldaThickTypes.Start)
                {

                }
                else if (jaldaThickPOSTModel.JaldaThickType == Constants.JaldaThickTypes.Terminate)
                {

                }
            }
            else
            {
                if ((bool)terminateContract)
                {
                    BodyMessage = "TERMINATED";
                    jaldaContract.Isterminated = true;
                    jaldaContractHandler.Update(jaldaContract);
                }

                return Request.CreateResponse(HttpStatusCode.BadRequest, new Models.QueryResponseModel
                {
                    meta = new Models.MetaModel
                    {
                        code = (int) HttpStatusCode.BadRequest,
                        errorMessage = ErrorMessage,
                    },
                    data= BodyMessage + "\r\n" + BodyTrailer,
                });
            }
        }

        private bool CheckValidityofPost(Models.POSTModels.JaldaThickPOSTModel jaldaThickPOSTModel, out CASPaymentDTO.Domain.JaldaContract relativeJaldaContract, out string ErrorMessage, out bool? terminateContract)
        {
            bool result = true;
            if (string.IsNullOrEmpty(jaldaThickPOSTModel.JaldaThickType) || jaldaThickPOSTModel.SerialNumber == null || jaldaThickPOSTModel.JaldaContractID == null)
            {
                terminateContract = false;
                relativeJaldaContract = null;
                ErrorMessage = "Null values in request";
                return false;
            }
            try
            {
                relativeJaldaContract = jaldaContractHandler.GetEntity((long)jaldaThickPOSTModel.JaldaContractID);

                ///////////////////////////////////////////////

                //Check if contract is already terminated
                if (relativeJaldaContract.Isterminated)
                {
                    ErrorMessage = "Jalda contract is already terminated.";
                    terminateContract = false;
                    return false;
                }
                //Check if contract is started
                else if (jaldaThickPOSTModel.JaldaThickType != Constants.JaldaThickTypes.Start && !relativeJaldaContract.IsStarted)
                {
                    ErrorMessage = "Jalda contract is not started. Send a thick with START type containing initial serial number and other attributes.";
                    terminateContract = false;
                    return false;
                }
                //Duplicate Start Thicks
                else if (jaldaThickPOSTModel.JaldaThickType == Constants.JaldaThickTypes.Start && relativeJaldaContract.IsStarted)
                {
                    ErrorMessage = "Contract is already started. Duplicate START thicks received.";
                    terminateContract = false;
                    return false;
                }
                //Duplicate Terminate Thicks
                else if (jaldaThickPOSTModel.JaldaThickType != Constants.JaldaThickTypes.Start && !relativeJaldaContract.IsStarted)
                {
                    ErrorMessage = "Contract is already terminated. Duplicate TERMINATE thicks received.";
                    terminateContract = false;
                    return false;
                }
                //////////////////////////////////////////////////


                //Check serial number integrity
                foreach (var item in relativeJaldaContract.JaldaTicksS)
                {
                    if (item.Serialnumber == jaldaThickPOSTModel.SerialNumber)
                    {
                        ErrorMessage = "Jalda Thick with serial number " + item.Serialnumber + " is already sent. Risk of replay attack.";
                        terminateContract = true;
                        return false;
                    }
                    else if (item.Serialnumber - jaldaThickPOSTModel.SerialNumber > 2)
                    {
                        ErrorMessage = "Jalda Thick with serial number " + item.Serialnumber + " is probably being missed";
                        terminateContract = true;
                        return false;
                    }
                }

                //Check not reaching max amount
                if ((relativeJaldaContract.JaldaTicksS.Count + 1) * relativeJaldaContract.Pricepertick > relativeJaldaContract.Maxamount)
                {
                    ErrorMessage = "Reached Maximum amount in jalda contract";
                    terminateContract = true;
                    return false;
                }

                //No problem found in incoming thick
                ErrorMessage = "";
                terminateContract = false;
                return result;
            }
            catch (Exception ex)
            {
                relativeJaldaContract = null;
                ErrorMessage = "Wrong Jalda Contract ID";
                terminateContract = false;
                return false;
            }
        }


        private bool CheckValidityofPost(CASPaymentDTO.Domain.JaldaContract jaldaContract, out string ErrorMessage)
        {
            ErrorMessage = "";
            return true;
        }

    }
}
